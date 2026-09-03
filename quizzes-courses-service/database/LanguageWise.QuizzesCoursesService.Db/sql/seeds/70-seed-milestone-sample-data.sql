-- Realistic, idempotent learning progress for development accounts.
-- User 1 has broad progress; users 2-5 focus on one or two active courses.

-- Remove only rows created by the superseded German-only development seed.
DELETE FROM Milestones
WHERE UserId = 1
  AND CompletedAt = '2026-01-01T00:00:00.0000000+00:00'
  AND LessonId IN (
      SELECT lesson.Id
      FROM Lessons lesson
      JOIN Courses course ON course.Id = lesson.CourseId
      WHERE course.Code = 'de'
  );

WITH LessonMilestoneSeeds (CourseCode, MaximumSortOrder, StartDate) AS (
    VALUES
        ('pl', 20, '2026-07-01'),
        ('de', 6,  '2026-08-01'),
        ('fr', 2,  '2026-08-10'),
        ('es', 1,  '2026-08-20')
)
INSERT OR IGNORE INTO Milestones (UserId, LessonId, CompletedAt)
SELECT
    1,
    lesson.Id,
    printf(
        '%sT09:00:00.0000000+00:00',
        date(seed.StartDate, printf('+%d days', lesson.SortOrder - 1)))
FROM LessonMilestoneSeeds seed
JOIN Courses course ON course.Code = seed.CourseCode
JOIN Lessons lesson
    ON lesson.CourseId = course.Id
   AND lesson.SortOrder <= seed.MaximumSortOrder;

WITH UserLessonMilestoneSeeds (UserId, CourseCode, MaximumSortOrder, StartDaysAgo) AS (
    VALUES
        (2, 'de', 5, 6),
        (2, 'nl', 2, 1),
        (3, 'fr', 4, 5),
        (3, 'es', 2, 1),
        (4, 'it', 3, 4),
        (5, 'pl', 4, 6),
        (5, 'nl', 2, 1)
)
INSERT OR IGNORE INTO Milestones (UserId, LessonId, CompletedAt)
SELECT
    seed.UserId,
    lesson.Id,
    printf(
        '%sT09:00:00.0000000+00:00',
        date('now', printf('-%d days', seed.StartDaysAgo - lesson.SortOrder + 1)))
FROM UserLessonMilestoneSeeds seed
JOIN Courses course ON course.Code = seed.CourseCode
JOIN Lessons lesson
    ON lesson.CourseId = course.Id
   AND lesson.SortOrder <= seed.MaximumSortOrder;

-- A completed Polish course contributes one passing attempt for every quiz.
INSERT INTO QuizAttempts
    (UserId, QuizId, Score, TotalQuestions, StartedAt, CompletedAt)
SELECT
    1,
    quiz.Id,
    COUNT(question.Id),
    COUNT(question.Id),
    printf('2026-07-%02dT10:00:00.0000000+00:00', lesson.SortOrder),
    printf('2026-07-%02dT10:15:00.0000000+00:00', lesson.SortOrder)
FROM Courses course
JOIN Lessons lesson ON lesson.CourseId = course.Id
JOIN Quizzes quiz ON quiz.LessonId = lesson.Id
JOIN QuizQuestions question ON question.QuizId = quiz.Id
WHERE course.Code = 'pl'
  AND NOT EXISTS (
      SELECT 1
      FROM QuizAttempts existing
      WHERE existing.UserId = 1
        AND existing.QuizId = quiz.Id
        AND existing.StartedAt =
            printf('2026-07-%02dT10:00:00.0000000+00:00', lesson.SortOrder)
  )
GROUP BY quiz.Id, lesson.SortOrder;

-- Other courses include failures, passes, a successful retry, and an open attempt.
WITH AttemptSeeds (
    CourseCode,
    LessonSlug,
    Score,
    StartedAt,
    CompletedAt
) AS (
    VALUES
        ('de', 'greetings',     7, '2026-08-01T10:00:00.0000000+00:00', '2026-08-01T10:12:00.0000000+00:00'),
        ('de', 'greetings',     9, '2026-08-02T10:00:00.0000000+00:00', '2026-08-02T10:10:00.0000000+00:00'),
        ('de', 'introductions', 8, '2026-08-03T10:00:00.0000000+00:00', '2026-08-03T10:14:00.0000000+00:00'),
        ('de', 'politeness',    6, '2026-08-04T10:00:00.0000000+00:00', '2026-08-04T10:11:00.0000000+00:00'),
        ('de', 'numbers',      10, '2026-08-05T10:00:00.0000000+00:00', '2026-08-05T10:09:00.0000000+00:00'),
        ('de', 'family',        0, '2026-08-06T10:00:00.0000000+00:00', NULL),
        ('fr', 'greetings',    10, '2026-08-10T10:00:00.0000000+00:00', '2026-08-10T10:08:00.0000000+00:00'),
        ('fr', 'introductions', 7, '2026-08-11T10:00:00.0000000+00:00', '2026-08-11T10:13:00.0000000+00:00'),
        ('es', 'greetings',     5, '2026-08-20T10:00:00.0000000+00:00', '2026-08-20T10:15:00.0000000+00:00')
)
INSERT INTO QuizAttempts
    (UserId, QuizId, Score, TotalQuestions, StartedAt, CompletedAt)
SELECT
    1,
    quiz.Id,
    seed.Score,
    COUNT(question.Id),
    seed.StartedAt,
    seed.CompletedAt
FROM AttemptSeeds seed
JOIN Courses course ON course.Code = seed.CourseCode
JOIN Lessons lesson
    ON lesson.CourseId = course.Id
   AND lesson.Slug = seed.LessonSlug
JOIN Quizzes quiz ON quiz.LessonId = lesson.Id
JOIN QuizQuestions question ON question.QuizId = quiz.Id
WHERE NOT EXISTS (
    SELECT 1
    FROM QuizAttempts existing
    WHERE existing.UserId = 1
      AND existing.QuizId = quiz.Id
      AND existing.StartedAt = seed.StartedAt
)
GROUP BY quiz.Id, seed.Score, seed.StartedAt, seed.CompletedAt
ORDER BY seed.StartedAt;

WITH SampleAttempts AS (
    SELECT attempt.Id, attempt.QuizId, attempt.Score, attempt.CompletedAt
    FROM QuizAttempts attempt
    JOIN Quizzes quiz ON quiz.Id = attempt.QuizId
    JOIN Lessons lesson ON lesson.Id = quiz.LessonId
    JOIN Courses course ON course.Id = lesson.CourseId
    WHERE attempt.UserId = 1
      AND attempt.CompletedAt IS NOT NULL
      AND (
          (course.Code = 'pl' AND attempt.StartedAt LIKE '2026-07-%')
          OR attempt.StartedAt IN (
              '2026-08-01T10:00:00.0000000+00:00',
              '2026-08-02T10:00:00.0000000+00:00',
              '2026-08-03T10:00:00.0000000+00:00',
              '2026-08-04T10:00:00.0000000+00:00',
              '2026-08-05T10:00:00.0000000+00:00',
              '2026-08-10T10:00:00.0000000+00:00',
              '2026-08-11T10:00:00.0000000+00:00',
              '2026-08-20T10:00:00.0000000+00:00'
          )
      )
)
INSERT OR IGNORE INTO QuizAnswers
    (AttemptId, QuestionId, StudentResponse, IsCorrect, AnsweredAt)
SELECT
    attempt.Id,
    question.Id,
    CASE
        WHEN question.SortOrder <= attempt.Score THEN question.CorrectAnswer
        WHEN question.Type = 'word_ordering' THEN '[]'
        ELSE 'Sample incorrect response'
    END,
    question.SortOrder <= attempt.Score,
    attempt.CompletedAt
FROM SampleAttempts attempt
JOIN QuizQuestions question ON question.QuizId = attempt.QuizId;

WITH SampleAttempts AS (
    SELECT attempt.UserId, attempt.QuizId, attempt.Score, attempt.CompletedAt
    FROM QuizAttempts attempt
    JOIN Quizzes quiz ON quiz.Id = attempt.QuizId
    JOIN Lessons lesson ON lesson.Id = quiz.LessonId
    JOIN Courses course ON course.Id = lesson.CourseId
    WHERE attempt.UserId = 1
      AND attempt.CompletedAt IS NOT NULL
      AND attempt.Score >= 8
      AND (
          (course.Code = 'pl' AND attempt.StartedAt LIKE '2026-07-%')
          OR attempt.StartedAt IN (
              '2026-08-02T10:00:00.0000000+00:00',
              '2026-08-03T10:00:00.0000000+00:00',
              '2026-08-05T10:00:00.0000000+00:00',
              '2026-08-10T10:00:00.0000000+00:00'
          )
      )
)
INSERT OR IGNORE INTO Milestones (UserId, QuizId, CompletedAt)
SELECT UserId, QuizId, MIN(CompletedAt)
FROM SampleAttempts
GROUP BY UserId, QuizId;

-- Course milestones are only created when every lesson and quiz prerequisite exists.
INSERT OR IGNORE INTO Milestones (UserId, CourseId, CompletedAt)
SELECT 1, course.Id, '2026-07-21T09:00:00.0000000+00:00'
FROM Courses course
WHERE course.Code = 'pl'
  AND NOT EXISTS (
      SELECT 1
      FROM Lessons lesson
      WHERE lesson.CourseId = course.Id
        AND NOT EXISTS (
            SELECT 1
            FROM Milestones milestone
            WHERE milestone.UserId = 1
              AND milestone.LessonId = lesson.Id
        )
  )
  AND NOT EXISTS (
      SELECT 1
      FROM Quizzes quiz
      JOIN Lessons lesson ON lesson.Id = quiz.LessonId
      WHERE lesson.CourseId = course.Id
        AND NOT EXISTS (
            SELECT 1
            FROM Milestones milestone
            WHERE milestone.UserId = 1
              AND milestone.QuizId = quiz.Id
        )
  );
