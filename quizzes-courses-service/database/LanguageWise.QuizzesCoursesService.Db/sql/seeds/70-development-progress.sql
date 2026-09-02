-- Keep the primary development account playable without relying on an AI provider.
INSERT OR IGNORE INTO Milestones (UserId, LessonId, CompletedAt)
SELECT 1, Lessons.Id, '2026-01-01T00:00:00.0000000+00:00'
FROM Lessons
JOIN Courses ON Courses.Id = Lessons.CourseId
WHERE Courses.Code = 'de';