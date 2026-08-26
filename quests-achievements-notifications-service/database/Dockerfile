FROM postgres:18

COPY sql/schema.sql /docker-entrypoint-initdb.d/001-schema.sql
COPY sql/seed.sql /docker-entrypoint-initdb.d/002-seed.sql

HEALTHCHECK --interval=10s --timeout=5s --start-period=10s --retries=6 \
    CMD pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB"
