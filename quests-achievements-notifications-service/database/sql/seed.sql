INSERT INTO api.sample_items (id, name, description, "createdAt") VALUES
    (1,  'First Steps',        'Awarded for completing your very first lesson.',  '2026-02-02T09:00:00Z'),
    (2,  'Five Course Streak', 'Complete five courses to earn a silver medal.',   '2026-02-03T09:15:00Z'),
    (3,  'Perfect Quiz',       'Score full marks on any quiz.',                   '2026-02-04T09:30:00Z'),
    (4,  'Seven Day Streak',   'Study every day for a week.',                     '2026-02-05T09:45:00Z'),
    (5,  'Helpful Voice',      'Reply to ten posts on the discussion forum.',     '2026-02-06T10:00:00Z'),
    (6,  'Night Owl',          'Finish an activity after midnight.',              '2026-02-07T10:15:00Z'),
    (7,  'Early Bird',         'Finish an activity before seven in the morning.', '2026-02-08T10:30:00Z'),
    (8,  'Polyglot',           'Reach level two in three different languages.',   '2026-02-09T10:45:00Z'),
    (9,  'Course Certificate', 'A generated certificate emailed on completion.',  '2026-02-10T11:00:00Z'),
    (10, 'Top of the Class',   'Finish first on a course leaderboard.',           '2026-02-11T11:15:00Z')
ON CONFLICT (id) DO UPDATE SET
    name = EXCLUDED.name,
    description = EXCLUDED.description,
    "createdAt" = EXCLUDED."createdAt";

SELECT setval(
    pg_get_serial_sequence('api.sample_items', 'id'),
    (SELECT max(id) FROM api.sample_items)
);