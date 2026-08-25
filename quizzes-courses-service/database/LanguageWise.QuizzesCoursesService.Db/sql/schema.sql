-- Schema for the student 3 (Quizzes and Courses) microservice database.
-- Applied idempotently on every start-up of the database service.

CREATE TABLE IF NOT EXISTS SampleItems (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Name        TEXT NOT NULL,
    Description TEXT NOT NULL DEFAULT '',
    CreatedAt   TEXT NOT NULL
);
