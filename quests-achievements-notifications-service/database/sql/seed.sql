INSERT INTO api.achievements (achievement_id, name, description, image, trigger, progress_needed) VALUES
    (1,  'First Course',         'Complete your first course',                         '/images/achievements/first-course.png',         'course-completion', 1),
    (2,  'Course Explorer',      'Complete five courses',                              '/images/achievements/course-explorer.png',      'course-completion', 5),
    (3,  'Course Champion',      'Complete ten courses',                               '/images/achievements/course-champion.png',      'course-completion', 10),
    (4,  'First Applause',       'Receive engagement on a community post',             '/images/achievements/first-applause.png',       'post-engagement',   1),
    (5,  'Crowd Pleaser',        'Receive engagement on ten community posts',          '/images/achievements/crowd-pleaser.png',        'post-engagement',   10),
    (6,  'Community Favourite',  'Receive engagement on fifty community posts',        '/images/achievements/community-favourite.png',  'post-engagement',   50),
    (7,  'Quiz Starter',         'Complete your first quiz',                           '/images/achievements/quiz-starter.png',         'quiz-result',       1),
    (8,  'Quiz Master',          'Complete ten quizzes',                               '/images/achievements/quiz-master.png',          'quiz-result',       10),
    (9,  'Three Day Streak',     'Log in on three consecutive days',                   '/images/achievements/three-day-streak.png',     'login-streak',      3),
    (10, 'Seven Day Streak',     'Log in on seven consecutive days',                   '/images/achievements/seven-day-streak.png',     'login-streak',      7),
    (11, 'Longest Login Streak', 'Your longest run of consecutive daily LanguageWise logins', '/images/achievements/longest-login-streak.png', 'login-streak',     -1);

SELECT setval(pg_get_serial_sequence('api.achievements', 'achievement_id'), 11);

INSERT INTO api.user_preferences (user_id, email) VALUES
    (1, 'amber@example.com'),
    (2, 'lachlan@example.com'),
    (3, 'roan@example.com'),
    (4, 'justin@example.com'),
    (5, 'kyan@example.com');

INSERT INTO api.user_achievements (user_id, achievement_id, progress) VALUES
    (1, 1, 1),
    (1, 2, 3),
    (1, 3, 3),
    (1, 4, 1),
    (1, 5, 6),
    (1, 6, 6),
    (2, 4, 1),
    (2, 5, 4),
    (2, 6, 4),
    (3, 7, 1),
    (3, 8, 7),
    (4, 9, 2),
    (4, 10, 2),
    (5, 9, 3),
    (5, 10, 5);

INSERT INTO api.notifications (user_id, trigger, time, email_subject, email_body) VALUES
    (1, 'course-completion', '2026-08-28T09:30:00Z', 'Lorem ipsum dolor sit amet', 'Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.'),
    (1, 'post-engagement',   '2026-08-29T14:15:00Z', 'Consectetur adipiscing elit', 'Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.'),
    (3, 'quiz-result',       '2026-08-27T11:45:00Z', 'Sed do eiusmod tempor', 'Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.'),
    (5, 'streak',            '2026-08-30T08:00:00Z', 'Ut labore et dolore magna', 'Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.');