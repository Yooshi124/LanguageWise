-- Retire the release-zero sample table from both fresh and existing volumes.
DROP TABLE IF EXISTS SampleItems;

CREATE TABLE IF NOT EXISTS Users (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Username      TEXT NOT NULL UNIQUE,
    Password      TEXT NOT NULL,
    LastLogin     TEXT,
    CurrentStreak INTEGER NOT NULL DEFAULT 0
);
