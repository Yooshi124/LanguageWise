-- UserId values match the Users seeded by the shared database service:
--   1 = amber, 2 = lachlan, 3 = roan, 4 = justin, 5 = kyan
-- Ranks are per-language and reflect each user's position within that language.
INSERT INTO LanguageRanking (UserId, Language, Score, Rank, UpdatedAt) VALUES
    -- Spanish leaderboard
    (1, 'spanish',  820, 1, '2026-02-14T10:00:00Z'),
    (2, 'spanish',  750, 2, '2026-02-14T10:00:00Z'),
    (5, 'spanish',  580, 3, '2026-02-14T10:00:00Z'),
    (3, 'spanish',  540, 4, '2026-02-14T10:00:00Z'),
    -- Japanese leaderboard
    (3, 'japanese', 690, 1, '2026-02-14T10:00:00Z'),
    (2, 'japanese', 520, 2, '2026-02-14T10:00:00Z'),
    (4, 'japanese', 410, 3, '2026-02-14T10:00:00Z'),
    -- Italian leaderboard
    (4, 'italian',  610, 1, '2026-02-14T10:00:00Z'),
    (2, 'italian',  430, 2, '2026-02-14T10:00:00Z'),
    -- French leaderboard
    (4, 'french',   620, 1, '2026-02-14T10:00:00Z'),
    (1, 'french',   470, 2, '2026-02-14T10:00:00Z');

INSERT INTO DiscussionRanking (UserId, PostCount, CommentCount, LikeCount, Score, Rank, UpdatedAt) VALUES
    (2, 3, 2, 0, 110, 1, '2026-02-14T10:00:00Z'),
    (4, 1, 1, 0,  60, 2, '2026-02-14T10:00:00Z'),
    (1, 1, 1, 2,  55, 3, '2026-02-14T10:00:00Z'),
    (3, 1, 0, 1,  40, 4, '2026-02-14T10:00:00Z'),
    (5, 0, 1, 1,  30, 5, '2026-02-14T10:00:00Z');
