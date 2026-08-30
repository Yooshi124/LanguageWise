INSERT INTO Courses (Code, Title, Description) VALUES
    ('de', 'German',  'Build a practical foundation in German.'),
    ('fr', 'French',  'Learn useful everyday French.'),
    ('it', 'Italian', 'Start speaking and understanding Italian.'),
    ('nl', 'Dutch',   'Discover the essentials of Dutch.'),
    ('es', 'Spanish', 'Develop your everyday Spanish.'),
    ('pl', 'Polish',  'Build confidence with everyday Polish.');

INSERT INTO Lessons (CourseId, Slug, Title, SortOrder, ContentMarkdown)
SELECT Id, 'welcome', 'Welcome to ' || Title, 1,
       '# Welcome to ' || Title || char(10) || char(10) ||
       'This course introduces useful words and expressions at a comfortable pace.' || char(10) || char(10) ||
       '## How to learn' || char(10) || char(10) ||
       '- Read each example aloud.' || char(10) ||
       '- Open the vocabulary sheet for key words.' || char(10) ||
       '- Revisit lessons whenever you need.'
FROM Courses;

INSERT INTO Lessons (CourseId, Slug, Title, SortOrder, ContentMarkdown)
SELECT Id, 'greetings', 'Everyday Greetings', 2,
       '# Everyday Greetings' || char(10) || char(10) ||
       'Greetings help begin and end simple conversations.' || char(10) || char(10) ||
       '> Practise saying each greeting aloud, then use the vocabulary sheet to review its meaning.'
FROM Courses;

INSERT INTO LessonVocabulary (LessonId, VocabularyJson)
SELECT l.Id,
       CASE c.Code
           WHEN 'de' THEN '{"words":[{"word":"Hallo","meaning":"Hello"},{"word":"Danke","meaning":"Thank you"}]}'
           WHEN 'fr' THEN '{"words":[{"word":"Bonjour","meaning":"Hello"},{"word":"Merci","meaning":"Thank you"}]}'
           WHEN 'it' THEN '{"words":[{"word":"Ciao","meaning":"Hello"},{"word":"Grazie","meaning":"Thank you"}]}'
           WHEN 'nl' THEN '{"words":[{"word":"Hallo","meaning":"Hello"},{"word":"Bedankt","meaning":"Thank you"}]}'
           WHEN 'es' THEN '{"words":[{"word":"Hola","meaning":"Hello"},{"word":"Gracias","meaning":"Thank you"}]}'
           WHEN 'pl' THEN '{"words":[{"word":"Cześć","meaning":"Hello"},{"word":"Dziękuję","meaning":"Thank you"}]}'
       END
FROM Lessons l
INNER JOIN Courses c ON c.Id = l.CourseId
WHERE l.Slug = 'welcome';

INSERT INTO LessonVocabulary (LessonId, VocabularyJson)
SELECT l.Id,
       CASE c.Code
           WHEN 'de' THEN '{"words":[{"word":"Guten Morgen","meaning":"Good morning"},{"word":"Auf Wiedersehen","meaning":"Goodbye"}]}'
           WHEN 'fr' THEN '{"words":[{"word":"Bonsoir","meaning":"Good evening"},{"word":"Au revoir","meaning":"Goodbye"}]}'
           WHEN 'it' THEN '{"words":[{"word":"Buongiorno","meaning":"Good morning"},{"word":"Arrivederci","meaning":"Goodbye"}]}'
           WHEN 'nl' THEN '{"words":[{"word":"Goedemorgen","meaning":"Good morning"},{"word":"Tot ziens","meaning":"Goodbye"}]}'
           WHEN 'es' THEN '{"words":[{"word":"Buenos días","meaning":"Good morning"},{"word":"Adiós","meaning":"Goodbye"}]}'
           WHEN 'pl' THEN '{"words":[{"word":"Dzień dobry","meaning":"Good morning"},{"word":"Do widzenia","meaning":"Goodbye"}]}'
       END
FROM Lessons l
INNER JOIN Courses c ON c.Id = l.CourseId
WHERE l.Slug = 'greetings';

INSERT INTO Quizzes (CourseId, Title, IsAi)
SELECT Id, Title || ' Greetings Check', 0 FROM Courses;

INSERT INTO QuizQuestions (QuizId, Content, Type, QuestionData, CorrectAnswer)
SELECT q.Id, 'Choose the greeting that means hello.', 'multiple_choice',
       CASE c.Code
           WHEN 'de' THEN '{"options":["Hallo","Danke","Bitte"]}'
           WHEN 'fr' THEN '{"options":["Bonjour","Merci","Oui"]}'
           WHEN 'it' THEN '{"options":["Ciao","Grazie","Prego"]}'
           WHEN 'nl' THEN '{"options":["Hallo","Bedankt","Alsjeblieft"]}'
           WHEN 'es' THEN '{"options":["Hola","Gracias","Por favor"]}'
           WHEN 'pl' THEN '{"options":["Cześć","Dziękuję","Proszę"]}'
       END,
       CASE c.Code
           WHEN 'de' THEN 'Hallo' WHEN 'fr' THEN 'Bonjour' WHEN 'it' THEN 'Ciao'
           WHEN 'nl' THEN 'Hallo' WHEN 'es' THEN 'Hola' WHEN 'pl' THEN 'Cześć'
       END
FROM Quizzes q
INNER JOIN Courses c ON c.Id = q.CourseId;

INSERT INTO Flashcards (CourseId, FrontText, BackText, IsAi)
SELECT Id,
       CASE Code WHEN 'de' THEN 'Hallo' WHEN 'fr' THEN 'Bonjour' WHEN 'it' THEN 'Ciao'
                 WHEN 'nl' THEN 'Hallo' WHEN 'es' THEN 'Hola' WHEN 'pl' THEN 'Cześć' END,
       'Hello', 0
FROM Courses;
