-- Idempotent development accounts used by the shared authentication service.
INSERT OR IGNORE INTO Users (Username, Password) VALUES
    ('amber',   'test'),
    ('lachlan', 'password'),
    ('roan',    'password'),
    ('justin',  'password'),
    ('kyan',    'password');
