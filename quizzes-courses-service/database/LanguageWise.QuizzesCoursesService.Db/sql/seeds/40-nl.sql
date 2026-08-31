-- Dutch lessons, vocabulary, quizzes, and questions. Requires schema.sql and seeds/00-courses.sql.
WITH LessonSeeds (Slug, Title, SortOrder, ContentMarkdown) AS (
    VALUES
    ('greetings', 'Greetings', 1, '## Learn in context

| Target language | English |
| --- | --- |
| Hallo, Noor! Welkom in Amsterdam. | Hello, Noor! Welcome to Amsterdam. |
| Goedemorgen, mevrouw. | Good morning, madam. |

## Mini dialogue

> **A:** Hallo, Noor! Welkom hier in Amsterdam.
> *Hello, Noor! Welcome here in Amsterdam.*
>
> **B:** Goedendag! Dank je wel.
> *Good day! Thank you.*
>
> **A:** Ben je klaar?
> *Are you ready?*
>
> **B:** Ja!
> *Yes!*

## Language note

**Hallo** is neutral; **goedemorgen** is a polite morning greeting. *Hoi* is informal.'),
    ('introductions', 'Introductions', 2, '## Learn in context

| Target language | English |
| --- | --- |
| Ik heet Daan en ik kom uit Canada. | My name is Daan and I come from Canada. |
| Hoe heet jij? — Ik heet Emma. | What is your name? — My name is Emma. |

## Mini dialogue

> **A:** Hallo! Ik heet Daan. Hoe heet jij?
> *Hello! My name is Daan. What is your name?*
>
> **B:** Ik heet Emma. Aangenaam!
> *My name is Emma. Nice to meet you!*
>
> **A:** Ik kom uit Canada. En jij?
> *I come from Canada. And you?*
>
> **B:** Ik kom uit België.
> *I come from Belgium.*

## Language note

Dutch **jij** is informal singular. *Ik ben Daan* is also a natural introduction.'),
    ('politeness', 'Politeness', 3, '## Learn in context

| Target language | English |
| --- | --- |
| Een koffie, alstublieft. Dank je wel! | A coffee, please. Thank you! |
| Pardon, kunt u mij helpen? | Excuse me, can you help me? |

## Mini dialogue

> **A:** Pardon, kunt u mij even helpen?
> *Excuse me, can you help me for a moment?*
>
> **B:** Ja, natuurlijk. Alstublieft.
> *Yes, of course. Here you are.*
>
> **A:** Bedankt!
> *Thanks!*
>
> **B:** Graag gedaan.
> *You’re welcome.*

## Language note

**Alstublieft** is formal/plural; **alsjeblieft** is informal. Both also mean “here you are.”'),
    ('numbers', 'Numbers', 4, '## Learn in context

| Target language | English |
| --- | --- |
| Ik heb twee kaartjes en tien euro. | I have two tickets and ten euros. |
| De trein vertrekt om drie uur. | The train leaves at three o’clock. |

## Worked usage

- **Ik heb twee kaartjes en tien euro.** — *I have two tickets and ten euros.*
- **De trein vertrekt om drie uur.** — *The train leaves at three o’clock.*

## Language note

Dutch joins number words: **drieëntwintig**. The diaeresis shows the vowels are pronounced separately.'),
    ('family', 'Family', 5, '## Learn in context

| Target language | English |
| --- | --- |
| Dit is mijn moeder en mijn vader. | This is my mother and my father. |
| Mijn zus heeft een broer. | My sister has a brother. |

## Mini dialogue

> **A:** Is dit jouw familie?
> *Is this your family?*
>
> **B:** Ja. Dit is mijn moeder en mijn vader.
> *Yes. This is my mother and my father.*
>
> **A:** Heb je een broer?
> *Do you have a brother?*
>
> **B:** Nee, maar ik heb een zus.
> *No, but I have a sister.*

## Language note

Dutch has common-gender **de** and neuter **het** nouns; *moeder* and *vader* take **de**.'),
    ('food', 'Food', 6, '## Learn in context

| Target language | English |
| --- | --- |
| Ik eet brood en kaas als ontbijt. | I eat bread and cheese for breakfast. |
| De appel is lekker. | The apple is tasty. |

## Mini dialogue

> **A:** Wat eet je als ontbijt?
> *What do you eat for breakfast?*
>
> **B:** Ik eet brood en kaas.
> *I eat bread and cheese.*
>
> **A:** En de appel?
> *And the apple?*
>
> **B:** De appel is lekker!
> *The apple is tasty!*

## Language note

**Lekker** can mean tasty, but also pleasant weather or a comfortable feeling.'),
    ('drinks', 'Drinks', 7, '## Learn in context

| Target language | English |
| --- | --- |
| Ik wil graag een glas water. | I would like a glass of water. |
| De koffie is heet en de thee is warm. | The coffee is hot and the tea is warm. |

## Mini dialogue

> **A:** Wil je koffie of thee?
> *Do you want coffee or tea?*
>
> **B:** Een glas water, alsjeblieft. De koffie is te heet.
> *A glass of water, please. The coffee is too hot.*

## Language note

Adding **graag** makes *ik wil* friendlier: literally “I want gladly.”'),
    ('home', 'Home', 8, '## Learn in context

| Target language | English |
| --- | --- |
| De keuken is in het appartement. | The kitchen is in the apartment. |
| Waar is de sleutel? — Op tafel. | Where is the key? — On the table. |

## Mini dialogue

> **A:** Waar is de sleutel?
> *Where is the key?*
>
> **B:** De sleutel is in de keuken.
> *The key is in the kitchen.*
>
> **A:** En jouw kamer?
> *And your room?*
>
> **B:** Mijn kamer is in het appartement.
> *My room is in the apartment.*

## Language note

Dutch often drops the article in location chunks: **op tafel**, but *in het huis*.'),
    ('travel', 'Travel', 9, '## Learn in context

| Target language | English |
| --- | --- |
| Het station ligt bij de luchthaven. | The station is near the airport. |
| Mijn kaartje zit in de koffer. | My ticket is in the suitcase. |

## Mini dialogue

> **A:** Hoe laat vertrekt de trein?
> *What time does the train leave?*
>
> **B:** Om drie uur, op het station.
> *At three o’clock, at the station.*
>
> **A:** Waar is mijn kaartje?
> *Where is my ticket?*
>
> **B:** Jouw kaartje zit in de koffer.
> *Your ticket is in the suitcase.*

## Language note

**Kaartje** is a diminutive of *kaart* (“card/map”) and commonly means a ticket.'),
    ('directions', 'Directions', 10, '## Learn in context

| Target language | English |
| --- | --- |
| Ga rechtdoor en dan naar links. | Go straight ahead and then left. |
| Waar is de straat? — Rechts van het hotel. | Where is the street? — To the right of the hotel. |

## Mini dialogue

> **A:** Pardon, waar is de straat naar het hotel?
> *Excuse me, where is the street to the hotel?*
>
> **B:** Ga eerst rechtdoor en dan naar links.
> *Go straight ahead first and then left.*
>
> **A:** En het hotel?
> *And the hotel?*
>
> **B:** Het is rechts.
> *It is on the right.*

## Language note

**Ga** is informal singular. Directions commonly use *naar links/rechts*.'),
    ('time-calendar', 'Time and Calendar', 11, '## Learn in context

| Target language | English |
| --- | --- |
| Vandaag is het maandag; morgen werk ik. | Today is Monday; tomorrow I work. |
| De les begint om acht uur. | The lesson begins at eight o’clock. |

## Mini dialogue

> **A:** Welke dag is het vandaag?
> *What day is it today?*
>
> **B:** Vandaag is het maandag.
> *Today is Monday.*
>
> **A:** Hoe laat begint de les?
> *What time does the lesson start?*
>
> **B:** Morgen om acht uur.
> *Tomorrow at eight o’clock.*

## Language note

Use **om** for exact time. Dutch days are written in lower case.'),
    ('weather', 'Weather', 12, '## Learn in context

| Target language | English |
| --- | --- |
| Vandaag is het zonnig, maar koud. | Today it is sunny but cold. |
| Morgen wordt het regenachtig en winderig. | Tomorrow it will become rainy and windy. |

## Mini dialogue

> **A:** Hoe is het weer vandaag?
> *How is the weather today?*
>
> **B:** Het is zonnig, maar koud.
> *It is sunny but cold.*
>
> **A:** En morgen?
> *And tomorrow?*
>
> **B:** Morgen wordt het regenachtig en heel winderig.
> *Tomorrow it will become rainy and very windy.*

## Language note

Weather uses neutral **het**: *het is koud*. **Wordt** describes a change.'),
    ('shopping', 'Shopping', 13, '## Learn in context

| Target language | English |
| --- | --- |
| Hoeveel kost deze jas? | How much does this coat cost? |
| De prijs is twintig euro; hij is goedkoop. | The price is twenty euros; it is cheap. |

## Mini dialogue

> **A:** Ik wil deze jas kopen. Wat is de prijs?
> *I want to buy this coat. What is the price?*
>
> **B:** De prijs is twintig euro.
> *The price is twenty euros.*
>
> **A:** Dat is goedkoop! Heeft u mijn maat?
> *That is cheap! Do you have my size?*
>
> **B:** Ja, natuurlijk.
> *Yes, of course.*

## Language note

**Goedkoop** is cheap; *voordelig* often means good value. **Maat** is clothing size.'),
    ('work-school', 'Work and School', 14, '## Learn in context

| Target language | English |
| --- | --- |
| Ik leer Nederlands op school. | I learn Dutch at school. |
| De docent werkt op kantoor. | The teacher works in an office. |

## Mini dialogue

> **A:** Wat doe je op school?
> *What do you do at school?*
>
> **B:** Ik leer Nederlands. Mijn leraar is aardig.
> *I learn Dutch. My teacher is nice.*
>
> **A:** En waar is je werk?
> *And where is your work?*
>
> **B:** Ik werk op kantoor.
> *I work at the office.*

## Language note

**Leraar** is a school teacher; **docent** is common in secondary and higher education.'),
    ('body-health', 'Body and Health', 15, '## Learn in context

| Target language | English |
| --- | --- |
| Mijn hoofd doet pijn; ik ben ziek. | My head hurts; I am ill. |
| De dokter onderzoekt mijn hand. | The doctor examines my hand. |

## Mini dialogue

> **A:** Hoe gaat het?
> *How are you?*
>
> **B:** Niet goed. Mijn hoofd doet pijn. Ik ben ziek.
> *Not good. My head hurts. I am ill.*
>
> **A:** Ga naar de dokter!
> *Go to the doctor!*
>
> **B:** Ja, de dokter onderzoekt ook mijn hand.
> *Yes, the doctor is also examining my hand.*

## Language note

Say **pijn doen**: *Mijn hoofd doet pijn*. **Ziek** usually means physically ill.'),
    ('emotions', 'Emotions', 16, '## Learn in context

| Target language | English |
| --- | --- |
| Ik ben blij, maar moe. | I am happy, but tired. |
| Zij is bang voor de hond. | She is afraid of the dog. |

## Mini dialogue

> **A:** Hoe voel je je?
> *How do you feel?*
>
> **B:** Ik ben heel blij, maar moe.
> *I am very happy, but tired.*
>
> **A:** En waarom is je zus verdrietig?
> *And why is your sister sad?*
>
> **B:** Ze is bang voor de hond.
> *She is afraid of the dog.*

## Language note

Use **bang voor** (“afraid of”). **Blij** is glad; *gelukkig* can also mean fortunate.'),
    ('hobbies', 'Hobbies', 17, '## Learn in context

| Target language | English |
| --- | --- |
| Ik lees graag en luister naar muziek. | I like reading and listen to music. |
| In het weekend sport ik en dans ik. | At the weekend I exercise and dance. |

## Mini dialogue

> **A:** Wat doe je graag in het weekend?
> *What do you like to do at the weekend?*
>
> **B:** Ik lees graag en luister naar muziek. En jij?
> *I like reading and listening to music. And you?*
>
> **A:** Ik sport en dans graag.
> *I like to exercise and dance.*

## Language note

With **luisteren**, use **naar**: *luisteren naar muziek*. Dutch uses **sporten** as a verb.'),
    ('nature-animals', 'Nature and Animals', 18, '## Learn in context

| Target language | English |
| --- | --- |
| De hond loopt in het bos. | The dog walks in the woods. |
| Een vogel zit in de boom. | A bird sits in the tree. |

## Worked usage

- **De hond loopt in het bos.** — *The dog walks in the woods.*
- **Een vogel zit in de boom.** — *A bird sits in the tree.*

## Language note

**Bos** means woods/forest. *Boom* is a tree and is a **de**-word.'),
    ('long-words', 'Long Words', 19, '## Learn in context

| Target language | English |
| --- | --- |
| Meervoudigepersoonlijkheidsstoornis is een lang woord. | Multiple-personality disorder is a long word. |
| De arbeidsongeschiktheidsverzekering helpt bij ziekte. | Disability insurance helps during illness. |

## Worked usage

- **Meervoudigepersoonlijkheidsstoornis is een lang woord.** — *Multiple-personality disorder is a long word.*
- **De arbeidsongeschiktheidsverzekering helpt bij ziekte.** — *Disability insurance helps during illness.*

## Language note

Dutch compounds are written together: **meervoudige + persoonlijkheids + stoornis**. In care, use the current precise clinical term.'),
    ('funny-unusual-words', 'Funny and Unusual Words', 20, '## Learn in context

| Target language | English |
| --- | --- |
| Bij ons thuis is het gezellig. | At our home it is cozy and sociable. |
| Na het eten gaan we uitwaaien op het strand. | After eating we go get fresh air on the beach. |

## Worked usage

- **Bij ons thuis is het gezellig.** — *At our home it is cozy and sociable.*
- **Na het eten gaan we uitwaaien op het strand.** — *After eating we go get fresh air on the beach.*

## Language note

**Gezellig** combines warmth, comfort, and company; English has no one exact equivalent. **Uitwaaien** is getting fresh air in the wind.')
)
INSERT OR IGNORE INTO Lessons (CourseId, Slug, Title, SortOrder, ContentMarkdown)
SELECT c.Id, s.Slug, s.Title, s.SortOrder, s.ContentMarkdown
FROM Courses c
CROSS JOIN LessonSeeds s
WHERE c.Code = 'nl';

WITH VocabularySeeds (LessonSlug, VocabularyJson) AS (
    VALUES
    ('greetings', '{"words":[{"word":"Hallo","meaning":"Hello"},{"word":"Goedendag","meaning":"Good day"},{"word":"Welkom","meaning":"Welcome"},{"word":"Ja","meaning":"Yes"},{"word":"Nee","meaning":"No"}]}'),
    ('introductions', '{"words":[{"word":"Ik heet ...","meaning":"My name is ..."},{"word":"Hoe heet je?","meaning":"What is your name?"},{"word":"Ik kom uit ...","meaning":"I come from ..."},{"word":"Aangenaam","meaning":"Nice to meet you"},{"word":"Dit is ...","meaning":"This is ..."}]}'),
    ('politeness', '{"words":[{"word":"Alsjeblieft","meaning":"Please"},{"word":"Bedankt","meaning":"Thank you"},{"word":"Sorry","meaning":"Sorry / excuse me"},{"word":"Graag gedaan","meaning":"You are welcome"},{"word":"Kun je helpen?","meaning":"Can you help?"}]}'),
    ('numbers', '{"words":[{"word":"een","meaning":"one"},{"word":"twee","meaning":"two"},{"word":"drie","meaning":"three"},{"word":"tien","meaning":"ten"},{"word":"honderd","meaning":"one hundred"}]}'),
    ('family', '{"words":[{"word":"de familie","meaning":"family"},{"word":"de moeder","meaning":"mother"},{"word":"de vader","meaning":"father"},{"word":"de broer","meaning":"brother"},{"word":"de zus","meaning":"sister"}]}'),
    ('food', '{"words":[{"word":"het brood","meaning":"bread"},{"word":"de kaas","meaning":"cheese"},{"word":"de appel","meaning":"apple"},{"word":"het ontbijt","meaning":"breakfast"},{"word":"lekker","meaning":"tasty"}]}'),
    ('drinks', '{"words":[{"word":"het water","meaning":"water"},{"word":"de koffie","meaning":"coffee"},{"word":"de thee","meaning":"tea"},{"word":"het bier","meaning":"beer"},{"word":"een glas","meaning":"a glass"}]}'),
    ('home', '{"words":[{"word":"het huis","meaning":"house"},{"word":"het appartement","meaning":"apartment"},{"word":"de kamer","meaning":"room"},{"word":"de keuken","meaning":"kitchen"},{"word":"de sleutel","meaning":"key"}]}'),
    ('travel', '{"words":[{"word":"het station","meaning":"train station"},{"word":"de luchthaven","meaning":"airport"},{"word":"het kaartje","meaning":"ticket"},{"word":"de koffer","meaning":"suitcase"},{"word":"vertrekken","meaning":"to depart"}]}'),
    ('directions', '{"words":[{"word":"links","meaning":"left"},{"word":"rechts","meaning":"right"},{"word":"rechtdoor","meaning":"straight ahead"},{"word":"de straat","meaning":"street"},{"word":"Waar is ...?","meaning":"Where is ...?"}]}'),
    ('time-calendar', '{"words":[{"word":"vandaag","meaning":"today"},{"word":"morgen","meaning":"tomorrow"},{"word":"gisteren","meaning":"yesterday"},{"word":"de klok","meaning":"clock"},{"word":"maandag","meaning":"Monday"}]}'),
    ('weather', '{"words":[{"word":"zonnig","meaning":"sunny"},{"word":"regenachtig","meaning":"rainy"},{"word":"de wind","meaning":"wind"},{"word":"koud","meaning":"cold"},{"word":"warm","meaning":"warm"}]}'),
    ('shopping', '{"words":[{"word":"kopen","meaning":"to buy"},{"word":"de prijs","meaning":"price"},{"word":"duur","meaning":"expensive"},{"word":"goedkoop","meaning":"cheap"},{"word":"de maat","meaning":"size"}]}'),
    ('work-school', '{"words":[{"word":"het werk","meaning":"work"},{"word":"de school","meaning":"school"},{"word":"de leraar","meaning":"teacher"},{"word":"leren","meaning":"to learn"},{"word":"het kantoor","meaning":"office"}]}'),
    ('body-health', '{"words":[{"word":"het hoofd","meaning":"head"},{"word":"de hand","meaning":"hand"},{"word":"de dokter","meaning":"doctor"},{"word":"ziek","meaning":"ill"},{"word":"Het doet pijn","meaning":"It hurts"}]}'),
    ('emotions', '{"words":[{"word":"blij","meaning":"happy"},{"word":"verdrietig","meaning":"sad"},{"word":"moe","meaning":"tired"},{"word":"opgewonden","meaning":"excited"},{"word":"bang zijn","meaning":"to be afraid"}]}'),
    ('hobbies', '{"words":[{"word":"lezen","meaning":"to read"},{"word":"naar muziek luisteren","meaning":"to listen to music"},{"word":"koken","meaning":"to cook"},{"word":"sporten","meaning":"to do sport"},{"word":"dansen","meaning":"to dance"}]}'),
    ('nature-animals', '{"words":[{"word":"de hond","meaning":"dog"},{"word":"de kat","meaning":"cat"},{"word":"de boom","meaning":"tree"},{"word":"het bos","meaning":"forest"},{"word":"de vogel","meaning":"bird"}]}'),
    ('long-words', '{"words":[{"word":"meervoudigepersoonlijkheidsstoornis","meaning":"multiple personality disorder"},{"word":"arbeidsongeschiktheidsverzekering","meaning":"disability insurance"},{"word":"aansprakelijkheidsverzekering","meaning":"liability insurance"},{"word":"kindercarnavalsoptocht","meaning":"children’s carnival parade"},{"word":"hottentottententententoonstelling","meaning":"a playful classic compound"}]}'),
    ('funny-unusual-words', '{"words":[{"word":"gezellig","meaning":"cozy and sociable"},{"word":"uitwaaien","meaning":"to clear one’s head in the wind"},{"word":"voorpret","meaning":"anticipatory enjoyment"},{"word":"uitbuiken","meaning":"to relax after a big meal"},{"word":"niksen","meaning":"deliberately doing nothing"}]}')
)
INSERT OR IGNORE INTO LessonVocabulary (LessonId, VocabularyJson)
SELECT l.Id, s.VocabularyJson
FROM VocabularySeeds s
INNER JOIN Courses c ON c.Code = 'nl'
INNER JOIN Lessons l ON l.CourseId = c.Id AND l.Slug = s.LessonSlug;

INSERT OR IGNORE INTO Quizzes (LessonId, Title)
SELECT l.Id, l.Title || ' Quiz'
FROM Courses c
INNER JOIN Lessons l ON l.CourseId = c.Id
WHERE c.Code = 'nl';

WITH QuestionSeeds (LessonSlug, SortOrder, Content, Type, QuestionData, CorrectAnswer) AS (
    VALUES
    ('greetings', 1, 'Which word means “Hello”?', 'multiple_choice', '{"options":["Hallo","Goedendag","Nee"]}', 'Hallo'),
    ('greetings', 2, 'Which greeting means “Good day”?', 'multiple_choice', '{"options":["Welkom","Goedendag","Ja"]}', 'Goedendag'),
    ('greetings', 3, 'Which word means “Welcome”?', 'multiple_choice', '{"options":["Welkom","Hallo","Nee"]}', 'Welkom'),
    ('greetings', 4, 'Which Dutch word means “Yes”?', 'multiple_choice', '{"options":["Nee","Ja","Goedemorgen"]}', 'Ja'),
    ('greetings', 5, 'Put the polite morning greeting in order.', 'word_ordering', '{"tokens":["mevrouw","Goedemorgen"]}', '["Goedemorgen","mevrouw"]'),
    ('greetings', 6, 'Put “Welcome here in Amsterdam” in order.', 'word_ordering', '{"tokens":["Amsterdam","hier","Welkom","in"]}', '["Welkom","hier","in","Amsterdam"]'),
    ('greetings', 7, 'Put “Hello, Noor” in order.', 'word_ordering', '{"tokens":["Noor","Hallo"]}', '["Hallo","Noor"]'),
    ('greetings', 8, 'Type the Dutch word for “No”.', 'free_text', '{}', 'Nee'),
    ('greetings', 9, 'Type the reply “Thank you”.', 'free_text', '{}', 'Dank je wel'),
    ('greetings', 10, 'Type the question “Are you ready?”', 'free_text', '{}', 'Ben je klaar?'),
    ('introductions', 1, 'Which phrase means “My name is …”?', 'multiple_choice', '{"options":["Ik heet ...","Hoe heet je?","Dit is ..."]}', 'Ik heet ...'),
    ('introductions', 2, 'How do you ask a friend for their name?', 'multiple_choice', '{"options":["Ik kom uit ...","Hoe heet je?","Aangenaam"]}', 'Hoe heet je?'),
    ('introductions', 3, 'Which phrase means “I come from …”?', 'multiple_choice', '{"options":["Dit is ...","Ik kom uit ...","Ik heet ..."]}', 'Ik kom uit ...'),
    ('introductions', 4, 'Which word means “Nice to meet you”?', 'multiple_choice', '{"options":["Welkom","Aangenaam","Bedankt"]}', 'Aangenaam'),
    ('introductions', 5, 'Put “My name is Daan” in order.', 'word_ordering', '{"tokens":["Daan","heet","Ik"]}', '["Ik","heet","Daan"]'),
    ('introductions', 6, 'Put “I come from Canada” in order.', 'word_ordering', '{"tokens":["Canada","uit","kom","Ik"]}', '["Ik","kom","uit","Canada"]'),
    ('introductions', 7, 'Put “What is your name?” in order.', 'word_ordering', '{"tokens":["jij","heet","Hoe"]}', '["Hoe","heet","jij"]'),
    ('introductions', 8, 'Type the Dutch phrase for “This is …”.', 'free_text', '{}', 'Dit is ...'),
    ('introductions', 9, 'Complete the introduction: “My name is Emma.”', 'free_text', '{}', 'Ik heet Emma.'),
    ('introductions', 10, 'Type the sentence “I come from Belgium.”', 'free_text', '{}', 'Ik kom uit België.'),
    ('politeness', 1, 'Which informal word means “Please”?', 'multiple_choice', '{"options":["Alsjeblieft","Bedankt","Sorry"]}', 'Alsjeblieft'),
    ('politeness', 2, 'Which word means “Thank you”?', 'multiple_choice', '{"options":["Sorry","Bedankt","Alstublieft"]}', 'Bedankt'),
    ('politeness', 3, 'Which phrase means “You are welcome”?', 'multiple_choice', '{"options":["Graag gedaan","Kun je helpen?","Dank je wel"]}', 'Graag gedaan'),
    ('politeness', 4, 'Which formal word can mean “Please” or “Here you are”?', 'multiple_choice', '{"options":["Alsjeblieft","Alstublieft","Pardon"]}', 'Alstublieft'),
    ('politeness', 5, 'Put “Excuse me, can you help me?” in order.', 'word_ordering', '{"tokens":["helpen","Pardon","u","mij","kunt"]}', '["Pardon","kunt","u","mij","helpen"]'),
    ('politeness', 6, 'Put “A coffee, please” in order.', 'word_ordering', '{"tokens":["alstublieft","koffie","Een"]}', '["Een","koffie","alstublieft"]'),
    ('politeness', 7, 'Put “Yes, of course” in order.', 'word_ordering', '{"tokens":["natuurlijk","Ja"]}', '["Ja","natuurlijk"]'),
    ('politeness', 8, 'Type the Dutch word for “Sorry”.', 'free_text', '{}', 'Sorry'),
    ('politeness', 9, 'Type the informal question “Can you help?”', 'free_text', '{}', 'Kun je helpen?'),
    ('politeness', 10, 'Type the reply “You are welcome.”', 'free_text', '{}', 'Graag gedaan.'),
    ('numbers', 1, 'Which Dutch word means “one”?', 'multiple_choice', '{"options":["een","twee","drie"]}', 'een'),
    ('numbers', 2, 'Which Dutch word means “two”?', 'multiple_choice', '{"options":["tien","twee","honderd"]}', 'twee'),
    ('numbers', 3, 'Which Dutch word means “ten”?', 'multiple_choice', '{"options":["drie","honderd","tien"]}', 'tien'),
    ('numbers', 4, 'Which Dutch word means “one hundred”?', 'multiple_choice', '{"options":["honderd","tien","een"]}', 'honderd'),
    ('numbers', 5, 'Put “I have two tickets and ten euros” in order.', 'word_ordering', '{"tokens":["euro","kaartjes","Ik","twee","en","tien","heb"]}', '["Ik","heb","twee","kaartjes","en","tien","euro"]'),
    ('numbers', 6, 'Put “The train leaves at three o’clock” in order.', 'word_ordering', '{"tokens":["drie","De","uur","om","trein","vertrekt"]}', '["De","trein","vertrekt","om","drie","uur"]'),
    ('numbers', 7, 'Put “I have ten euros” in order.', 'word_ordering', '{"tokens":["euro","heb","Ik","tien"]}', '["Ik","heb","tien","euro"]'),
    ('numbers', 8, 'Type the Dutch word for “three”.', 'free_text', '{}', 'drie'),
    ('numbers', 9, 'Type the Dutch number twenty-three as one word.', 'free_text', '{}', 'drieëntwintig'),
    ('numbers', 10, 'Type the Dutch word for “one hundred”.', 'free_text', '{}', 'honderd'),
    ('family', 1, 'Which phrase means “the family”?', 'multiple_choice', '{"options":["de familie","de moeder","de broer"]}', 'de familie'),
    ('family', 2, 'Which phrase means “the mother”?', 'multiple_choice', '{"options":["de vader","de moeder","de zus"]}', 'de moeder'),
    ('family', 3, 'Which phrase means “the father”?', 'multiple_choice', '{"options":["de vader","de broer","de familie"]}', 'de vader'),
    ('family', 4, 'Which phrase means “the brother”?', 'multiple_choice', '{"options":["de zus","de moeder","de broer"]}', 'de broer'),
    ('family', 5, 'Put “This is my mother and my father” in order.', 'word_ordering', '{"tokens":["vader","Dit","moeder","mijn","en","is","mijn"]}', '["Dit","is","mijn","moeder","en","mijn","vader"]'),
    ('family', 6, 'Put “My sister has a brother” in order.', 'word_ordering', '{"tokens":["broer","Mijn","een","heeft","zus"]}', '["Mijn","zus","heeft","een","broer"]'),
    ('family', 7, 'Put “Do you have a brother?” in order.', 'word_ordering', '{"tokens":["broer","je","Heb","een"]}', '["Heb","je","een","broer"]'),
    ('family', 8, 'Type the Dutch phrase for “the sister”.', 'free_text', '{}', 'de zus'),
    ('family', 9, 'Type the question “Is this your family?”', 'free_text', '{}', 'Is dit jouw familie?'),
    ('family', 10, 'Complete the reply: “No, but I have a sister.”', 'free_text', '{}', 'Nee, maar ik heb een zus.'),
    ('food', 1, 'Which phrase means “the bread”?', 'multiple_choice', '{"options":["het brood","de kaas","de appel"]}', 'het brood'),
    ('food', 2, 'Which phrase means “the cheese”?', 'multiple_choice', '{"options":["de appel","de kaas","het ontbijt"]}', 'de kaas'),
    ('food', 3, 'Which phrase means “the apple”?', 'multiple_choice', '{"options":["de appel","het brood","lekker"]}', 'de appel'),
    ('food', 4, 'Which word means “tasty”?', 'multiple_choice', '{"options":["ontbijt","lekker","brood"]}', 'lekker'),
    ('food', 5, 'Put “I eat bread and cheese for breakfast” in order.', 'word_ordering', '{"tokens":["ontbijt","brood","Ik","als","kaas","eet","en"]}', '["Ik","eet","brood","en","kaas","als","ontbijt"]'),
    ('food', 6, 'Put “The apple is tasty” in order.', 'word_ordering', '{"tokens":["lekker","appel","De","is"]}', '["De","appel","is","lekker"]'),
    ('food', 7, 'Put “What do you eat for breakfast?” in order.', 'word_ordering', '{"tokens":["ontbijt","je","Wat","als","eet"]}', '["Wat","eet","je","als","ontbijt"]'),
    ('food', 8, 'Type the Dutch phrase for “breakfast”.', 'free_text', '{}', 'het ontbijt'),
    ('food', 9, 'Type the sentence “I eat bread and cheese.”', 'free_text', '{}', 'Ik eet brood en kaas.'),
    ('food', 10, 'Type the reply “The apple is tasty!”', 'free_text', '{}', 'De appel is lekker!'),
    ('drinks', 1, 'Which phrase means “the water”?', 'multiple_choice', '{"options":["het water","de koffie","de thee"]}', 'het water'),
    ('drinks', 2, 'Which phrase means “the coffee”?', 'multiple_choice', '{"options":["de thee","de koffie","het bier"]}', 'de koffie'),
    ('drinks', 3, 'Which phrase means “the tea”?', 'multiple_choice', '{"options":["de thee","een glas","het water"]}', 'de thee'),
    ('drinks', 4, 'Which phrase means “a glass”?', 'multiple_choice', '{"options":["het bier","het water","een glas"]}', 'een glas'),
    ('drinks', 5, 'Put “I would like a glass of water” in order.', 'word_ordering', '{"tokens":["water","graag","Ik","glas","een","wil"]}', '["Ik","wil","graag","een","glas","water"]'),
    ('drinks', 6, 'Put “The coffee is hot and the tea is warm” in order.', 'word_ordering', '{"tokens":["warm","koffie","thee","De","heet","de","is","en","is"]}', '["De","koffie","is","heet","en","de","thee","is","warm"]'),
    ('drinks', 7, 'Put “Do you want coffee or tea?” in order.', 'word_ordering', '{"tokens":["thee","koffie","je","of","Wil"]}', '["Wil","je","koffie","of","thee"]'),
    ('drinks', 8, 'Type the Dutch phrase for “the beer”.', 'free_text', '{}', 'het bier'),
    ('drinks', 9, 'Type the request “A glass of water, please.”', 'free_text', '{}', 'Een glas water, alsjeblieft.'),
    ('drinks', 10, 'Type the sentence “The coffee is too hot.”', 'free_text', '{}', 'De koffie is te heet.'),
    ('home', 1, 'Which phrase means “the house”?', 'multiple_choice', '{"options":["het huis","de kamer","de keuken"]}', 'het huis'),
    ('home', 2, 'Which phrase means “the apartment”?', 'multiple_choice', '{"options":["de sleutel","het appartement","het huis"]}', 'het appartement'),
    ('home', 3, 'Which phrase means “the room”?', 'multiple_choice', '{"options":["de kamer","de keuken","de sleutel"]}', 'de kamer'),
    ('home', 4, 'Which phrase means “the key”?', 'multiple_choice', '{"options":["het huis","de sleutel","het appartement"]}', 'de sleutel'),
    ('home', 5, 'Put “The kitchen is in the apartment” in order.', 'word_ordering', '{"tokens":["appartement","keuken","het","De","in","is"]}', '["De","keuken","is","in","het","appartement"]'),
    ('home', 6, 'Put “The key is in the kitchen” in order.', 'word_ordering', '{"tokens":["keuken","sleutel","de","De","in","is"]}', '["De","sleutel","is","in","de","keuken"]'),
    ('home', 7, 'Put “My room is in the apartment” in order.', 'word_ordering', '{"tokens":["appartement","Mijn","in","kamer","het","is"]}', '["Mijn","kamer","is","in","het","appartement"]'),
    ('home', 8, 'Type the Dutch phrase for “the kitchen”.', 'free_text', '{}', 'de keuken'),
    ('home', 9, 'Type the question “Where is the key?”', 'free_text', '{}', 'Waar is de sleutel?'),
    ('home', 10, 'Type the short location “On the table.”', 'free_text', '{}', 'Op tafel.'),
    ('travel', 1, 'Which phrase means “the train station”?', 'multiple_choice', '{"options":["het station","de luchthaven","het kaartje"]}', 'het station'),
    ('travel', 2, 'Which phrase means “the airport”?', 'multiple_choice', '{"options":["de koffer","de luchthaven","het station"]}', 'de luchthaven'),
    ('travel', 3, 'Which phrase means “the ticket”?', 'multiple_choice', '{"options":["het kaartje","de koffer","de luchthaven"]}', 'het kaartje'),
    ('travel', 4, 'Which verb means “to depart”?', 'multiple_choice', '{"options":["vertrekken","kopen","leren"]}', 'vertrekken'),
    ('travel', 5, 'Put “The station is near the airport” in order.', 'word_ordering', '{"tokens":["luchthaven","station","de","Het","bij","ligt"]}', '["Het","station","ligt","bij","de","luchthaven"]'),
    ('travel', 6, 'Put “My ticket is in the suitcase” in order.', 'word_ordering', '{"tokens":["koffer","Mijn","de","kaartje","in","zit"]}', '["Mijn","kaartje","zit","in","de","koffer"]'),
    ('travel', 7, 'Put “What time does the train leave?” in order.', 'word_ordering', '{"tokens":["trein","laat","de","Hoe","vertrekt"]}', '["Hoe","laat","vertrekt","de","trein"]'),
    ('travel', 8, 'Type the Dutch phrase for “the suitcase”.', 'free_text', '{}', 'de koffer'),
    ('travel', 9, 'Type the answer “At three o’clock, at the station.”', 'free_text', '{}', 'Om drie uur, op het station.'),
    ('travel', 10, 'Type the sentence “Your ticket is in the suitcase.”', 'free_text', '{}', 'Jouw kaartje zit in de koffer.'),
    ('directions', 1, 'Which word means “left”?', 'multiple_choice', '{"options":["links","rechts","rechtdoor"]}', 'links'),
    ('directions', 2, 'Which word means “right”?', 'multiple_choice', '{"options":["rechtdoor","rechts","links"]}', 'rechts'),
    ('directions', 3, 'Which word means “straight ahead”?', 'multiple_choice', '{"options":["rechtdoor","links","straat"]}', 'rechtdoor'),
    ('directions', 4, 'Which phrase means “the street”?', 'multiple_choice', '{"options":["de straat","het hotel","de sleutel"]}', 'de straat'),
    ('directions', 5, 'Put “Go straight ahead and then left” in order.', 'word_ordering', '{"tokens":["links","rechtdoor","naar","Ga","dan","en"]}', '["Ga","rechtdoor","en","dan","naar","links"]'),
    ('directions', 6, 'Put “Go straight ahead first and then left” in order.', 'word_ordering', '{"tokens":["links","eerst","dan","rechtdoor","Ga","naar","en"]}', '["Ga","eerst","rechtdoor","en","dan","naar","links"]'),
    ('directions', 7, 'Put “Where is the street to the hotel?” in order.', 'word_ordering', '{"tokens":["hotel","straat","het","Waar","de","naar","is"]}', '["Waar","is","de","straat","naar","het","hotel"]'),
    ('directions', 8, 'Type the question pattern “Where is …?”', 'free_text', '{}', 'Waar is ...?'),
    ('directions', 9, 'Type the answer “It is on the right.”', 'free_text', '{}', 'Het is rechts.'),
    ('directions', 10, 'Type the phrase “to the left”.', 'free_text', '{}', 'naar links'),
    ('time-calendar', 1, 'Which word means “today”?', 'multiple_choice', '{"options":["vandaag","morgen","gisteren"]}', 'vandaag'),
    ('time-calendar', 2, 'Which word means “tomorrow”?', 'multiple_choice', '{"options":["maandag","morgen","vandaag"]}', 'morgen'),
    ('time-calendar', 3, 'Which word means “yesterday”?', 'multiple_choice', '{"options":["gisteren","morgen","de klok"]}', 'gisteren'),
    ('time-calendar', 4, 'Which word is the Dutch name for Monday?', 'multiple_choice', '{"options":["vandaag","maandag","morgen"]}', 'maandag'),
    ('time-calendar', 5, 'Put “Today is Monday” in order.', 'word_ordering', '{"tokens":["maandag","Vandaag","het","is"]}', '["Vandaag","is","het","maandag"]'),
    ('time-calendar', 6, 'Put “The lesson begins at eight o’clock” in order.', 'word_ordering', '{"tokens":["uur","les","acht","De","om","begint"]}', '["De","les","begint","om","acht","uur"]'),
    ('time-calendar', 7, 'Put “Tomorrow I work” in order.', 'word_ordering', '{"tokens":["ik","Morgen","werk"]}', '["Morgen","werk","ik"]'),
    ('time-calendar', 8, 'Type the Dutch phrase for “the clock”.', 'free_text', '{}', 'de klok'),
    ('time-calendar', 9, 'Type the question “What day is it today?”', 'free_text', '{}', 'Welke dag is het vandaag?'),
    ('time-calendar', 10, 'Type the answer “Tomorrow at eight o’clock.”', 'free_text', '{}', 'Morgen om acht uur.'),
    ('weather', 1, 'Which word means “sunny”?', 'multiple_choice', '{"options":["zonnig","regenachtig","koud"]}', 'zonnig'),
    ('weather', 2, 'Which word means “rainy”?', 'multiple_choice', '{"options":["warm","regenachtig","zonnig"]}', 'regenachtig'),
    ('weather', 3, 'Which word means “cold”?', 'multiple_choice', '{"options":["koud","warm","wind"]}', 'koud'),
    ('weather', 4, 'Which word means “warm”?', 'multiple_choice', '{"options":["zonnig","koud","warm"]}', 'warm'),
    ('weather', 5, 'Put “Today it is sunny but cold” in order.', 'word_ordering', '{"tokens":["koud","zonnig","Vandaag","maar","het","is"]}', '["Vandaag","is","het","zonnig","maar","koud"]'),
    ('weather', 6, 'Put “Tomorrow it will become rainy and windy” in order.', 'word_ordering', '{"tokens":["winderig","Morgen","regenachtig","wordt","en","het"]}', '["Morgen","wordt","het","regenachtig","en","winderig"]'),
    ('weather', 7, 'Put “How is the weather today?” in order.', 'word_ordering', '{"tokens":["weer","vandaag","het","is","Hoe"]}', '["Hoe","is","het","weer","vandaag"]'),
    ('weather', 8, 'Type the Dutch phrase for “the wind”.', 'free_text', '{}', 'de wind'),
    ('weather', 9, 'Type the sentence “It is sunny, but cold.”', 'free_text', '{}', 'Het is zonnig, maar koud.'),
    ('weather', 10, 'Type the phrase “very windy”.', 'free_text', '{}', 'heel winderig'),
    ('shopping', 1, 'Which verb means “to buy”?', 'multiple_choice', '{"options":["kopen","leren","vertrekken"]}', 'kopen'),
    ('shopping', 2, 'Which word means “expensive”?', 'multiple_choice', '{"options":["goedkoop","duur","prijs"]}', 'duur'),
    ('shopping', 3, 'Which word means “cheap”?', 'multiple_choice', '{"options":["duur","maat","goedkoop"]}', 'goedkoop'),
    ('shopping', 4, 'Which phrase means “the size”?', 'multiple_choice', '{"options":["de prijs","de maat","de jas"]}', 'de maat'),
    ('shopping', 5, 'Put “How much does this coat cost?” in order.', 'word_ordering', '{"tokens":["jas","deze","kost","Hoeveel"]}', '["Hoeveel","kost","deze","jas"]'),
    ('shopping', 6, 'Put “I want to buy this coat” in order.', 'word_ordering', '{"tokens":["kopen","jas","wil","deze","Ik"]}', '["Ik","wil","deze","jas","kopen"]'),
    ('shopping', 7, 'Put “The price is twenty euros” in order.', 'word_ordering', '{"tokens":["euro","prijs","twintig","De","is"]}', '["De","prijs","is","twintig","euro"]'),
    ('shopping', 8, 'Type the Dutch phrase for “the price”.', 'free_text', '{}', 'de prijs'),
    ('shopping', 9, 'Type the reaction “That is cheap!”', 'free_text', '{}', 'Dat is goedkoop!'),
    ('shopping', 10, 'Type the polite question “Do you have my size?”', 'free_text', '{}', 'Heeft u mijn maat?'),
    ('work-school', 1, 'Which phrase means “the work”?', 'multiple_choice', '{"options":["het werk","de school","het kantoor"]}', 'het werk'),
    ('work-school', 2, 'Which phrase means “the school”?', 'multiple_choice', '{"options":["de leraar","de school","het werk"]}', 'de school'),
    ('work-school', 3, 'Which phrase means “the teacher”?', 'multiple_choice', '{"options":["de leraar","het kantoor","de school"]}', 'de leraar'),
    ('work-school', 4, 'Which verb means “to learn”?', 'multiple_choice', '{"options":["werken","leren","kopen"]}', 'leren'),
    ('work-school', 5, 'Put “I learn Dutch at school” in order.', 'word_ordering', '{"tokens":["school","Nederlands","Ik","op","leer"]}', '["Ik","leer","Nederlands","op","school"]'),
    ('work-school', 6, 'Put “The teacher works in an office” in order.', 'word_ordering', '{"tokens":["kantoor","docent","op","De","werkt"]}', '["De","docent","werkt","op","kantoor"]'),
    ('work-school', 7, 'Put “I work at the office” in order.', 'word_ordering', '{"tokens":["kantoor","werk","op","Ik"]}', '["Ik","werk","op","kantoor"]'),
    ('work-school', 8, 'Type the Dutch phrase for “the office”.', 'free_text', '{}', 'het kantoor'),
    ('work-school', 9, 'Type the question “What do you do at school?”', 'free_text', '{}', 'Wat doe je op school?'),
    ('work-school', 10, 'Type the sentence “My teacher is nice.”', 'free_text', '{}', 'Mijn leraar is aardig.'),
    ('body-health', 1, 'Which phrase means “the head”?', 'multiple_choice', '{"options":["het hoofd","de hand","de dokter"]}', 'het hoofd'),
    ('body-health', 2, 'Which phrase means “the hand”?', 'multiple_choice', '{"options":["de dokter","de hand","het hoofd"]}', 'de hand'),
    ('body-health', 3, 'Which phrase means “the doctor”?', 'multiple_choice', '{"options":["de dokter","de hand","het hoofd"]}', 'de dokter'),
    ('body-health', 4, 'Which word means “ill”?', 'multiple_choice', '{"options":["ziek","blij","moe"]}', 'ziek'),
    ('body-health', 5, 'Put “My head hurts” in order.', 'word_ordering', '{"tokens":["pijn","hoofd","Mijn","doet"]}', '["Mijn","hoofd","doet","pijn"]'),
    ('body-health', 6, 'Put “The doctor examines my hand” in order.', 'word_ordering', '{"tokens":["hand","dokter","mijn","De","onderzoekt"]}', '["De","dokter","onderzoekt","mijn","hand"]'),
    ('body-health', 7, 'Put “Go to the doctor” in order.', 'word_ordering', '{"tokens":["dokter","naar","Ga","de"]}', '["Ga","naar","de","dokter"]'),
    ('body-health', 8, 'Type the Dutch phrase for “It hurts”.', 'free_text', '{}', 'Het doet pijn'),
    ('body-health', 9, 'Type the sentence “I am ill.”', 'free_text', '{}', 'Ik ben ziek.'),
    ('body-health', 10, 'Type the question “How are you?”', 'free_text', '{}', 'Hoe gaat het?'),
    ('emotions', 1, 'Which word means “happy”?', 'multiple_choice', '{"options":["blij","verdrietig","moe"]}', 'blij'),
    ('emotions', 2, 'Which word means “sad”?', 'multiple_choice', '{"options":["opgewonden","verdrietig","blij"]}', 'verdrietig'),
    ('emotions', 3, 'Which word means “tired”?', 'multiple_choice', '{"options":["bang","blij","moe"]}', 'moe'),
    ('emotions', 4, 'Which word means “excited”?', 'multiple_choice', '{"options":["verdrietig","opgewonden","moe"]}', 'opgewonden'),
    ('emotions', 5, 'Put “I am happy, but tired” in order.', 'word_ordering', '{"tokens":["moe","blij","Ik","maar","ben"]}', '["Ik","ben","blij","maar","moe"]'),
    ('emotions', 6, 'Put “She is afraid of the dog” in order.', 'word_ordering', '{"tokens":["hond","bang","Zij","de","voor","is"]}', '["Zij","is","bang","voor","de","hond"]'),
    ('emotions', 7, 'Put “How do you feel?” in order.', 'word_ordering', '{"tokens":["je","Hoe","je","voel"]}', '["Hoe","voel","je","je"]'),
    ('emotions', 8, 'Type the Dutch phrase for “to be afraid”.', 'free_text', '{}', 'bang zijn'),
    ('emotions', 9, 'Type the question “Why is your sister sad?”', 'free_text', '{}', 'Waarom is je zus verdrietig?'),
    ('emotions', 10, 'Type the sentence “I am very happy, but tired.”', 'free_text', '{}', 'Ik ben heel blij, maar moe.'),
    ('hobbies', 1, 'Which verb means “to read”?', 'multiple_choice', '{"options":["lezen","koken","dansen"]}', 'lezen'),
    ('hobbies', 2, 'Which phrase means “to listen to music”?', 'multiple_choice', '{"options":["naar muziek luisteren","sporten","koken"]}', 'naar muziek luisteren'),
    ('hobbies', 3, 'Which verb means “to cook”?', 'multiple_choice', '{"options":["dansen","koken","lezen"]}', 'koken'),
    ('hobbies', 4, 'Which verb means “to dance”?', 'multiple_choice', '{"options":["sporten","lezen","dansen"]}', 'dansen'),
    ('hobbies', 5, 'Put “I like reading and listen to music” in order.', 'word_ordering', '{"tokens":["muziek","graag","naar","Ik","en","lees","luister"]}', '["Ik","lees","graag","en","luister","naar","muziek"]'),
    ('hobbies', 6, 'Put “At the weekend I exercise and dance” in order.', 'word_ordering', '{"tokens":["dans","weekend","sport","het","en","In","ik","ik"]}', '["In","het","weekend","sport","ik","en","dans","ik"]'),
    ('hobbies', 7, 'Put “I exercise and like to dance” in order.', 'word_ordering', '{"tokens":["graag","dans","sport","Ik","en"]}', '["Ik","sport","en","dans","graag"]'),
    ('hobbies', 8, 'Type the Dutch verb for “to do sport”.', 'free_text', '{}', 'sporten'),
    ('hobbies', 9, 'Type the question “What do you like to do at the weekend?”', 'free_text', '{}', 'Wat doe je graag in het weekend?'),
    ('hobbies', 10, 'Type the phrase required after luisteren: “to music”.', 'free_text', '{}', 'naar muziek'),
    ('nature-animals', 1, 'Which phrase means “the dog”?', 'multiple_choice', '{"options":["de hond","de kat","de vogel"]}', 'de hond'),
    ('nature-animals', 2, 'Which phrase means “the cat”?', 'multiple_choice', '{"options":["de boom","de kat","het bos"]}', 'de kat'),
    ('nature-animals', 3, 'Which phrase means “the tree”?', 'multiple_choice', '{"options":["de boom","het bos","de vogel"]}', 'de boom'),
    ('nature-animals', 4, 'Which phrase means “the forest”?', 'multiple_choice', '{"options":["de hond","de boom","het bos"]}', 'het bos'),
    ('nature-animals', 5, 'Put “The dog walks in the woods” in order.', 'word_ordering', '{"tokens":["bos","loopt","hond","het","De","in"]}', '["De","hond","loopt","in","het","bos"]'),
    ('nature-animals', 6, 'Put “A bird sits in the tree” in order.', 'word_ordering', '{"tokens":["boom","vogel","de","Een","in","zit"]}', '["Een","vogel","zit","in","de","boom"]'),
    ('nature-animals', 7, 'Put “The bird is in the forest” in order.', 'word_ordering', '{"tokens":["bos","vogel","het","De","in","is"]}', '["De","vogel","is","in","het","bos"]'),
    ('nature-animals', 8, 'Type the Dutch phrase for “the bird”.', 'free_text', '{}', 'de vogel'),
    ('nature-animals', 9, 'Type the sentence “The dog walks in the woods.”', 'free_text', '{}', 'De hond loopt in het bos.'),
    ('nature-animals', 10, 'Type the sentence “A bird sits in the tree.”', 'free_text', '{}', 'Een vogel zit in de boom.'),
    ('long-words', 1, 'Which word means “multiple personality disorder”?', 'multiple_choice', '{"options":["meervoudigepersoonlijkheidsstoornis","arbeidsongeschiktheidsverzekering","aansprakelijkheidsverzekering"]}', 'meervoudigepersoonlijkheidsstoornis'),
    ('long-words', 2, 'Which word means “disability insurance”?', 'multiple_choice', '{"options":["kindercarnavalsoptocht","arbeidsongeschiktheidsverzekering","hottentottententententoonstelling"]}', 'arbeidsongeschiktheidsverzekering'),
    ('long-words', 3, 'Which word means “liability insurance”?', 'multiple_choice', '{"options":["aansprakelijkheidsverzekering","arbeidsongeschiktheidsverzekering","kindercarnavalsoptocht"]}', 'aansprakelijkheidsverzekering'),
    ('long-words', 4, 'Which word means “children’s carnival parade”?', 'multiple_choice', '{"options":["hottentottententententoonstelling","meervoudigepersoonlijkheidsstoornis","kindercarnavalsoptocht"]}', 'kindercarnavalsoptocht'),
    ('long-words', 5, 'Put “Multiple personality disorder is a long word” in order.', 'word_ordering', '{"tokens":["woord","lang","Meervoudigepersoonlijkheidsstoornis","een","is"]}', '["Meervoudigepersoonlijkheidsstoornis","is","een","lang","woord"]'),
    ('long-words', 6, 'Put “The disability insurance helps during illness” in order.', 'word_ordering', '{"tokens":["ziekte","helpt","arbeidsongeschiktheidsverzekering","bij","De"]}', '["De","arbeidsongeschiktheidsverzekering","helpt","bij","ziekte"]'),
    ('long-words', 7, 'Put “The children’s carnival parade is long” in order.', 'word_ordering', '{"tokens":["lang","kindercarnavalsoptocht","De","is"]}', '["De","kindercarnavalsoptocht","is","lang"]'),
    ('long-words', 8, 'Type the Dutch word for “liability insurance”.', 'free_text', '{}', 'aansprakelijkheidsverzekering'),
    ('long-words', 9, 'Type the Dutch word for “children’s carnival parade”.', 'free_text', '{}', 'kindercarnavalsoptocht'),
    ('long-words', 10, 'Type the playful classic Dutch compound from the vocabulary.', 'free_text', '{}', 'hottentottententententoonstelling'),
    ('funny-unusual-words', 1, 'Which word describes something cozy and sociable?', 'multiple_choice', '{"options":["gezellig","uitwaaien","niksen"]}', 'gezellig'),
    ('funny-unusual-words', 2, 'Which verb means clearing your head in the wind?', 'multiple_choice', '{"options":["uitbuiken","uitwaaien","niksen"]}', 'uitwaaien'),
    ('funny-unusual-words', 3, 'Which word means anticipatory enjoyment?', 'multiple_choice', '{"options":["voorpret","gezellig","uitbuiken"]}', 'voorpret'),
    ('funny-unusual-words', 4, 'Which verb means deliberately doing nothing?', 'multiple_choice', '{"options":["uitwaaien","voorpret","niksen"]}', 'niksen'),
    ('funny-unusual-words', 5, 'Put “At our home it is cozy and sociable” in order.', 'word_ordering', '{"tokens":["gezellig","thuis","het","Bij","ons","is"]}', '["Bij","ons","thuis","is","het","gezellig"]'),
    ('funny-unusual-words', 6, 'Put “After eating we go get fresh air on the beach” in order.', 'word_ordering', '{"tokens":["strand","gaan","eten","uitwaaien","we","het","Na","op","het"]}', '["Na","het","eten","gaan","we","uitwaaien","op","het","strand"]'),
    ('funny-unusual-words', 7, 'Put “We enjoy the anticipation” in order.', 'word_ordering', '{"tokens":["voorpret","genieten","de","We","van"]}', '["We","genieten","van","de","voorpret"]'),
    ('funny-unusual-words', 8, 'Type the verb for relaxing after a big meal.', 'free_text', '{}', 'uitbuiken'),
    ('funny-unusual-words', 9, 'Type the verb for deliberately doing nothing.', 'free_text', '{}', 'niksen'),
    ('funny-unusual-words', 10, 'Type the sentence “At our home it is cozy and sociable.”', 'free_text', '{}', 'Bij ons thuis is het gezellig.')
)
INSERT OR IGNORE INTO QuizQuestions (QuizId, SortOrder, Content, Type, QuestionData, CorrectAnswer)
SELECT q.Id, s.SortOrder, s.Content, s.Type, s.QuestionData, s.CorrectAnswer
FROM QuestionSeeds s
INNER JOIN Courses c ON c.Code = 'nl'
INNER JOIN Lessons l ON l.CourseId = c.Id AND l.Slug = s.LessonSlug
INNER JOIN Quizzes q ON q.LessonId = l.Id;
