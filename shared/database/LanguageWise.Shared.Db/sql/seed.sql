-- Idempotent development accounts used by the shared authentication service.
LOCK TABLE Users IN SHARE ROW EXCLUSIVE MODE;

INSERT INTO Users (Id, Username, Password) VALUES
    (1, 'amber',   'test'),
    (2, 'lachlan', 'password'),
    (3, 'roan',    'password'),
    (4, 'justin',  'password'),
    (5, 'kyan',    'password')
ON CONFLICT DO NOTHING;

SELECT setval(pg_get_serial_sequence('users', 'id'),
    GREATEST((SELECT MAX(Id) FROM Users), (SELECT last_value FROM users_id_seq)), true);

UPDATE Users SET IsAdmin = TRUE WHERE Username = 'lachlan';

INSERT INTO AdminSettings (Name, IsEnabled) VALUES
    ('AIMode', TRUE),
    ('RAG',    TRUE),
    ('MCP',    TRUE)
ON CONFLICT DO NOTHING;
