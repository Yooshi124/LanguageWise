PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS SampleItems (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Name        TEXT NOT NULL,
    Description TEXT NOT NULL,
    CreatedAt   TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Posts (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId      INTEGER NOT NULL,
    AuthorName  TEXT NOT NULL DEFAULT '',
    Title       TEXT NOT NULL,
    Content     TEXT NOT NULL,
    Category    TEXT NOT NULL,
    CreatedAt   TEXT NOT NULL,
    UpdatedAt   TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_Posts_UserId ON Posts (UserId);
CREATE INDEX IF NOT EXISTS IX_Posts_Category ON Posts (Category);

CREATE TABLE IF NOT EXISTS Comments (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    PostId      INTEGER NOT NULL,
    UserId      INTEGER NOT NULL,
    AuthorName  TEXT NOT NULL DEFAULT '',
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
    CHECK ((PostId IS NULL) <> (CommentId IS NULL))
);

CREATE INDEX IF NOT EXISTS IX_Likes_PostId ON Likes (PostId);
CREATE INDEX IF NOT EXISTS IX_Likes_CommentId ON Likes (CommentId);
CREATE UNIQUE INDEX IF NOT EXISTS UX_Likes_User_Post
    ON Likes (UserId, PostId) WHERE PostId IS NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS UX_Likes_User_Comment
    ON Likes (UserId, CommentId) WHERE CommentId IS NOT NULL;

-- One row per uploaded image. The bytes are not stored here: StorageKey names a file
-- under the image store directory.
CREATE TABLE IF NOT EXISTS Images (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    PostId      INTEGER,
    CommentId   INTEGER,
    StorageKey  TEXT NOT NULL UNIQUE,
    FileName    TEXT NOT NULL,
    ContentType TEXT NOT NULL,
    SizeBytes   INTEGER NOT NULL,
    UploadedAt  TEXT NOT NULL,
    FOREIGN KEY (PostId) REFERENCES Posts (Id) ON DELETE CASCADE,
    FOREIGN KEY (CommentId) REFERENCES Comments (Id) ON DELETE CASCADE,
    CHECK ((PostId IS NULL) <> (CommentId IS NULL))
);

CREATE INDEX IF NOT EXISTS IX_Images_PostId ON Images (PostId);
CREATE INDEX IF NOT EXISTS IX_Images_CommentId ON Images (CommentId);
