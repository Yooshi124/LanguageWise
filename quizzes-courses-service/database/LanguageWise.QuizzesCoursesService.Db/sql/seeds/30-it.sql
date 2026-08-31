-- Idempotent Italian lesson, vocabulary, and quiz seed data.
-- Requires schema.sql followed by seeds/00-courses.sql.

-- Shared lesson catalogue metadata, paired only with Italian lesson Markdown.
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
    ('it', 'greetings', '## Learn in context

| Target language | English |
| --- | --- |
| Ciao, Marta! Benvenuta a Roma. | Hi, Marta! Welcome to Rome. |
| Buongiorno, signore. | Good morning, sir. |

## Mini dialogue

> **A:** Ciao, Marta! Benvenuta qui a Roma.
> *Hi, Marta! Welcome here to Rome.*
>
> **B:** Buongiorno! Grazie mille.
> *Good morning! Thanks a lot.*
>
> **A:** Sei pronta?
> *Are you ready?*
>
> **B:** Sì!
> *Yes!*

## Language note

**Ciao** is informal and means both hello and goodbye. Use **buongiorno** in formal daytime encounters.'),
    ('it', 'introductions', '## Learn in context

| Target language | English |
| --- | --- |
| Mi chiamo Luca e vengo dall’Italia. | My name is Luca and I come from Italy. |
| Come ti chiami? — Mi chiamo Sara. | What is your name? — My name is Sara. |

## Mini dialogue

> **A:** Ciao! Mi chiamo Luca. Come ti chiami?
> *Hi! My name is Luca. What is your name?*
>
> **B:** Mi chiamo Sara. Piacere!
> *My name is Sara. Nice to meet you!*
>
> **A:** Vengo dall’Italia. E tu?
> *I come from Italy. And you?*
>
> **B:** Vengo dalla Spagna.
> *I come from Spain.*

## Language note

Italian often omits the subject pronoun: **Mi chiamo** already means “I am called.”'),
    ('it', 'politeness', '## Learn in context

| Target language | English |
| --- | --- |
| Per favore, un caffè. Grazie! | A coffee, please. Thank you! |
| Scusi, può aiutarmi? | Excuse me, can you help me? |

## Mini dialogue

> **A:** Scusi, può aiutarmi, per favore?
> *Excuse me, can you help me, please?*
>
> **B:** Certo, prego.
> *Of course, go ahead.*
>
> **A:** Un caffè, per favore. Grazie!
> *A coffee, please. Thank you!*
>
> **B:** Prego!
> *You’re welcome!*

## Language note

Use **scusi** with a stranger and **scusa** with a friend. **Prego** can answer “thank you.”'),
    ('it', 'numbers', '## Learn in context

| Target language | English |
| --- | --- |
| Ho due biglietti e dieci euro. | I have two tickets and ten euros. |
| Il treno parte alle tre. | The train leaves at three. |

## Worked usage

- **Ho due biglietti e dieci euro.** — *I have two tickets and ten euros.*
- **Il treno parte alle tre.** — *The train leaves at three.*

## Language note

Italian joins *a + le* into **alle** for clock time: *alle tre*.'),
    ('it', 'family', '## Learn in context

| Target language | English |
| --- | --- |
| Questa è mia madre e questo è mio padre. | This is my mother and this is my father. |
| Mia sorella ha un fratello. | My sister has a brother. |

## Mini dialogue

> **A:** Questa è la tua famiglia?
> *Is this your family?*
>
> **B:** Sì. Questa è mia madre e questo è mio padre.
> *Yes. This is my mother and this is my father.*
>
> **A:** Hai un fratello?
> *Do you have a brother?*
>
> **B:** No, ma ho una sorella.
> *No, but I have a sister.*

## Language note

With close singular family, Italian often drops the article: **mia madre**.'),
    ('it', 'food', '## Learn in context

| Target language | English |
| --- | --- |
| A colazione mangio pane e formaggio. | For breakfast I eat bread and cheese. |
| La mela è deliziosa. | The apple is delicious. |

## Mini dialogue

> **A:** Cosa mangi a colazione?
> *What do you eat for breakfast?*
>
> **B:** Mangio pane e formaggio.
> *I eat bread and cheese.*
>
> **A:** E la mela?
> *And the apple?*
>
> **B:** La mela è buona!
> *The apple is good!*

## Language note

**Colazione** is breakfast; *pranzo* is lunch and *cena* is dinner.'),
    ('it', 'drinks', '## Learn in context

| Target language | English |
| --- | --- |
| Vorrei un bicchiere d’acqua. | I would like a glass of water. |
| Il caffè è caldo; il tè è tiepido. | The coffee is hot; the tea is lukewarm. |

## Mini dialogue

> **A:** Vuoi un caffè o un tè?
> *Do you want a coffee or a tea?*
>
> **B:** Un bicchiere d’acqua, per favore. Il caffè è troppo caldo.
> *A glass of water, please. The coffee is too hot.*

## Language note

**Vorrei** is the polite conditional of *volere*, useful when ordering.'),
    ('it', 'home', '## Learn in context

| Target language | English |
| --- | --- |
| La cucina è nell’appartamento. | The kitchen is in the apartment. |
| Dov’è la chiave? — Sul tavolo. | Where is the key? — On the table. |

## Mini dialogue

> **A:** Dov’è la chiave?
> *Where is the key?*
>
> **B:** La chiave è in cucina.
> *The key is in the kitchen.*
>
> **A:** E la tua stanza?
> *And your room?*
>
> **B:** La mia stanza è nell’appartamento.
> *My room is in the apartment.*

## Language note

Italian combines prepositions and articles: **nel** = *in + il*, **sul** = *su + il*.'),
    ('it', 'travel', '## Learn in context

| Target language | English |
| --- | --- |
| La stazione è vicino all’aeroporto. | The station is near the airport. |
| Il biglietto è nella valigia. | The ticket is in the suitcase. |

## Mini dialogue

> **A:** A che ora parte il treno?
> *At what time does the train leave?*
>
> **B:** Alle tre, dalla stazione.
> *At three, from the station.*
>
> **A:** Dov’è il mio biglietto?
> *Where is my ticket?*
>
> **B:** Il tuo biglietto è nella valigia.
> *Your ticket is in the suitcase.*

## Language note

**Biglietto** can be a transport ticket, a theatre ticket, or a small note.'),
    ('it', 'directions', '## Learn in context

| Target language | English |
| --- | --- |
| Vai sempre dritto, poi gira a sinistra. | Go straight ahead, then turn left. |
| Dov’è la strada? — A destra della banca. | Where is the street? — To the right of the bank. |

## Mini dialogue

> **A:** Scusi, dov’è la strada per la banca?
> *Excuse me, where is the street to the bank?*
>
> **B:** Vai sempre dritto, poi gira subito a sinistra.
> *Go straight ahead, then turn left right away.*
>
> **A:** E la banca?
> *And the bank?*
>
> **B:** È a destra.
> *It is on the right.*

## Language note

**Gira** is informal singular; *giri* is the polite command.'),
    ('it', 'time-calendar', '## Learn in context

| Target language | English |
| --- | --- |
| Oggi è lunedì; domani è martedì. | Today is Monday; tomorrow is Tuesday. |
| La lezione inizia alle otto. | The lesson starts at eight. |

## Mini dialogue

> **A:** Che giorno è oggi?
> *What day is today?*
>
> **B:** Oggi è lunedì.
> *Today is Monday.*
>
> **A:** A che ora inizia la lezione?
> *At what time does the lesson start?*
>
> **B:** Domani alle otto.
> *Tomorrow at eight.*

## Language note

Italian days are not capitalised and often end in accented *-ì*: **lunedì**.'),
    ('it', 'weather', '## Learn in context

| Target language | English |
| --- | --- |
| Oggi c’è il sole, ma fa freddo. | Today it is sunny, but it is cold. |
| Domani sarà nuvoloso e ventoso. | Tomorrow it will be cloudy and windy. |

## Mini dialogue

> **A:** Che tempo fa oggi?
> *What is the weather today?*
>
> **B:** C’è il sole, ma fa freddo.
> *It is sunny, but it is cold.*
>
> **A:** E domani?
> *And tomorrow?*
>
> **B:** Domani sarà caldo e soleggiato.
> *Tomorrow it will be warm and sunny.*

## Language note

Italian says **fa caldo/freddo** and **c’è il sole** (“there is the sun”).'),
    ('it', 'shopping', '## Learn in context

| Target language | English |
| --- | --- |
| Quanto costa questa maglietta? | How much does this T-shirt cost? |
| Costa venti euro; la taglia è piccola. | It costs twenty euros; the size is small. |

## Mini dialogue

> **A:** Vorrei comprare questa maglietta. Qual è il prezzo?
> *I would like to buy this T-shirt. What is the price?*
>
> **B:** Costa venti euro.
> *It costs twenty euros.*
>
> **A:** Non è costoso! Avete la mia taglia?
> *It’s not expensive! Do you have my size?*
>
> **B:** Sì, certo.
> *Yes, of course.*

## Language note

Use **comprare** for “to buy” and **prezzo** for price. A shop assistant may ask **Che taglia porta?**—“What size do you wear?”'),
    ('it', 'work-school', '## Learn in context

| Target language | English |
| --- | --- |
| Studio italiano a scuola. | I study Italian at school. |
| L’insegnante lavora in ufficio. | The teacher works in an office. |

## Mini dialogue

> **A:** Cosa fai a scuola?
> *What do you do at school?*
>
> **B:** Imparo l’italiano. La mia insegnante è brava.
> *I learn Italian. My teacher is good.*
>
> **A:** E dov’è il tuo lavoro?
> *And where is your work?*
>
> **B:** Lavoro in ufficio.
> *I work in an office.*

## Language note

**Studio** can mean “I study” and “study/office” as a noun; context separates them.'),
    ('it', 'body-health', '## Learn in context

| Target language | English |
| --- | --- |
| Mi fa male la testa; sono malato. | My head hurts; I am ill. |
| Il medico guarda la mia mano. | The doctor looks at my hand. |

## Mini dialogue

> **A:** Come stai?
> *How are you?*
>
> **B:** Non bene. Mi fa male la testa. Sono malato.
> *Not well. My head hurts. I am ill.*
>
> **A:** Vai dal medico!
> *Go to the doctor!*
>
> **B:** Sì, il medico guarda anche la mia mano.
> *Yes, the doctor is also looking at my hand.*

## Language note

**Mi fa male** literally means “it does me bad.” *Medico* is a doctor.'),
    ('it', 'emotions', '## Learn in context

| Target language | English |
| --- | --- |
| Sono felice, ma stanco. | I am happy, but tired. |
| Lei ha paura del cane. | She is afraid of the dog. |

## Mini dialogue

> **A:** Come ti senti?
> *How do you feel?*
>
> **B:** Sono molto felice, ma stanco.
> *I am very happy, but tired.*
>
> **A:** E perché tua sorella è triste?
> *And why is your sister sad?*
>
> **B:** Ha paura del cane.
> *She is afraid of the dog.*

## Language note

Adjectives agree: **stanco** for a man and *stanca* for a woman.'),
    ('it', 'hobbies', '## Learn in context

| Target language | English |
| --- | --- |
| Mi piace leggere e ascoltare musica. | I like reading and listening to music. |
| Nel weekend faccio sport e ballo. | At the weekend I do sport and dance. |

## Mini dialogue

> **A:** Cosa ti piace fare nel weekend?
> *What do you like to do at the weekend?*
>
> **B:** Mi piace leggere e ascoltare musica. E tu?
> *I like reading and listening to music. And you?*
>
> **A:** Mi piace fare sport e ballare.
> *I like doing sport and dancing.*

## Language note

**Mi piace** literally means “it pleases me”; follow it with an infinitive.'),
    ('it', 'nature-animals', '## Learn in context

| Target language | English |
| --- | --- |
| Il cane corre nel bosco. | The dog runs in the woods. |
| Un uccello è sull’albero. | A bird is on the tree. |

## Worked usage

- **Il cane corre nel bosco.** — *The dog runs in the woods.*
- **Un uccello è sull’albero.** — *A bird is on the tree.*

## Language note

**Bosco** means woods; *foresta* is usually a larger forest.'),
    ('it', 'long-words', '## Learn in context

| Target language | English |
| --- | --- |
| Lo fa precipitevolissimevolmente. | He or she does it with extreme speed. |
| L’elettroencefalografista legge il risultato. | The electroencephalograph specialist reads the result. |

## Worked usage

- **Lo fa precipitevolissimevolmente.** — *He or she does it with extreme speed.*
- **L’elettroencefalografista legge il risultato.** — *The electroencephalograph specialist reads the result.*

## Language note

**Precipitevolissimevolmente** grows through Italian suffixes; it is not a German-style compound.'),
    ('it', 'funny-unusual-words', '## Learn in context

| Target language | English |
| --- | --- |
| Mio zio è un pantofolaio: resta volentieri a casa. | My uncle is a homebody: he happily stays at home. |
| Dopo pranzo mi spaparanzo sul divano. | After lunch I sprawl comfortably on the sofa. |

## Worked usage

- **Mio zio è un pantofolaio: resta volentieri a casa.** — *My uncle is a homebody: he happily stays at home.*
- **Dopo pranzo mi spaparanzo sul divano.** — *After lunch I sprawl comfortably on the sofa.*

## Language note

**Pantofolaio** comes from *pantofola* (“slipper”) and is affectionate only in a friendly tone. An **abbiocco** is the sleepy feeling after a large meal.')
)
INSERT OR IGNORE INTO Lessons (CourseId, Slug, Title, SortOrder, ContentMarkdown)
SELECT c.Id, s.Slug, s.Title, s.SortOrder, content.ContentMarkdown
FROM Courses c
INNER JOIN LessonContentSeeds content ON content.CourseCode = c.Code
INNER JOIN LessonSeeds s ON s.Slug = content.LessonSlug
WHERE c.Code = 'it';

-- Construct one ordered vocabulary JSON document for each Italian lesson.
WITH WordSeeds (CourseCode, LessonSlug, Position, Word, Meaning) AS (
    VALUES
    ('it', 'greetings', 1, 'Ciao', 'Hello'),
    ('it', 'greetings', 2, 'Buongiorno', 'Good day'),
    ('it', 'greetings', 3, 'Benvenuto', 'Welcome'),
    ('it', 'greetings', 4, 'Sì', 'Yes'),
    ('it', 'greetings', 5, 'No', 'No'),
    ('it', 'introductions', 1, 'Mi chiamo ...', 'My name is ...'),
    ('it', 'introductions', 2, 'Come ti chiami?', 'What is your name?'),
    ('it', 'introductions', 3, 'Vengo da ...', 'I come from ...'),
    ('it', 'introductions', 4, 'Piacere', 'Nice to meet you'),
    ('it', 'introductions', 5, 'Questo è ...', 'This is ...'),
    ('it', 'politeness', 1, 'Per favore', 'Please'),
    ('it', 'politeness', 2, 'Grazie', 'Thank you'),
    ('it', 'politeness', 3, 'Scusa', 'Sorry / excuse me'),
    ('it', 'politeness', 4, 'Prego', 'You are welcome'),
    ('it', 'politeness', 5, 'Puoi aiutare?', 'Can you help?'),
    ('it', 'numbers', 1, 'uno', 'one'),
    ('it', 'numbers', 2, 'due', 'two'),
    ('it', 'numbers', 3, 'tre', 'three'),
    ('it', 'numbers', 4, 'dieci', 'ten'),
    ('it', 'numbers', 5, 'cento', 'one hundred'),
    ('it', 'family', 1, 'la famiglia', 'family'),
    ('it', 'family', 2, 'la madre', 'mother'),
    ('it', 'family', 3, 'il padre', 'father'),
    ('it', 'family', 4, 'il fratello', 'brother'),
    ('it', 'family', 5, 'la sorella', 'sister'),
    ('it', 'food', 1, 'il pane', 'bread'),
    ('it', 'food', 2, 'il formaggio', 'cheese'),
    ('it', 'food', 3, 'la mela', 'apple'),
    ('it', 'food', 4, 'la colazione', 'breakfast'),
    ('it', 'food', 5, 'buono', 'tasty'),
    ('it', 'drinks', 1, 'l’acqua', 'water'),
    ('it', 'drinks', 2, 'il caffè', 'coffee'),
    ('it', 'drinks', 3, 'il tè', 'tea'),
    ('it', 'drinks', 4, 'la birra', 'beer'),
    ('it', 'drinks', 5, 'un bicchiere', 'a glass'),
    ('it', 'home', 1, 'la casa', 'house'),
    ('it', 'home', 2, 'l’appartamento', 'apartment'),
    ('it', 'home', 3, 'la stanza', 'room'),
    ('it', 'home', 4, 'la cucina', 'kitchen'),
    ('it', 'home', 5, 'la chiave', 'key'),
    ('it', 'travel', 1, 'la stazione', 'train station'),
    ('it', 'travel', 2, 'l’aeroporto', 'airport'),
    ('it', 'travel', 3, 'il biglietto', 'ticket'),
    ('it', 'travel', 4, 'la valigia', 'suitcase'),
    ('it', 'travel', 5, 'partire', 'to depart'),
    ('it', 'directions', 1, 'a sinistra', 'left'),
    ('it', 'directions', 2, 'a destra', 'right'),
    ('it', 'directions', 3, 'sempre dritto', 'straight ahead'),
    ('it', 'directions', 4, 'la strada', 'street'),
    ('it', 'directions', 5, 'Dov’è ...?', 'Where is ...?'),
    ('it', 'time-calendar', 1, 'oggi', 'today'),
    ('it', 'time-calendar', 2, 'domani', 'tomorrow'),
    ('it', 'time-calendar', 3, 'ieri', 'yesterday'),
    ('it', 'time-calendar', 4, 'l’orologio', 'clock'),
    ('it', 'time-calendar', 5, 'lunedì', 'Monday'),
    ('it', 'weather', 1, 'soleggiato', 'sunny'),
    ('it', 'weather', 2, 'piovoso', 'rainy'),
    ('it', 'weather', 3, 'il vento', 'wind'),
    ('it', 'weather', 4, 'freddo', 'cold'),
    ('it', 'weather', 5, 'caldo', 'warm'),
    ('it', 'shopping', 1, 'comprare', 'to buy'),
    ('it', 'shopping', 2, 'il prezzo', 'price'),
    ('it', 'shopping', 3, 'costoso', 'expensive'),
    ('it', 'shopping', 4, 'economico', 'cheap'),
    ('it', 'shopping', 5, 'la taglia', 'size'),
    ('it', 'work-school', 1, 'il lavoro', 'work'),
    ('it', 'work-school', 2, 'la scuola', 'school'),
    ('it', 'work-school', 3, 'l’insegnante', 'teacher'),
    ('it', 'work-school', 4, 'imparare', 'to learn'),
    ('it', 'work-school', 5, 'l’ufficio', 'office'),
    ('it', 'body-health', 1, 'la testa', 'head'),
    ('it', 'body-health', 2, 'la mano', 'hand'),
    ('it', 'body-health', 3, 'il medico', 'doctor'),
    ('it', 'body-health', 4, 'malato', 'ill'),
    ('it', 'body-health', 5, 'Fa male', 'It hurts'),
    ('it', 'emotions', 1, 'felice', 'happy'),
    ('it', 'emotions', 2, 'triste', 'sad'),
    ('it', 'emotions', 3, 'stanco', 'tired'),
    ('it', 'emotions', 4, 'eccitato', 'excited'),
    ('it', 'emotions', 5, 'avere paura', 'to be afraid'),
    ('it', 'hobbies', 1, 'leggere', 'to read'),
    ('it', 'hobbies', 2, 'ascoltare musica', 'to listen to music'),
    ('it', 'hobbies', 3, 'cucinare', 'to cook'),
    ('it', 'hobbies', 4, 'fare sport', 'to do sport'),
    ('it', 'hobbies', 5, 'ballare', 'to dance'),
    ('it', 'nature-animals', 1, 'il cane', 'dog'),
    ('it', 'nature-animals', 2, 'il gatto', 'cat'),
    ('it', 'nature-animals', 3, 'l’albero', 'tree'),
    ('it', 'nature-animals', 4, 'la foresta', 'forest'),
    ('it', 'nature-animals', 5, 'l’uccello', 'bird'),
    ('it', 'long-words', 1, 'precipitevolissimevolmente', 'very precipitously; a famous adverb'),
    ('it', 'long-words', 2, 'psiconeuroendocrinoimmunologia', 'psychoneuroendocrinoimmunology'),
    ('it', 'long-words', 3, 'elettroencefalografista', 'electroencephalograph technician'),
    ('it', 'long-words', 4, 'incomprensibilmente', 'incomprehensibly'),
    ('it', 'long-words', 5, 'costituzionalizzazione', 'constitutionalization'),
    ('it', 'funny-unusual-words', 1, 'pantofolaio', 'a homebody'),
    ('it', 'funny-unusual-words', 2, 'abbiocco', 'sleepiness after a large meal'),
    ('it', 'funny-unusual-words', 3, 'spaparanzarsi', 'to sprawl comfortably'),
    ('it', 'funny-unusual-words', 4, 'magari', 'if only; perhaps, depending on context'),
    ('it', 'funny-unusual-words', 5, 'passeggiata', 'a leisurely walk')
)
INSERT OR IGNORE INTO LessonVocabulary (LessonId, VocabularyJson)
SELECT l.Id, json_object('words', json((
    SELECT json_group_array(json_object('word', ordered.Word, 'meaning', ordered.Meaning))
    FROM (
        SELECT Word, Meaning
        FROM WordSeeds
        WHERE CourseCode = c.Code AND LessonSlug = l.Slug
        ORDER BY Position
    ) ordered
)))
FROM Lessons l
INNER JOIN Courses c ON c.Id = l.CourseId
WHERE c.Code = 'it';

INSERT OR IGNORE INTO Quizzes (LessonId, Title)
SELECT l.Id, l.Title || ' Quiz'
FROM Lessons l
INNER JOIN Courses c ON c.Id = l.CourseId
WHERE c.Code = 'it';

WITH QuestionSeeds (CourseCode, LessonSlug, SortOrder, Content, Type, QuestionData, CorrectAnswer) AS (
    VALUES
    ('it', 'greetings', 1, 'Which Italian word or phrase means “Hello”?', 'multiple_choice', '{"options":["Ciao","Buongiorno","Benvenuto"]}', 'Ciao'),
    ('it', 'greetings', 2, 'Which Italian word or phrase means “Good day”?', 'multiple_choice', '{"options":["Benvenuto","Sì","Buongiorno"]}', 'Buongiorno'),
    ('it', 'greetings', 3, 'Which Italian word or phrase means “Welcome”?', 'multiple_choice', '{"options":["No","Benvenuto","Sì"]}', 'Benvenuto'),
    ('it', 'greetings', 4, 'Which Italian word or phrase means “Yes”?', 'multiple_choice', '{"options":["Sì","No","Ciao"]}', 'Sì'),
    ('it', 'greetings', 5, 'Put this lesson sentence in Italian order: “Hi, Marta! Welcome to Rome.”', 'word_ordering', '{"tokens":["Marta","a","Ciao","Benvenuta","Roma"]}', '["Ciao","Marta","Benvenuta","a","Roma"]'),
    ('it', 'greetings', 6, 'Put this lesson sentence in Italian order: “Good morning, sir.”', 'word_ordering', '{"tokens":["signore","Buongiorno"]}', '["Buongiorno","signore"]'),
    ('it', 'greetings', 7, 'Put this lesson sentence in Italian order: “Hi, Marta! Welcome here to Rome.”', 'word_ordering', '{"tokens":["Marta","qui","Roma","Ciao","Benvenuta","a"]}', '["Ciao","Marta","Benvenuta","qui","a","Roma"]'),
    ('it', 'greetings', 8, 'Type the Italian word or phrase for “No”.', 'free_text', '{}', 'No'),
    ('it', 'greetings', 9, 'Translate into Italian: “Hi, Marta! Welcome to Rome.”', 'free_text', '{}', 'Ciao, Marta! Benvenuta a Roma.'),
    ('it', 'greetings', 10, 'Translate into Italian: “Good morning, sir.”', 'free_text', '{}', 'Buongiorno, signore.'),
    ('it', 'introductions', 1, 'Which Italian word or phrase means “My name is ...”?', 'multiple_choice', '{"options":["Mi chiamo ...","Come ti chiami?","Vengo da ..."]}', 'Mi chiamo ...'),
    ('it', 'introductions', 2, 'Which Italian word or phrase means “What is your name?”', 'multiple_choice', '{"options":["Vengo da ...","Piacere","Come ti chiami?"]}', 'Come ti chiami?'),
    ('it', 'introductions', 3, 'Which Italian word or phrase means “I come from ...”?', 'multiple_choice', '{"options":["Questo è ...","Vengo da ...","Piacere"]}', 'Vengo da ...'),
    ('it', 'introductions', 4, 'Which Italian word or phrase means “Nice to meet you”?', 'multiple_choice', '{"options":["Piacere","Questo è ...","Mi chiamo ..."]}', 'Piacere'),
    ('it', 'introductions', 5, 'Put this lesson sentence in Italian order: “My name is Luca and I come from Italy.”', 'word_ordering', '{"tokens":["chiamo","e","dall’Italia","Mi","Luca","vengo"]}', '["Mi","chiamo","Luca","e","vengo","dall’Italia"]'),
    ('it', 'introductions', 6, 'Put this lesson sentence in Italian order: “What is your name? — My name is Sara.”', 'word_ordering', '{"tokens":["ti","Mi","Sara","Come","chiami","chiamo"]}', '["Come","ti","chiami","Mi","chiamo","Sara"]'),
    ('it', 'introductions', 7, 'Put this lesson sentence in Italian order: “Hi! My name is Luca. What is your name?”', 'word_ordering', '{"tokens":["Mi","Luca","ti","Ciao","chiamo","Come","chiami"]}', '["Ciao","Mi","chiamo","Luca","Come","ti","chiami"]'),
    ('it', 'introductions', 8, 'Type the Italian word or phrase for “This is ...”.', 'free_text', '{}', 'Questo è ...'),
    ('it', 'introductions', 9, 'Translate into Italian: “My name is Luca and I come from Italy.”', 'free_text', '{}', 'Mi chiamo Luca e vengo dall’Italia.'),
    ('it', 'introductions', 10, 'Translate into Italian: “What is your name? — My name is Sara.”', 'free_text', '{}', 'Come ti chiami? — Mi chiamo Sara.'),
    ('it', 'politeness', 1, 'Which Italian word or phrase means “Please”?', 'multiple_choice', '{"options":["Per favore","Grazie","Scusa"]}', 'Per favore'),
    ('it', 'politeness', 2, 'Which Italian word or phrase means “Thank you”?', 'multiple_choice', '{"options":["Scusa","Prego","Grazie"]}', 'Grazie'),
    ('it', 'politeness', 3, 'Which Italian word or phrase means “Sorry / excuse me”?', 'multiple_choice', '{"options":["Puoi aiutare?","Scusa","Prego"]}', 'Scusa'),
    ('it', 'politeness', 4, 'Which Italian word or phrase means “You are welcome”?', 'multiple_choice', '{"options":["Prego","Puoi aiutare?","Per favore"]}', 'Prego'),
    ('it', 'politeness', 5, 'Put this lesson sentence in Italian order: “A coffee, please. Thank you!”', 'word_ordering', '{"tokens":["favore","caffè","Per","un","Grazie"]}', '["Per","favore","un","caffè","Grazie"]'),
    ('it', 'politeness', 6, 'Put this lesson sentence in Italian order: “Excuse me, can you help me?”', 'word_ordering', '{"tokens":["può","Scusi","aiutarmi"]}', '["Scusi","può","aiutarmi"]'),
    ('it', 'politeness', 7, 'Put this lesson sentence in Italian order: “Excuse me, can you help me, please?”', 'word_ordering', '{"tokens":["può","per","Scusi","aiutarmi","favore"]}', '["Scusi","può","aiutarmi","per","favore"]'),
    ('it', 'politeness', 8, 'Type the Italian word or phrase for “Can you help?”', 'free_text', '{}', 'Puoi aiutare?'),
    ('it', 'politeness', 9, 'Translate into Italian: “A coffee, please. Thank you!”', 'free_text', '{}', 'Per favore, un caffè. Grazie!'),
    ('it', 'politeness', 10, 'Translate into Italian: “Excuse me, can you help me?”', 'free_text', '{}', 'Scusi, può aiutarmi?'),
    ('it', 'numbers', 1, 'Which Italian word or phrase means “one”?', 'multiple_choice', '{"options":["uno","due","tre"]}', 'uno'),
    ('it', 'numbers', 2, 'Which Italian word or phrase means “two”?', 'multiple_choice', '{"options":["tre","dieci","due"]}', 'due'),
    ('it', 'numbers', 3, 'Which Italian word or phrase means “three”?', 'multiple_choice', '{"options":["cento","tre","dieci"]}', 'tre'),
    ('it', 'numbers', 4, 'Which Italian word or phrase means “ten”?', 'multiple_choice', '{"options":["dieci","cento","uno"]}', 'dieci'),
    ('it', 'numbers', 5, 'Put this lesson sentence in Italian order: “I have two tickets and ten euros.”', 'word_ordering', '{"tokens":["due","e","euro","Ho","biglietti","dieci"]}', '["Ho","due","biglietti","e","dieci","euro"]'),
    ('it', 'numbers', 6, 'Put this lesson sentence in Italian order: “The train leaves at three.”', 'word_ordering', '{"tokens":["treno","alle","Il","parte","tre"]}', '["Il","treno","parte","alle","tre"]'),
    ('it', 'numbers', 7, 'Put this lesson sentence in Italian order: “two tickets and ten euros”', 'word_ordering', '{"tokens":["biglietti","dieci","due","e","euro"]}', '["due","biglietti","e","dieci","euro"]'),
    ('it', 'numbers', 8, 'Type the Italian word or phrase for “one hundred”.', 'free_text', '{}', 'cento'),
    ('it', 'numbers', 9, 'Translate into Italian: “I have two tickets and ten euros.”', 'free_text', '{}', 'Ho due biglietti e dieci euro.'),
    ('it', 'numbers', 10, 'Translate into Italian: “The train leaves at three.”', 'free_text', '{}', 'Il treno parte alle tre.'),
    ('it', 'family', 1, 'Which Italian word or phrase means “family”?', 'multiple_choice', '{"options":["la famiglia","la madre","il padre"]}', 'la famiglia'),
    ('it', 'family', 2, 'Which Italian word or phrase means “mother”?', 'multiple_choice', '{"options":["il padre","il fratello","la madre"]}', 'la madre'),
    ('it', 'family', 3, 'Which Italian word or phrase means “father”?', 'multiple_choice', '{"options":["la sorella","il padre","il fratello"]}', 'il padre'),
    ('it', 'family', 4, 'Which Italian word or phrase means “brother”?', 'multiple_choice', '{"options":["il fratello","la sorella","la famiglia"]}', 'il fratello'),
    ('it', 'family', 5, 'Put this lesson sentence in Italian order: “This is my mother and this is my father.”', 'word_ordering', '{"tokens":["è","madre","questo","mio","Questa","mia","e","è","padre"]}', '["Questa","è","mia","madre","e","questo","è","mio","padre"]'),
    ('it', 'family', 6, 'Put this lesson sentence in Italian order: “My sister has a brother.”', 'word_ordering', '{"tokens":["sorella","un","Mia","ha","fratello"]}', '["Mia","sorella","ha","un","fratello"]'),
    ('it', 'family', 7, 'Put this lesson sentence in Italian order: “Is this your family?”', 'word_ordering', '{"tokens":["è","tua","Questa","la","famiglia"]}', '["Questa","è","la","tua","famiglia"]'),
    ('it', 'family', 8, 'Type the Italian word or phrase for “sister”.', 'free_text', '{}', 'la sorella'),
    ('it', 'family', 9, 'Translate into Italian: “This is my mother and this is my father.”', 'free_text', '{}', 'Questa è mia madre e questo è mio padre.'),
    ('it', 'family', 10, 'Translate into Italian: “My sister has a brother.”', 'free_text', '{}', 'Mia sorella ha un fratello.'),
    ('it', 'food', 1, 'Which Italian word or phrase means “bread”?', 'multiple_choice', '{"options":["il pane","il formaggio","la mela"]}', 'il pane'),
    ('it', 'food', 2, 'Which Italian word or phrase means “cheese”?', 'multiple_choice', '{"options":["la mela","la colazione","il formaggio"]}', 'il formaggio'),
    ('it', 'food', 3, 'Which Italian word or phrase means “apple”?', 'multiple_choice', '{"options":["buono","la mela","la colazione"]}', 'la mela'),
    ('it', 'food', 4, 'Which Italian word or phrase means “breakfast”?', 'multiple_choice', '{"options":["la colazione","buono","il pane"]}', 'la colazione'),
    ('it', 'food', 5, 'Put this lesson sentence in Italian order: “For breakfast I eat bread and cheese.”', 'word_ordering', '{"tokens":["colazione","pane","formaggio","A","mangio","e"]}', '["A","colazione","mangio","pane","e","formaggio"]'),
    ('it', 'food', 6, 'Put this lesson sentence in Italian order: “The apple is delicious.”', 'word_ordering', '{"tokens":["mela","deliziosa","La","è"]}', '["La","mela","è","deliziosa"]'),
    ('it', 'food', 7, 'Put this lesson sentence in Italian order: “What do you eat for breakfast?”', 'word_ordering', '{"tokens":["mangi","colazione","Cosa","a"]}', '["Cosa","mangi","a","colazione"]'),
    ('it', 'food', 8, 'Type the Italian word or phrase for “tasty”.', 'free_text', '{}', 'buono'),
    ('it', 'food', 9, 'Translate into Italian: “For breakfast I eat bread and cheese.”', 'free_text', '{}', 'A colazione mangio pane e formaggio.'),
    ('it', 'food', 10, 'Translate into Italian: “The apple is delicious.”', 'free_text', '{}', 'La mela è deliziosa.'),
    ('it', 'drinks', 1, 'Which Italian word or phrase means “water”?', 'multiple_choice', '{"options":["l’acqua","il caffè","il tè"]}', 'l’acqua'),
    ('it', 'drinks', 2, 'Which Italian word or phrase means “coffee”?', 'multiple_choice', '{"options":["il tè","la birra","il caffè"]}', 'il caffè'),
    ('it', 'drinks', 3, 'Which Italian word or phrase means “tea”?', 'multiple_choice', '{"options":["un bicchiere","il tè","la birra"]}', 'il tè'),
    ('it', 'drinks', 4, 'Which Italian word or phrase means “beer”?', 'multiple_choice', '{"options":["la birra","un bicchiere","l’acqua"]}', 'la birra'),
    ('it', 'drinks', 5, 'Put this lesson sentence in Italian order: “I would like a glass of water.”', 'word_ordering', '{"tokens":["un","d’acqua","Vorrei","bicchiere"]}', '["Vorrei","un","bicchiere","d’acqua"]'),
    ('it', 'drinks', 6, 'Put this lesson sentence in Italian order: “The coffee is hot; the tea is lukewarm.”', 'word_ordering', '{"tokens":["caffè","caldo","tè","tiepido","Il","è","il","è"]}', '["Il","caffè","è","caldo","il","tè","è","tiepido"]'),
    ('it', 'drinks', 7, 'Put this lesson sentence in Italian order: “Do you want a coffee or a tea?”', 'word_ordering', '{"tokens":["un","o","tè","Vuoi","caffè","un"]}', '["Vuoi","un","caffè","o","un","tè"]'),
    ('it', 'drinks', 8, 'Type the Italian word or phrase for “a glass”.', 'free_text', '{}', 'un bicchiere'),
    ('it', 'drinks', 9, 'Translate into Italian: “I would like a glass of water.”', 'free_text', '{}', 'Vorrei un bicchiere d’acqua.'),
    ('it', 'drinks', 10, 'Translate into Italian: “The coffee is hot; the tea is lukewarm.”', 'free_text', '{}', 'Il caffè è caldo; il tè è tiepido.'),
    ('it', 'home', 1, 'Which Italian word or phrase means “house”?', 'multiple_choice', '{"options":["la casa","l’appartamento","la stanza"]}', 'la casa'),
    ('it', 'home', 2, 'Which Italian word or phrase means “apartment”?', 'multiple_choice', '{"options":["la stanza","la cucina","l’appartamento"]}', 'l’appartamento'),
    ('it', 'home', 3, 'Which Italian word or phrase means “room”?', 'multiple_choice', '{"options":["la chiave","la stanza","la cucina"]}', 'la stanza'),
    ('it', 'home', 4, 'Which Italian word or phrase means “kitchen”?', 'multiple_choice', '{"options":["la cucina","la chiave","la casa"]}', 'la cucina'),
    ('it', 'home', 5, 'Put this lesson sentence in Italian order: “The kitchen is in the apartment.”', 'word_ordering', '{"tokens":["cucina","nell’appartamento","La","è"]}', '["La","cucina","è","nell’appartamento"]'),
    ('it', 'home', 6, 'Put this lesson sentence in Italian order: “Where is the key? — On the table.”', 'word_ordering', '{"tokens":["la","Sul","Dov’è","chiave","tavolo"]}', '["Dov’è","la","chiave","Sul","tavolo"]'),
    ('it', 'home', 7, 'Put this lesson sentence in Italian order: “Where is the key?”', 'word_ordering', '{"tokens":["la","Dov’è","chiave"]}', '["Dov’è","la","chiave"]'),
    ('it', 'home', 8, 'Type the Italian word or phrase for “key”.', 'free_text', '{}', 'la chiave'),
    ('it', 'home', 9, 'Translate into Italian: “The kitchen is in the apartment.”', 'free_text', '{}', 'La cucina è nell’appartamento.'),
    ('it', 'home', 10, 'Translate into Italian: “Where is the key? — On the table.”', 'free_text', '{}', 'Dov’è la chiave? — Sul tavolo.'),
    ('it', 'travel', 1, 'Which Italian word or phrase means “train station”?', 'multiple_choice', '{"options":["la stazione","l’aeroporto","il biglietto"]}', 'la stazione'),
    ('it', 'travel', 2, 'Which Italian word or phrase means “airport”?', 'multiple_choice', '{"options":["il biglietto","la valigia","l’aeroporto"]}', 'l’aeroporto'),
    ('it', 'travel', 3, 'Which Italian word or phrase means “ticket”?', 'multiple_choice', '{"options":["partire","il biglietto","la valigia"]}', 'il biglietto'),
    ('it', 'travel', 4, 'Which Italian word or phrase means “suitcase”?', 'multiple_choice', '{"options":["la valigia","partire","la stazione"]}', 'la valigia'),
    ('it', 'travel', 5, 'Put this lesson sentence in Italian order: “The station is near the airport.”', 'word_ordering', '{"tokens":["stazione","vicino","La","è","all’aeroporto"]}', '["La","stazione","è","vicino","all’aeroporto"]'),
    ('it', 'travel', 6, 'Put this lesson sentence in Italian order: “The ticket is in the suitcase.”', 'word_ordering', '{"tokens":["biglietto","nella","Il","è","valigia"]}', '["Il","biglietto","è","nella","valigia"]'),
    ('it', 'travel', 7, 'Put this lesson sentence in Italian order: “At what time does the train leave?”', 'word_ordering', '{"tokens":["che","parte","treno","A","ora","il"]}', '["A","che","ora","parte","il","treno"]'),
    ('it', 'travel', 8, 'Type the Italian word or phrase for “to depart”.', 'free_text', '{}', 'partire'),
    ('it', 'travel', 9, 'Translate into Italian: “The station is near the airport.”', 'free_text', '{}', 'La stazione è vicino all’aeroporto.'),
    ('it', 'travel', 10, 'Translate into Italian: “The ticket is in the suitcase.”', 'free_text', '{}', 'Il biglietto è nella valigia.'),
    ('it', 'directions', 1, 'Which Italian word or phrase means “left”?', 'multiple_choice', '{"options":["a sinistra","a destra","sempre dritto"]}', 'a sinistra'),
    ('it', 'directions', 2, 'Which Italian word or phrase means “right”?', 'multiple_choice', '{"options":["sempre dritto","la strada","a destra"]}', 'a destra'),
    ('it', 'directions', 3, 'Which Italian word or phrase means “straight ahead”?', 'multiple_choice', '{"options":["Dov’è ...?","sempre dritto","la strada"]}', 'sempre dritto'),
    ('it', 'directions', 4, 'Which Italian word or phrase means “street”?', 'multiple_choice', '{"options":["la strada","Dov’è ...?","a sinistra"]}', 'la strada'),
    ('it', 'directions', 5, 'Put this lesson sentence in Italian order: “Go straight ahead, then turn left.”', 'word_ordering', '{"tokens":["sempre","poi","a","Vai","dritto","gira","sinistra"]}', '["Vai","sempre","dritto","poi","gira","a","sinistra"]'),
    ('it', 'directions', 6, 'Put this lesson sentence in Italian order: “Where is the street? — To the right of the bank.”', 'word_ordering', '{"tokens":["la","A","della","Dov’è","strada","destra","banca"]}', '["Dov’è","la","strada","A","destra","della","banca"]'),
    ('it', 'directions', 7, 'Put this lesson sentence in Italian order: “Excuse me, where is the street to the bank?”', 'word_ordering', '{"tokens":["dov’è","strada","la","Scusi","la","per","banca"]}', '["Scusi","dov’è","la","strada","per","la","banca"]'),
    ('it', 'directions', 8, 'Type the Italian word or phrase for “Where is ...?”', 'free_text', '{}', 'Dov’è ...?'),
    ('it', 'directions', 9, 'Translate into Italian: “Go straight ahead, then turn left.”', 'free_text', '{}', 'Vai sempre dritto, poi gira a sinistra.'),
    ('it', 'directions', 10, 'Translate into Italian: “Where is the street? — To the right of the bank.”', 'free_text', '{}', 'Dov’è la strada? — A destra della banca.'),
    ('it', 'time-calendar', 1, 'Which Italian word or phrase means “today”?', 'multiple_choice', '{"options":["oggi","domani","ieri"]}', 'oggi'),
    ('it', 'time-calendar', 2, 'Which Italian word or phrase means “tomorrow”?', 'multiple_choice', '{"options":["ieri","l’orologio","domani"]}', 'domani'),
    ('it', 'time-calendar', 3, 'Which Italian word or phrase means “yesterday”?', 'multiple_choice', '{"options":["lunedì","ieri","l’orologio"]}', 'ieri'),
    ('it', 'time-calendar', 4, 'Which Italian word or phrase means “clock”?', 'multiple_choice', '{"options":["l’orologio","lunedì","oggi"]}', 'l’orologio'),
    ('it', 'time-calendar', 5, 'Put this lesson sentence in Italian order: “Today is Monday; tomorrow is Tuesday.”', 'word_ordering', '{"tokens":["è","domani","martedì","Oggi","lunedì","è"]}', '["Oggi","è","lunedì","domani","è","martedì"]'),
    ('it', 'time-calendar', 6, 'Put this lesson sentence in Italian order: “The lesson starts at eight.”', 'word_ordering', '{"tokens":["lezione","alle","La","inizia","otto"]}', '["La","lezione","inizia","alle","otto"]'),
    ('it', 'time-calendar', 7, 'Put this lesson sentence in Italian order: “What day is today?”', 'word_ordering', '{"tokens":["giorno","oggi","Che","è"]}', '["Che","giorno","è","oggi"]'),
    ('it', 'time-calendar', 8, 'Type the Italian word or phrase for “Monday”.', 'free_text', '{}', 'lunedì'),
    ('it', 'time-calendar', 9, 'Translate into Italian: “Today is Monday; tomorrow is Tuesday.”', 'free_text', '{}', 'Oggi è lunedì; domani è martedì.'),
    ('it', 'time-calendar', 10, 'Translate into Italian: “The lesson starts at eight.”', 'free_text', '{}', 'La lezione inizia alle otto.'),
    ('it', 'weather', 1, 'Which Italian word or phrase means “sunny”?', 'multiple_choice', '{"options":["soleggiato","piovoso","il vento"]}', 'soleggiato'),
    ('it', 'weather', 2, 'Which Italian word or phrase means “rainy”?', 'multiple_choice', '{"options":["il vento","freddo","piovoso"]}', 'piovoso'),
    ('it', 'weather', 3, 'Which Italian word or phrase means “wind”?', 'multiple_choice', '{"options":["caldo","il vento","freddo"]}', 'il vento'),
    ('it', 'weather', 4, 'Which Italian word or phrase means “cold”?', 'multiple_choice', '{"options":["freddo","caldo","soleggiato"]}', 'freddo'),
    ('it', 'weather', 5, 'Put this lesson sentence in Italian order: “Today it is sunny, but it is cold.”', 'word_ordering', '{"tokens":["c’è","sole","fa","Oggi","il","ma","freddo"]}', '["Oggi","c’è","il","sole","ma","fa","freddo"]'),
    ('it', 'weather', 6, 'Put this lesson sentence in Italian order: “Tomorrow it will be cloudy and windy.”', 'word_ordering', '{"tokens":["sarà","e","Domani","nuvoloso","ventoso"]}', '["Domani","sarà","nuvoloso","e","ventoso"]'),
    ('it', 'weather', 7, 'Put this lesson sentence in Italian order: “What is the weather today?”', 'word_ordering', '{"tokens":["tempo","oggi","Che","fa"]}', '["Che","tempo","fa","oggi"]'),
    ('it', 'weather', 8, 'Type the Italian word or phrase for “warm”.', 'free_text', '{}', 'caldo'),
    ('it', 'weather', 9, 'Translate into Italian: “Today it is sunny, but it is cold.”', 'free_text', '{}', 'Oggi c’è il sole, ma fa freddo.'),
    ('it', 'weather', 10, 'Translate into Italian: “Tomorrow it will be cloudy and windy.”', 'free_text', '{}', 'Domani sarà nuvoloso e ventoso.'),
    ('it', 'shopping', 1, 'Which Italian word or phrase means “to buy”?', 'multiple_choice', '{"options":["comprare","il prezzo","costoso"]}', 'comprare'),
    ('it', 'shopping', 2, 'Which Italian word or phrase means “price”?', 'multiple_choice', '{"options":["costoso","economico","il prezzo"]}', 'il prezzo'),
    ('it', 'shopping', 3, 'Which Italian word or phrase means “expensive”?', 'multiple_choice', '{"options":["la taglia","costoso","economico"]}', 'costoso'),
    ('it', 'shopping', 4, 'Which Italian word or phrase means “cheap”?', 'multiple_choice', '{"options":["economico","la taglia","comprare"]}', 'economico'),
    ('it', 'shopping', 5, 'Put this lesson sentence in Italian order: “How much does this T-shirt cost?”', 'word_ordering', '{"tokens":["costa","maglietta","Quanto","questa"]}', '["Quanto","costa","questa","maglietta"]'),
    ('it', 'shopping', 6, 'Put this lesson sentence in Italian order: “It costs twenty euros; the size is small.”', 'word_ordering', '{"tokens":["venti","la","è","Costa","euro","taglia","piccola"]}', '["Costa","venti","euro","la","taglia","è","piccola"]'),
    ('it', 'shopping', 7, 'Put this lesson sentence in Italian order: “I would like to buy this T-shirt. What is the price?”', 'word_ordering', '{"tokens":["comprare","maglietta","è","prezzo","Vorrei","questa","Qual","il"]}', '["Vorrei","comprare","questa","maglietta","Qual","è","il","prezzo"]'),
    ('it', 'shopping', 8, 'Type the Italian word or phrase for “size”.', 'free_text', '{}', 'la taglia'),
    ('it', 'shopping', 9, 'Translate into Italian: “How much does this T-shirt cost?”', 'free_text', '{}', 'Quanto costa questa maglietta?'),
    ('it', 'shopping', 10, 'Translate into Italian: “It costs twenty euros; the size is small.”', 'free_text', '{}', 'Costa venti euro; la taglia è piccola.'),
    ('it', 'work-school', 1, 'Which Italian word or phrase means “work”?', 'multiple_choice', '{"options":["il lavoro","la scuola","l’insegnante"]}', 'il lavoro'),
    ('it', 'work-school', 2, 'Which Italian word or phrase means “school”?', 'multiple_choice', '{"options":["l’insegnante","imparare","la scuola"]}', 'la scuola'),
    ('it', 'work-school', 3, 'Which Italian word or phrase means “teacher”?', 'multiple_choice', '{"options":["l’ufficio","l’insegnante","imparare"]}', 'l’insegnante'),
    ('it', 'work-school', 4, 'Which Italian word or phrase means “to learn”?', 'multiple_choice', '{"options":["imparare","l’ufficio","il lavoro"]}', 'imparare'),
    ('it', 'work-school', 5, 'Put this lesson sentence in Italian order: “I study Italian at school.”', 'word_ordering', '{"tokens":["italiano","scuola","Studio","a"]}', '["Studio","italiano","a","scuola"]'),
    ('it', 'work-school', 6, 'Put this lesson sentence in Italian order: “The teacher works in an office.”', 'word_ordering', '{"tokens":["lavora","ufficio","L’insegnante","in"]}', '["L’insegnante","lavora","in","ufficio"]'),
    ('it', 'work-school', 7, 'Put this lesson sentence in Italian order: “What do you do at school?”', 'word_ordering', '{"tokens":["fai","scuola","Cosa","a"]}', '["Cosa","fai","a","scuola"]'),
    ('it', 'work-school', 8, 'Type the Italian word or phrase for “office”.', 'free_text', '{}', 'l’ufficio'),
    ('it', 'work-school', 9, 'Translate into Italian: “I study Italian at school.”', 'free_text', '{}', 'Studio italiano a scuola.'),
    ('it', 'work-school', 10, 'Translate into Italian: “The teacher works in an office.”', 'free_text', '{}', 'L’insegnante lavora in ufficio.'),
    ('it', 'body-health', 1, 'Which Italian word or phrase means “head”?', 'multiple_choice', '{"options":["la testa","la mano","il medico"]}', 'la testa'),
    ('it', 'body-health', 2, 'Which Italian word or phrase means “hand”?', 'multiple_choice', '{"options":["il medico","malato","la mano"]}', 'la mano'),
    ('it', 'body-health', 3, 'Which Italian word or phrase means “doctor”?', 'multiple_choice', '{"options":["Fa male","il medico","malato"]}', 'il medico'),
    ('it', 'body-health', 4, 'Which Italian word or phrase means “ill”?', 'multiple_choice', '{"options":["malato","Fa male","la testa"]}', 'malato'),
    ('it', 'body-health', 5, 'Put this lesson sentence in Italian order: “My head hurts; I am ill.”', 'word_ordering', '{"tokens":["fa","la","sono","Mi","male","testa","malato"]}', '["Mi","fa","male","la","testa","sono","malato"]'),
    ('it', 'body-health', 6, 'Put this lesson sentence in Italian order: “The doctor looks at my hand.”', 'word_ordering', '{"tokens":["medico","la","mano","Il","guarda","mia"]}', '["Il","medico","guarda","la","mia","mano"]'),
    ('it', 'body-health', 7, 'Put this lesson sentence in Italian order: “How are you?”', 'word_ordering', '{"tokens":["stai","Come"]}', '["Come","stai"]'),
    ('it', 'body-health', 8, 'Type the Italian word or phrase for “It hurts”.', 'free_text', '{}', 'Fa male'),
    ('it', 'body-health', 9, 'Translate into Italian: “My head hurts; I am ill.”', 'free_text', '{}', 'Mi fa male la testa; sono malato.'),
    ('it', 'body-health', 10, 'Translate into Italian: “The doctor looks at my hand.”', 'free_text', '{}', 'Il medico guarda la mia mano.'),
    ('it', 'emotions', 1, 'Which Italian word or phrase means “happy”?', 'multiple_choice', '{"options":["felice","triste","stanco"]}', 'felice'),
    ('it', 'emotions', 2, 'Which Italian word or phrase means “sad”?', 'multiple_choice', '{"options":["stanco","eccitato","triste"]}', 'triste'),
    ('it', 'emotions', 3, 'Which Italian word or phrase means “tired”?', 'multiple_choice', '{"options":["avere paura","stanco","eccitato"]}', 'stanco'),
    ('it', 'emotions', 4, 'Which Italian word or phrase means “excited”?', 'multiple_choice', '{"options":["eccitato","avere paura","felice"]}', 'eccitato'),
    ('it', 'emotions', 5, 'Put this lesson sentence in Italian order: “I am happy, but tired.”', 'word_ordering', '{"tokens":["felice","stanco","Sono","ma"]}', '["Sono","felice","ma","stanco"]'),
    ('it', 'emotions', 6, 'Put this lesson sentence in Italian order: “She is afraid of the dog.”', 'word_ordering', '{"tokens":["ha","del","Lei","paura","cane"]}', '["Lei","ha","paura","del","cane"]'),
    ('it', 'emotions', 7, 'Put this lesson sentence in Italian order: “How do you feel?”', 'word_ordering', '{"tokens":["ti","Come","senti"]}', '["Come","ti","senti"]'),
    ('it', 'emotions', 8, 'Type the Italian word or phrase for “to be afraid”.', 'free_text', '{}', 'avere paura'),
    ('it', 'emotions', 9, 'Translate into Italian: “I am happy, but tired.”', 'free_text', '{}', 'Sono felice, ma stanco.'),
    ('it', 'emotions', 10, 'Translate into Italian: “She is afraid of the dog.”', 'free_text', '{}', 'Lei ha paura del cane.'),
    ('it', 'hobbies', 1, 'Which Italian word or phrase means “to read”?', 'multiple_choice', '{"options":["leggere","ascoltare musica","cucinare"]}', 'leggere'),
    ('it', 'hobbies', 2, 'Which Italian word or phrase means “to listen to music”?', 'multiple_choice', '{"options":["cucinare","fare sport","ascoltare musica"]}', 'ascoltare musica'),
    ('it', 'hobbies', 3, 'Which Italian word or phrase means “to cook”?', 'multiple_choice', '{"options":["ballare","cucinare","fare sport"]}', 'cucinare'),
    ('it', 'hobbies', 4, 'Which Italian word or phrase means “to do sport”?', 'multiple_choice', '{"options":["fare sport","ballare","leggere"]}', 'fare sport'),
    ('it', 'hobbies', 5, 'Put this lesson sentence in Italian order: “I like reading and listening to music.”', 'word_ordering', '{"tokens":["piace","e","musica","Mi","leggere","ascoltare"]}', '["Mi","piace","leggere","e","ascoltare","musica"]'),
    ('it', 'hobbies', 6, 'Put this lesson sentence in Italian order: “At the weekend I do sport and dance.”', 'word_ordering', '{"tokens":["weekend","sport","ballo","Nel","faccio","e"]}', '["Nel","weekend","faccio","sport","e","ballo"]'),
    ('it', 'hobbies', 7, 'Put this lesson sentence in Italian order: “What do you like to do at the weekend?”', 'word_ordering', '{"tokens":["ti","fare","weekend","Cosa","piace","nel"]}', '["Cosa","ti","piace","fare","nel","weekend"]'),
    ('it', 'hobbies', 8, 'Type the Italian word or phrase for “to dance”.', 'free_text', '{}', 'ballare'),
    ('it', 'hobbies', 9, 'Translate into Italian: “I like reading and listening to music.”', 'free_text', '{}', 'Mi piace leggere e ascoltare musica.'),
    ('it', 'hobbies', 10, 'Translate into Italian: “At the weekend I do sport and dance.”', 'free_text', '{}', 'Nel weekend faccio sport e ballo.'),
    ('it', 'nature-animals', 1, 'Which Italian word or phrase means “dog”?', 'multiple_choice', '{"options":["il cane","il gatto","l’albero"]}', 'il cane'),
    ('it', 'nature-animals', 2, 'Which Italian word or phrase means “cat”?', 'multiple_choice', '{"options":["l’albero","la foresta","il gatto"]}', 'il gatto'),
    ('it', 'nature-animals', 3, 'Which Italian word or phrase means “tree”?', 'multiple_choice', '{"options":["l’uccello","l’albero","la foresta"]}', 'l’albero'),
    ('it', 'nature-animals', 4, 'Which Italian word or phrase means “forest”?', 'multiple_choice', '{"options":["la foresta","l’uccello","il cane"]}', 'la foresta'),
    ('it', 'nature-animals', 5, 'Put this lesson sentence in Italian order: “The dog runs in the woods.”', 'word_ordering', '{"tokens":["cane","nel","Il","corre","bosco"]}', '["Il","cane","corre","nel","bosco"]'),
    ('it', 'nature-animals', 6, 'Put this lesson sentence in Italian order: “A bird is on the tree.”', 'word_ordering', '{"tokens":["uccello","sull’albero","Un","è"]}', '["Un","uccello","è","sull’albero"]'),
    ('it', 'nature-animals', 7, 'Put this lesson sentence in Italian order: “in the woods”', 'word_ordering', '{"tokens":["bosco","nel"]}', '["nel","bosco"]'),
    ('it', 'nature-animals', 8, 'Type the Italian word or phrase for “bird”.', 'free_text', '{}', 'l’uccello'),
    ('it', 'nature-animals', 9, 'Translate into Italian: “The dog runs in the woods.”', 'free_text', '{}', 'Il cane corre nel bosco.'),
    ('it', 'nature-animals', 10, 'Translate into Italian: “A bird is on the tree.”', 'free_text', '{}', 'Un uccello è sull’albero.'),
    ('it', 'long-words', 1, 'Which Italian word or phrase means “very precipitously; a famous adverb”?', 'multiple_choice', '{"options":["precipitevolissimevolmente","psiconeuroendocrinoimmunologia","elettroencefalografista"]}', 'precipitevolissimevolmente'),
    ('it', 'long-words', 2, 'Which Italian word or phrase means “psychoneuroendocrinoimmunology”?', 'multiple_choice', '{"options":["elettroencefalografista","incomprensibilmente","psiconeuroendocrinoimmunologia"]}', 'psiconeuroendocrinoimmunologia'),
    ('it', 'long-words', 3, 'Which Italian word or phrase means “electroencephalograph technician”?', 'multiple_choice', '{"options":["costituzionalizzazione","elettroencefalografista","incomprensibilmente"]}', 'elettroencefalografista'),
    ('it', 'long-words', 4, 'Which Italian word or phrase means “incomprehensibly”?', 'multiple_choice', '{"options":["incomprensibilmente","costituzionalizzazione","precipitevolissimevolmente"]}', 'incomprensibilmente'),
    ('it', 'long-words', 5, 'Put this lesson sentence in Italian order: “He or she does it with extreme speed.”', 'word_ordering', '{"tokens":["fa","Lo","precipitevolissimevolmente"]}', '["Lo","fa","precipitevolissimevolmente"]'),
    ('it', 'long-words', 6, 'Put this lesson sentence in Italian order: “The electroencephalograph specialist reads the result.”', 'word_ordering', '{"tokens":["legge","risultato","L’elettroencefalografista","il"]}', '["L’elettroencefalografista","legge","il","risultato"]'),
    ('it', 'long-words', 7, 'Put this lesson sentence in Italian order: “reads the result”', 'word_ordering', '{"tokens":["il","legge","risultato"]}', '["legge","il","risultato"]'),
    ('it', 'long-words', 8, 'Type the Italian word or phrase for “constitutionalization”.', 'free_text', '{}', 'costituzionalizzazione'),
    ('it', 'long-words', 9, 'Translate into Italian: “He or she does it with extreme speed.”', 'free_text', '{}', 'Lo fa precipitevolissimevolmente.'),
    ('it', 'long-words', 10, 'Translate into Italian: “The electroencephalograph specialist reads the result.”', 'free_text', '{}', 'L’elettroencefalografista legge il risultato.'),
    ('it', 'funny-unusual-words', 1, 'Which Italian word or phrase means “a homebody”?', 'multiple_choice', '{"options":["pantofolaio","abbiocco","spaparanzarsi"]}', 'pantofolaio'),
    ('it', 'funny-unusual-words', 2, 'Which Italian word or phrase means “sleepiness after a large meal”?', 'multiple_choice', '{"options":["spaparanzarsi","magari","abbiocco"]}', 'abbiocco'),
    ('it', 'funny-unusual-words', 3, 'Which Italian word or phrase means “to sprawl comfortably”?', 'multiple_choice', '{"options":["passeggiata","spaparanzarsi","magari"]}', 'spaparanzarsi'),
    ('it', 'funny-unusual-words', 4, 'Which Italian word or phrase means “if only; perhaps, depending on context”?', 'multiple_choice', '{"options":["magari","passeggiata","pantofolaio"]}', 'magari'),
    ('it', 'funny-unusual-words', 5, 'Put this lesson sentence in Italian order: “My uncle is a homebody: he happily stays at home.”', 'word_ordering', '{"tokens":["zio","un","resta","a","Mio","è","pantofolaio","volentieri","casa"]}', '["Mio","zio","è","un","pantofolaio","resta","volentieri","a","casa"]'),
    ('it', 'funny-unusual-words', 6, 'Put this lesson sentence in Italian order: “After lunch I sprawl comfortably on the sofa.”', 'word_ordering', '{"tokens":["pranzo","spaparanzo","divano","Dopo","mi","sul"]}', '["Dopo","pranzo","mi","spaparanzo","sul","divano"]'),
    ('it', 'funny-unusual-words', 7, 'Put this lesson sentence in Italian order: “happily stays at home”', 'word_ordering', '{"tokens":["volentieri","casa","resta","a"]}', '["resta","volentieri","a","casa"]'),
    ('it', 'funny-unusual-words', 8, 'Type the Italian word or phrase for “a leisurely walk”.', 'free_text', '{}', 'passeggiata'),
    ('it', 'funny-unusual-words', 9, 'Translate into Italian: “My uncle is a homebody: he happily stays at home.”', 'free_text', '{}', 'Mio zio è un pantofolaio: resta volentieri a casa.'),
    ('it', 'funny-unusual-words', 10, 'Translate into Italian: “After lunch I sprawl comfortably on the sofa.”', 'free_text', '{}', 'Dopo pranzo mi spaparanzo sul divano.')
)
INSERT OR IGNORE INTO QuizQuestions
    (QuizId, SortOrder, Content, Type, QuestionData, CorrectAnswer)
SELECT q.Id, s.SortOrder, s.Content, s.Type, s.QuestionData, s.CorrectAnswer
FROM QuestionSeeds s
INNER JOIN Courses c ON c.Code = 'it' AND c.Code = s.CourseCode
INNER JOIN Lessons l ON l.CourseId = c.Id AND l.Slug = s.LessonSlug
INNER JOIN Quizzes q ON q.LessonId = l.Id;
