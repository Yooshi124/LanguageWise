CREATE ROLE shared_app LOGIN PASSWORD 'languagewise';
CREATE ROLE mini_games_app LOGIN PASSWORD 'languagewise';
CREATE ROLE quizzes_courses_app LOGIN PASSWORD 'languagewise';
CREATE ROLE chat_discussion_app LOGIN PASSWORD 'languagewise';
CREATE ROLE quests_app LOGIN PASSWORD 'languagewise';

CREATE DATABASE shared OWNER shared_app;
CREATE DATABASE mini_games OWNER mini_games_app;
CREATE DATABASE quizzes_courses OWNER quizzes_courses_app;
CREATE DATABASE chat_discussion OWNER chat_discussion_app;
CREATE DATABASE quests_achievements_notifications OWNER quests_app;

REVOKE CONNECT ON DATABASE shared FROM PUBLIC;
REVOKE CONNECT ON DATABASE mini_games FROM PUBLIC;
REVOKE CONNECT ON DATABASE quizzes_courses FROM PUBLIC;
REVOKE CONNECT ON DATABASE chat_discussion FROM PUBLIC;
REVOKE CONNECT ON DATABASE quests_achievements_notifications FROM PUBLIC;