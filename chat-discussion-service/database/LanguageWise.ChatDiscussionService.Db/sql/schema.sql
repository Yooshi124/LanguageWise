-- Schema for the chat discussion microservice database.
-- Applied idempotently on every start-up of the database service.

PRAGMA foreign_keys = ON;

-- Retained for the existing database-service health and seed wiring.
CREATE TABLE IF NOT EXISTS SampleItems (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Name        TEXT NOT NULL,
    Description TEXT NOT NULL,
    CreatedAt   TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Posts (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId      INTEGER NOT NULL,
    Title       TEXT NOT NULL,
    Content     TEXT NOT NULL,
    CreatedAt   TEXT NOT NULL,
    UpdatedAt   TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Comments (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    PostId      INTEGER NOT NULL,
    UserId      INTEGER NOT NULL,
    Content     TEXT NOT NULL,
    CreatedAt   TEXT NOT NULL,
    UpdatedAt   TEXT NOT NULL,
    FOREIGN KEY (PostId) REFERENCES Posts (Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_Comments_PostId ON Comments (PostId);

CREATE TABLE IF NOT EXISTS Likes (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId      INTEGER NOT NULL,
    PostId      INTEGER,
    CommentId   INTEGER,
    CreatedAt   TEXT NOT NULL,
    FOREIGN KEY (PostId) REFERENCES Posts (Id) ON DELETE CASCADE,
    FOREIGN KEY (CommentId) REFERENCES Comments (Id) ON DELETE CASCADE,
    CHECK (PostId IS NOT NULL OR CommentId IS NOT NULL)
);

CREATE INDEX IF NOT EXISTS IX_Likes_PostId ON Likes (PostId);
CREATE INDEX IF NOT EXISTS IX_Likes_CommentId ON Likes (CommentId);

CREATE TABLE IF NOT EXISTS Images (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    PostId      INTEGER,
    CommentId   INTEGER,
    FileUrl     TEXT NOT NULL,
    FileName    TEXT NOT NULL,
    UploadedAt  TEXT NOT NULL,
    FOREIGN KEY (PostId) REFERENCES Posts (Id) ON DELETE CASCADE,
    FOREIGN KEY (CommentId) REFERENCES Comments (Id) ON DELETE CASCADE,
    CHECK (PostId IS NOT NULL OR CommentId IS NOT NULL)
);

CREATE INDEX IF NOT EXISTS IX_Images_PostId ON Images (PostId);
CREATE INDEX IF NOT EXISTS IX_Images_CommentId ON Images (CommentId);
