
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

INSERT INTO Posts (Id, UserId, Title, Content, CreatedAt, UpdatedAt) VALUES
    (1, 101, 'Welcome to the discussion forum',
        'Introduce yourself and tell us what language you are learning.',
        '2026-02-12T09:00:00Z', '2026-02-12T09:00:00Z'),
    (2, 102, 'Best way to practise vocabulary',
        'What routine helps you remember new words?',
        '2026-02-12T10:00:00Z', '2026-02-12T10:00:00Z'),
    (3, 103, 'Share a useful resource',
        'Post a book, podcast, or channel that has helped you.',
        '2026-02-12T11:00:00Z', '2026-02-12T11:00:00Z');

INSERT INTO Comments (Id, PostId, UserId, Content, CreatedAt, UpdatedAt) VALUES
    (1, 1, 102, 'I am learning Spanish and looking forward to practising here.',
        '2026-02-12T09:30:00Z', '2026-02-12T09:30:00Z'),
    (2, 2, 101, 'I use flashcards every morning on my commute.',
        '2026-02-12T10:30:00Z', '2026-02-12T10:30:00Z'),
    (3, 3, 104, 'The podcast Coffee Break Languages is a great starting point.',
        '2026-02-12T11:30:00Z', '2026-02-12T11:30:00Z');

INSERT INTO Likes (Id, UserId, PostId, CommentId, CreatedAt) VALUES
    (1, 103, 1, NULL, '2026-02-12T12:00:00Z'),
    (2, 104, 2, NULL, '2026-02-12T12:05:00Z'),
    (3, 101, NULL, 1, '2026-02-12T12:10:00Z'),
    (4, 102, NULL, 3, '2026-02-12T12:15:00Z');

INSERT INTO Images (Id, PostId, CommentId, FileUrl, FileName, UploadedAt) VALUES
    (1, 1, NULL, '/uploads/welcome.png', 'welcome.png', '2026-02-12T12:20:00Z'),
    (2, NULL, 2, '/uploads/flashcards.jpg', 'flashcards.jpg', '2026-02-12T12:25:00Z');
