-- Seed data for the student 2 (Discussion Forum) microservice database.
-- Only executed when SampleItems is empty, so it is safe to re-run.
-- The project specification (section 2.2) requires a minimum of ten records.

INSERT INTO SampleItems (Name, Description, CreatedAt) VALUES
    ('Introduce yourself',     'Say hello and tell us which language you study.', '2026-02-02T09:00:00Z'),
    ('Study tips thread',      'Share the routine that actually works for you.',  '2026-02-03T09:15:00Z'),
    ('Grammar questions',      'Ask about anything that will not stick.',         '2026-02-04T09:30:00Z'),
    ('Pronunciation help',     'Post a recording and ask for feedback.',          '2026-02-05T09:45:00Z'),
    ('Weekly progress check',  'Report what you finished this week.',             '2026-02-06T10:00:00Z'),
    ('Resource swap',          'Recommend books, podcasts and channels.',         '2026-02-07T10:15:00Z'),
    ('Find a study partner',   'Match with someone at a similar level.',          '2026-02-08T10:30:00Z'),
    ('Milestone celebrations', 'Post when you pass a course or a quiz.',          '2026-02-09T10:45:00Z'),
    ('Bug reports',            'Tell the team when something breaks.',            '2026-02-10T11:00:00Z'),
    ('Off topic lounge',       'Everything that does not fit anywhere else.',     '2026-02-11T11:15:00Z');
