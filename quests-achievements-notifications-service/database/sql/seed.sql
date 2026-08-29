INSERT INTO api.achievements (achievement_id, name, image, trigger, progress_needed) VALUES
    (1,  'First Course',        '/images/achievements/first-course.png',        'course-completion', 1),
    (2,  'Course Explorer',     '/images/achievements/course-explorer.png',     'course-completion', 5),
    (3,  'Course Champion',     '/images/achievements/course-champion.png',     'course-completion', 10),
    (4,  'First Applause',      '/images/achievements/first-applause.png',      'post-engagement',   1),
    (5,  'Crowd Pleaser',       '/images/achievements/crowd-pleaser.png',       'post-engagement',   10),
    (6,  'Community Favourite', '/images/achievements/community-favourite.png', 'post-engagement',   50),
    (7,  'Quiz Starter',        '/images/achievements/quiz-starter.png',        'quiz-result',       1),
    (8,  'Quiz Master',         '/images/achievements/quiz-master.png',         'quiz-result',       10),
    (9,  'Three Day Streak',    '/images/achievements/three-day-streak.png',    'streak',            3),
    (10, 'Seven Day Streak',    '/images/achievements/seven-day-streak.png',    'streak',            7)
ON CONFLICT (achievement_id) DO UPDATE SET
    name = EXCLUDED.name,
    image = EXCLUDED.image,
    trigger = EXCLUDED.trigger,
    progress_needed = EXCLUDED.progress_needed;

SELECT setval(
    pg_get_serial_sequence('api.achievements', 'achievement_id'),
    (SELECT max(achievement_id) FROM api.achievements)
);

INSERT INTO api.user_preferences (user_id, email) VALUES
    (1, 'amber@example.com'),
    (2, 'lachlan@example.com'),
    (3, 'roan@example.com'),
    (4, 'justin@example.com'),
    (5, 'kyan@example.com')
ON CONFLICT (user_id) DO UPDATE SET
    email = EXCLUDED.email;

INSERT INTO api.user_achievements (user_id, achievement_id, progress) VALUES
    (1, 1, 1),
    (1, 2, 3),
    (1, 5, 6),
    (2, 4, 1),
    (2, 5, 4),
    (3, 7, 1),
    (3, 8, 7),
    (4, 9, 2),
    (5, 9, 3),
    (5, 10, 5)
ON CONFLICT (user_id, achievement_id) DO UPDATE SET
    progress = EXCLUDED.progress;