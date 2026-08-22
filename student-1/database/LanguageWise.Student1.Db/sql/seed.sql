-- Seed data for the student 1 (Mini Games) microservice database.
-- Only executed when SampleItems is empty, so it is safe to re-run.
-- The project specification (section 2.2) requires a minimum of ten records.

INSERT INTO SampleItems (Name, Description, CreatedAt) VALUES
    ('Crossword',        'Fill the grid from a themed vocabulary list.',         '2026-02-02T09:00:00Z'),
    ('Word Jumble',      'Unscramble the letters to reveal the word.',           '2026-02-03T09:15:00Z'),
    ('Picture Match',    'Match a word to the image that shows it.',             '2026-02-04T09:30:00Z'),
    ('Glass Bridge',     'Pick the correct word or fall through the panel.',     '2026-02-05T09:45:00Z'),
    ('Odd One Out',      'Spot the word that does not belong to the set.',       '2026-02-06T10:00:00Z'),
    ('Listening Drill',  'Hear a word and choose the matching spelling.',        '2026-02-07T10:15:00Z'),
    ('Speed Round',      'Answer as many prompts as possible in sixty seconds.', '2026-02-08T10:30:00Z'),
    ('Memory Pairs',     'Flip cards to pair a word with its translation.',      '2026-02-09T10:45:00Z'),
    ('Sentence Builder', 'Drag words into order to build a valid sentence.',     '2026-02-10T11:00:00Z'),
    ('Daily Challenge',  'A rotating activity that refreshes every morning.',    '2026-02-11T11:15:00Z');
