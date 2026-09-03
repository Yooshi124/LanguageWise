INSERT INTO api.achievements (achievement_id, name, description, image, trigger, progress_needed) VALUES
    (1,  'First Lesson',         'Complete your first lesson',                         '/images/achievements/first-lesson.png',          'lesson-completion',      1),
    (2,  'Committed Learner',    'Complete five lessons',                              '/images/achievements/committed-learner.png',     'lesson-completion',      5),
    (3,  'Lesson Scholar',       'Complete twenty lessons',                            '/images/achievements/lesson-scholar.png',        'lesson-completion',     20),
    (4,  'First Contribution',   'Make your first community contribution',             '/images/achievements/first-contribution.png',    'community-contribution', 1),
    (5,  'Community Regular',    'Make ten community contributions',                   '/images/achievements/community-regular.png',     'community-contribution',10),
    (6,  'Community Champion',   'Make fifty community contributions',                 '/images/achievements/community-champion.png',    'community-contribution',50),
    (7,  'First Game Win',       'Win your first mini-game',                           '/images/achievements/first-game-win.png',        'minigame-win',           1),
    (8,  'Game Night',           'Win ten mini-games',                                 '/images/achievements/game-night.png',            'minigame-win',          10),
    (9,  'Three Day Streak',     'Log in on three consecutive days',                   '/images/achievements/three-day-streak.png',     'login-streak',      3),
    (10, 'Seven Day Streak',     'Log in on seven consecutive days',                   '/images/achievements/seven-day-streak.png',     'login-streak',      7),
    (11, 'Longest Login Streak', 'Your longest run of consecutive daily LanguageWise logins', '/images/achievements/longest-login-streak.png', 'login-streak',     -1),
    (12, 'Games Master',         'Win fifty mini-games',                               '/images/achievements/games-master.png',          'minigame-win',          50);

SELECT setval(pg_get_serial_sequence('api.achievements', 'achievement_id'), 12);

INSERT INTO api.user_preferences (user_id, email) VALUES
    (1, 'amber@example.com'),
    (2, 'lachlan@example.com'),
    (3, 'roan@example.com'),
    (4, 'justin@example.com'),
    (5, 'kyan@example.com');

INSERT INTO api.user_achievements (user_id, achievement_id, progress) VALUES
    (4, 9, 2),
    (4, 10, 2),
    (4, 11, 2),
    (5, 9, 3),
    (5, 10, 5),
    (5, 11, 5);

INSERT INTO api.notifications (user_id, trigger, time, email_subject, email_body) VALUES
    (4, 'login-streak', '2026-08-30T08:00:00Z', 'Daily learning streak', 'Hi Justin, you continued your daily learning streak.');