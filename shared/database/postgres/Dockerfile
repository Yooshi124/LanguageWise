FROM postgres:18

COPY databases.sql /docker-entrypoint-initdb.d/000-databases.sql
COPY init-quests.sql /docker-entrypoint-initdb.d/001-quests.sql
COPY --from=quests_sql schema.sql seed.sql /seed/quests/

HEALTHCHECK --interval=10s --timeout=5s --start-period=10s --retries=6 \
    CMD pg_isready -h 127.0.0.1 -U "$POSTGRES_USER" -d "$POSTGRES_DB"