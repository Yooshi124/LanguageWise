-- Seed data for the shared microservice database.
-- Only executed when SampleItems is empty, so it is safe to re-run.
-- The project specification (section 2.2) requires a minimum of ten records.

INSERT INTO SampleItems (Name, Description, CreatedAt) VALUES
    ('Welcome to LanguageWise',  'The unified home page for the team project.',          '2026-01-05T09:00:00Z'),
    ('Release 0 kick-off',       'Microservices, Docker Compose and a DevOps baseline.', '2026-01-06T09:15:00Z'),
    ('Mini Games',               'Student 1 owns the games and activities feature.',     '2026-01-07T09:30:00Z'),
    ('Discussion Forum',         'Student 2 owns the discussion and chat feature.',      '2026-01-08T09:45:00Z'),
    ('Quizzes and Courses',      'Student 3 owns the quizzes and courses feature.',      '2026-01-09T10:00:00Z'),
    ('Quests and Achievements',  'Student 4 owns achievements and notifications.',       '2026-01-10T10:15:00Z'),
    ('Leaderboard',              'Student 5 owns the leaderboard and analytics.',        '2026-01-11T10:30:00Z'),
    ('Shared CSS theme',         'A consistent look and feel across every frontend.',    '2026-01-12T10:45:00Z'),
    ('Docker Compose',           'One command brings the whole application up.',         '2026-01-13T11:00:00Z'),
    ('GitHub Actions',           'Every pull request is built and tested before merge.', '2026-01-14T11:15:00Z');

INSERT INTO Users (Username, Password) VALUES
    ('amber',   'test'),
    ('lachlan', 'password'),
    ('roan',    'password'),
    ('justin',  'password'),
    ('kyan',    'password');
