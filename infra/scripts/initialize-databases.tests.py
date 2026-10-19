import base64
import hashlib
import hmac
import importlib.util
import json
from pathlib import Path
import subprocess
import unittest
from unittest.mock import patch


SPEC = importlib.util.spec_from_file_location("initializer", Path(__file__).with_name("initialize-databases.py"))
if SPEC is None or SPEC.loader is None:
    raise RuntimeError("Initializer module could not be loaded")
initializer = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(initializer)


class InitializerTests(unittest.TestCase):
    def setUp(self):
        self.arns = {
            owner[2]: f"arn:aws:secretsmanager:ap-southeast-2:123456789012:secret:{owner[2]}-abcdef"
            for owner in initializer.OWNERS
        }
        self.instance = initializer.Initializer(
            "demo.example.rds.amazonaws.com", {"username": "lw_admin", "password": "fixture-only"},  # pragma: allowlist secret
            self.arns, Path("unused"),
        )

    def test_completed_owner_does_not_seed_rotate_or_publish(self):
        with patch.object(self.instance, "bootstrap_database"), patch.object(self.instance, "sql", return_value="complete") as sql, patch.object(initializer, "execute") as execute, patch.object(self.instance, "seed_sql") as seed:
            self.instance.initialize_owner(*initializer.OWNERS[0])
        self.assertEqual(sql.call_count, 1)
        execute.assert_not_called()
        seed.assert_not_called()

    def test_seeded_owner_recovers_failed_publication_without_reseeding(self):
        with patch.object(self.instance, "bootstrap_database"), patch.object(self.instance, "sql", side_effect=["pending", "t", "", ""]) as sql, patch.object(initializer, "execute") as execute, patch.object(self.instance, "seed_sql") as seed:
            self.instance.initialize_owner(*initializer.OWNERS[0])
        seed.assert_not_called()
        command, payload, environment = execute.call_args.args
        secret = json.loads(payload)
        self.assertIn("file:///dev/stdin", command)
        self.assertNotIn(secret["password"], " ".join(command))
        self.assertNotIn(secret["password"], " ".join(call.args[1] for call in sql.call_args_list))
        self.assertIn("SSL Mode=VerifyFull", secret["connection"])
        self.assertIn(initializer.CERTIFICATE, secret["connection"])
        self.assertIn("sslmode=verify-full", secret["uri"])
        self.assertNotIn("RDS_MASTER_SECRET", environment)
        self.assertNotIn("PGPASSWORD", environment)

    def test_existing_unmarked_database_fails_closed(self):
        with patch.object(self.instance, "bootstrap_database"), patch.object(self.instance, "sql", side_effect=["pending", "f", "t"]), patch.object(initializer, "execute") as execute, patch.object(self.instance, "seed_sql") as seed:
            with self.assertRaisesRegex(RuntimeError, "unmarked"):
                self.instance.initialize_owner(*initializer.OWNERS[-1])
        execute.assert_not_called()
        seed.assert_not_called()

    def test_fresh_seed_is_transactional_and_publication_is_last(self):
        with patch.object(self.instance, "bootstrap_database"), patch.object(self.instance, "sql", side_effect=["pending", "f", "f", "", "", ""]) as sql, patch.object(self.instance, "seed_sql", return_value="SELECT 'seed-fixture';"), patch.object(initializer, "execute"):
            self.instance.initialize_owner(*initializer.OWNERS[0])
        transaction = sql.call_args_list[3].args[1]
        self.assertTrue(transaction.startswith("BEGIN;"))
        self.assertIn('SET LOCAL ROLE "shared_app";', transaction)
        self.assertIn("SET seeded=true", transaction)
        self.assertTrue(transaction.endswith("COMMIT;"))
        self.assertIn("SET published=true", sql.call_args_list[-1].args[1])

    def test_failed_publication_never_marks_credentials_complete(self):
        with patch.object(self.instance, "bootstrap_database"), patch.object(self.instance, "sql", side_effect=["pending", "t", ""]) as sql, patch.object(initializer, "execute", side_effect=RuntimeError("failed")):
            with self.assertRaises(RuntimeError):
                self.instance.initialize_owner(*initializer.OWNERS[0])
        self.assertFalse(any("SET published=true" in call.args[1] for call in sql.call_args_list))

    def test_changed_secret_arn_does_not_rotate_live_credentials(self):
        with patch.object(self.instance, "bootstrap_database"), patch.object(self.instance, "sql", return_value="mismatch") as sql, patch.object(initializer, "execute") as execute:
            with self.assertRaisesRegex(RuntimeError, "initialized database"):
                self.instance.initialize_owner(*initializer.OWNERS[0])
        self.assertEqual(sql.call_count, 1)
        execute.assert_not_called()

    def test_psql_uses_verified_tls_and_no_password_arguments(self):
        with patch.object(initializer, "execute", return_value="t") as execute:
            self.instance.sql("postgres", "SELECT true;")
        command, payload, environment = execute.call_args.args
        self.assertEqual(environment["PGSSLMODE"], "verify-full")
        self.assertEqual(environment["PGSSLROOTCERT"], initializer.CERTIFICATE)
        self.assertEqual(environment["PGPASSWORD"], "fixture-only")
        self.assertNotIn("fixture-only", " ".join(command))
        self.assertEqual(payload, "SELECT true;")

    def test_subprocess_errors_never_expose_output(self):
        result = subprocess.CompletedProcess(["fixture"], 1, "sensitive-fixture", "sensitive-fixture")
        with patch.object(initializer.subprocess, "run", return_value=result):
            with self.assertRaises(RuntimeError) as caught:
                initializer.execute(["fixture"], "input-fixture", {})
        self.assertNotIn("sensitive-fixture", str(caught.exception))

    def test_scram_verifier_matches_postgresql_wire_format(self):
        with patch.object(initializer.secrets, "token_bytes", return_value=b"0123456789abcdef"):
            verifier = initializer.password_verifier("fixture-only")
        algorithm, iterations_salt, keys = verifier.split("$")
        iterations, salt = iterations_salt.split(":")
        stored, server = keys.split(":")
        salted = hashlib.pbkdf2_hmac("sha256", b"fixture-only", base64.b64decode(salt), int(iterations))
        self.assertEqual(algorithm, "SCRAM-SHA-256")
        self.assertEqual(base64.b64decode(stored), hashlib.sha256(hmac.digest(salted, b"Client Key", "sha256")).digest())
        self.assertEqual(base64.b64decode(server), hmac.digest(salted, b"Server Key", "sha256"))

    def test_identifiers_and_secret_inventory_fail_closed(self):
        with self.assertRaises(ValueError):
            initializer.identifier("postgres; DROP DATABASE postgres")
        with self.assertRaises(ValueError):
            initializer.Initializer("host", self.instance.master, {}, Path("unused"))

    def test_real_seed_manifest_loads_without_executing_sql(self):
        root = Path(__file__).resolve().parents[2]
        directories = {
            "shared": root / "shared/database/LanguageWise.Shared.Db/sql",
            "mini_games": root / "mini-games-service/database/sql",
            "quizzes_courses": root / "quizzes-courses-service/database/LanguageWise.QuizzesCoursesService.Db/sql",
            "chat_discussion": root / "chat-discussion-service/database/LanguageWise.ChatDiscussionService.Db/sql",
            "quests_achievements_notifications": root / "quests-achievements-notifications-service/database/sql",
        }
        with patch.object(self.instance, "sql_root") as sql_root:
            sql_root.__truediv__.side_effect = lambda database: directories[database]
            for database in directories:
                with self.subTest(database=database):
                    self.assertIn("CREATE", self.instance.seed_sql(database))
            quiz_sql = self.instance.seed_sql("quizzes_courses")
            self.assertIn("INSERT INTO SeedHistory", quiz_sql)
            self.assertIn("pg_get_serial_sequence", quiz_sql)


if __name__ == "__main__":
    unittest.main()