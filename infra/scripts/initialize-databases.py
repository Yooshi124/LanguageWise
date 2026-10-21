import base64
import hashlib
import hmac
import json
import os
from pathlib import Path
import re
import secrets
import subprocess
import sys
from urllib.parse import quote, urlencode


CERTIFICATE = "/etc/ssl/certs/rds-global-bundle.pem"
OWNERS = (
    ("shared", "shared_app", "db-shared"),
    ("mini_games", "mini_games_app", "db-mini-games"),
    ("quizzes_courses", "quizzes_courses_app", "db-quizzes-courses"),
    ("chat_discussion", "chat_discussion_app", "db-chat-discussion"),
    ("quests_achievements_notifications", "quests_app", "db-quests"),
)
QUIZ_SEEDS = (
    "00-courses.sql", "10-de.sql", "20-fr.sql", "30-it.sql", "40-nl.sql",
    "50-es.sql", "60-pl.sql", "70-seed-milestone-sample-data.sql",
)


def identifier(value: str) -> str:
    if not re.fullmatch(r"[a-z_][a-z0-9_]{0,62}", value):
        raise ValueError("Invalid database identifier")
    return '"' + value + '"'


def literal(value: str) -> str:
    return "'" + value.replace("'", "''") + "'"


def password_verifier(password: str) -> str:
    salt = secrets.token_bytes(16)
    salted = hashlib.pbkdf2_hmac("sha256", password.encode("ascii"), salt, 4096)
    client_key = hmac.digest(salted, b"Client Key", "sha256")
    stored_key = hashlib.sha256(client_key).digest()
    server_key = hmac.digest(salted, b"Server Key", "sha256")
    encode = lambda value: base64.b64encode(value).decode("ascii")
    return f"SCRAM-SHA-256$4096:{encode(salt)}${encode(stored_key)}:{encode(server_key)}"


def execute(command: list[str], payload: str, environment: dict[str, str]) -> str:
    try:
        result = subprocess.run(  # noqa: S603 -- fixed internal argv; no shell, payload on stdin.
            command, input=payload, text=True, capture_output=True,
            env=environment, timeout=300, check=False,
        )
    except (OSError, subprocess.TimeoutExpired):
        raise RuntimeError("Initializer subprocess unavailable or timed out") from None
    if result.returncode:
        raise RuntimeError("Initializer subprocess failed; output suppressed")
    return result.stdout.strip()


class Initializer:
    def __init__(self, host: str, master: dict[str, str], secret_arns: dict[str, str], sql_root: Path):
        if not re.fullmatch(r"[a-zA-Z0-9.-]+", host):
            raise ValueError("Invalid RDS hostname")
        identifier(master["username"])
        if set(secret_arns) != {owner[2] for owner in OWNERS}:
            raise ValueError("Exactly five database secret ARNs are required")
        if not all(re.fullmatch(r"arn:aws:secretsmanager:[a-z0-9-]+:[0-9]{12}:secret:[A-Za-z0-9/_+=.@-]+", arn) for arn in secret_arns.values()):
            raise ValueError("Invalid database secret ARN")
        self.host = host
        self.master = master
        self.secret_arns = secret_arns
        self.sql_root = sql_root
        self.environment = {key: value for key, value in os.environ.items() if key not in {"RDS_MASTER_SECRET", "PGPASSWORD", "PGOPTIONS", "PGSERVICE", "PGSERVICEFILE"}}

    def sql(self, database: str, statement: str) -> str:
        environment = self.environment | {
            "PGHOST": self.host, "PGPORT": "5432", "PGDATABASE": database,
            "PGUSER": self.master["username"], "PGPASSWORD": self.master["password"],
            "PGSSLMODE": "verify-full", "PGSSLROOTCERT": CERTIFICATE,
            "PGCONNECT_TIMEOUT": "15", "PGOPTIONS": "-c statement_timeout=240000 -c lock_timeout=30000",
        }
        return execute([
            "psql", "-X", "--no-password", "--quiet", "--tuples-only", "--no-align",
            "--set", "ON_ERROR_STOP=1", "--set", "VERBOSITY=terse",
        ], statement, environment)

    def bootstrap_database(self, database: str, role: str) -> None:
        database_id, role_id = identifier(database), identifier(role)
        master_id = identifier(self.master["username"])
        if self.sql("postgres", f"SELECT EXISTS(SELECT FROM pg_roles WHERE rolname={literal(role)});") != "t":  # noqa: S608 -- literal() escapes the validated identifier value.
            self.sql("postgres", f"CREATE ROLE {role_id} LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOINHERIT;")
        self.sql("postgres", f"GRANT {role_id} TO {master_id} WITH ADMIN TRUE, INHERIT FALSE, SET TRUE;")
        if self.sql("postgres", f"SELECT EXISTS(SELECT FROM pg_database WHERE datname={literal(database)});") != "t":  # noqa: S608 -- literal() escapes the validated identifier value.
            self.sql("postgres", f"CREATE DATABASE {database_id} OWNER {role_id};")
        self.sql("postgres", f"REVOKE ALL ON DATABASE {database_id} FROM PUBLIC;")
        self.sql(database, """
            CREATE SCHEMA IF NOT EXISTS lw_bootstrap;
            REVOKE ALL ON SCHEMA lw_bootstrap FROM PUBLIC;
            CREATE TABLE IF NOT EXISTS lw_bootstrap.state (
                singleton boolean PRIMARY KEY DEFAULT true CHECK (singleton),
                seeded boolean NOT NULL DEFAULT false,
                published boolean NOT NULL DEFAULT false,
                secret_arn text
            );
            INSERT INTO lw_bootstrap.state (singleton) VALUES (true) ON CONFLICT DO NOTHING;
        """)

    def seed_sql(self, database: str) -> str:
        directory = self.sql_root / database
        scripts = [(directory / "schema.sql").read_text(encoding="utf-8")]
        if database == "quizzes_courses":
            if {path.name for path in (directory / "seeds").glob("*.sql")} != set(QUIZ_SEEDS):
                raise ValueError("Quiz seed manifest mismatch")
            scripts.extend((directory / "seeds" / name).read_text(encoding="utf-8") for name in QUIZ_SEEDS)
            scripts.append("INSERT INTO SeedHistory (Name) VALUES ('70-seed-milestone-sample-data.sql') ON CONFLICT DO NOTHING;")
            scripts.append("""
                DO $$ DECLARE identity_table text; BEGIN
                FOREACH identity_table IN ARRAY ARRAY['courses', 'lessons', 'lessonvocabulary',
                    'quizzes', 'quizquestions', 'quizattempts', 'quizanswers', 'milestones'] LOOP
                    EXECUTE format('SELECT setval(%L, GREATEST(COALESCE((SELECT MAX(id) FROM %I), 0),
                        (SELECT last_value FROM %s)), true)',
                        pg_get_serial_sequence(identity_table, 'id'), identity_table,
                        pg_get_serial_sequence(identity_table, 'id'));
                END LOOP; END $$;
            """)
        elif database != "mini_games":
            scripts.append((directory / "seed.sql").read_text(encoding="utf-8"))
        return "\n".join(scripts)

    def initialize_owner(self, database: str, role: str, secret_key: str) -> None:
        self.bootstrap_database(database, role)
        secret_arn = self.secret_arns[secret_key]
        status = self.sql(database, f"SELECT CASE WHEN NOT published THEN 'pending' WHEN secret_arn={literal(secret_arn)} THEN 'complete' ELSE 'mismatch' END FROM lw_bootstrap.state;")  # noqa: S608 -- validated ARN is escaped by literal().
        if status == "complete":
            return
        if status != "pending":
            raise RuntimeError("Refusing to replace credentials for an initialized database")
        if self.sql(database, "SELECT seeded FROM lw_bootstrap.state;") != "t":
            if self.sql(database, "SELECT EXISTS(SELECT FROM pg_tables WHERE schemaname NOT IN ('pg_catalog', 'information_schema', 'lw_bootstrap'));") == "t":
                raise RuntimeError("Refusing to seed an existing unmarked database")
            if database == "quests_achievements_notifications":
                self.sql("postgres", """
                    DO $$ BEGIN
                        IF NOT EXISTS(SELECT FROM pg_roles WHERE rolname='web_anon') THEN
                            CREATE ROLE web_anon NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION;
                        END IF;
                    END $$;
                    GRANT web_anon TO quests_app WITH INHERIT FALSE, SET TRUE;
                """)
            statement = "\n".join([
                "BEGIN;", f"SET LOCAL ROLE {identifier(role)};",
                self.seed_sql(database), "RESET ROLE;",
                "UPDATE lw_bootstrap.state SET seeded=true;", "COMMIT;",
            ])
            self.sql(database, statement)
        password = secrets.token_urlsafe(48)
        self.sql("postgres", f"ALTER ROLE {identifier(role)} PASSWORD {literal(password_verifier(password))};")
        connection = (
            f"Host={self.host};Port=5432;Database={database};Username={role};Password={password};"
            f"SSL Mode=VerifyFull;Root Certificate={CERTIFICATE};Maximum Pool Size=10;Include Error Detail=false"
        )
        query = urlencode({"sslmode": "verify-full", "sslrootcert": CERTIFICATE, "connect_timeout": "15"})
        uri = f"postgresql://{quote(role, safe='')}:{quote(password, safe='')}@{self.host}:5432/{database}?{query}"
        payload = json.dumps({"connection": connection, "uri": uri, "password": password})
        execute([
            "aws", "secretsmanager", "put-secret-value", "--secret-id", secret_arn,
            "--secret-string", "file:///dev/stdin", "--no-cli-pager",
        ], payload, self.environment)
        self.sql(database, f"UPDATE lw_bootstrap.state SET published=true, secret_arn={literal(secret_arn)};")  # noqa: S608 -- validated ARN is escaped by literal().

    def run(self) -> None:
        for owner in OWNERS:
            self.initialize_owner(*owner)


def main() -> int:
    try:
        master = json.loads(os.environ.pop("RDS_MASTER_SECRET"))
        initializer = Initializer(
            os.environ["RDS_HOST"], master, json.loads(os.environ["DATABASE_SECRET_ARNS"]),
            Path("/initializer/sql"),
        )
        initializer.run()
    except Exception:
        print("Database initialization failed; sensitive diagnostics suppressed.", file=sys.stderr)
        return 1
    print("All five databases initialized; application credentials published.")
    return 0


if __name__ == "__main__":
    sys.exit(main())