PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Games (
    Id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    GameType            TEXT NOT NULL CHECK (GameType IN ('guess_the_word', 'word_search', 'associations')),
    CourseCode          TEXT NOT NULL,
    Solution            TEXT NOT NULL,
    Words               TEXT NOT NULL CHECK (json_valid(Words)),
    Difficulty          TEXT NOT NULL DEFAULT 'intermediate' CHECK (Difficulty IN ('easy', 'intermediate', 'hard')),
    CreatedAt           TEXT NOT NULL,
    ExpiresAt           TEXT
);

CREATE TABLE IF NOT EXISTS GameAttempts (
    Id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    GameId              INTEGER NOT NULL,
    UserId              INTEGER NOT NULL CHECK (UserId > 0),
    Score               INTEGER NOT NULL DEFAULT 0 CHECK (Score >= 0),
    IsWon               INTEGER NOT NULL DEFAULT 0 CHECK (IsWon IN (0, 1)),
    IsComplete          INTEGER NOT NULL DEFAULT 0 CHECK (IsComplete IN (0, 1)),
    AttemptCount        INTEGER NOT NULL DEFAULT 0 CHECK (AttemptCount >= 0),
    StartedAt           TEXT NOT NULL,
    CompletedAt         TEXT,
    TimeSpentSeconds    INTEGER NOT NULL DEFAULT 0 CHECK (TimeSpentSeconds >= 0),
    FOREIGN KEY (GameId) REFERENCES Games(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_Games_CourseCode ON Games(CourseCode);
CREATE INDEX IF NOT EXISTS IX_GameAttempts_UserId ON GameAttempts(UserId);
CREATE INDEX IF NOT EXISTS IX_GameAttempts_GameId ON GameAttempts(GameId);
CREATE INDEX IF NOT EXISTS IX_GameAttempts_UserId_CreatedAt ON GameAttempts(UserId, StartedAt);
