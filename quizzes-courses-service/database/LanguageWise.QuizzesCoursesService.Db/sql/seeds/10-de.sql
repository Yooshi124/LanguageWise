-- German lessons, vocabulary, and quizzes. Requires schema.sql and seeds/00-courses.sql.
WITH LessonSeeds (Slug, Title, SortOrder) AS (
    VALUES
    ('greetings', 'Greetings', 1),
    ('introductions', 'Introductions', 2),
    ('politeness', 'Politeness', 3),
    ('numbers', 'Numbers', 4),
    ('family', 'Family', 5),
    ('food', 'Food', 6),
    ('drinks', 'Drinks', 7),
    ('home', 'Home', 8),
    ('travel', 'Travel', 9),
    ('directions', 'Directions', 10),
    ('time-calendar', 'Time and Calendar', 11),
    ('weather', 'Weather', 12),
    ('shopping', 'Shopping', 13),
    ('work-school', 'Work and School', 14),
    ('body-health', 'Body and Health', 15),
    ('emotions', 'Emotions', 16),
    ('hobbies', 'Hobbies', 17),
    ('nature-animals', 'Nature and Animals', 18),
    ('long-words', 'Long Words', 19),
    ('funny-unusual-words', 'Funny and Unusual Words', 20)
),
LessonContentSeeds (CourseCode, LessonSlug, ContentMarkdown) AS (
    VALUES
    ('de', 'greetings', '## Learn in context

| Target language | English |
| --- | --- |
| Hallo, Anna! Guten Tag. | Hello, Anna! Good day. |
| Willkommen in Berlin. | Welcome to Berlin. |

## Mini dialogue

> **A:** Hallo, Anna! Guten Tag. Willkommen in Berlin!
> *Hello, Anna! Good day. Welcome to Berlin!*
>
> **B:** Hallo! Danke schön. Bist du neu hier?
> *Hello! Thank you. Are you new here?*
>
> **A:** Ja!
> *Yes!*

## Language note

Use **Hallo** with friends and **Guten Tag** in more formal daytime situations. German nouns, including *Tag*, are capitalised.'),
    ('de', 'introductions', '## Learn in context

| Target language | English |
| --- | --- |
| Ich heiße Lara. Ich komme aus Kanada. | My name is Lara. I come from Canada. |
| Wie heißt du? — Ich heiße Ben. | What is your name? — My name is Ben. |

## Mini dialogue

> **A:** Ich heiße Lara. Ich komme aus Kanada. Wie heißt du?
> *My name is Lara. I come from Canada. What is your name?*
>
> **B:** Ich heiße Ben. Freut mich!
> *My name is Ben. Nice to meet you!*

## Language note

With **du**, ask *Wie heißt du?*; with a stranger, use formal **Sie**: *Wie heißen Sie?*.'),
    ('de', 'politeness', '## Learn in context

| Target language | English |
| --- | --- |
| Einen Kaffee, bitte. Danke! | A coffee, please. Thank you! |
| Entschuldigung, können Sie helfen? | Excuse me, can you help? |

## Mini dialogue

> **A:** Entschuldigung, können Sie mir helfen?
> *Excuse me, can you help me?*
>
> **B:** Ja, gern. Bitte schön.
> *Yes, gladly. Here you are.*
>
> **A:** Danke! Gern geschehen?
> *Thank you! You are welcome?*

## Language note

**Bitte** can mean “please,” “here you are,” or “you are welcome,” depending on the situation.'),
    ('de', 'numbers', '## Learn in context

| Target language | English |
| --- | --- |
| Ich habe zwei Tickets und zehn Euro. | I have two tickets and ten euros. |
| Der Bus fährt um drei Uhr. | The bus leaves at three o’clock. |

## Worked usage

- **Ich habe zwei Tickets und zehn Euro.** — *I have two tickets and ten euros.*
- **Der Bus fährt um drei Uhr.** — *The bus leaves at three o’clock.*

## Language note

German numbers from 21 put the ones first: *einundzwanzig* is literally “one-and-twenty.”'),
    ('de', 'family', '## Learn in context

| Target language | English |
| --- | --- |
| Das ist meine Mutter und mein Vater. | This is my mother and my father. |
| Meine Schwester hat einen Bruder. | My sister has a brother. |

## Mini dialogue

> **A:** Ist das deine Familie?
> *Is that your family?*
>
> **B:** Ja. Das ist meine Mutter und mein Vater.
> *Yes. This is my mother and my father.*
>
> **A:** Hast du einen Bruder?
> *Do you have a brother?*
>
> **B:** Nein, aber ich habe eine Schwester.
> *No, but I have a sister.*

## Language note

Possessives change with gender: **meine** Mutter but **mein** Vater.'),
    ('de', 'food', '## Learn in context

| Target language | English |
| --- | --- |
| Zum Frühstück esse ich Brot und Käse. | For breakfast I eat bread and cheese. |
| Der Apfel ist lecker. | The apple is tasty. |

## Mini dialogue

> **A:** Was isst du zum Frühstück?
> *What do you eat for breakfast?*
>
> **B:** Ich esse Brot und Käse.
> *I eat bread and cheese.*
>
> **A:** Und der Apfel?
> *And the apple?*
>
> **B:** Der Apfel ist lecker!
> *The apple is tasty!*

## Language note

**Frühstück** literally means “early piece”: *früh* + *Stück*.'),
    ('de', 'drinks', '## Learn in context

| Target language | English |
| --- | --- |
| Ich möchte ein Glas Wasser. | I would like a glass of water. |
| Der Kaffee ist heiß, der Tee ist warm. | The coffee is hot; the tea is warm. |

## Mini dialogue

> **A:** Möchtest du Kaffee oder Tee?
> *Would you like coffee or tea?*
>
> **B:** Für mich ein Glas Wasser, bitte. Der Kaffee ist zu heiß.
> *For me a glass of water, please. The coffee is too hot.*

## Language note

**Ich möchte ...** is a polite way to order, softer than a direct “I want.”'),
    ('de', 'home', '## Learn in context

| Target language | English |
| --- | --- |
| Die Küche ist in der Wohnung. | The kitchen is in the apartment. |
| Wo ist der Schlüssel? — Auf dem Tisch. | Where is the key? — On the table. |

## Mini dialogue

> **A:** Wo ist der Schlüssel?
> *Where is the key?*
>
> **B:** Der Schlüssel ist in der Küche.
> *The key is in the kitchen.*
>
> **A:** Und dein Zimmer?
> *And your room?*
>
> **B:** Mein Zimmer ist in der Wohnung.
> *My room is in the apartment.*

## Language note

Learn each German noun with its article: **die** Küche, but **der** Schlüssel.'),
    ('de', 'travel', '## Learn in context

| Target language | English |
| --- | --- |
| Der Zug fährt vom Bahnhof ab. | The train departs from the station. |
| Meine Fahrkarte ist im Koffer. | My ticket is in the suitcase. |

## Mini dialogue

> **A:** Wann fährt der Zug ab?
> *When does the train depart?*
>
> **B:** Um drei Uhr, am Bahnhof.
> *At three o’clock, at the station.*
>
> **A:** Wo ist meine Fahrkarte?
> *Where is my ticket?*
>
> **B:** Deine Fahrkarte ist im Koffer.
> *Your ticket is in the suitcase.*

## Language note

In *abfahren*, **ab** moves to the end: *Der Zug fährt ... ab*.'),
    ('de', 'directions', '## Learn in context

| Target language | English |
| --- | --- |
| Gehen Sie geradeaus und dann links. | Go straight ahead and then left. |
| Wo ist die Straße? — Rechts vom Hotel. | Where is the street? — To the right of the hotel. |

## Mini dialogue

> **A:** Entschuldigung, wo ist die Straße zum Hotel?
> *Excuse me, where is the street to the hotel?*
>
> **B:** Gehen Sie geradeaus und dann nach links.
> *Go straight ahead and then to the left.*
>
> **A:** Und das Hotel?
> *And the hotel?*
>
> **B:** Es ist rechts.
> *It is on the right.*

## Language note

Formal directions use **Sie**; the verb still comes second: *Gehen Sie geradeaus*.'),
    ('de', 'time-calendar', '## Learn in context

| Target language | English |
| --- | --- |
| Heute ist Montag; morgen habe ich frei. | Today is Monday; tomorrow I am free. |
| Um acht Uhr beginnt die Schule. | School starts at eight o’clock. |

## Mini dialogue

> **A:** Welcher Tag ist heute?
> *What day is today?*
>
> **B:** Heute ist Montag.
> *Today is Monday.*
>
> **A:** Wann beginnt die Schule?
> *When does school begin?*
>
> **B:** Morgen um acht Uhr.
> *Tomorrow at eight o’clock.*

## Language note

Use **um** for clock time. **Morgen** can mean “tomorrow” or “morning” from context.'),
    ('de', 'weather', '## Learn in context

| Target language | English |
| --- | --- |
| Heute ist es sonnig, aber kalt. | Today it is sunny but cold. |
| Morgen wird es regnerisch und windig. | Tomorrow it will be rainy and windy. |

## Mini dialogue

> **A:** Wie ist das Wetter heute?
> *How is the weather today?*
>
> **B:** Es ist sonnig, aber kalt.
> *It is sunny but cold.*
>
> **A:** Und morgen?
> *And tomorrow?*
>
> **B:** Morgen wird es regnerisch und sehr windig.
> *Tomorrow it will be rainy and very windy.*

## Language note

Weather uses impersonal **es**: *Es ist kalt*. **wird** describes a change.'),
    ('de', 'shopping', '## Learn in context

| Target language | English |
| --- | --- |
| Wie viel kostet das? — Der Preis ist zehn Euro. | How much does that cost? — The price is ten euros. |
| Haben Sie diese Größe? | Do you have this size? |

## Mini dialogue

> **A:** Ich möchte diese Jacke kaufen. Wie viel kostet sie?
> *I would like to buy this jacket. How much does it cost?*
>
> **B:** Der Preis ist zwanzig Euro.
> *The price is twenty euros.*
>
> **A:** Das ist billig! Haben Sie meine Größe?
> *That is cheap! Do you have my size?*
>
> **B:** Ja, natürlich.
> *Yes, of course.*

## Language note

**billig** means cheap; **günstig** often sounds more positive: good value.'),
    ('de', 'work-school', '## Learn in context

| Target language | English |
| --- | --- |
| Ich lerne Deutsch in der Schule. | I learn German at school. |
| Mein Lehrer arbeitet im Büro. | My teacher works in the office. |

## Mini dialogue

> **A:** Was machst du in der Schule?
> *What do you do at school?*
>
> **B:** Ich lerne Deutsch. Mein Lehrer ist sehr gut.
> *I learn German. My teacher is very good.*
>
> **A:** Und wo arbeitest du?
> *And where do you work?*
>
> **B:** Ich arbeite im Büro.
> *I work in the office.*

## Language note

**Arbeit** means work as an activity and can also mean a job or piece of work.'),
    ('de', 'body-health', '## Learn in context

| Target language | English |
| --- | --- |
| Mein Kopf tut weh. Ich bin krank. | My head hurts. I am ill. |
| Der Arzt untersucht meine Hand. | The doctor examines my hand. |

## Mini dialogue

> **A:** Wie geht es dir?
> *How are you?*
>
> **B:** Nicht gut. Mein Kopf tut weh. Ich bin krank.
> *Not good. My head hurts. I am ill.*
>
> **A:** Geh zum Arzt!
> *Go to the doctor!*
>
> **B:** Ja, der Arzt untersucht auch meine Hand.
> *Yes, the doctor is also examining my hand.*

## Language note

Pain uses **tun weh**: *Der Kopf tut weh*—literally, “the head does pain.”'),
    ('de', 'emotions', '## Learn in context

| Target language | English |
| --- | --- |
| Ich bin glücklich, aber müde. | I am happy but tired. |
| Sie hat Angst vor dem Hund. | She is afraid of the dog. |

## Mini dialogue

> **A:** Wie fühlst du dich heute?
> *How do you feel today?*
>
> **B:** Ich bin glücklich, aber ein bisschen müde.
> *I am happy, but a little tired.*
>
> **A:** Und deine Schwester?
> *And your sister?*
>
> **B:** Sie hat Angst vor dem großen Hund.
> *She is afraid of the big dog.*

## Language note

Say **Angst haben vor** + a noun: *Angst vor dem Hund haben*.'),
    ('de', 'hobbies', '## Learn in context

| Target language | English |
| --- | --- |
| Ich lese gern und höre Musik. | I like reading and listening to music. |
| Am Samstag treibe ich Sport. | On Saturday I do sport. |

## Mini dialogue

> **A:** Was machst du gern am Wochenende?
> *What do you like doing on the weekend?*
>
> **B:** Ich lese gern und höre Musik. Und du?
> *I like reading and listening to music. And you?*
>
> **A:** Ich treibe gern Sport und tanze.
> *I like doing sport and dancing.*

## Language note

Learn the infinitives **lesen**, **Musik hören**, and **kochen**; put **gern** after a conjugated verb: *Ich koche gern*.'),
    ('de', 'nature-animals', '## Learn in context

| Target language | English |
| --- | --- |
| Der Hund läuft im Wald. | The dog runs in the forest. |
| Ein Vogel sitzt auf dem Baum. | A bird sits in the tree. |

## Worked usage

- **Der Hund läuft im Wald.** — *The dog runs in the forest.*
- **Ein Vogel sitzt auf dem Baum.** — *A bird sits in the tree.*

## Language note

**im** = *in dem*. Learn *im Wald* as one useful location chunk.'),
    ('de', 'long-words', '## Learn in context

| Target language | English |
| --- | --- |
| Das Rindfleischetikettierungsüberwachungsaufgabenübertragungsgesetz ist ein Gesetz. | The beef-labeling-supervision-duty-transfer law is a law. |
| Eine Kraftfahrzeughaftpflichtversicherung schützt Autofahrer. | Motor-vehicle liability insurance protects drivers. |

## Worked usage

- **Das Rindfleischetikettierungsüberwachungsaufgabenübertragungsgesetz ist ein Gesetz.** — *The beef-labeling-supervision-duty-transfer law is a law.*
- **Eine Kraftfahrzeughaftpflichtversicherung schützt Autofahrer.** — *Motor-vehicle liability insurance protects drivers.*

## Language note

German compounds join meaningful parts: **Rindfleisch + Etikettierung + Überwachung + Aufgaben + Übertragung + Gesetz**. The final noun, *Gesetz*, determines the gender.'),
    ('de', 'funny-unusual-words', '## Learn in context

| Target language | English |
| --- | --- |
| Ich habe einen Ohrwurm von diesem Lied. | I have a catchy tune from this song stuck in my head. |
| Nach dem Essen gehen wir spazieren. | After eating we go for a walk. |

## Worked usage

- **Ich habe einen Ohrwurm von diesem Lied.** — *I have a catchy tune from this song stuck in my head.*
- **Nach dem Essen gehen wir spazieren.** — *After eating we go for a walk.*

## Language note

**Ohrwurm** literally means “earworm,” a tune you cannot stop hearing. **Wanderlust** is a desire to travel, and **Kopfkino** is a vivid imagined scene. Playful vocabulary should describe situations, not people.')
)
INSERT OR IGNORE INTO Lessons (CourseId, Slug, Title, SortOrder, ContentMarkdown)
SELECT c.Id, s.Slug, s.Title, s.SortOrder, content.ContentMarkdown
FROM Courses c
CROSS JOIN LessonSeeds s
INNER JOIN LessonContentSeeds content
    ON content.CourseCode = c.Code AND content.LessonSlug = s.Slug
WHERE c.Code = 'de';

-- Seed individual words, then let SQLite construct valid vocabulary JSON documents.
WITH WordSeeds (CourseCode, LessonSlug, Position, Word, Meaning) AS (
    VALUES
    ('de', 'greetings', 1, 'Hallo', 'Hello'),
    ('de', 'greetings', 2, 'Guten Tag', 'Good day'),
    ('de', 'greetings', 3, 'Willkommen', 'Welcome'),
    ('de', 'greetings', 4, 'Ja', 'Yes'),
    ('de', 'greetings', 5, 'Nein', 'No'),
    ('de', 'introductions', 1, 'Ich heiße ...', 'My name is ...'),
    ('de', 'introductions', 2, 'Wie heißt du?', 'What is your name?'),
    ('de', 'introductions', 3, 'Ich komme aus ...', 'I come from ...'),
    ('de', 'introductions', 4, 'Freut mich', 'Nice to meet you'),
    ('de', 'introductions', 5, 'Das ist ...', 'This is ...'),
    ('de', 'politeness', 1, 'Bitte', 'Please'),
    ('de', 'politeness', 2, 'Danke', 'Thank you'),
    ('de', 'politeness', 3, 'Entschuldigung', 'Sorry / excuse me'),
    ('de', 'politeness', 4, 'Gern geschehen', 'You are welcome'),
    ('de', 'politeness', 5, 'Können Sie helfen?', 'Can you help?'),
    ('de', 'numbers', 1, 'eins', 'one'),
    ('de', 'numbers', 2, 'zwei', 'two'),
    ('de', 'numbers', 3, 'drei', 'three'),
    ('de', 'numbers', 4, 'zehn', 'ten'),
    ('de', 'numbers', 5, 'hundert', 'one hundred'),
    ('de', 'family', 1, 'die Familie', 'family'),
    ('de', 'family', 2, 'die Mutter', 'mother'),
    ('de', 'family', 3, 'der Vater', 'father'),
    ('de', 'family', 4, 'der Bruder', 'brother'),
    ('de', 'family', 5, 'die Schwester', 'sister'),
    ('de', 'food', 1, 'das Brot', 'bread'),
    ('de', 'food', 2, 'der Käse', 'cheese'),
    ('de', 'food', 3, 'der Apfel', 'apple'),
    ('de', 'food', 4, 'das Frühstück', 'breakfast'),
    ('de', 'food', 5, 'lecker', 'tasty'),
    ('de', 'drinks', 1, 'das Wasser', 'water'),
    ('de', 'drinks', 2, 'der Kaffee', 'coffee'),
    ('de', 'drinks', 3, 'der Tee', 'tea'),
    ('de', 'drinks', 4, 'das Bier', 'beer'),
    ('de', 'drinks', 5, 'ein Glas', 'a glass'),
    ('de', 'home', 1, 'das Haus', 'house'),
    ('de', 'home', 2, 'die Wohnung', 'apartment'),
    ('de', 'home', 3, 'das Zimmer', 'room'),
    ('de', 'home', 4, 'die Küche', 'kitchen'),
    ('de', 'home', 5, 'der Schlüssel', 'key'),
    ('de', 'travel', 1, 'der Bahnhof', 'train station'),
    ('de', 'travel', 2, 'der Flughafen', 'airport'),
    ('de', 'travel', 3, 'die Fahrkarte', 'ticket'),
    ('de', 'travel', 4, 'der Koffer', 'suitcase'),
    ('de', 'travel', 5, 'abfahren', 'to depart'),
    ('de', 'directions', 1, 'links', 'left'),
    ('de', 'directions', 2, 'rechts', 'right'),
    ('de', 'directions', 3, 'geradeaus', 'straight ahead'),
    ('de', 'directions', 4, 'die Straße', 'street'),
    ('de', 'directions', 5, 'Wo ist ...?', 'Where is ...?'),
    ('de', 'time-calendar', 1, 'heute', 'today'),
    ('de', 'time-calendar', 2, 'morgen', 'tomorrow'),
    ('de', 'time-calendar', 3, 'gestern', 'yesterday'),
    ('de', 'time-calendar', 4, 'die Uhr', 'clock'),
    ('de', 'time-calendar', 5, 'der Montag', 'Monday'),
    ('de', 'weather', 1, 'sonnig', 'sunny'),
    ('de', 'weather', 2, 'regnerisch', 'rainy'),
    ('de', 'weather', 3, 'der Wind', 'wind'),
    ('de', 'weather', 4, 'kalt', 'cold'),
    ('de', 'weather', 5, 'warm', 'warm'),
    ('de', 'shopping', 1, 'kaufen', 'to buy'),
    ('de', 'shopping', 2, 'der Preis', 'price'),
    ('de', 'shopping', 3, 'teuer', 'expensive'),
    ('de', 'shopping', 4, 'billig', 'cheap'),
    ('de', 'shopping', 5, 'die Größe', 'size'),
    ('de', 'work-school', 1, 'die Arbeit', 'work'),
    ('de', 'work-school', 2, 'die Schule', 'school'),
    ('de', 'work-school', 3, 'der Lehrer', 'teacher'),
    ('de', 'work-school', 4, 'lernen', 'to learn'),
    ('de', 'work-school', 5, 'das Büro', 'office'),
    ('de', 'body-health', 1, 'der Kopf', 'head'),
    ('de', 'body-health', 2, 'die Hand', 'hand'),
    ('de', 'body-health', 3, 'der Arzt', 'doctor'),
    ('de', 'body-health', 4, 'krank', 'ill'),
    ('de', 'body-health', 5, 'Es tut weh', 'It hurts'),
    ('de', 'emotions', 1, 'glücklich', 'happy'),
    ('de', 'emotions', 2, 'traurig', 'sad'),
    ('de', 'emotions', 3, 'müde', 'tired'),
    ('de', 'emotions', 4, 'aufgeregt', 'excited'),
    ('de', 'emotions', 5, 'Angst haben', 'to be afraid'),
    ('de', 'hobbies', 1, 'lesen', 'to read'),
    ('de', 'hobbies', 2, 'Musik hören', 'to listen to music'),
    ('de', 'hobbies', 3, 'kochen', 'to cook'),
    ('de', 'hobbies', 4, 'Sport treiben', 'to do sport'),
    ('de', 'hobbies', 5, 'tanzen', 'to dance'),
    ('de', 'nature-animals', 1, 'der Hund', 'dog'),
    ('de', 'nature-animals', 2, 'die Katze', 'cat'),
    ('de', 'nature-animals', 3, 'der Baum', 'tree'),
    ('de', 'nature-animals', 4, 'der Wald', 'forest'),
    ('de', 'nature-animals', 5, 'der Vogel', 'bird'),
    ('de', 'long-words', 1, 'Rindfleischetikettierungsüberwachungsaufgabenübertragungsgesetz', 'law delegating duties for supervising beef labeling'),
    ('de', 'long-words', 2, 'Donaudampfschifffahrtsgesellschaft', 'Danube steamship company'),
    ('de', 'long-words', 3, 'Kraftfahrzeughaftpflichtversicherung', 'motor-vehicle liability insurance'),
    ('de', 'long-words', 4, 'Arbeitsunfähigkeitsbescheinigung', 'certificate of incapacity for work'),
    ('de', 'long-words', 5, 'Geschwindigkeitsbegrenzung', 'speed limit'),
    ('de', 'funny-unusual-words', 1, 'Ohrwurm', 'a catchy tune stuck in your head'),
    ('de', 'funny-unusual-words', 2, 'Wanderlust', 'desire to travel'),
    ('de', 'funny-unusual-words', 3, 'Fernweh', 'longing for faraway places'),
    ('de', 'funny-unusual-words', 4, 'Fingerspitzengefühl', 'tact and intuitive sensitivity'),
    ('de', 'funny-unusual-words', 5, 'Kopfkino', 'a vivid imagined scene in your head')
)
INSERT OR IGNORE INTO LessonVocabulary (LessonId, VocabularyJson)
SELECT l.Id, json_object('words', json((
    SELECT json_group_array(json_object('word', ordered.Word, 'meaning', ordered.Meaning))
    FROM (SELECT Word, Meaning FROM WordSeeds WHERE CourseCode = c.Code AND LessonSlug = l.Slug ORDER BY Position) ordered
)))
FROM Lessons l
INNER JOIN Courses c ON c.Id = l.CourseId AND c.Code = 'de';

INSERT OR IGNORE INTO Quizzes (LessonId, Title)
SELECT l.Id, l.Title || ' Quiz'
FROM Lessons l
INNER JOIN Courses c ON c.Id = l.CourseId
WHERE c.Code = 'de';

WITH QuestionSeeds (LessonSlug, SortOrder, Content, Type, QuestionData, CorrectAnswer) AS (
    VALUES
    ('greetings', 1, 'Which word means “Hello”?', 'multiple_choice', '{"options":["Hallo","Danke","Nein"]}', 'Hallo'),
    ('greetings', 2, 'Which greeting is suitable during the day in a formal situation?', 'multiple_choice', '{"options":["Guten Tag","Hallo","Nein"]}', 'Guten Tag'),
    ('greetings', 3, 'Which word means “Welcome”?', 'multiple_choice', '{"options":["Willkommen","Bitte","Ja"]}', 'Willkommen'),
    ('greetings', 4, 'Which word means “Yes”?', 'multiple_choice', '{"options":["Nein","Ja","Hallo"]}', 'Ja'),
    ('greetings', 5, 'Put the formal daytime greeting in order.', 'word_ordering', '{"tokens":["Tag","Guten"]}', '["Guten","Tag"]'),
    ('greetings', 6, 'Put “Welcome to Berlin” in order.', 'word_ordering', '{"tokens":["Berlin","Willkommen","in"]}', '["Willkommen","in","Berlin"]'),
    ('greetings', 7, 'Put “Hello Anna” in order.', 'word_ordering', '{"tokens":["Anna","Hallo"]}', '["Hallo","Anna"]'),
    ('greetings', 8, 'Type the German word for “No”.', 'free_text', '{}', 'Nein'),
    ('greetings', 9, 'Complete the reply: “Thank you very much.”', 'free_text', '{}', 'Danke schön'),
    ('greetings', 10, 'Type the question “Are you new here?”', 'free_text', '{}', 'Bist du neu hier?'),

    ('introductions', 1, 'Which phrase means “My name is …”?', 'multiple_choice', '{"options":["Ich heiße ...","Wie heißt du?","Freut mich"]}', 'Ich heiße ...'),
    ('introductions', 2, 'How do you ask a friend for their name?', 'multiple_choice', '{"options":["Wie heißt du?","Ich komme aus ...","Das ist ..."]}', 'Wie heißt du?'),
    ('introductions', 3, 'Which phrase means “Nice to meet you”?', 'multiple_choice', '{"options":["Freut mich","Guten Tag","Nein"]}', 'Freut mich'),
    ('introductions', 4, 'Which phrase begins “I come from …”?', 'multiple_choice', '{"options":["Das ist ...","Ich komme aus ...","Ich heiße ..."]}', 'Ich komme aus ...'),
    ('introductions', 5, 'Put “My name is Lara” in order.', 'word_ordering', '{"tokens":["Lara","heiße","Ich"]}', '["Ich","heiße","Lara"]'),
    ('introductions', 6, 'Put “I come from Canada” in order.', 'word_ordering', '{"tokens":["Kanada","komme","aus","Ich"]}', '["Ich","komme","aus","Kanada"]'),
    ('introductions', 7, 'Put “What is your name?” in order.', 'word_ordering', '{"tokens":["du","Wie","heißt"]}', '["Wie","heißt","du"]'),
    ('introductions', 8, 'Type the German phrase for “This is …”.', 'free_text', '{}', 'Das ist ...'),
    ('introductions', 9, 'Type the formal question “What is your name?”', 'free_text', '{}', 'Wie heißen Sie?'),
    ('introductions', 10, 'Complete the introduction: “My name is Ben.”', 'free_text', '{}', 'Ich heiße Ben.'),

    ('politeness', 1, 'Which word means “Please”?', 'multiple_choice', '{"options":["Bitte","Danke","Entschuldigung"]}', 'Bitte'),
    ('politeness', 2, 'Which word means “Thank you”?', 'multiple_choice', '{"options":["Danke","Bitte","Nein"]}', 'Danke'),
    ('politeness', 3, 'Which word can mean “Sorry” or “Excuse me”?', 'multiple_choice', '{"options":["Entschuldigung","Willkommen","Hallo"]}', 'Entschuldigung'),
    ('politeness', 4, 'Which phrase means “You are welcome”?', 'multiple_choice', '{"options":["Gern geschehen","Können Sie helfen?","Danke"]}', 'Gern geschehen'),
    ('politeness', 5, 'Put “Can you help?” in polite order.', 'word_ordering', '{"tokens":["helfen","Sie","Können"]}', '["Können","Sie","helfen"]'),
    ('politeness', 6, 'Put “A coffee, please” in order.', 'word_ordering', '{"tokens":["bitte","Kaffee","Einen"]}', '["Einen","Kaffee","bitte"]'),
    ('politeness', 7, 'Put “Excuse me, can you help me?” in order.', 'word_ordering', '{"tokens":["mir","Entschuldigung","helfen","Sie","können"]}', '["Entschuldigung","können","Sie","mir","helfen"]'),
    ('politeness', 8, 'Type the phrase “You are welcome”.', 'free_text', '{}', 'Gern geschehen'),
    ('politeness', 9, 'Type the polite question “Can you help?”', 'free_text', '{}', 'Können Sie helfen?'),
    ('politeness', 10, 'Complete the reply: “Yes, gladly.”', 'free_text', '{}', 'Ja, gern.'),

    ('numbers', 1, 'Which German number means “one”?', 'multiple_choice', '{"options":["eins","zwei","drei"]}', 'eins'),
    ('numbers', 2, 'Which German number means “two”?', 'multiple_choice', '{"options":["drei","zwei","zehn"]}', 'zwei'),
    ('numbers', 3, 'Which German number means “ten”?', 'multiple_choice', '{"options":["hundert","eins","zehn"]}', 'zehn'),
    ('numbers', 4, 'Which German number means “one hundred”?', 'multiple_choice', '{"options":["zwei","hundert","drei"]}', 'hundert'),
    ('numbers', 5, 'Put “I have two tickets and ten euros” in order.', 'word_ordering', '{"tokens":["Euro","zwei","Ich","zehn","Tickets","habe","und"]}', '["Ich","habe","zwei","Tickets","und","zehn","Euro"]'),
    ('numbers', 6, 'Put “The bus leaves at three o’clock” in order.', 'word_ordering', '{"tokens":["Uhr","Bus","um","fährt","Der","drei"]}', '["Der","Bus","fährt","um","drei","Uhr"]'),
    ('numbers', 7, 'Put “two tickets and ten euros” in order.', 'word_ordering', '{"tokens":["zehn","Tickets","Euro","und","zwei"]}', '["zwei","Tickets","und","zehn","Euro"]'),
    ('numbers', 8, 'Type the German number for “three”.', 'free_text', '{}', 'drei'),
    ('numbers', 9, 'Type the German number for twenty-one.', 'free_text', '{}', 'einundzwanzig'),
    ('numbers', 10, 'Type the sentence “The bus leaves at three o’clock.”', 'free_text', '{}', 'Der Bus fährt um drei Uhr.'),

    ('family', 1, 'Which German word means “family”?', 'multiple_choice', '{"options":["die Familie","die Mutter","der Vater"]}', 'die Familie'),
    ('family', 2, 'Which German word means “mother”?', 'multiple_choice', '{"options":["die Schwester","die Mutter","der Bruder"]}', 'die Mutter'),
    ('family', 3, 'Which German word means “father”?', 'multiple_choice', '{"options":["der Bruder","der Vater","die Familie"]}', 'der Vater'),
    ('family', 4, 'Which German word means “brother”?', 'multiple_choice', '{"options":["die Mutter","die Schwester","der Bruder"]}', 'der Bruder'),
    ('family', 5, 'Put “This is my mother and my father” in order.', 'word_ordering', '{"tokens":["Vater","meine","ist","und","Das","Mutter","mein"]}', '["Das","ist","meine","Mutter","und","mein","Vater"]'),
    ('family', 6, 'Put “My sister has a brother” in order.', 'word_ordering', '{"tokens":["einen","Meine","Bruder","Schwester","hat"]}', '["Meine","Schwester","hat","einen","Bruder"]'),
    ('family', 7, 'Put “No, but I have a sister” in order.', 'word_ordering', '{"tokens":["Schwester","aber","Nein","eine","ich","habe"]}', '["Nein","aber","ich","habe","eine","Schwester"]'),
    ('family', 8, 'Type the German phrase for “the family”.', 'free_text', '{}', 'die Familie'),
    ('family', 9, 'Type the question “Do you have a brother?”', 'free_text', '{}', 'Hast du einen Bruder?'),
    ('family', 10, 'Type the sentence “This is my mother and my father.”', 'free_text', '{}', 'Das ist meine Mutter und mein Vater.'),

    ('food', 1, 'Which German word means “bread”?', 'multiple_choice', '{"options":["das Brot","der Käse","der Apfel"]}', 'das Brot'),
    ('food', 2, 'Which German word means “cheese”?', 'multiple_choice', '{"options":["das Frühstück","der Käse","das Brot"]}', 'der Käse'),
    ('food', 3, 'Which German word means “apple”?', 'multiple_choice', '{"options":["lecker","das Brot","der Apfel"]}', 'der Apfel'),
    ('food', 4, 'Which German word means “breakfast”?', 'multiple_choice', '{"options":["der Käse","das Frühstück","lecker"]}', 'das Frühstück'),
    ('food', 5, 'Put “For breakfast I eat bread and cheese” in order.', 'word_ordering', '{"tokens":["Brot","Zum","Käse","esse","Frühstück","und","ich"]}', '["Zum","Frühstück","esse","ich","Brot","und","Käse"]'),
    ('food', 6, 'Put “The apple is tasty” in order.', 'word_ordering', '{"tokens":["lecker","Apfel","ist","Der"]}', '["Der","Apfel","ist","lecker"]'),
    ('food', 7, 'Put “I eat bread and cheese” in order.', 'word_ordering', '{"tokens":["Käse","esse","Brot","Ich","und"]}', '["Ich","esse","Brot","und","Käse"]'),
    ('food', 8, 'Type the German phrase for “breakfast”.', 'free_text', '{}', 'das Frühstück'),
    ('food', 9, 'Type the German word for “tasty”.', 'free_text', '{}', 'lecker'),
    ('food', 10, 'Type the question “What do you eat for breakfast?”', 'free_text', '{}', 'Was isst du zum Frühstück?'),

    ('drinks', 1, 'Which German word means “water”?', 'multiple_choice', '{"options":["das Wasser","der Kaffee","der Tee"]}', 'das Wasser'),
    ('drinks', 2, 'Which German word means “coffee”?', 'multiple_choice', '{"options":["das Bier","der Kaffee","das Wasser"]}', 'der Kaffee'),
    ('drinks', 3, 'Which German word means “tea”?', 'multiple_choice', '{"options":["der Kaffee","ein Glas","der Tee"]}', 'der Tee'),
    ('drinks', 4, 'Which German word means “beer”?', 'multiple_choice', '{"options":["das Wasser","das Bier","der Tee"]}', 'das Bier'),
    ('drinks', 5, 'Put “I would like a glass of water” in order.', 'word_ordering', '{"tokens":["Wasser","möchte","Glas","Ich","ein"]}', '["Ich","möchte","ein","Glas","Wasser"]'),
    ('drinks', 6, 'Put “The coffee is hot” in order.', 'word_ordering', '{"tokens":["heiß","Kaffee","ist","Der"]}', '["Der","Kaffee","ist","heiß"]'),
    ('drinks', 7, 'Put “Would you like coffee or tea?” in order.', 'word_ordering', '{"tokens":["Tee","du","oder","Möchtest","Kaffee"]}', '["Möchtest","du","Kaffee","oder","Tee"]'),
    ('drinks', 8, 'Type the German phrase for “a glass”.', 'free_text', '{}', 'ein Glas'),
    ('drinks', 9, 'Type the order “For me a glass of water, please.”', 'free_text', '{}', 'Für mich ein Glas Wasser, bitte.'),
    ('drinks', 10, 'Type the sentence “The coffee is too hot.”', 'free_text', '{}', 'Der Kaffee ist zu heiß.'),

    ('home', 1, 'Which German word means “house”?', 'multiple_choice', '{"options":["das Haus","die Wohnung","das Zimmer"]}', 'das Haus'),
    ('home', 2, 'Which German word means “apartment”?', 'multiple_choice', '{"options":["die Küche","die Wohnung","das Haus"]}', 'die Wohnung'),
    ('home', 3, 'Which German word means “kitchen”?', 'multiple_choice', '{"options":["der Schlüssel","das Zimmer","die Küche"]}', 'die Küche'),
    ('home', 4, 'Which German word means “key”?', 'multiple_choice', '{"options":["die Wohnung","der Schlüssel","das Zimmer"]}', 'der Schlüssel'),
    ('home', 5, 'Put “The kitchen is in the apartment” in order.', 'word_ordering', '{"tokens":["Wohnung","Küche","der","ist","Die","in"]}', '["Die","Küche","ist","in","der","Wohnung"]'),
    ('home', 6, 'Put “The key is in the kitchen” in order.', 'word_ordering', '{"tokens":["Küche","Schlüssel","in","ist","Der","der"]}', '["Der","Schlüssel","ist","in","der","Küche"]'),
    ('home', 7, 'Put “My room is in the apartment” in order.', 'word_ordering', '{"tokens":["Wohnung","Zimmer","Mein","der","in","ist"]}', '["Mein","Zimmer","ist","in","der","Wohnung"]'),
    ('home', 8, 'Type the German phrase for “the room”.', 'free_text', '{}', 'das Zimmer'),
    ('home', 9, 'Type the question “Where is the key?”', 'free_text', '{}', 'Wo ist der Schlüssel?'),
    ('home', 10, 'Complete the location reply: “On the table.”', 'free_text', '{}', 'Auf dem Tisch.'),

    ('travel', 1, 'Which German word means “train station”?', 'multiple_choice', '{"options":["der Bahnhof","der Flughafen","die Fahrkarte"]}', 'der Bahnhof'),
    ('travel', 2, 'Which German word means “airport”?', 'multiple_choice', '{"options":["der Koffer","der Flughafen","der Bahnhof"]}', 'der Flughafen'),
    ('travel', 3, 'Which German word means “ticket”?', 'multiple_choice', '{"options":["die Fahrkarte","der Koffer","der Bahnhof"]}', 'die Fahrkarte'),
    ('travel', 4, 'Which German word means “suitcase”?', 'multiple_choice', '{"options":["der Flughafen","die Fahrkarte","der Koffer"]}', 'der Koffer'),
    ('travel', 5, 'Put “The train departs from the station” in order.', 'word_ordering', '{"tokens":["Bahnhof","Zug","ab","vom","fährt","Der"]}', '["Der","Zug","fährt","vom","Bahnhof","ab"]'),
    ('travel', 6, 'Put “My ticket is in the suitcase” in order.', 'word_ordering', '{"tokens":["Koffer","ist","Meine","im","Fahrkarte"]}', '["Meine","Fahrkarte","ist","im","Koffer"]'),
    ('travel', 7, 'Put “When does the train depart?” in order.', 'word_ordering', '{"tokens":["Zug","ab","Wann","fährt","der"]}', '["Wann","fährt","der","Zug","ab"]'),
    ('travel', 8, 'Type the German verb meaning “to depart”.', 'free_text', '{}', 'abfahren'),
    ('travel', 9, 'Complete the departure-time reply: “At three o’clock.”', 'free_text', '{}', 'Um drei Uhr.'),
    ('travel', 10, 'Type the sentence “Your ticket is in the suitcase.”', 'free_text', '{}', 'Deine Fahrkarte ist im Koffer.'),

    ('directions', 1, 'Which German word means “left”?', 'multiple_choice', '{"options":["links","rechts","geradeaus"]}', 'links'),
    ('directions', 2, 'Which German word means “right”?', 'multiple_choice', '{"options":["geradeaus","rechts","links"]}', 'rechts'),
    ('directions', 3, 'Which German word means “straight ahead”?', 'multiple_choice', '{"options":["rechts","links","geradeaus"]}', 'geradeaus'),
    ('directions', 4, 'Which German phrase means “the street”?', 'multiple_choice', '{"options":["Wo ist ...?","die Straße","geradeaus"]}', 'die Straße'),
    ('directions', 5, 'Put “Go straight ahead and then left” in order.', 'word_ordering', '{"tokens":["links","Sie","dann","geradeaus","Gehen","und"]}', '["Gehen","Sie","geradeaus","und","dann","links"]'),
    ('directions', 6, 'Put “Where is the street?” in order.', 'word_ordering', '{"tokens":["Straße","Wo","die","ist"]}', '["Wo","ist","die","Straße"]'),
    ('directions', 7, 'Put “Go straight ahead and then to the left” in order.', 'word_ordering', '{"tokens":["links","nach","geradeaus","Sie","dann","Gehen","und"]}', '["Gehen","Sie","geradeaus","und","dann","nach","links"]'),
    ('directions', 8, 'Type the German question pattern “Where is …?”', 'free_text', '{}', 'Wo ist ...?'),
    ('directions', 9, 'Complete the location reply: “It is on the right.”', 'free_text', '{}', 'Es ist rechts.'),
    ('directions', 10, 'Type the question “Excuse me, where is the street to the hotel?”', 'free_text', '{}', 'Entschuldigung, wo ist die Straße zum Hotel?'),

    ('time-calendar', 1, 'Which German word means “today”?', 'multiple_choice', '{"options":["heute","morgen","gestern"]}', 'heute'),
    ('time-calendar', 2, 'Which German word means “tomorrow”?', 'multiple_choice', '{"options":["gestern","morgen","heute"]}', 'morgen'),
    ('time-calendar', 3, 'Which German word means “yesterday”?', 'multiple_choice', '{"options":["morgen","heute","gestern"]}', 'gestern'),
    ('time-calendar', 4, 'Which German phrase means “Monday”?', 'multiple_choice', '{"options":["die Uhr","der Montag","heute"]}', 'der Montag'),
    ('time-calendar', 5, 'Put “Today is Monday” in order.', 'word_ordering', '{"tokens":["Montag","ist","Heute"]}', '["Heute","ist","Montag"]'),
    ('time-calendar', 6, 'Put “School starts at eight o’clock” in order.', 'word_ordering', '{"tokens":["Schule","Uhr","beginnt","Um","acht","die"]}', '["Um","acht","Uhr","beginnt","die","Schule"]'),
    ('time-calendar', 7, 'Put “Tomorrow at eight o’clock” in order.', 'word_ordering', '{"tokens":["Uhr","Morgen","acht","um"]}', '["Morgen","um","acht","Uhr"]'),
    ('time-calendar', 8, 'Type the German phrase for “the clock”.', 'free_text', '{}', 'die Uhr'),
    ('time-calendar', 9, 'Type the question “What day is today?”', 'free_text', '{}', 'Welcher Tag ist heute?'),
    ('time-calendar', 10, 'Type the question “When does school begin?”', 'free_text', '{}', 'Wann beginnt die Schule?'),

    ('weather', 1, 'Which German word means “sunny”?', 'multiple_choice', '{"options":["sonnig","regnerisch","kalt"]}', 'sonnig'),
    ('weather', 2, 'Which German word means “rainy”?', 'multiple_choice', '{"options":["warm","regnerisch","sonnig"]}', 'regnerisch'),
    ('weather', 3, 'Which German word means “cold”?', 'multiple_choice', '{"options":["der Wind","warm","kalt"]}', 'kalt'),
    ('weather', 4, 'Which German word means “warm”?', 'multiple_choice', '{"options":["regnerisch","kalt","warm"]}', 'warm'),
    ('weather', 5, 'Put “Today it is sunny but cold” in order.', 'word_ordering', '{"tokens":["kalt","es","aber","Heute","sonnig","ist"]}', '["Heute","ist","es","sonnig","aber","kalt"]'),
    ('weather', 6, 'Put “Tomorrow it will be rainy and windy” in order.', 'word_ordering', '{"tokens":["windig","Morgen","regnerisch","es","wird","und"]}', '["Morgen","wird","es","regnerisch","und","windig"]'),
    ('weather', 7, 'Put “It is sunny but cold” in order.', 'word_ordering', '{"tokens":["aber","Es","kalt","sonnig","ist"]}', '["Es","ist","sonnig","aber","kalt"]'),
    ('weather', 8, 'Type the German phrase for “the wind”.', 'free_text', '{}', 'der Wind'),
    ('weather', 9, 'Type the question “How is the weather today?”', 'free_text', '{}', 'Wie ist das Wetter heute?'),
    ('weather', 10, 'Type the forecast “Tomorrow it will be rainy and very windy.”', 'free_text', '{}', 'Morgen wird es regnerisch und sehr windig.'),

    ('shopping', 1, 'Which German verb means “to buy”?', 'multiple_choice', '{"options":["kaufen","teuer","billig"]}', 'kaufen'),
    ('shopping', 2, 'Which German phrase means “the price”?', 'multiple_choice', '{"options":["die Größe","der Preis","kaufen"]}', 'der Preis'),
    ('shopping', 3, 'Which German word means “cheap”?', 'multiple_choice', '{"options":["teuer","billig","der Preis"]}', 'billig'),
    ('shopping', 4, 'Which German phrase means “the size”?', 'multiple_choice', '{"options":["kaufen","die Größe","teuer"]}', 'die Größe'),
    ('shopping', 5, 'Put “How much does that cost?” in order.', 'word_ordering', '{"tokens":["kostet","viel","das","Wie"]}', '["Wie","viel","kostet","das"]'),
    ('shopping', 6, 'Put “The price is ten euros” in order.', 'word_ordering', '{"tokens":["Euro","Preis","zehn","ist","Der"]}', '["Der","Preis","ist","zehn","Euro"]'),
    ('shopping', 7, 'Put “Do you have this size?” in order.', 'word_ordering', '{"tokens":["diese","Haben","Größe","Sie"]}', '["Haben","Sie","diese","Größe"]'),
    ('shopping', 8, 'Type the German word for “expensive”.', 'free_text', '{}', 'teuer'),
    ('shopping', 9, 'Type the sentence “I would like to buy this jacket.”', 'free_text', '{}', 'Ich möchte diese Jacke kaufen.'),
    ('shopping', 10, 'Complete the reaction: “That is cheap!”', 'free_text', '{}', 'Das ist billig!'),

    ('work-school', 1, 'Which German phrase means “work”?', 'multiple_choice', '{"options":["die Arbeit","die Schule","das Büro"]}', 'die Arbeit'),
    ('work-school', 2, 'Which German phrase means “school”?', 'multiple_choice', '{"options":["der Lehrer","die Schule","die Arbeit"]}', 'die Schule'),
    ('work-school', 3, 'Which German phrase means “teacher”?', 'multiple_choice', '{"options":["das Büro","lernen","der Lehrer"]}', 'der Lehrer'),
    ('work-school', 4, 'Which German verb means “to learn”?', 'multiple_choice', '{"options":["die Schule","lernen","die Arbeit"]}', 'lernen'),
    ('work-school', 5, 'Put “I learn German at school” in order.', 'word_ordering', '{"tokens":["Schule","Deutsch","Ich","in","lerne","der"]}', '["Ich","lerne","Deutsch","in","der","Schule"]'),
    ('work-school', 6, 'Put “My teacher works in the office” in order.', 'word_ordering', '{"tokens":["Büro","arbeitet","Lehrer","im","Mein"]}', '["Mein","Lehrer","arbeitet","im","Büro"]'),
    ('work-school', 7, 'Put “I work in the office” in order.', 'word_ordering', '{"tokens":["Büro","arbeite","Ich","im"]}', '["Ich","arbeite","im","Büro"]'),
    ('work-school', 8, 'Type the German phrase for “the office”.', 'free_text', '{}', 'das Büro'),
    ('work-school', 9, 'Type the question “What do you do at school?”', 'free_text', '{}', 'Was machst du in der Schule?'),
    ('work-school', 10, 'Type the sentence “My teacher is very good.”', 'free_text', '{}', 'Mein Lehrer ist sehr gut.'),

    ('body-health', 1, 'Which German phrase means “head”?', 'multiple_choice', '{"options":["der Kopf","die Hand","der Arzt"]}', 'der Kopf'),
    ('body-health', 2, 'Which German phrase means “hand”?', 'multiple_choice', '{"options":["krank","die Hand","der Kopf"]}', 'die Hand'),
    ('body-health', 3, 'Which German phrase means “doctor”?', 'multiple_choice', '{"options":["Es tut weh","der Arzt","die Hand"]}', 'der Arzt'),
    ('body-health', 4, 'Which German word means “ill”?', 'multiple_choice', '{"options":["der Kopf","krank","der Arzt"]}', 'krank'),
    ('body-health', 5, 'Put “My head hurts” in order.', 'word_ordering', '{"tokens":["weh","Kopf","tut","Mein"]}', '["Mein","Kopf","tut","weh"]'),
    ('body-health', 6, 'Put “The doctor examines my hand” in order.', 'word_ordering', '{"tokens":["Hand","untersucht","Arzt","meine","Der"]}', '["Der","Arzt","untersucht","meine","Hand"]'),
    ('body-health', 7, 'Put “I am ill” in order.', 'word_ordering', '{"tokens":["krank","bin","Ich"]}', '["Ich","bin","krank"]'),
    ('body-health', 8, 'Type the German phrase for “It hurts”.', 'free_text', '{}', 'Es tut weh'),
    ('body-health', 9, 'Type the question “How are you?”', 'free_text', '{}', 'Wie geht es dir?'),
    ('body-health', 10, 'Type the instruction “Go to the doctor!”', 'free_text', '{}', 'Geh zum Arzt!'),

    ('emotions', 1, 'Which German word means “happy”?', 'multiple_choice', '{"options":["glücklich","traurig","müde"]}', 'glücklich'),
    ('emotions', 2, 'Which German word means “sad”?', 'multiple_choice', '{"options":["aufgeregt","traurig","glücklich"]}', 'traurig'),
    ('emotions', 3, 'Which German word means “tired”?', 'multiple_choice', '{"options":["Angst haben","glücklich","müde"]}', 'müde'),
    ('emotions', 4, 'Which German word means “excited”?', 'multiple_choice', '{"options":["traurig","aufgeregt","müde"]}', 'aufgeregt'),
    ('emotions', 5, 'Put “I am happy but tired” in order.', 'word_ordering', '{"tokens":["müde","glücklich","aber","Ich","bin"]}', '["Ich","bin","glücklich","aber","müde"]'),
    ('emotions', 6, 'Put “She is afraid of the dog” in order.', 'word_ordering', '{"tokens":["Hund","Angst","dem","Sie","vor","hat"]}', '["Sie","hat","Angst","vor","dem","Hund"]'),
    ('emotions', 7, 'Put “How do you feel today?” in order.', 'word_ordering', '{"tokens":["heute","dich","Wie","du","fühlst"]}', '["Wie","fühlst","du","dich","heute"]'),
    ('emotions', 8, 'Type the German phrase meaning “to be afraid”.', 'free_text', '{}', 'Angst haben'),
    ('emotions', 9, 'Type the sentence “I am happy, but a little tired.”', 'free_text', '{}', 'Ich bin glücklich, aber ein bisschen müde.'),
    ('emotions', 10, 'Type the sentence “She is afraid of the big dog.”', 'free_text', '{}', 'Sie hat Angst vor dem großen Hund.'),

    ('hobbies', 1, 'Which German verb means “to read”?', 'multiple_choice', '{"options":["lesen","kochen","tanzen"]}', 'lesen'),
    ('hobbies', 2, 'Which German phrase means “to listen to music”?', 'multiple_choice', '{"options":["Sport treiben","Musik hören","lesen"]}', 'Musik hören'),
    ('hobbies', 3, 'Which German verb means “to cook”?', 'multiple_choice', '{"options":["tanzen","kochen","lesen"]}', 'kochen'),
    ('hobbies', 4, 'Which German phrase means “to do sport”?', 'multiple_choice', '{"options":["Musik hören","Sport treiben","kochen"]}', 'Sport treiben'),
    ('hobbies', 5, 'Put “I like reading and listening to music” in order.', 'word_ordering', '{"tokens":["Musik","gern","Ich","höre","lese","und"]}', '["Ich","lese","gern","und","höre","Musik"]'),
    ('hobbies', 6, 'Put “On Saturday I do sport” in order.', 'word_ordering', '{"tokens":["Sport","Samstag","treibe","Am","ich"]}', '["Am","Samstag","treibe","ich","Sport"]'),
    ('hobbies', 7, 'Put “I like doing sport and dancing” in order.', 'word_ordering', '{"tokens":["tanze","Sport","gern","Ich","treibe","und"]}', '["Ich","treibe","gern","Sport","und","tanze"]'),
    ('hobbies', 8, 'Type the German verb meaning “to dance”.', 'free_text', '{}', 'tanzen'),
    ('hobbies', 9, 'Type the question “What do you like doing on the weekend?”', 'free_text', '{}', 'Was machst du gern am Wochenende?'),
    ('hobbies', 10, 'Type the sentence “I like cooking.”', 'free_text', '{}', 'Ich koche gern.'),

    ('nature-animals', 1, 'Which German phrase means “dog”?', 'multiple_choice', '{"options":["der Hund","die Katze","der Vogel"]}', 'der Hund'),
    ('nature-animals', 2, 'Which German phrase means “cat”?', 'multiple_choice', '{"options":["der Wald","die Katze","der Hund"]}', 'die Katze'),
    ('nature-animals', 3, 'Which German phrase means “tree”?', 'multiple_choice', '{"options":["der Vogel","der Baum","der Wald"]}', 'der Baum'),
    ('nature-animals', 4, 'Which German phrase means “forest”?', 'multiple_choice', '{"options":["der Baum","der Hund","der Wald"]}', 'der Wald'),
    ('nature-animals', 5, 'Put “The dog runs in the forest” in order.', 'word_ordering', '{"tokens":["Wald","läuft","Hund","im","Der"]}', '["Der","Hund","läuft","im","Wald"]'),
    ('nature-animals', 6, 'Put “A bird sits in the tree” in order.', 'word_ordering', '{"tokens":["Baum","Vogel","dem","sitzt","Ein","auf"]}', '["Ein","Vogel","sitzt","auf","dem","Baum"]'),
    ('nature-animals', 7, 'Put the location phrase “in the forest” in order.', 'word_ordering', '{"tokens":["Wald","im"]}', '["im","Wald"]'),
    ('nature-animals', 8, 'Type the German phrase for “the bird”.', 'free_text', '{}', 'der Vogel'),
    ('nature-animals', 9, 'Type the German location phrase “in the forest”.', 'free_text', '{}', 'im Wald'),
    ('nature-animals', 10, 'Type the German location phrase “in the tree”.', 'free_text', '{}', 'auf dem Baum'),

    ('long-words', 1, 'Which word means “law delegating duties for supervising beef labeling”?', 'multiple_choice', '{"options":["Rindfleischetikettierungsüberwachungsaufgabenübertragungsgesetz","Donaudampfschifffahrtsgesellschaft","Geschwindigkeitsbegrenzung"]}', 'Rindfleischetikettierungsüberwachungsaufgabenübertragungsgesetz'),
    ('long-words', 2, 'Which word means “Danube steamship company”?', 'multiple_choice', '{"options":["Arbeitsunfähigkeitsbescheinigung","Donaudampfschifffahrtsgesellschaft","Kraftfahrzeughaftpflichtversicherung"]}', 'Donaudampfschifffahrtsgesellschaft'),
    ('long-words', 3, 'Which word means “motor-vehicle liability insurance”?', 'multiple_choice', '{"options":["Geschwindigkeitsbegrenzung","Rindfleischetikettierungsüberwachungsaufgabenübertragungsgesetz","Kraftfahrzeughaftpflichtversicherung"]}', 'Kraftfahrzeughaftpflichtversicherung'),
    ('long-words', 4, 'Which word means “certificate of incapacity for work”?', 'multiple_choice', '{"options":["Donaudampfschifffahrtsgesellschaft","Arbeitsunfähigkeitsbescheinigung","Geschwindigkeitsbegrenzung"]}', 'Arbeitsunfähigkeitsbescheinigung'),
    ('long-words', 5, 'Put the beef-labeling law sentence in order.', 'word_ordering', '{"tokens":["Gesetz","Rindfleischetikettierungsüberwachungsaufgabenübertragungsgesetz","ein","Das","ist"]}', '["Das","Rindfleischetikettierungsüberwachungsaufgabenübertragungsgesetz","ist","ein","Gesetz"]'),
    ('long-words', 6, 'Put the motor-vehicle insurance sentence in order.', 'word_ordering', '{"tokens":["Autofahrer","Kraftfahrzeughaftpflichtversicherung","Eine","schützt"]}', '["Eine","Kraftfahrzeughaftpflichtversicherung","schützt","Autofahrer"]'),
    ('long-words', 7, 'Build the beef-labeling law compound from its parts.', 'word_ordering', '{"tokens":["Gesetz","Aufgaben","Rindfleisch","Übertragung","Etikettierung","Überwachung"]}', '["Rindfleisch","Etikettierung","Überwachung","Aufgaben","Übertragung","Gesetz"]'),
    ('long-words', 8, 'Type the German word for “speed limit”.', 'free_text', '{}', 'Geschwindigkeitsbegrenzung'),
    ('long-words', 9, 'Type the German beef-labeling supervision law.', 'free_text', '{}', 'Rindfleischetikettierungsüberwachungsaufgabenübertragungsgesetz'),
    ('long-words', 10, 'Type the sentence “A motor-vehicle liability insurance protects drivers.”', 'free_text', '{}', 'Eine Kraftfahrzeughaftpflichtversicherung schützt Autofahrer.'),

    ('funny-unusual-words', 1, 'Which word means “a catchy tune stuck in your head”?', 'multiple_choice', '{"options":["Ohrwurm","Wanderlust","Kopfkino"]}', 'Ohrwurm'),
    ('funny-unusual-words', 2, 'Which word means “desire to travel”?', 'multiple_choice', '{"options":["Fernweh","Wanderlust","Fingerspitzengefühl"]}', 'Wanderlust'),
    ('funny-unusual-words', 3, 'Which word means “longing for faraway places”?', 'multiple_choice', '{"options":["Kopfkino","Ohrwurm","Fernweh"]}', 'Fernweh'),
    ('funny-unusual-words', 4, 'Which word means “tact and intuitive sensitivity”?', 'multiple_choice', '{"options":["Wanderlust","Fingerspitzengefühl","Kopfkino"]}', 'Fingerspitzengefühl'),
    ('funny-unusual-words', 5, 'Put “I have a catchy tune from this song stuck in my head” in order.', 'word_ordering', '{"tokens":["Lied","Ohrwurm","Ich","diesem","einen","von","habe"]}', '["Ich","habe","einen","Ohrwurm","von","diesem","Lied"]'),
    ('funny-unusual-words', 6, 'Put “After eating we go for a walk” in order.', 'word_ordering', '{"tokens":["gehen","Essen","spazieren","wir","dem","Nach"]}', '["Nach","dem","Essen","gehen","wir","spazieren"]'),
    ('funny-unusual-words', 7, 'Put the phrase “from this song” in order.', 'word_ordering', '{"tokens":["Lied","von","diesem"]}', '["von","diesem","Lied"]'),
    ('funny-unusual-words', 8, 'Type the word for “a vivid imagined scene in your head”.', 'free_text', '{}', 'Kopfkino'),
    ('funny-unusual-words', 9, 'Type the sentence “I have a catchy tune from this song stuck in my head.”', 'free_text', '{}', 'Ich habe einen Ohrwurm von diesem Lied.'),
    ('funny-unusual-words', 10, 'Type the sentence “After eating we go for a walk.”', 'free_text', '{}', 'Nach dem Essen gehen wir spazieren.')
)
INSERT OR IGNORE INTO QuizQuestions (QuizId, SortOrder, Content, Type, QuestionData, CorrectAnswer)
SELECT q.Id, s.SortOrder, s.Content, s.Type, s.QuestionData, s.CorrectAnswer
FROM QuestionSeeds s
INNER JOIN Lessons l ON l.Slug = s.LessonSlug
INNER JOIN Courses c ON c.Id = l.CourseId AND c.Code = 'de'
INNER JOIN Quizzes q ON q.LessonId = l.Id;
