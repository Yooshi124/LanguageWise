-- Seed data for the student 4 (Quests and Achievements) microservice database.
-- Only executed when SampleItems is empty, so it is safe to re-run.
-- The project specification (section 2.2) requires a minimum of ten records.

INSERT INTO SampleItems (Name, Description, CreatedAt) VALUES
    ('First Steps',        'Awarded for completing your very first lesson.',  '2026-02-02T09:00:00Z'),
    ('Five Course Streak', 'Complete five courses to earn a silver medal.',   '2026-02-03T09:15:00Z'),
    ('Perfect Quiz',       'Score full marks on any quiz.',                   '2026-02-04T09:30:00Z'),
    ('Seven Day Streak',   'Study every day for a week.',                     '2026-02-05T09:45:00Z'),
    ('Helpful Voice',      'Reply to ten posts on the discussion forum.',     '2026-02-06T10:00:00Z'),
    ('Night Owl',          'Finish an activity after midnight.',              '2026-02-07T10:15:00Z'),
    ('Early Bird',         'Finish an activity before seven in the morning.', '2026-02-08T10:30:00Z'),
    ('Polyglot',           'Reach level two in three different languages.',   '2026-02-09T10:45:00Z'),
    ('Course Certificate', 'A generated certificate emailed on completion.',  '2026-02-10T11:00:00Z'),
    ('Top of the Class',   'Finish first on a course leaderboard.',           '2026-02-11T11:15:00Z');
