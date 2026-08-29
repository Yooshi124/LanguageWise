
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

-- UserId values match the Users seeded by the shared database service, so a
-- signed-in account actually owns some of this content and can exercise the
-- owner-only edit and delete routes:
--   1 = amber, 2 = lachlan, 3 = roan, 4 = justin, 5 = kyan
INSERT INTO Posts (Id, UserId, Title, Content, Category, CreatedAt, UpdatedAt) VALUES
    (1, 2, 'Welcome to the discussion forum',
        'Introduce yourself and tell us what language you are learning.',
        'global', '2026-02-12T09:00:00Z', '2026-02-12T09:00:00Z'),
    (2, 2, 'Best way to practise vocabulary',
        'What routine helps you remember new words?',
        'spanish', '2026-02-12T10:00:00Z', '2026-02-12T10:00:00Z'),
    (3, 4, 'Share a useful resource',
        'Post a book, podcast, or channel that has helped you.',
        'global', '2026-02-12T11:00:00Z', '2026-02-12T11:00:00Z'),
    (4, 1, 'Italian pronunciation drills',
        'Rolling the double consonants is still catching me out.',
        'italian', '2026-02-13T09:00:00Z', '2026-02-13T09:00:00Z'),
    (5, 3, 'Weekly Japanese study check-in',
        'Post how many kanji you added this week.',
        'japanese', '2026-02-13T10:00:00Z', '2026-02-13T10:00:00Z'),
    (6, 2, 'Help with the Spanish subjunctive',
        'When does the subjunctive actually become necessary?',
        'spanish', '2026-02-13T11:00:00Z', '2026-02-13T11:00:00Z');

INSERT INTO Comments (Id, PostId, UserId, Content, CreatedAt, UpdatedAt) VALUES
    (1, 1, 1, 'I am learning Spanish and looking forward to practising here.',
        '2026-02-12T09:30:00Z', '2026-02-12T09:30:00Z'),
    (2, 2, 5, 'I use flashcards every morning on my commute.',
        '2026-02-12T10:30:00Z', '2026-02-12T10:30:00Z'),
    (3, 3, 2, 'The podcast Coffee Break Languages is a great starting point.',
        '2026-02-12T11:30:00Z', '2026-02-12T11:30:00Z'),
    (4, 6, 4, 'It shows up after expressions of doubt, wishes and emotion.',
        '2026-02-13T11:30:00Z', '2026-02-13T11:30:00Z'),
    (5, 2, 2, 'Spacing the reviews out mattered more than the app I picked.',
        '2026-02-13T12:00:00Z', '2026-02-13T12:00:00Z');

INSERT INTO Likes (Id, UserId, PostId, CommentId, CreatedAt) VALUES
    (1, 3, 1,    NULL, '2026-02-12T12:00:00Z'),
    (2, 4, 1,    NULL, '2026-02-12T12:02:00Z'),
    (3, 5, 2,    NULL, '2026-02-12T12:05:00Z'),
    (4, 1, 6,    NULL, '2026-02-13T12:10:00Z'),
    (5, 2, NULL, 1,    '2026-02-12T12:10:00Z'),
    (6, 1, NULL, 3,    '2026-02-12T12:15:00Z'),
    (7, 2, NULL, 4,    '2026-02-13T12:20:00Z');

INSERT INTO Images (Id, PostId, CommentId, FileUrl, FileName, UploadedAt) VALUES
    (1, 1,    NULL, '/uploads/welcome.png',    'welcome.png',    '2026-02-12T12:20:00Z'),
    (2, NULL, 2,    '/uploads/flashcards.jpg', 'flashcards.jpg', '2026-02-12T12:25:00Z');
