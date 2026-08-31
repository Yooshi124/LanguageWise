PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Courses (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Code        TEXT NOT NULL UNIQUE CHECK (Code IN ('de', 'fr', 'it', 'nl', 'es', 'pl')),
    Title       TEXT NOT NULL,
    Description TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS Lessons (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    CourseId        INTEGER NOT NULL,
    Slug            TEXT NOT NULL,
    Title           TEXT NOT NULL,
    SortOrder       INTEGER NOT NULL CHECK (SortOrder > 0),
    ContentMarkdown TEXT NOT NULL,
    FOREIGN KEY (CourseId) REFERENCES Courses(Id) ON DELETE CASCADE,
    UNIQUE (CourseId, Slug),
    UNIQUE (CourseId, SortOrder)
);

CREATE TABLE IF NOT EXISTS LessonVocabulary (
    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
    LessonId       INTEGER NOT NULL UNIQUE,
    VocabularyJson TEXT NOT NULL CHECK (json_valid(VocabularyJson)),
    FOREIGN KEY (LessonId) REFERENCES Lessons(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS Quizzes (
    Id       INTEGER PRIMARY KEY AUTOINCREMENT,
    LessonId INTEGER NOT NULL UNIQUE,
    Title    TEXT NOT NULL,
    FOREIGN KEY (LessonId) REFERENCES Lessons(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS QuizQuestions (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    QuizId        INTEGER NOT NULL,
    SortOrder     INTEGER NOT NULL CHECK (SortOrder > 0),
    Content       TEXT NOT NULL,
    Type          TEXT NOT NULL CHECK (Type IN ('multiple_choice', 'word_ordering', 'free_text')),
    QuestionData  TEXT NOT NULL CHECK (json_valid(QuestionData)),
    CorrectAnswer TEXT NOT NULL,
    FOREIGN KEY (QuizId) REFERENCES Quizzes(Id) ON DELETE CASCADE,
    UNIQUE (QuizId, SortOrder)
);

CREATE TABLE IF NOT EXISTS QuizAttempts (
    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId         INTEGER NOT NULL CHECK (UserId > 0),
    QuizId         INTEGER NOT NULL,
    Score          INTEGER NOT NULL DEFAULT 0 CHECK (Score >= 0 AND Score <= TotalQuestions),
    TotalQuestions INTEGER NOT NULL CHECK (TotalQuestions > 0),
    StartedAt      TEXT NOT NULL,
    CompletedAt    TEXT,
    FOREIGN KEY (QuizId) REFERENCES Quizzes(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS QuizAnswers (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    AttemptId       INTEGER NOT NULL,
    QuestionId      INTEGER NOT NULL,
    StudentResponse TEXT NOT NULL,
    IsCorrect       INTEGER NOT NULL CHECK (IsCorrect IN (0, 1)),
    AnsweredAt      TEXT NOT NULL,
    FOREIGN KEY (AttemptId) REFERENCES QuizAttempts(Id) ON DELETE CASCADE,
    FOREIGN KEY (QuestionId) REFERENCES QuizQuestions(Id) ON DELETE CASCADE,
    UNIQUE (AttemptId, QuestionId)
);

CREATE TABLE IF NOT EXISTS Milestones (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId      INTEGER NOT NULL CHECK (UserId > 0),
    CourseId    INTEGER,
    LessonId    INTEGER,
    QuizId      INTEGER,
    CompletedAt TEXT NOT NULL,
    FOREIGN KEY (CourseId) REFERENCES Courses(Id) ON DELETE CASCADE,
    FOREIGN KEY (LessonId) REFERENCES Lessons(Id) ON DELETE CASCADE,
    FOREIGN KEY (QuizId) REFERENCES Quizzes(Id) ON DELETE CASCADE,
    CHECK ((CourseId IS NOT NULL) + (LessonId IS NOT NULL) + (QuizId IS NOT NULL) = 1)
);

CREATE UNIQUE INDEX IF NOT EXISTS UX_Milestones_User_Course
    ON Milestones(UserId, CourseId) WHERE CourseId IS NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS UX_Milestones_User_Lesson
    ON Milestones(UserId, LessonId) WHERE LessonId IS NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS UX_Milestones_User_Quiz
    ON Milestones(UserId, QuizId) WHERE QuizId IS NOT NULL;

CREATE INDEX IF NOT EXISTS IX_Lessons_CourseId ON Lessons(CourseId);
CREATE INDEX IF NOT EXISTS IX_QuizQuestions_QuizId ON QuizQuestions(QuizId);
CREATE INDEX IF NOT EXISTS IX_QuizAttempts_UserId ON QuizAttempts(UserId);
CREATE INDEX IF NOT EXISTS IX_QuizAttempts_QuizId_UserId ON QuizAttempts(QuizId, UserId);
