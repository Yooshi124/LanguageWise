-- Seed data for the student 3 (Quizzes and Courses) microservice database.
-- Only executed when SampleItems is empty, so it is safe to re-run.
-- The project specification (section 2.2) requires a minimum of ten records.

INSERT INTO SampleItems (Name, Description, CreatedAt) VALUES
    ('Beginner Vocabulary',   'Fifty everyday words to start with.',           '2026-02-02T09:00:00Z'),
    ('Numbers and Dates',     'Counting, ordinals and the calendar.',          '2026-02-03T09:15:00Z'),
    ('Present Tense Drill',   'Conjugate regular verbs in the present tense.', '2026-02-04T09:30:00Z'),
    ('Past Tense Drill',      'Regular and irregular past forms.',             '2026-02-05T09:45:00Z'),
    ('Food and Drink',        'Order a meal and read a menu.',                 '2026-02-06T10:00:00Z'),
    ('Travel Essentials',     'Directions, transport and accommodation.',      '2026-02-07T10:15:00Z'),
    ('Sentence Ordering',     'Click the words in order to build a sentence.', '2026-02-08T10:30:00Z'),
    ('Listening Quiz',        'Answer questions about a short audio clip.',    '2026-02-09T10:45:00Z'),
    ('Reading Comprehension', 'Read a passage and answer five questions.',     '2026-02-10T11:00:00Z'),
    ('Final Assessment',      'A mixed quiz covering the whole course.',       '2026-02-11T11:15:00Z');
