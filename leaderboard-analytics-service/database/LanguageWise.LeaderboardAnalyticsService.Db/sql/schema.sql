PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS LanguageRanking (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId      INTEGER NOT NULL,
    Language    TEXT NOT NULL,
    Score       INTEGER NOT NULL DEFAULT 0,
    Rank        INTEGER NOT NULL DEFAULT 0,
    UpdatedAt   TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_LanguageRanking_UserId ON LanguageRanking (UserId);
CREATE INDEX IF NOT EXISTS IX_LanguageRanking_Language ON LanguageRanking (Language);
CREATE UNIQUE INDEX IF NOT EXISTS UX_LanguageRanking_User_Language
    ON LanguageRanking (UserId, Language);

CREATE TABLE IF NOT EXISTS DiscussionRanking (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId       INTEGER NOT NULL UNIQUE,
    PostCount    INTEGER NOT NULL DEFAULT 0,
    CommentCount INTEGER NOT NULL DEFAULT 0,
    LikeCount    INTEGER NOT NULL DEFAULT 0,
    Score        INTEGER NOT NULL DEFAULT 0,
    Rank         INTEGER NOT NULL DEFAULT 0,
    UpdatedAt    TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_DiscussionRanking_UserId ON DiscussionRanking (UserId);
