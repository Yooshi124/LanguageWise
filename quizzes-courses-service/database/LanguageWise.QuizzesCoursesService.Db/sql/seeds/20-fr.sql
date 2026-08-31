-- Idempotent French lesson, vocabulary, and quiz seed data. Requires schema.sql and seeds/00-courses.sql.

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
    ('fr', 'greetings', '## Learn in context

| Target language | English |
| --- | --- |
| Bonjour, Léa ! Bienvenue à Paris. | Hello, Léa! Welcome to Paris. |
| Salut, Paul ! | Hi, Paul! |

## Mini dialogue

> **A:** Bonjour, Léa ! Bienvenue à Paris. Ça va ?
> *Hello, Léa! Welcome to Paris. How are you?*
>
> **B:** Salut, Paul ! Merci beaucoup.
> *Hi, Paul! Thank you very much.*
>
> **A:** Tu es prête ?
> *Are you ready?*
>
> **B:** Oui !
> *Yes!*

## Language note

Use **bonjour** by day with anyone; **salut** is informal and works for both “hi” and “bye.”'),
    ('fr', 'introductions', '## Learn in context

| Target language | English |
| --- | --- |
| Je m’appelle Camille et je viens du Canada. | My name is Camille and I come from Canada. |
| Comment tu t’appelles ? — Je m’appelle Noé. | What is your name? — My name is Noé. |

## Mini dialogue

> **A:** Bonjour ! Je m’appelle Camille. Comment tu t’appelles ?
> *Hello! My name is Camille. What is your name?*
>
> **B:** Je m’appelle Noé. Enchanté !
> *My name is Noé. Nice to meet you!*
>
> **A:** Je viens du Canada. Et toi ?
> *I come from Canada. And you?*
>
> **B:** Je viens de Paris.
> *I come from Paris.*

## Language note

French has informal **tu** and formal/plural **vous**. Formally ask *Comment vous appelez-vous ?*.'),
    ('fr', 'politeness', '## Learn in context

| Target language | English |
| --- | --- |
| S’il vous plaît, un café. Merci beaucoup. | A coffee, please. Thank you very much. |
| Pardon, pouvez-vous m’aider ? | Excuse me, can you help me? |

## Mini dialogue

> **A:** Pardon, pouvez-vous m’aider, s’il vous plaît ?
> *Excuse me, can you help me, please?*
>
> **B:** Oui, bien sûr.
> *Yes, of course.*
>
> **A:** Un café, s’il vous plaît. Merci beaucoup !
> *A coffee, please. Thank you very much!*
>
> **B:** De rien !
> *You’re welcome!*

## Language note

Reply **de rien** (“it’s nothing”) to mean “you’re welcome.”'),
    ('fr', 'numbers', '## Learn in context

| Target language | English |
| --- | --- |
| J’ai deux billets et dix euros. | I have two tickets and ten euros. |
| Le train part à trois heures. | The train leaves at three o’clock. |

## Worked usage

- **J’ai deux billets et dix euros.** — *I have two tickets and ten euros.*
- **Le train part à trois heures.** — *The train leaves at three o’clock.*

## Language note

In *deux euros*, pronounce the final *x* as /z/ because of liaison.'),
    ('fr', 'family', '## Learn in context

| Target language | English |
| --- | --- |
| Voici ma mère et mon père. | Here are my mother and my father. |
| Mon frère a une sœur. | My brother has a sister. |

## Mini dialogue

> **A:** Voici ma famille : ma mère et mon père.
> *Here is my family: my mother and my father.*
>
> **B:** Tu as un frère ?
> *Do you have a brother?*
>
> **A:** Non, mais j’ai une sœur.
> *No, but I have a sister.*

## Language note

Use **mon** before a feminine noun beginning with a vowel: *mon amie* makes pronunciation smoother.'),
    ('fr', 'food', '## Learn in context

| Target language | English |
| --- | --- |
| Je mange du pain et du fromage. | I eat bread and cheese. |
| La pomme est délicieuse. | The apple is delicious. |

## Mini dialogue

> **A:** Qu’est-ce que tu manges au petit déjeuner ?
> *What do you eat for breakfast?*
>
> **B:** Je mange du pain avec du fromage.
> *I eat bread with cheese.*
>
> **A:** Et la pomme ?
> *And the apple?*
>
> **B:** Elle est délicieuse !
> *It is delicious!*

## Language note

French usually uses **du/de la** for an unspecified amount: *du pain*.'),
    ('fr', 'drinks', '## Learn in context

| Target language | English |
| --- | --- |
| Je voudrais un verre d’eau, s’il vous plaît. | I would like a glass of water, please. |
| Le café est chaud et le thé est bon. | The coffee is hot and the tea is good. |

## Mini dialogue

> **A:** Tu veux un café ou un thé ?
> *Do you want a coffee or a tea?*
>
> **B:** Un verre d’eau, s’il vous plaît. Le café est trop chaud.
> *A glass of water, please. The coffee is too hot.*

## Language note

After *verre*, write **d’eau**, not *de eau*: *de* contracts before a vowel.'),
    ('fr', 'home', '## Learn in context

| Target language | English |
| --- | --- |
| La cuisine est dans l’appartement. | The kitchen is in the apartment. |
| Où est la clé ? — Sur la table. | Where is the key? — On the table. |

## Mini dialogue

> **A:** Où est la clé ?
> *Where is the key?*
>
> **B:** La clé est dans la cuisine.
> *The key is in the kitchen.*
>
> **A:** Et ta chambre ?
> *And your room?*
>
> **B:** Ma chambre est dans l’appartement.
> *My room is in the apartment.*

## Language note

French contracts *de + le* to **du** and *à + le* to **au**.'),
    ('fr', 'travel', '## Learn in context

| Target language | English |
| --- | --- |
| La gare est près de l’aéroport. | The station is near the airport. |
| Mon billet est dans la valise. | My ticket is in the suitcase. |

## Mini dialogue

> **A:** À quelle heure part le train ?
> *At what time does the train leave?*
>
> **B:** À trois heures, à la gare.
> *At three o’clock, at the station.*
>
> **A:** Où est mon billet ?
> *Where is my ticket?*
>
> **B:** Ton billet est dans la valise.
> *Your ticket is in the suitcase.*

## Language note

With **partir**, use *de* for a departure point: *partir de la gare*.'),
    ('fr', 'directions', '## Learn in context

| Target language | English |
| --- | --- |
| Allez tout droit, puis à gauche. | Go straight ahead, then left. |
| Où est la rue ? — À droite du musée. | Where is the street? — To the right of the museum. |

## Mini dialogue

> **A:** Pardon, où est la rue du musée ?
> *Excuse me, where is the museum street?*
>
> **B:** Allez tout droit, puis tournez à gauche.
> *Go straight ahead, then turn left.*
>
> **A:** Et le musée ?
> *And the museum?*
>
> **B:** Il est à droite.
> *It is on the right.*

## Language note

**Allez** is the polite/plural command; use informal singular *va* with a friend.'),
    ('fr', 'time-calendar', '## Learn in context

| Target language | English |
| --- | --- |
| Aujourd’hui, c’est lundi; demain, je travaille. | Today is Monday; tomorrow I work. |
| Le cours commence à huit heures. | The class starts at eight o’clock. |

## Mini dialogue

> **A:** Quel jour sommes-nous aujourd’hui ?
> *What day is it today?*
>
> **B:** Aujourd’hui, c’est lundi.
> *Today is Monday.*
>
> **A:** À quelle heure commence le cours ?
> *At what time does the class start?*
>
> **B:** Demain à huit heures.
> *Tomorrow at eight o’clock.*

## Language note

Say **le lundi** for a habitual Monday, but **lundi** for this coming Monday.'),
    ('fr', 'weather', '## Learn in context

| Target language | English |
| --- | --- |
| Il fait froid et il y a du vent. | It is cold and windy. |
| Demain, il fera beau. | Tomorrow, the weather will be nice. |

## Mini dialogue

> **A:** Quel temps fait-il aujourd’hui ?
> *What is the weather today?*
>
> **B:** Aujourd’hui, il fait froid et il y a du vent.
> *Today it is cold and windy.*
>
> **A:** Et demain ?
> *And tomorrow?*
>
> **B:** Demain, il fera chaud et ensoleillé.
> *Tomorrow it will be warm and sunny.*

## Language note

French uses **il fait** for conditions but **il y a du vent**—literally “there is wind.”'),
    ('fr', 'shopping', '## Learn in context

| Target language | English |
| --- | --- |
| Combien coûte cette chemise ? | How much does this shirt cost? |
| Elle coûte vingt euros; elle est bon marché. | It costs twenty euros; it is inexpensive. |

## Mini dialogue

> **A:** Je voudrais acheter cette chemise. Quel est le prix ?
> *I would like to buy this shirt. What is the price?*
>
> **B:** Elle coûte vingt euros.
> *It costs twenty euros.*
>
> **A:** Ce n’est pas cher ! Avez-vous ma taille ?
> *That’s not expensive! Do you have my size?*
>
> **B:** Oui, bien sûr.
> *Yes, of course.*

## Language note

**Cher** agrees: *une robe chère*. **Bon marché** never takes a plural *s*.'),
    ('fr', 'work-school', '## Learn in context

| Target language | English |
| --- | --- |
| J’apprends le français à l’école. | I learn French at school. |
| Le professeur travaille dans son bureau. | The teacher works in his or her office. |

## Mini dialogue

> **A:** Qu’est-ce que tu fais à l’école ?
> *What do you do at school?*
>
> **B:** J’apprends le français. Mon professeur est gentil.
> *I learn French. My teacher is kind.*
>
> **A:** Et où est ton travail ?
> *And where is your work?*
>
> **B:** Je travaille dans un bureau.
> *I work in an office.*

## Language note

**Bureau** can be an office, a desk, or an administrative bureau; context decides.'),
    ('fr', 'body-health', '## Learn in context

| Target language | English |
| --- | --- |
| J’ai mal à la tête; je suis malade. | My head hurts; I am ill. |
| Le médecin regarde ma main. | The doctor looks at my hand. |

## Mini dialogue

> **A:** Ça ne va pas ?
> *Are you not well?*
>
> **B:** Non, j’ai mal à la tête. Je suis malade.
> *No, my head hurts. I am ill.*
>
> **A:** Va chez le médecin !
> *Go to the doctor!*
>
> **B:** Oui, il regarde aussi ma main.
> *Yes, he is also looking at my hand.*

## Language note

Use **avoir mal à** + body part: *J’ai mal au bras*.'),
    ('fr', 'emotions', '## Learn in context

| Target language | English |
| --- | --- |
| Je suis heureuse, mais fatiguée. | I am happy, but tired. |
| Il a peur du chien. | He is afraid of the dog. |

## Mini dialogue

> **A:** Comment te sens-tu ?
> *How do you feel?*
>
> **B:** Je suis très heureuse, mais fatiguée.
> *I am very happy, but tired.*
>
> **A:** Et pourquoi ton frère est triste ?
> *And why is your brother sad?*
>
> **B:** Il a peur du grand chien.
> *He is afraid of the big dog.*

## Language note

Adjectives agree: *heureux* is masculine and **heureuse** feminine. **Avoir peur** is “to have fear.”'),
    ('fr', 'hobbies', '## Learn in context

| Target language | English |
| --- | --- |
| J’aime lire et écouter de la musique. | I like reading and listening to music. |
| Le samedi, je fais du sport. | On Saturdays, I do sport. |

## Mini dialogue

> **A:** Qu’est-ce que tu aimes faire le week-end ?
> *What do you like to do on the weekend?*
>
> **B:** J’aime lire et écouter de la musique. Et toi ?
> *I like reading and listening to music. And you?*
>
> **A:** J’aime faire du sport et danser.
> *I like doing sport and dancing.*

## Language note

French says **faire du sport**, literally “to do some sport.”'),
    ('fr', 'nature-animals', '## Learn in context

| Target language | English |
| --- | --- |
| Le chat dort sous l’arbre. | The cat sleeps under the tree. |
| Un oiseau vole au-dessus de la forêt. | A bird flies above the forest. |

## Worked usage

- **Le chat dort sous l’arbre.** — *The cat sleeps under the tree.*
- **Un oiseau vole au-dessus de la forêt.** — *A bird flies above the forest.*

## Language note

Before a vowel, **le/la** becomes **l’**: *l’arbre*, *l’oiseau*.'),
    ('fr', 'long-words', '## Learn in context

| Target language | English |
| --- | --- |
| C’est vraisemblablement une décision anticonstitutionnelle. | It is probably an unconstitutional decision. |
| L’incompréhensibilité du texte est évidente. | The incomprehensibility of the text is obvious. |

## Worked usage

- **C’est vraisemblablement une décision anticonstitutionnelle.** — *It is probably an unconstitutional decision.*
- **L’incompréhensibilité du texte est évidente.** — *The incomprehensibility of the text is obvious.*

## Language note

Break **anticonstitutionnellement** into *anti- + constitution + -nel + -lement* rather than reading letter by letter.'),
    ('fr', 'funny-unusual-words', '## Learn in context

| Target language | English |
| --- | --- |
| Le pamplemousse est sur la table. | The grapefruit is on the table. |
| Après le cours, je flâne et je fais des gribouillis. | After class, I stroll and make scribbles. |

## Worked usage

- **Le pamplemousse est sur la table.** — *The grapefruit is on the table.*
- **Après le cours, je flâne et je fais des gribouillis.** — *After class, I stroll and make scribbles.*

## Language note

**Pamplemousse** reached French through Dutch *pompelmoes*. Its sound is playful, but it is the ordinary word for grapefruit.')
)
INSERT OR IGNORE INTO Lessons (CourseId, Slug, Title, SortOrder, ContentMarkdown)
SELECT c.Id, s.Slug, s.Title, s.SortOrder, content.ContentMarkdown
FROM Courses c
CROSS JOIN LessonSeeds s
INNER JOIN LessonContentSeeds content
    ON content.CourseCode = c.Code AND content.LessonSlug = s.Slug
WHERE c.Code = 'fr';

WITH WordSeeds (CourseCode, LessonSlug, Position, Word, Meaning) AS (
    VALUES
    ('fr', 'greetings', 1, 'Bonjour', 'Hello'),
    ('fr', 'greetings', 2, 'Salut', 'Hi'),
    ('fr', 'greetings', 3, 'Bienvenue', 'Welcome'),
    ('fr', 'greetings', 4, 'Oui', 'Yes'),
    ('fr', 'greetings', 5, 'Non', 'No'),
    ('fr', 'introductions', 1, 'Je m’appelle ...', 'My name is ...'),
    ('fr', 'introductions', 2, 'Comment tu t’appelles ?', 'What is your name?'),
    ('fr', 'introductions', 3, 'Je viens de ...', 'I come from ...'),
    ('fr', 'introductions', 4, 'Enchanté(e)', 'Nice to meet you'),
    ('fr', 'introductions', 5, 'Voici ...', 'This is ...'),
    ('fr', 'politeness', 1, 'S’il vous plaît', 'Please'),
    ('fr', 'politeness', 2, 'Merci', 'Thank you'),
    ('fr', 'politeness', 3, 'Pardon', 'Sorry / excuse me'),
    ('fr', 'politeness', 4, 'De rien', 'You are welcome'),
    ('fr', 'politeness', 5, 'Pouvez-vous aider ?', 'Can you help?'),
    ('fr', 'numbers', 1, 'un', 'one'),
    ('fr', 'numbers', 2, 'deux', 'two'),
    ('fr', 'numbers', 3, 'trois', 'three'),
    ('fr', 'numbers', 4, 'dix', 'ten'),
    ('fr', 'numbers', 5, 'cent', 'one hundred'),
    ('fr', 'family', 1, 'la famille', 'family'),
    ('fr', 'family', 2, 'la mère', 'mother'),
    ('fr', 'family', 3, 'le père', 'father'),
    ('fr', 'family', 4, 'le frère', 'brother'),
    ('fr', 'family', 5, 'la sœur', 'sister'),
    ('fr', 'food', 1, 'le pain', 'bread'),
    ('fr', 'food', 2, 'le fromage', 'cheese'),
    ('fr', 'food', 3, 'la pomme', 'apple'),
    ('fr', 'food', 4, 'le petit déjeuner', 'breakfast'),
    ('fr', 'food', 5, 'délicieux', 'tasty'),
    ('fr', 'drinks', 1, 'l’eau', 'water'),
    ('fr', 'drinks', 2, 'le café', 'coffee'),
    ('fr', 'drinks', 3, 'le thé', 'tea'),
    ('fr', 'drinks', 4, 'la bière', 'beer'),
    ('fr', 'drinks', 5, 'un verre', 'a glass'),
    ('fr', 'home', 1, 'la maison', 'house'),
    ('fr', 'home', 2, 'l’appartement', 'apartment'),
    ('fr', 'home', 3, 'la chambre', 'room'),
    ('fr', 'home', 4, 'la cuisine', 'kitchen'),
    ('fr', 'home', 5, 'la clé', 'key'),
    ('fr', 'travel', 1, 'la gare', 'train station'),
    ('fr', 'travel', 2, 'l’aéroport', 'airport'),
    ('fr', 'travel', 3, 'le billet', 'ticket'),
    ('fr', 'travel', 4, 'la valise', 'suitcase'),
    ('fr', 'travel', 5, 'partir', 'to depart'),
    ('fr', 'directions', 1, 'à gauche', 'left'),
    ('fr', 'directions', 2, 'à droite', 'right'),
    ('fr', 'directions', 3, 'tout droit', 'straight ahead'),
    ('fr', 'directions', 4, 'la rue', 'street'),
    ('fr', 'directions', 5, 'Où est ... ?', 'Where is ...?'),
    ('fr', 'time-calendar', 1, 'aujourd’hui', 'today'),
    ('fr', 'time-calendar', 2, 'demain', 'tomorrow'),
    ('fr', 'time-calendar', 3, 'hier', 'yesterday'),
    ('fr', 'time-calendar', 4, 'l’heure', 'time; hour'),
    ('fr', 'time-calendar', 5, 'lundi', 'Monday'),
    ('fr', 'weather', 1, 'ensoleillé', 'sunny'),
    ('fr', 'weather', 2, 'pluvieux', 'rainy'),
    ('fr', 'weather', 3, 'le vent', 'wind'),
    ('fr', 'weather', 4, 'froid', 'cold'),
    ('fr', 'weather', 5, 'chaud', 'warm'),
    ('fr', 'shopping', 1, 'acheter', 'to buy'),
    ('fr', 'shopping', 2, 'le prix', 'price'),
    ('fr', 'shopping', 3, 'cher', 'expensive'),
    ('fr', 'shopping', 4, 'bon marché', 'cheap'),
    ('fr', 'shopping', 5, 'la taille', 'size'),
    ('fr', 'work-school', 1, 'le travail', 'work'),
    ('fr', 'work-school', 2, 'l’école', 'school'),
    ('fr', 'work-school', 3, 'le professeur', 'teacher'),
    ('fr', 'work-school', 4, 'apprendre', 'to learn'),
    ('fr', 'work-school', 5, 'le bureau', 'office'),
    ('fr', 'body-health', 1, 'la tête', 'head'),
    ('fr', 'body-health', 2, 'la main', 'hand'),
    ('fr', 'body-health', 3, 'le médecin', 'doctor'),
    ('fr', 'body-health', 4, 'malade', 'ill'),
    ('fr', 'body-health', 5, 'J’ai mal', 'It hurts'),
    ('fr', 'emotions', 1, 'heureux', 'happy'),
    ('fr', 'emotions', 2, 'triste', 'sad'),
    ('fr', 'emotions', 3, 'fatigué', 'tired'),
    ('fr', 'emotions', 4, 'excité', 'excited'),
    ('fr', 'emotions', 5, 'avoir peur', 'to be afraid'),
    ('fr', 'hobbies', 1, 'lire', 'to read'),
    ('fr', 'hobbies', 2, 'écouter de la musique', 'to listen to music'),
    ('fr', 'hobbies', 3, 'cuisiner', 'to cook'),
    ('fr', 'hobbies', 4, 'faire du sport', 'to do sport'),
    ('fr', 'hobbies', 5, 'danser', 'to dance'),
    ('fr', 'nature-animals', 1, 'le chien', 'dog'),
    ('fr', 'nature-animals', 2, 'le chat', 'cat'),
    ('fr', 'nature-animals', 3, 'l’arbre', 'tree'),
    ('fr', 'nature-animals', 4, 'la forêt', 'forest'),
    ('fr', 'nature-animals', 5, 'l’oiseau', 'bird'),
    ('fr', 'long-words', 1, 'anticonstitutionnellement', 'in an unconstitutional manner'),
    ('fr', 'long-words', 2, 'vraisemblablement', 'probably / plausibly'),
    ('fr', 'long-words', 3, 'incompréhensibilité', 'incomprehensibility'),
    ('fr', 'long-words', 4, 'intergouvernementalisation', 'intergovernmentalization'),
    ('fr', 'long-words', 5, 'désinstitutionnalisation', 'deinstitutionalization'),
    ('fr', 'funny-unusual-words', 1, 'pamplemousse', 'grapefruit'),
    ('fr', 'funny-unusual-words', 2, 'gribouillis', 'scribble'),
    ('fr', 'funny-unusual-words', 3, 'ronchonner', 'to grumble'),
    ('fr', 'funny-unusual-words', 4, 'flâner', 'to stroll aimlessly'),
    ('fr', 'funny-unusual-words', 5, 'chouchou', 'teacher’s pet / darling')
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
INNER JOIN Courses c ON c.Id = l.CourseId AND c.Code = 'fr';

INSERT OR IGNORE INTO Quizzes (LessonId, Title)
SELECT l.Id, l.Title || ' Quiz'
FROM Lessons l
INNER JOIN Courses c ON c.Id = l.CourseId
WHERE c.Code = 'fr';

WITH QuestionSeeds (CourseCode, LessonSlug, SortOrder, Content, Type, QuestionData, CorrectAnswer) AS (
    VALUES
    ('fr', 'greetings', 1, 'Which French word or phrase means “Hello”?', 'multiple_choice', '{"options":["Bonjour","Salut","Bienvenue"]}', 'Bonjour'),
    ('fr', 'greetings', 2, 'Which informal French greeting means “Hi”?', 'multiple_choice', '{"options":["Bienvenue","Oui","Salut"]}', 'Salut'),
    ('fr', 'greetings', 3, 'Which French word or phrase means “Welcome”?', 'multiple_choice', '{"options":["Non","Bienvenue","Oui"]}', 'Bienvenue'),
    ('fr', 'greetings', 4, 'Which French word or phrase means “Yes”?', 'multiple_choice', '{"options":["Oui","Non","Bonjour"]}', 'Oui'),
    ('fr', 'greetings', 5, 'Put this lesson sentence in French order: “Hello, Léa! Welcome to Paris.”', 'word_ordering', '{"tokens":["Léa","à","Bonjour","Bienvenue","Paris"]}', '["Bonjour","Léa","Bienvenue","à","Paris"]'),
    ('fr', 'greetings', 6, 'Put this lesson sentence in French order: “Hi, Paul!”', 'word_ordering', '{"tokens":["Paul","Salut"]}', '["Salut","Paul"]'),
    ('fr', 'greetings', 7, 'Put this lesson sentence in French order: “Hello, Léa! Welcome to Paris. How are you?”', 'word_ordering', '{"tokens":["Léa","à","Ça","Bonjour","Bienvenue","Paris","va"]}', '["Bonjour","Léa","Bienvenue","à","Paris","Ça","va"]'),
    ('fr', 'greetings', 8, 'Type the French word or phrase for “No”.', 'free_text', '{}', 'Non'),
    ('fr', 'greetings', 9, 'Translate into French: “Hello, Léa! Welcome to Paris.”', 'free_text', '{}', 'Bonjour, Léa ! Bienvenue à Paris.'),
    ('fr', 'greetings', 10, 'Translate into French: “Hi, Paul!”', 'free_text', '{}', 'Salut, Paul !'),
    ('fr', 'introductions', 1, 'Which French word or phrase means “My name is ...”?', 'multiple_choice', '{"options":["Je m’appelle ...","Comment tu t’appelles ?","Je viens de ..."]}', 'Je m’appelle ...'),
    ('fr', 'introductions', 2, 'Which French word or phrase means “What is your name?”', 'multiple_choice', '{"options":["Je viens de ...","Enchanté(e)","Comment tu t’appelles ?"]}', 'Comment tu t’appelles ?'),
    ('fr', 'introductions', 3, 'Which French word or phrase means “I come from ...”?', 'multiple_choice', '{"options":["Voici ...","Je viens de ...","Enchanté(e)"]}', 'Je viens de ...'),
    ('fr', 'introductions', 4, 'Which French word or phrase means “Nice to meet you”?', 'multiple_choice', '{"options":["Enchanté(e)","Voici ...","Je m’appelle ..."]}', 'Enchanté(e)'),
    ('fr', 'introductions', 5, 'Put this lesson sentence in French order: “My name is Camille and I come from Canada.”', 'word_ordering', '{"tokens":["m’appelle","et","viens","Canada","Je","Camille","je","du"]}', '["Je","m’appelle","Camille","et","je","viens","du","Canada"]'),
    ('fr', 'introductions', 6, 'Put this lesson sentence in French order: “What is your name? — My name is Noé.”', 'word_ordering', '{"tokens":["tu","Je","Noé","Comment","t’appelles","m’appelle"]}', '["Comment","tu","t’appelles","Je","m’appelle","Noé"]'),
    ('fr', 'introductions', 7, 'Put this lesson sentence in French order: “Hello! My name is Camille. What is your name?”', 'word_ordering', '{"tokens":["Je","Camille","tu","Bonjour","m’appelle","Comment","t’appelles"]}', '["Bonjour","Je","m’appelle","Camille","Comment","tu","t’appelles"]'),
    ('fr', 'introductions', 8, 'Type the French word or phrase for “This is ...”.', 'free_text', '{}', 'Voici ...'),
    ('fr', 'introductions', 9, 'Translate into French: “My name is Camille and I come from Canada.”', 'free_text', '{}', 'Je m’appelle Camille et je viens du Canada.'),
    ('fr', 'introductions', 10, 'Translate into French: “What is your name? — My name is Noé.”', 'free_text', '{}', 'Comment tu t’appelles ? — Je m’appelle Noé.'),
    ('fr', 'politeness', 1, 'Which French word or phrase means “Please”?', 'multiple_choice', '{"options":["S’il vous plaît","Merci","Pardon"]}', 'S’il vous plaît'),
    ('fr', 'politeness', 2, 'Which French word or phrase means “Thank you”?', 'multiple_choice', '{"options":["Pardon","De rien","Merci"]}', 'Merci'),
    ('fr', 'politeness', 3, 'Which French word or phrase means “Sorry / excuse me”?', 'multiple_choice', '{"options":["Pouvez-vous aider ?","Pardon","De rien"]}', 'Pardon'),
    ('fr', 'politeness', 4, 'Which French word or phrase means “You are welcome”?', 'multiple_choice', '{"options":["De rien","Pouvez-vous aider ?","S’il vous plaît"]}', 'De rien'),
    ('fr', 'politeness', 5, 'Put this lesson sentence in French order: “A coffee, please. Thank you very much.”', 'word_ordering', '{"tokens":["vous","un","Merci","S’il","plaît","café","beaucoup"]}', '["S’il","vous","plaît","un","café","Merci","beaucoup"]'),
    ('fr', 'politeness', 6, 'Put this lesson sentence in French order: “Excuse me, can you help me?”', 'word_ordering', '{"tokens":["pouvez-vous","Pardon","m’aider"]}', '["Pardon","pouvez-vous","m’aider"]'),
    ('fr', 'politeness', 7, 'Put this lesson sentence in French order: “Excuse me, can you help me, please?”', 'word_ordering', '{"tokens":["pouvez-vous","s’il","plaît","Pardon","m’aider","vous"]}', '["Pardon","pouvez-vous","m’aider","s’il","vous","plaît"]'),
    ('fr', 'politeness', 8, 'Type the French word or phrase for “Can you help?”', 'free_text', '{}', 'Pouvez-vous aider ?'),
    ('fr', 'politeness', 9, 'Translate into French: “A coffee, please. Thank you very much.”', 'free_text', '{}', 'S’il vous plaît, un café. Merci beaucoup.'),
    ('fr', 'politeness', 10, 'Translate into French: “Excuse me, can you help me?”', 'free_text', '{}', 'Pardon, pouvez-vous m’aider ?'),
    ('fr', 'numbers', 1, 'Which French word or phrase means “one”?', 'multiple_choice', '{"options":["un","deux","trois"]}', 'un'),
    ('fr', 'numbers', 2, 'Which French word or phrase means “two”?', 'multiple_choice', '{"options":["trois","dix","deux"]}', 'deux'),
    ('fr', 'numbers', 3, 'Which French word or phrase means “three”?', 'multiple_choice', '{"options":["cent","trois","dix"]}', 'trois'),
    ('fr', 'numbers', 4, 'Which French word or phrase means “ten”?', 'multiple_choice', '{"options":["dix","cent","un"]}', 'dix'),
    ('fr', 'numbers', 5, 'Put this lesson sentence in French order: “I have two tickets and ten euros.”', 'word_ordering', '{"tokens":["deux","et","euros","J’ai","billets","dix"]}', '["J’ai","deux","billets","et","dix","euros"]'),
    ('fr', 'numbers', 6, 'Put this lesson sentence in French order: “The train leaves at three o’clock.”', 'word_ordering', '{"tokens":["train","à","heures","Le","part","trois"]}', '["Le","train","part","à","trois","heures"]'),
    ('fr', 'numbers', 7, 'Put this lesson sentence in French order: “two tickets and ten euros”', 'word_ordering', '{"tokens":["billets","dix","deux","et","euros"]}', '["deux","billets","et","dix","euros"]'),
    ('fr', 'numbers', 8, 'Type the French word or phrase for “one hundred”.', 'free_text', '{}', 'cent'),
    ('fr', 'numbers', 9, 'Translate into French: “I have two tickets and ten euros.”', 'free_text', '{}', 'J’ai deux billets et dix euros.'),
    ('fr', 'numbers', 10, 'Translate into French: “The train leaves at three o’clock.”', 'free_text', '{}', 'Le train part à trois heures.'),
    ('fr', 'family', 1, 'Which French word or phrase means “family”?', 'multiple_choice', '{"options":["la famille","la mère","le père"]}', 'la famille'),
    ('fr', 'family', 2, 'Which French word or phrase means “mother”?', 'multiple_choice', '{"options":["le père","le frère","la mère"]}', 'la mère'),
    ('fr', 'family', 3, 'Which French word or phrase means “father”?', 'multiple_choice', '{"options":["la sœur","le père","le frère"]}', 'le père'),
    ('fr', 'family', 4, 'Which French word or phrase means “brother”?', 'multiple_choice', '{"options":["le frère","la sœur","la famille"]}', 'le frère'),
    ('fr', 'family', 5, 'Put this lesson sentence in French order: “Here are my mother and my father.”', 'word_ordering', '{"tokens":["ma","et","père","Voici","mère","mon"]}', '["Voici","ma","mère","et","mon","père"]'),
    ('fr', 'family', 6, 'Put this lesson sentence in French order: “My brother has a sister.”', 'word_ordering', '{"tokens":["frère","une","Mon","a","sœur"]}', '["Mon","frère","a","une","sœur"]'),
    ('fr', 'family', 7, 'Put this lesson sentence in French order: “Here is my family: my mother and my father.”', 'word_ordering', '{"tokens":["ma","ma","et","père","Voici","famille","mère","mon"]}', '["Voici","ma","famille","ma","mère","et","mon","père"]'),
    ('fr', 'family', 8, 'Type the French word or phrase for “sister”.', 'free_text', '{}', 'la sœur'),
    ('fr', 'family', 9, 'Translate into French: “Here are my mother and my father.”', 'free_text', '{}', 'Voici ma mère et mon père.'),
    ('fr', 'family', 10, 'Translate into French: “My brother has a sister.”', 'free_text', '{}', 'Mon frère a une sœur.'),
    ('fr', 'food', 1, 'Which French word or phrase means “bread”?', 'multiple_choice', '{"options":["le pain","le fromage","la pomme"]}', 'le pain'),
    ('fr', 'food', 2, 'Which French word or phrase means “cheese”?', 'multiple_choice', '{"options":["la pomme","le petit déjeuner","le fromage"]}', 'le fromage'),
    ('fr', 'food', 3, 'Which French word or phrase means “apple”?', 'multiple_choice', '{"options":["délicieux","la pomme","le petit déjeuner"]}', 'la pomme'),
    ('fr', 'food', 4, 'Which French word or phrase means “breakfast”?', 'multiple_choice', '{"options":["le petit déjeuner","délicieux","le pain"]}', 'le petit déjeuner'),
    ('fr', 'food', 5, 'Put this lesson sentence in French order: “I eat bread and cheese.”', 'word_ordering', '{"tokens":["mange","pain","du","Je","du","et","fromage"]}', '["Je","mange","du","pain","et","du","fromage"]'),
    ('fr', 'food', 6, 'Put this lesson sentence in French order: “The apple is delicious.”', 'word_ordering', '{"tokens":["pomme","délicieuse","La","est"]}', '["La","pomme","est","délicieuse"]'),
    ('fr', 'food', 7, 'Put this lesson sentence in French order: “What do you eat for breakfast?”', 'word_ordering', '{"tokens":["que","manges","petit","Qu’est-ce","tu","au","déjeuner"]}', '["Qu’est-ce","que","tu","manges","au","petit","déjeuner"]'),
    ('fr', 'food', 8, 'Type the French word or phrase for “tasty”.', 'free_text', '{}', 'délicieux'),
    ('fr', 'food', 9, 'Translate into French: “I eat bread and cheese.”', 'free_text', '{}', 'Je mange du pain et du fromage.'),
    ('fr', 'food', 10, 'Translate into French: “The apple is delicious.”', 'free_text', '{}', 'La pomme est délicieuse.'),
    ('fr', 'drinks', 1, 'Which French word or phrase means “water”?', 'multiple_choice', '{"options":["l’eau","le café","le thé"]}', 'l’eau'),
    ('fr', 'drinks', 2, 'Which French word or phrase means “coffee”?', 'multiple_choice', '{"options":["le thé","la bière","le café"]}', 'le café'),
    ('fr', 'drinks', 3, 'Which French word or phrase means “tea”?', 'multiple_choice', '{"options":["un verre","le thé","la bière"]}', 'le thé'),
    ('fr', 'drinks', 4, 'Which French word or phrase means “beer”?', 'multiple_choice', '{"options":["la bière","un verre","l’eau"]}', 'la bière'),
    ('fr', 'drinks', 5, 'Put this lesson sentence in French order: “I would like a glass of water, please.”', 'word_ordering', '{"tokens":["voudrais","verre","s’il","plaît","Je","un","d’eau","vous"]}', '["Je","voudrais","un","verre","d’eau","s’il","vous","plaît"]'),
    ('fr', 'drinks', 6, 'Put this lesson sentence in French order: “The coffee is hot and the tea is good.”', 'word_ordering', '{"tokens":["café","chaud","le","est","Le","est","et","thé","bon"]}', '["Le","café","est","chaud","et","le","thé","est","bon"]'),
    ('fr', 'drinks', 7, 'Put this lesson sentence in French order: “Do you want a coffee or a tea?”', 'word_ordering', '{"tokens":["veux","café","un","Tu","un","ou","thé"]}', '["Tu","veux","un","café","ou","un","thé"]'),
    ('fr', 'drinks', 8, 'Type the French word or phrase for “a glass”.', 'free_text', '{}', 'un verre'),
    ('fr', 'drinks', 9, 'Translate into French: “I would like a glass of water, please.”', 'free_text', '{}', 'Je voudrais un verre d’eau, s’il vous plaît.'),
    ('fr', 'drinks', 10, 'Translate into French: “The coffee is hot and the tea is good.”', 'free_text', '{}', 'Le café est chaud et le thé est bon.'),
    ('fr', 'home', 1, 'Which French word or phrase means “house”?', 'multiple_choice', '{"options":["la maison","l’appartement","la chambre"]}', 'la maison'),
    ('fr', 'home', 2, 'Which French word or phrase means “apartment”?', 'multiple_choice', '{"options":["la chambre","la cuisine","l’appartement"]}', 'l’appartement'),
    ('fr', 'home', 3, 'Which French word or phrase means “room”?', 'multiple_choice', '{"options":["la clé","la chambre","la cuisine"]}', 'la chambre'),
    ('fr', 'home', 4, 'Which French word or phrase means “kitchen”?', 'multiple_choice', '{"options":["la cuisine","la clé","la maison"]}', 'la cuisine'),
    ('fr', 'home', 5, 'Put this lesson sentence in French order: “The kitchen is in the apartment.”', 'word_ordering', '{"tokens":["cuisine","dans","La","est","l’appartement"]}', '["La","cuisine","est","dans","l’appartement"]'),
    ('fr', 'home', 6, 'Put this lesson sentence in French order: “Where is the key? — On the table.”', 'word_ordering', '{"tokens":["est","clé","la","Où","la","Sur","table"]}', '["Où","est","la","clé","Sur","la","table"]'),
    ('fr', 'home', 7, 'Put this lesson sentence in French order: “Where is the key?”', 'word_ordering', '{"tokens":["est","clé","Où","la"]}', '["Où","est","la","clé"]'),
    ('fr', 'home', 8, 'Type the French word or phrase for “key”.', 'free_text', '{}', 'la clé'),
    ('fr', 'home', 9, 'Translate into French: “The kitchen is in the apartment.”', 'free_text', '{}', 'La cuisine est dans l’appartement.'),
    ('fr', 'home', 10, 'Translate into French: “Where is the key? — On the table.”', 'free_text', '{}', 'Où est la clé ? — Sur la table.'),
    ('fr', 'travel', 1, 'Which French word or phrase means “train station”?', 'multiple_choice', '{"options":["la gare","l’aéroport","le billet"]}', 'la gare'),
    ('fr', 'travel', 2, 'Which French word or phrase means “airport”?', 'multiple_choice', '{"options":["le billet","la valise","l’aéroport"]}', 'l’aéroport'),
    ('fr', 'travel', 3, 'Which French word or phrase means “ticket”?', 'multiple_choice', '{"options":["partir","le billet","la valise"]}', 'le billet'),
    ('fr', 'travel', 4, 'Which French word or phrase means “suitcase”?', 'multiple_choice', '{"options":["la valise","partir","la gare"]}', 'la valise'),
    ('fr', 'travel', 5, 'Put this lesson sentence in French order: “The station is near the airport.”', 'word_ordering', '{"tokens":["gare","près","l’aéroport","La","est","de"]}', '["La","gare","est","près","de","l’aéroport"]'),
    ('fr', 'travel', 6, 'Put this lesson sentence in French order: “My ticket is in the suitcase.”', 'word_ordering', '{"tokens":["billet","dans","valise","Mon","est","la"]}', '["Mon","billet","est","dans","la","valise"]'),
    ('fr', 'travel', 7, 'Put this lesson sentence in French order: “At what time does the train leave?”', 'word_ordering', '{"tokens":["quelle","part","train","À","heure","le"]}', '["À","quelle","heure","part","le","train"]'),
    ('fr', 'travel', 8, 'Type the French word or phrase for “to depart”.', 'free_text', '{}', 'partir'),
    ('fr', 'travel', 9, 'Translate into French: “The station is near the airport.”', 'free_text', '{}', 'La gare est près de l’aéroport.'),
    ('fr', 'travel', 10, 'Translate into French: “My ticket is in the suitcase.”', 'free_text', '{}', 'Mon billet est dans la valise.'),
    ('fr', 'directions', 1, 'Which French word or phrase means “left”?', 'multiple_choice', '{"options":["à gauche","à droite","tout droit"]}', 'à gauche'),
    ('fr', 'directions', 2, 'Which French word or phrase means “right”?', 'multiple_choice', '{"options":["tout droit","la rue","à droite"]}', 'à droite'),
    ('fr', 'directions', 3, 'Which French word or phrase means “straight ahead”?', 'multiple_choice', '{"options":["Où est ... ?","tout droit","la rue"]}', 'tout droit'),
    ('fr', 'directions', 4, 'Which French word or phrase means “street”?', 'multiple_choice', '{"options":["la rue","Où est ... ?","à gauche"]}', 'la rue'),
    ('fr', 'directions', 5, 'Put this lesson sentence in French order: “Go straight ahead, then left.”', 'word_ordering', '{"tokens":["tout","puis","gauche","Allez","droit","à"]}', '["Allez","tout","droit","puis","à","gauche"]'),
    ('fr', 'directions', 6, 'Put this lesson sentence in French order: “Where is the street? — To the right of the museum.”', 'word_ordering', '{"tokens":["est","rue","droite","musée","Où","la","À","du"]}', '["Où","est","la","rue","À","droite","du","musée"]'),
    ('fr', 'directions', 7, 'Put this lesson sentence in French order: “Excuse me, where is the museum street?”', 'word_ordering', '{"tokens":["où","la","du","Pardon","est","rue","musée"]}', '["Pardon","où","est","la","rue","du","musée"]'),
    ('fr', 'directions', 8, 'Type the French word or phrase for “Where is ...?”', 'free_text', '{}', 'Où est ... ?'),
    ('fr', 'directions', 9, 'Translate into French: “Go straight ahead, then left.”', 'free_text', '{}', 'Allez tout droit, puis à gauche.'),
    ('fr', 'directions', 10, 'Translate into French: “Where is the street? — To the right of the museum.”', 'free_text', '{}', 'Où est la rue ? — À droite du musée.'),
    ('fr', 'time-calendar', 1, 'Which French word or phrase means “today”?', 'multiple_choice', '{"options":["aujourd’hui","demain","hier"]}', 'aujourd’hui'),
    ('fr', 'time-calendar', 2, 'Which French word or phrase means “tomorrow”?', 'multiple_choice', '{"options":["hier","l’heure","demain"]}', 'demain'),
    ('fr', 'time-calendar', 3, 'Which French word or phrase means “yesterday”?', 'multiple_choice', '{"options":["lundi","hier","l’heure"]}', 'hier'),
    ('fr', 'time-calendar', 4, 'Which French word or phrase means “time” or “hour”?', 'multiple_choice', '{"options":["l’heure","lundi","aujourd’hui"]}', 'l’heure'),
    ('fr', 'time-calendar', 5, 'Put this lesson sentence in French order: “Today is Monday; tomorrow I work.”', 'word_ordering', '{"tokens":["c’est","demain","travaille","Aujourd’hui","lundi","je"]}', '["Aujourd’hui","c’est","lundi","demain","je","travaille"]'),
    ('fr', 'time-calendar', 6, 'Put this lesson sentence in French order: “The class starts at eight o’clock.”', 'word_ordering', '{"tokens":["cours","à","heures","Le","commence","huit"]}', '["Le","cours","commence","à","huit","heures"]'),
    ('fr', 'time-calendar', 7, 'Put this lesson sentence in French order: “What day is it today?”', 'word_ordering', '{"tokens":["jour","aujourd’hui","Quel","sommes-nous"]}', '["Quel","jour","sommes-nous","aujourd’hui"]'),
    ('fr', 'time-calendar', 8, 'Type the French word or phrase for “Monday”.', 'free_text', '{}', 'lundi'),
    ('fr', 'time-calendar', 9, 'Translate into French: “Today is Monday; tomorrow I work.”', 'free_text', '{}', 'Aujourd’hui, c’est lundi; demain, je travaille.'),
    ('fr', 'time-calendar', 10, 'Translate into French: “The class starts at eight o’clock.”', 'free_text', '{}', 'Le cours commence à huit heures.'),
    ('fr', 'weather', 1, 'Which French word or phrase means “sunny”?', 'multiple_choice', '{"options":["ensoleillé","pluvieux","le vent"]}', 'ensoleillé'),
    ('fr', 'weather', 2, 'Which French word or phrase means “rainy”?', 'multiple_choice', '{"options":["le vent","froid","pluvieux"]}', 'pluvieux'),
    ('fr', 'weather', 3, 'Which French word or phrase means “wind”?', 'multiple_choice', '{"options":["chaud","le vent","froid"]}', 'le vent'),
    ('fr', 'weather', 4, 'Which French word or phrase means “cold”?', 'multiple_choice', '{"options":["froid","chaud","ensoleillé"]}', 'froid'),
    ('fr', 'weather', 5, 'Put this lesson sentence in French order: “It is cold and windy.”', 'word_ordering', '{"tokens":["fait","et","y","du","Il","froid","il","a","vent"]}', '["Il","fait","froid","et","il","y","a","du","vent"]'),
    ('fr', 'weather', 6, 'Put this lesson sentence in French order: “Tomorrow, the weather will be nice.”', 'word_ordering', '{"tokens":["il","beau","Demain","fera"]}', '["Demain","il","fera","beau"]'),
    ('fr', 'weather', 7, 'Put this lesson sentence in French order: “What is the weather today?”', 'word_ordering', '{"tokens":["temps","aujourd’hui","Quel","fait-il"]}', '["Quel","temps","fait-il","aujourd’hui"]'),
    ('fr', 'weather', 8, 'Type the French word or phrase for “warm”.', 'free_text', '{}', 'chaud'),
    ('fr', 'weather', 9, 'Translate into French: “It is cold and windy.”', 'free_text', '{}', 'Il fait froid et il y a du vent.'),
    ('fr', 'weather', 10, 'Translate into French: “Tomorrow, the weather will be nice.”', 'free_text', '{}', 'Demain, il fera beau.'),
    ('fr', 'shopping', 1, 'Which French word or phrase means “to buy”?', 'multiple_choice', '{"options":["acheter","le prix","cher"]}', 'acheter'),
    ('fr', 'shopping', 2, 'Which French word or phrase means “price”?', 'multiple_choice', '{"options":["cher","bon marché","le prix"]}', 'le prix'),
    ('fr', 'shopping', 3, 'Which French word or phrase means “expensive”?', 'multiple_choice', '{"options":["la taille","cher","bon marché"]}', 'cher'),
    ('fr', 'shopping', 4, 'Which French word or phrase means “cheap”?', 'multiple_choice', '{"options":["bon marché","la taille","acheter"]}', 'bon marché'),
    ('fr', 'shopping', 5, 'Put this lesson sentence in French order: “How much does this shirt cost?”', 'word_ordering', '{"tokens":["coûte","chemise","Combien","cette"]}', '["Combien","coûte","cette","chemise"]'),
    ('fr', 'shopping', 6, 'Put this lesson sentence in French order: “It costs twenty euros; it is inexpensive.”', 'word_ordering', '{"tokens":["coûte","euros","est","marché","Elle","vingt","elle","bon"]}', '["Elle","coûte","vingt","euros","elle","est","bon","marché"]'),
    ('fr', 'shopping', 7, 'Put this lesson sentence in French order: “I would like to buy this shirt. What is the price?”', 'word_ordering', '{"tokens":["voudrais","cette","Quel","le","Je","acheter","chemise","est","prix"]}', '["Je","voudrais","acheter","cette","chemise","Quel","est","le","prix"]'),
    ('fr', 'shopping', 8, 'Type the French word or phrase for “size”.', 'free_text', '{}', 'la taille'),
    ('fr', 'shopping', 9, 'Translate into French: “How much does this shirt cost?”', 'free_text', '{}', 'Combien coûte cette chemise ?'),
    ('fr', 'shopping', 10, 'Translate into French: “It costs twenty euros; it is inexpensive.”', 'free_text', '{}', 'Elle coûte vingt euros; elle est bon marché.'),
    ('fr', 'work-school', 1, 'Which French word or phrase means “work”?', 'multiple_choice', '{"options":["le travail","l’école","le professeur"]}', 'le travail'),
    ('fr', 'work-school', 2, 'Which French word or phrase means “school”?', 'multiple_choice', '{"options":["le professeur","apprendre","l’école"]}', 'l’école'),
    ('fr', 'work-school', 3, 'Which French word or phrase means “teacher”?', 'multiple_choice', '{"options":["le bureau","le professeur","apprendre"]}', 'le professeur'),
    ('fr', 'work-school', 4, 'Which French word or phrase means “to learn”?', 'multiple_choice', '{"options":["apprendre","le bureau","le travail"]}', 'apprendre'),
    ('fr', 'work-school', 5, 'Put this lesson sentence in French order: “I learn French at school.”', 'word_ordering', '{"tokens":["le","à","J’apprends","français","l’école"]}', '["J’apprends","le","français","à","l’école"]'),
    ('fr', 'work-school', 6, 'Put this lesson sentence in French order: “The teacher works in his or her office.”', 'word_ordering', '{"tokens":["professeur","dans","bureau","Le","travaille","son"]}', '["Le","professeur","travaille","dans","son","bureau"]'),
    ('fr', 'work-school', 7, 'Put this lesson sentence in French order: “What do you do at school?”', 'word_ordering', '{"tokens":["que","fais","l’école","Qu’est-ce","tu","à"]}', '["Qu’est-ce","que","tu","fais","à","l’école"]'),
    ('fr', 'work-school', 8, 'Type the French word or phrase for “office”.', 'free_text', '{}', 'le bureau'),
    ('fr', 'work-school', 9, 'Translate into French: “I learn French at school.”', 'free_text', '{}', 'J’apprends le français à l’école.'),
    ('fr', 'work-school', 10, 'Translate into French: “The teacher works in his or her office.”', 'free_text', '{}', 'Le professeur travaille dans son bureau.'),
    ('fr', 'body-health', 1, 'Which French word or phrase means “head”?', 'multiple_choice', '{"options":["la tête","la main","le médecin"]}', 'la tête'),
    ('fr', 'body-health', 2, 'Which French word or phrase means “hand”?', 'multiple_choice', '{"options":["le médecin","malade","la main"]}', 'la main'),
    ('fr', 'body-health', 3, 'Which French word or phrase means “doctor”?', 'multiple_choice', '{"options":["J’ai mal","le médecin","malade"]}', 'le médecin'),
    ('fr', 'body-health', 4, 'Which French word or phrase means “ill”?', 'multiple_choice', '{"options":["malade","J’ai mal","la tête"]}', 'malade'),
    ('fr', 'body-health', 5, 'Put this lesson sentence in French order: “My head hurts; I am ill.”', 'word_ordering', '{"tokens":["mal","la","je","malade","J’ai","à","tête","suis"]}', '["J’ai","mal","à","la","tête","je","suis","malade"]'),
    ('fr', 'body-health', 6, 'Put this lesson sentence in French order: “The doctor looks at my hand.”', 'word_ordering', '{"tokens":["médecin","ma","Le","regarde","main"]}', '["Le","médecin","regarde","ma","main"]'),
    ('fr', 'body-health', 7, 'Put this lesson sentence in French order: “Are you not well?”', 'word_ordering', '{"tokens":["ne","pas","Ça","va"]}', '["Ça","ne","va","pas"]'),
    ('fr', 'body-health', 8, 'Type the French word or phrase for “It hurts”.', 'free_text', '{}', 'J’ai mal'),
    ('fr', 'body-health', 9, 'Translate into French: “My head hurts; I am ill.”', 'free_text', '{}', 'J’ai mal à la tête; je suis malade.'),
    ('fr', 'body-health', 10, 'Translate into French: “The doctor looks at my hand.”', 'free_text', '{}', 'Le médecin regarde ma main.'),
    ('fr', 'emotions', 1, 'Which French word or phrase means “happy”?', 'multiple_choice', '{"options":["heureux","triste","fatigué"]}', 'heureux'),
    ('fr', 'emotions', 2, 'Which French word or phrase means “sad”?', 'multiple_choice', '{"options":["fatigué","excité","triste"]}', 'triste'),
    ('fr', 'emotions', 3, 'Which French word or phrase means “tired”?', 'multiple_choice', '{"options":["avoir peur","fatigué","excité"]}', 'fatigué'),
    ('fr', 'emotions', 4, 'Which French word or phrase means “excited”?', 'multiple_choice', '{"options":["excité","avoir peur","heureux"]}', 'excité'),
    ('fr', 'emotions', 5, 'Put this lesson sentence in French order: “I am happy, but tired.”', 'word_ordering', '{"tokens":["suis","mais","Je","heureuse","fatiguée"]}', '["Je","suis","heureuse","mais","fatiguée"]'),
    ('fr', 'emotions', 6, 'Put this lesson sentence in French order: “He is afraid of the dog.”', 'word_ordering', '{"tokens":["a","du","Il","peur","chien"]}', '["Il","a","peur","du","chien"]'),
    ('fr', 'emotions', 7, 'Put this lesson sentence in French order: “How do you feel?”', 'word_ordering', '{"tokens":["te","Comment","sens-tu"]}', '["Comment","te","sens-tu"]'),
    ('fr', 'emotions', 8, 'Type the French word or phrase for “to be afraid”.', 'free_text', '{}', 'avoir peur'),
    ('fr', 'emotions', 9, 'Translate into French: “I am happy, but tired.”', 'free_text', '{}', 'Je suis heureuse, mais fatiguée.'),
    ('fr', 'emotions', 10, 'Translate into French: “He is afraid of the dog.”', 'free_text', '{}', 'Il a peur du chien.'),
    ('fr', 'hobbies', 1, 'Which French word or phrase means “to read”?', 'multiple_choice', '{"options":["lire","écouter de la musique","cuisiner"]}', 'lire'),
    ('fr', 'hobbies', 2, 'Which French word or phrase means “to listen to music”?', 'multiple_choice', '{"options":["cuisiner","faire du sport","écouter de la musique"]}', 'écouter de la musique'),
    ('fr', 'hobbies', 3, 'Which French word or phrase means “to cook”?', 'multiple_choice', '{"options":["danser","cuisiner","faire du sport"]}', 'cuisiner'),
    ('fr', 'hobbies', 4, 'Which French word or phrase means “to do sport”?', 'multiple_choice', '{"options":["faire du sport","danser","lire"]}', 'faire du sport'),
    ('fr', 'hobbies', 5, 'Put this lesson sentence in French order: “I like reading and listening to music.”', 'word_ordering', '{"tokens":["lire","écouter","la","J’aime","et","de","musique"]}', '["J’aime","lire","et","écouter","de","la","musique"]'),
    ('fr', 'hobbies', 6, 'Put this lesson sentence in French order: “On Saturdays, I do sport.”', 'word_ordering', '{"tokens":["samedi","fais","sport","Le","je","du"]}', '["Le","samedi","je","fais","du","sport"]'),
    ('fr', 'hobbies', 7, 'Put this lesson sentence in French order: “What do you like to do on the weekend?”', 'word_ordering', '{"tokens":["que","aimes","le","Qu’est-ce","tu","faire","week-end"]}', '["Qu’est-ce","que","tu","aimes","faire","le","week-end"]'),
    ('fr', 'hobbies', 8, 'Type the French word or phrase for “to dance”.', 'free_text', '{}', 'danser'),
    ('fr', 'hobbies', 9, 'Translate into French: “I like reading and listening to music.”', 'free_text', '{}', 'J’aime lire et écouter de la musique.'),
    ('fr', 'hobbies', 10, 'Translate into French: “On Saturdays, I do sport.”', 'free_text', '{}', 'Le samedi, je fais du sport.'),
    ('fr', 'nature-animals', 1, 'Which French word or phrase means “dog”?', 'multiple_choice', '{"options":["le chien","le chat","l’arbre"]}', 'le chien'),
    ('fr', 'nature-animals', 2, 'Which French word or phrase means “cat”?', 'multiple_choice', '{"options":["l’arbre","la forêt","le chat"]}', 'le chat'),
    ('fr', 'nature-animals', 3, 'Which French word or phrase means “tree”?', 'multiple_choice', '{"options":["l’oiseau","l’arbre","la forêt"]}', 'l’arbre'),
    ('fr', 'nature-animals', 4, 'Which French word or phrase means “forest”?', 'multiple_choice', '{"options":["la forêt","l’oiseau","le chien"]}', 'la forêt'),
    ('fr', 'nature-animals', 5, 'Put this lesson sentence in French order: “The cat sleeps under the tree.”', 'word_ordering', '{"tokens":["chat","sous","Le","dort","l’arbre"]}', '["Le","chat","dort","sous","l’arbre"]'),
    ('fr', 'nature-animals', 6, 'Put this lesson sentence in French order: “A bird flies above the forest.”', 'word_ordering', '{"tokens":["oiseau","au-dessus","la","Un","vole","de","forêt"]}', '["Un","oiseau","vole","au-dessus","de","la","forêt"]'),
    ('fr', 'nature-animals', 7, 'Put this lesson sentence in French order: “above the forest”', 'word_ordering', '{"tokens":["de","forêt","au-dessus","la"]}', '["au-dessus","de","la","forêt"]'),
    ('fr', 'nature-animals', 8, 'Type the French word or phrase for “bird”.', 'free_text', '{}', 'l’oiseau'),
    ('fr', 'nature-animals', 9, 'Translate into French: “The cat sleeps under the tree.”', 'free_text', '{}', 'Le chat dort sous l’arbre.'),
    ('fr', 'nature-animals', 10, 'Translate into French: “A bird flies above the forest.”', 'free_text', '{}', 'Un oiseau vole au-dessus de la forêt.'),
    ('fr', 'long-words', 1, 'Which French word or phrase means “in an unconstitutional manner”?', 'multiple_choice', '{"options":["anticonstitutionnellement","vraisemblablement","incompréhensibilité"]}', 'anticonstitutionnellement'),
    ('fr', 'long-words', 2, 'Which French word or phrase means “probably / plausibly”?', 'multiple_choice', '{"options":["incompréhensibilité","intergouvernementalisation","vraisemblablement"]}', 'vraisemblablement'),
    ('fr', 'long-words', 3, 'Which French word or phrase means “incomprehensibility”?', 'multiple_choice', '{"options":["désinstitutionnalisation","incompréhensibilité","intergouvernementalisation"]}', 'incompréhensibilité'),
    ('fr', 'long-words', 4, 'Which French word or phrase means “intergovernmentalization”?', 'multiple_choice', '{"options":["intergouvernementalisation","désinstitutionnalisation","anticonstitutionnellement"]}', 'intergouvernementalisation'),
    ('fr', 'long-words', 5, 'Put this lesson sentence in French order: “It is probably an unconstitutional decision.”', 'word_ordering', '{"tokens":["vraisemblablement","décision","C’est","une","anticonstitutionnelle"]}', '["C’est","vraisemblablement","une","décision","anticonstitutionnelle"]'),
    ('fr', 'long-words', 6, 'Put this lesson sentence in French order: “The incomprehensibility of the text is obvious.”', 'word_ordering', '{"tokens":["du","est","L’incompréhensibilité","texte","évidente"]}', '["L’incompréhensibilité","du","texte","est","évidente"]'),
    ('fr', 'long-words', 7, 'Put this lesson sentence in French order: “an unconstitutional decision”', 'word_ordering', '{"tokens":["décision","une","anticonstitutionnelle"]}', '["une","décision","anticonstitutionnelle"]'),
    ('fr', 'long-words', 8, 'Type the French word or phrase for “deinstitutionalization”.', 'free_text', '{}', 'désinstitutionnalisation'),
    ('fr', 'long-words', 9, 'Translate into French: “It is probably an unconstitutional decision.”', 'free_text', '{}', 'C’est vraisemblablement une décision anticonstitutionnelle.'),
    ('fr', 'long-words', 10, 'Translate into French: “The incomprehensibility of the text is obvious.”', 'free_text', '{}', 'L’incompréhensibilité du texte est évidente.'),
    ('fr', 'funny-unusual-words', 1, 'Which French word or phrase means “grapefruit”?', 'multiple_choice', '{"options":["pamplemousse","gribouillis","ronchonner"]}', 'pamplemousse'),
    ('fr', 'funny-unusual-words', 2, 'Which French word or phrase means “scribble”?', 'multiple_choice', '{"options":["ronchonner","flâner","gribouillis"]}', 'gribouillis'),
    ('fr', 'funny-unusual-words', 3, 'Which French word or phrase means “to grumble”?', 'multiple_choice', '{"options":["chouchou","ronchonner","flâner"]}', 'ronchonner'),
    ('fr', 'funny-unusual-words', 4, 'Which French word or phrase means “to stroll aimlessly”?', 'multiple_choice', '{"options":["flâner","chouchou","pamplemousse"]}', 'flâner'),
    ('fr', 'funny-unusual-words', 5, 'Put this lesson sentence in French order: “The grapefruit is on the table.”', 'word_ordering', '{"tokens":["pamplemousse","sur","table","Le","est","la"]}', '["Le","pamplemousse","est","sur","la","table"]'),
    ('fr', 'funny-unusual-words', 6, 'Put this lesson sentence in French order: “After class, I stroll and make scribbles.”', 'word_ordering', '{"tokens":["le","je","et","fais","gribouillis","Après","cours","flâne","je","des"]}', '["Après","le","cours","je","flâne","et","je","fais","des","gribouillis"]'),
    ('fr', 'funny-unusual-words', 7, 'Put this lesson sentence in French order: “after class”', 'word_ordering', '{"tokens":["le","Après","cours"]}', '["Après","le","cours"]'),
    ('fr', 'funny-unusual-words', 8, 'Type the French word or phrase for “teacher’s pet / darling”.', 'free_text', '{}', 'chouchou'),
    ('fr', 'funny-unusual-words', 9, 'Translate into French: “The grapefruit is on the table.”', 'free_text', '{}', 'Le pamplemousse est sur la table.'),
    ('fr', 'funny-unusual-words', 10, 'Translate into French: “After class, I stroll and make scribbles.”', 'free_text', '{}', 'Après le cours, je flâne et je fais des gribouillis.')
)
INSERT OR IGNORE INTO QuizQuestions
    (QuizId, SortOrder, Content, Type, QuestionData, CorrectAnswer)
SELECT q.Id, s.SortOrder, s.Content, s.Type, s.QuestionData, s.CorrectAnswer
FROM QuestionSeeds s
INNER JOIN Courses c ON c.Code = s.CourseCode AND c.Code = 'fr'
INNER JOIN Lessons l ON l.CourseId = c.Id AND l.Slug = s.LessonSlug
INNER JOIN Quizzes q ON q.LessonId = l.Id;
