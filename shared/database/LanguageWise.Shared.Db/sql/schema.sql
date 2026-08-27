-- Schema for the shared microservice database.
-- Applied idempotently on every start-up of the database service.

CREATE TABLE IF NOT EXISTS SampleItems (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Name        TEXT NOT NULL,
    Description TEXT NOT NULL DEFAULT '',
    CreatedAt   TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Users (
    Id       INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL UNIQUE,
    Password TEXT NOT NULL
);
