PRAGMA foreign_keys = ON;

-- Retire the release-zero sample table from both fresh and existing volumes.
DROP TABLE IF EXISTS SampleItems;

-- One row per place a post can live. Every forum except Global mirrors a course
-- in the quizzes and courses service: CourseId is that service's course ID, and
-- it is what the sync matches on, so renaming a course renames its forum here
-- without stranding the posts inside it.
--
-- Code is the stable, readable identity used in URLs and in the API. It is set
-- once from the course code and never resynced, so a course code change does not
-- break every link to the forum.
CREATE TABLE IF NOT EXISTS Forums (
    Id       INTEGER PRIMARY KEY AUTOINCREMENT,
    CourseId INTEGER UNIQUE,
    Code     TEXT NOT NULL UNIQUE,
    Name     TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Posts (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId      INTEGER NOT NULL,
    AuthorName  TEXT NOT NULL DEFAULT '',
    Title       TEXT NOT NULL,
    Content     TEXT NOT NULL,
    ForumId     INTEGER NOT NULL,
    CreatedAt   TEXT NOT NULL,
    UpdatedAt   TEXT NOT NULL,
    FOREIGN KEY (ForumId) REFERENCES Forums (Id)
);

CREATE INDEX IF NOT EXISTS IX_Posts_UserId ON Posts (UserId);
CREATE INDEX IF NOT EXISTS IX_Posts_ForumId ON Posts (ForumId);

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
