\connect quests_achievements_notifications
\i /seed/quests/schema.sql
\i /seed/quests/seed.sql

GRANT web_anon TO quests_app;