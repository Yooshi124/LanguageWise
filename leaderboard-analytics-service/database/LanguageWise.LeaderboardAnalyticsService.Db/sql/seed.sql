-- Seed data for the student 5 (Leaderboard and Analytics) microservice database.
-- Only executed when SampleItems is empty, so it is safe to re-run.
-- The project specification (section 2.2) requires a minimum of ten records.

INSERT INTO SampleItems (Name, Description, CreatedAt) VALUES
    ('Global Rank',         'Your position across every language and course.', '2026-02-02T09:00:00Z'),
    ('Course Rank',         'Your position within a single course.',           '2026-02-03T09:15:00Z'),
    ('Language Rank',       'Your position within a single language.',         '2026-02-04T09:30:00Z'),
    ('Weekly Points',       'Points earned over the last seven days.',         '2026-02-05T09:45:00Z'),
    ('Accuracy Rate',       'Correct answers as a share of all answers.',      '2026-02-06T10:00:00Z'),
    ('Study Time',          'Minutes spent in activities and quizzes.',        '2026-02-07T10:15:00Z'),
    ('Streak Length',       'Consecutive days with at least one activity.',    '2026-02-08T10:30:00Z'),
    ('Forum Contributions', 'Posts and replies on the discussion forum.',      '2026-02-09T10:45:00Z'),
    ('Improvement Trend',   'How your score has moved over four weeks.',       '2026-02-10T11:00:00Z'),
    ('Cohort Comparison',   'How you compare against everyone at your level.', '2026-02-11T11:15:00Z');
