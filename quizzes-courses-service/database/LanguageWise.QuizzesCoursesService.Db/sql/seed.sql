INSERT INTO Courses (Code, Title, Description) VALUES
    ('de', 'German', 'Build a practical foundation in German.'),
    ('fr', 'French', 'Learn useful everyday French.'),
    ('it', 'Italian', 'Start speaking and understanding Italian.'),
    ('nl', 'Dutch', 'Discover the essentials of Dutch.'),
    ('es', 'Spanish', 'Develop your everyday Spanish.'),
    ('pl', 'Polish', 'Build confidence with everyday Polish.');

-- A shared catalogue keeps all six courses in the same subject order.
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
-- Store content as rows rather than branching on course or lesson in a CASE expression.
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
| Bitte, ein Kaffee. Danke! | A coffee, please. Thank you! |
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

**Ohrwurm** literally means “earworm,” a tune you cannot stop hearing. **Wanderlust** is a desire to travel, and **Kopfkino** is a vivid imagined scene. Playful vocabulary should describe situations, not people.'),
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

**Pamplemousse** reached French through Dutch *pompelmoes*. Its sound is playful, but it is the ordinary word for grapefruit.'),
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

**Pantofolaio** comes from *pantofola* (“slipper”) and is affectionate only in a friendly tone. An **abbiocco** is the sleepy feeling after a large meal.'),
    ('nl', 'greetings', '## Learn in context

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
    ('nl', 'introductions', '## Learn in context

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
    ('nl', 'politeness', '## Learn in context

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
    ('nl', 'numbers', '## Learn in context

| Target language | English |
| --- | --- |
| Ik heb twee kaartjes en tien euro. | I have two tickets and ten euros. |
| De trein vertrekt om drie uur. | The train leaves at three o’clock. |

## Worked usage

- **Ik heb twee kaartjes en tien euro.** — *I have two tickets and ten euros.*
- **De trein vertrekt om drie uur.** — *The train leaves at three o’clock.*

## Language note

Dutch joins number words: **drieëntwintig**. The diaeresis shows the vowels are pronounced separately.'),
    ('nl', 'family', '## Learn in context

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
    ('nl', 'food', '## Learn in context

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
    ('nl', 'drinks', '## Learn in context

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
    ('nl', 'home', '## Learn in context

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
    ('nl', 'travel', '## Learn in context

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
    ('nl', 'directions', '## Learn in context

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
    ('nl', 'time-calendar', '## Learn in context

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
    ('nl', 'weather', '## Learn in context

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
    ('nl', 'shopping', '## Learn in context

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
    ('nl', 'work-school', '## Learn in context

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
    ('nl', 'body-health', '## Learn in context

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
    ('nl', 'emotions', '## Learn in context

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
    ('nl', 'hobbies', '## Learn in context

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
    ('nl', 'nature-animals', '## Learn in context

| Target language | English |
| --- | --- |
| De hond loopt in het bos. | The dog walks in the woods. |
| Een vogel zit in de boom. | A bird sits in the tree. |

## Worked usage

- **De hond loopt in het bos.** — *The dog walks in the woods.*
- **Een vogel zit in de boom.** — *A bird sits in the tree.*

## Language note

**Bos** means woods/forest. *Boom* is a tree and is a **de**-word.'),
    ('nl', 'long-words', '## Learn in context

| Target language | English |
| --- | --- |
| Meervoudigepersoonlijkheidsstoornis is een lang woord. | Multiple-personality disorder is a long word. |
| De arbeidsongeschiktheidsverzekering helpt bij ziekte. | Disability insurance helps during illness. |

## Worked usage

- **Meervoudigepersoonlijkheidsstoornis is een lang woord.** — *Multiple-personality disorder is a long word.*
- **De arbeidsongeschiktheidsverzekering helpt bij ziekte.** — *Disability insurance helps during illness.*

## Language note

Dutch compounds are written together: **meervoudige + persoonlijkheids + stoornis**. In care, use the current precise clinical term.'),
    ('nl', 'funny-unusual-words', '## Learn in context

| Target language | English |
| --- | --- |
| Bij ons thuis is het gezellig. | At our home it is cozy and sociable. |
| Na het eten gaan we uitwaaien op het strand. | After eating we go get fresh air on the beach. |

## Worked usage

- **Bij ons thuis is het gezellig.** — *At our home it is cozy and sociable.*
- **Na het eten gaan we uitwaaien op het strand.** — *After eating we go get fresh air on the beach.*

## Language note

**Gezellig** combines warmth, comfort, and company; English has no one exact equivalent. **Uitwaaien** is getting fresh air in the wind.'),
    ('es', 'greetings', '## Learn in context

| Target language | English |
| --- | --- |
| Hola, Ana. ¡Bienvenida a Madrid! | Hello, Ana. Welcome to Madrid! |
| Buenos días, señor. | Good morning, sir. |

## Mini dialogue

> **A:** Hola, Ana. ¡Bienvenida a Madrid! ¿Qué tal?
> *Hello, Ana. Welcome to Madrid! How are you?*
>
> **B:** ¡Buenos días! Muchas gracias.
> *Good morning! Thank you very much.*
>
> **A:** ¿Estás lista?
> *Are you ready?*
>
> **B:** ¡Sí!
> *Yes!*

## Language note

**Hola** works any time. **Buenos días** is a polite morning greeting; Spanish uses inverted punctuation.'),
    ('es', 'introductions', '## Learn in context

| Target language | English |
| --- | --- |
| Me llamo Sofía y vengo de Canadá. | My name is Sofía and I come from Canada. |
| ¿Cómo te llamas? — Me llamo Diego. | What is your name? — My name is Diego. |

## Mini dialogue

> **A:** ¡Hola! Me llamo Sofía. ¿Cómo te llamas?
> *Hi! My name is Sofía. What is your name?*
>
> **B:** Me llamo Diego. ¡Mucho gusto!
> *My name is Diego. Nice to meet you!*
>
> **A:** Vengo de Canadá. ¿Y tú?
> *I come from Canada. And you?*
>
> **B:** Vengo de México.
> *I come from Mexico.*

## Language note

Spanish often omits **yo** because the ending in **llamo** already identifies “I.”'),
    ('es', 'politeness', '## Learn in context

| Target language | English |
| --- | --- |
| Un café, por favor. Muchas gracias. | A coffee, please. Thank you very much. |
| Perdón, ¿puede ayudarme? | Excuse me, can you help me? |

## Mini dialogue

> **A:** Perdón, ¿puede ayudarme, por favor?
> *Excuse me, can you help me, please?*
>
> **B:** Sí, claro.
> *Yes, of course.*
>
> **A:** Un café, por favor. ¡Muchas gracias!
> *A coffee, please. Thank you very much!*
>
> **B:** ¡De nada!
> *You’re welcome!*

## Language note

**Por favor** means “please.” **Perdón** gets attention or apologises; *disculpe* is another polite option.'),
    ('es', 'numbers', '## Learn in context

| Target language | English |
| --- | --- |
| Tengo dos billetes y diez euros. | I have two tickets and ten euros. |
| El tren sale a las tres. | The train leaves at three. |

## Worked usage

- **Tengo dos billetes y diez euros.** — *I have two tickets and ten euros.*
- **El tren sale a las tres.** — *The train leaves at three.*

## Language note

Use **a la una** for one o’clock but **a las** for every other hour.'),
    ('es', 'family', '## Learn in context

| Target language | English |
| --- | --- |
| Esta es mi madre y este es mi padre. | This is my mother and this is my father. |
| Mi hermana tiene un hermano. | My sister has a brother. |

## Mini dialogue

> **A:** ¿Es esta tu familia?
> *Is this your family?*
>
> **B:** Sí. Esta es mi madre y este es mi padre.
> *Yes. This is my mother and this is my father.*
>
> **A:** ¿Tienes un hermano?
> *Do you have a brother?*
>
> **B:** No, pero tengo una hermana.
> *No, but I have a sister.*

## Language note

**Mi** does not change for gender: *mi madre*, *mi padre*. It changes for plural: *mis padres*.'),
    ('es', 'food', '## Learn in context

| Target language | English |
| --- | --- |
| Como pan y queso en el desayuno. | I eat bread and cheese at breakfast. |
| La manzana está deliciosa. | The apple is delicious. |

## Mini dialogue

> **A:** ¿Qué comes en el desayuno?
> *What do you eat for breakfast?*
>
> **B:** Como pan y queso.
> *I eat bread and cheese.*
>
> **A:** ¿Y la manzana?
> *And the apple?*
>
> **B:** ¡La manzana está deliciosa!
> *The apple is delicious!*

## Language note

**Está deliciosa** describes the apple’s current taste; *es deliciosa* describes it more generally.'),
    ('es', 'drinks', '## Learn in context

| Target language | English |
| --- | --- |
| Quisiera un vaso de agua, por favor. | I would like a glass of water, please. |
| El café está caliente y el té está tibio. | The coffee is hot and the tea is lukewarm. |

## Mini dialogue

> **A:** ¿Quieres un café o un té?
> *Do you want a coffee or a tea?*
>
> **B:** Un vaso de agua, por favor. El café está muy caliente.
> *A glass of water, please. The coffee is very hot.*

## Language note

**Quisiera** is a polite “I would like.” Spanish uses **un vaso de agua** for a glass of water.'),
    ('es', 'home', '## Learn in context

| Target language | English |
| --- | --- |
| La cocina está en el apartamento. | The kitchen is in the apartment. |
| ¿Dónde está la llave? — En la mesa. | Where is the key? — On the table. |

## Mini dialogue

> **A:** ¿Dónde está la llave?
> *Where is the key?*
>
> **B:** La llave está en la cocina.
> *The key is in the kitchen.*
>
> **A:** ¿Y tu habitación?
> *And your room?*
>
> **B:** Mi habitación está en el apartamento.
> *My room is in the apartment.*

## Language note

Spanish contracts **a el** to *al* and **de el** to *del*, but not with *la*.'),
    ('es', 'travel', '## Learn in context

| Target language | English |
| --- | --- |
| La estación está cerca del aeropuerto. | The station is near the airport. |
| El billete está en la maleta. | The ticket is in the suitcase. |

## Mini dialogue

> **A:** ¿A qué hora sale el tren?
> *At what time does the train leave?*
>
> **B:** A las tres, en la estación.
> *At three, at the station.*
>
> **A:** ¿Dónde está mi billete?
> *Where is my ticket?*
>
> **B:** Tu billete está en la maleta.
> *Your ticket is in the suitcase.*

## Language note

**Billete** can be a ticket or a banknote; say *billete de tren* when clarity helps.'),
    ('es', 'directions', '## Learn in context

| Target language | English |
| --- | --- |
| Siga todo recto y gire a la izquierda. | Go straight ahead and turn left. |
| ¿Dónde está la calle? — A la derecha del banco. | Where is the street? — To the right of the bank. |

## Mini dialogue

> **A:** Perdón, ¿dónde está la calle del banco?
> *Excuse me, where is the bank’s street?*
>
> **B:** Siga todo recto y luego gire a la izquierda.
> *Go straight ahead and then turn left.*
>
> **A:** ¿Y el banco?
> *And the bank?*
>
> **B:** Está a la derecha.
> *It is on the right.*

## Language note

**Siga** and **gire** are polite **usted** commands. Use *sigue* and *gira* with a friend.'),
    ('es', 'time-calendar', '## Learn in context

| Target language | English |
| --- | --- |
| Hoy es lunes; mañana tengo clase. | Today is Monday; tomorrow I have class. |
| La clase empieza a las ocho. | The class starts at eight. |

## Mini dialogue

> **A:** ¿Qué día es hoy?
> *What day is today?*
>
> **B:** Hoy es lunes.
> *Today is Monday.*
>
> **A:** ¿A qué hora empieza la clase?
> *At what time does the class start?*
>
> **B:** Mañana a las ocho.
> *Tomorrow at eight.*

## Language note

Days and months are lowercase. **Mañana** can mean “tomorrow” or “morning.”'),
    ('es', 'weather', '## Learn in context

| Target language | English |
| --- | --- |
| Hoy hace sol, pero hace frío. | Today it is sunny, but it is cold. |
| Mañana estará lluvioso y habrá viento. | Tomorrow it will be rainy and there will be wind. |

## Mini dialogue

> **A:** ¿Qué tiempo hace hoy?
> *What is the weather today?*
>
> **B:** Está soleado, pero hace frío.
> *It is sunny, but it is cold.*
>
> **A:** ¿Y mañana?
> *And tomorrow?*
>
> **B:** Mañana estará lluvioso y habrá mucho viento.
> *Tomorrow it will be rainy and there will be a lot of wind.*

## Language note

Spanish uses **hace** for conditions: *hace frío*. **Hay viento** means “there is wind.”'),
    ('es', 'shopping', '## Learn in context

| Target language | English |
| --- | --- |
| ¿Cuánto cuesta esta camisa? | How much does this shirt cost? |
| Cuesta veinte euros; la talla es mediana. | It costs twenty euros; the size is medium. |

## Mini dialogue

> **A:** Quiero comprar esta camisa. ¿Cuál es el precio?
> *I want to buy this shirt. What is the price?*
>
> **B:** Cuesta veinte euros.
> *It costs twenty euros.*
>
> **A:** ¡No es caro! ¿Tiene mi talla?
> *It’s not expensive! Do you have my size?*
>
> **B:** Sí, claro.
> *Yes, of course.*

## Language note

**Talla** is clothing size. *Barato* is cheap; **económico** can sound more complimentary.'),
    ('es', 'work-school', '## Learn in context

| Target language | English |
| --- | --- |
| Aprendo español en la escuela. | I learn Spanish at school. |
| La profesora trabaja en la oficina. | The teacher works in the office. |

## Mini dialogue

> **A:** ¿Qué haces en la escuela?
> *What do you do at school?*
>
> **B:** Aprendo español. Mi profesora es muy buena.
> *I learn Spanish. My teacher is very good.*
>
> **A:** ¿Y dónde está tu trabajo?
> *And where is your work?*
>
> **B:** Trabajo en la oficina.
> *I work in the office.*

## Language note

**Escuela** is school; *colegio* and *instituto* vary by country and educational level.'),
    ('es', 'body-health', '## Learn in context

| Target language | English |
| --- | --- |
| Me duele la cabeza; estoy enfermo. | My head hurts; I am ill. |
| El médico mira mi mano. | The doctor looks at my hand. |

## Mini dialogue

> **A:** ¿Cómo estás?
> *How are you?*
>
> **B:** No muy bien. Me duele la cabeza. Estoy enfermo.
> *Not very well. My head hurts. I am ill.*
>
> **A:** ¡Ve al médico!
> *Go to the doctor!*
>
> **B:** Sí, el médico mira mi mano también.
> *Yes, the doctor is also looking at my hand.*

## Language note

Use **me duele** with a singular body part and **me duelen** with plural *manos*.'),
    ('es', 'emotions', '## Learn in context

| Target language | English |
| --- | --- |
| Estoy feliz, pero cansada. | I am happy, but tired. |
| Él tiene miedo del perro. | He is afraid of the dog. |

## Mini dialogue

> **A:** ¿Cómo te sientes?
> *How do you feel?*
>
> **B:** Estoy muy feliz, pero cansada.
> *I am very happy, but tired.*
>
> **A:** ¿Y por qué está triste tu hermano?
> *And why is your brother sad?*
>
> **B:** Tiene miedo del perro.
> *He is afraid of the dog.*

## Language note

Use **estar** for temporary states such as *estoy cansada*. **Tener miedo** means “to have fear.”'),
    ('es', 'hobbies', '## Learn in context

| Target language | English |
| --- | --- |
| Me gusta leer y escuchar música. | I like reading and listening to music. |
| Los sábados hago deporte y bailo. | On Saturdays I do sport and dance. |

## Mini dialogue

> **A:** ¿Qué te gusta hacer los fines de semana?
> *What do you like to do on weekends?*
>
> **B:** Me gusta leer y escuchar música. ¿Y tú?
> *I like reading and listening to music. And you?*
>
> **A:** Me gusta hacer deporte y bailar.
> *I like doing sport and dancing.*

## Language note

**Me gusta** literally means “it is pleasing to me”; the next verb remains infinitive.'),
    ('es', 'nature-animals', '## Learn in context

| Target language | English |
| --- | --- |
| El perro corre por el bosque. | The dog runs through the forest. |
| Un pájaro está en el árbol. | A bird is in the tree. |

## Worked usage

- **El perro corre por el bosque.** — *The dog runs through the forest.*
- **Un pájaro está en el árbol.** — *A bird is in the tree.*

## Language note

Use **por** for movement through an area. **Árbol** has a written stress accent.'),
    ('es', 'long-words', '## Learn in context

| Target language | English |
| --- | --- |
| El electroencefalografista analiza el informe. | The electroencephalograph specialist analyses the report. |
| La anticonstitucionalidad es un tema jurídico. | Unconstitutionality is a legal topic. |

## Worked usage

- **El electroencefalografista analiza el informe.** — *The electroencephalograph specialist analyses the report.*
- **La anticonstitucionalidad es un tema jurídico.** — *Unconstitutionality is a legal topic.*

## Language note

Spanish technical words use Greek and Latin roots: **electro + encefalo + grafista**. Look for familiar chunks. **Otorrinolaringólogo** is another technical word: an ear, nose, and throat doctor.'),
    ('es', 'funny-unusual-words', '## Learn in context

| Target language | English |
| --- | --- |
| Después de comer, seguimos hablando durante la sobremesa. | After eating, we keep talking during the post-meal conversation. |
| Mañana tengo que madrugar. | Tomorrow I have to get up very early. |

## Worked usage

- **Después de comer, seguimos hablando durante la sobremesa.** — *After eating, we keep talking during the post-meal conversation.*
- **Mañana tengo que madrugar.** — *Tomorrow I have to get up very early.*

## Language note

**Sobremesa** is an unhurried post-meal conversation, valued in many Spanish-speaking places. **Madrugar** means getting up early.'),
    ('pl', 'greetings', '## Learn in context

| Target language | English |
| --- | --- |
| Cześć, Aniu! Witaj w Warszawie. | Hi, Ania! Welcome to Warsaw. |
| Dzień dobry, pani. | Good day, madam. |

## Mini dialogue

> **A:** Cześć, Aniu! Witaj w pięknej Warszawie.
> *Hi, Ania! Welcome to beautiful Warsaw.*
>
> **B:** Dzień dobry! Dziękuję bardzo.
> *Good day! Thank you very much.*
>
> **A:** Jesteś gotowa?
> *Are you ready?*
>
> **B:** Tak!
> *Yes!*

## Language note

**Cześć** is informal and can mean hi or bye. **Dzień dobry** is a safe polite daytime greeting.'),
    ('pl', 'introductions', '## Learn in context

| Target language | English |
| --- | --- |
| Mam na imię Ola i jestem z Kanady. | My name is Ola and I am from Canada. |
| Jak masz na imię? — Mam na imię Tomek. | What is your name? — My name is Tomek. |

## Mini dialogue

> **A:** Cześć! Mam na imię Ola. Jak masz na imię?
> *Hi! My name is Ola. What is your name?*
>
> **B:** Mam na imię Tomek. Miło mi!
> *My name is Tomek. Nice to meet you!*
>
> **A:** Jestem z Kanady. A ty?
> *I am from Canada. And you?*
>
> **B:** Jestem z Polski.
> *I am from Poland.*

## Language note

**Mam na imię** literally means “I have as a name,” a reliable beginner introduction.'),
    ('pl', 'politeness', '## Learn in context

| Target language | English |
| --- | --- |
| Poproszę kawę. Dziękuję! | A coffee, please. |
| Przepraszam, czy może mi pani pomóc? | Excuse me, can you help me? |

## Mini dialogue

> **A:** Przepraszam, czy może mi pani trochę pomóc?
> *Excuse me, can you help me a little, madam?*
>
> **B:** Tak, oczywiście.
> *Yes, of course.*
>
> **A:** Poproszę kawę. Bardzo dziękuję!
> *A coffee, please. Thank you very much!*
>
> **B:** Nie ma za co.
> *You’re welcome.*

## Language note

**Poproszę** is a natural ordering phrase. **Przepraszam** can mean both “sorry” and “excuse me.”'),
    ('pl', 'numbers', '## Learn in context

| Target language | English |
| --- | --- |
| Mam dwa bilety i dziesięć euro. | I have two tickets and ten euros. |
| Pociąg odjeżdża o trzeciej. | The train leaves at three. |

## Worked usage

- **Mam dwa bilety i dziesięć euro.** — *I have two tickets and ten euros.*
- **Pociąg odjeżdża o trzeciej.** — *The train leaves at three.*

## Language note

Numbers affect nouns: **dwa bilety** but *pięć biletów*. Learn numbers with a noun.'),
    ('pl', 'family', '## Learn in context

| Target language | English |
| --- | --- |
| To jest moja mama i mój tata. | This is my mum and my dad. |
| Moja siostra ma brata. | My sister has a brother. |

## Mini dialogue

> **A:** Czy to jest twoja rodzina?
> *Is this your family?*
>
> **B:** Tak. To jest moja mama i mój tata.
> *Yes. This is my mum and my dad.*
>
> **A:** Masz brata?
> *Do you have a brother?*
>
> **B:** Nie, ale mam siostrę.
> *No, but I have a sister.*

## Language note

Possessives agree with gender: **moja** mama, **mój** tata, **moje** dziecko.'),
    ('pl', 'food', '## Learn in context

| Target language | English |
| --- | --- |
| Na śniadanie jem chleb i ser. | For breakfast I eat bread and cheese. |
| Jabłko jest pyszne. | The apple is delicious. |

## Mini dialogue

> **A:** Co jesz na śniadanie?
> *What do you eat for breakfast?*
>
> **B:** Jem chleb i ser.
> *I eat bread and cheese.*
>
> **A:** A jabłko?
> *And the apple?*
>
> **B:** Jabłko jest smaczne!
> *The apple is tasty!*

## Language note

**Na śniadanie** uses *na* where English uses “for breakfast.”'),
    ('pl', 'drinks', '## Learn in context

| Target language | English |
| --- | --- |
| Poproszę szklankę wody. | A glass of water, please. |
| Kawa jest gorąca, a herbata ciepła. | Coffee is hot, and tea is warm. |

## Mini dialogue

> **A:** Chcesz kawę czy herbatę?
> *Do you want coffee or tea?*
>
> **B:** Poproszę szklankę wody. Kawa jest za gorąca.
> *A glass of water, please. The coffee is too hot.*

## Language note

After a measure word, Polish changes the noun: **szklanka wody**, not dictionary-form *woda*.'),
    ('pl', 'home', '## Learn in context

| Target language | English |
| --- | --- |
| Kuchnia jest w mieszkaniu. | The kitchen is in the apartment. |
| Gdzie jest klucz? — Na stole. | Where is the key? — On the table. |

## Mini dialogue

> **A:** Gdzie jest klucz?
> *Where is the key?*
>
> **B:** Klucz jest w kuchni.
> *The key is in the kitchen.*
>
> **A:** A twój pokój?
> *And your room?*
>
> **B:** Mój pokój jest w mieszkaniu.
> *My room is in the apartment.*

## Language note

Locations trigger cases: **na stole** means “on the table,” while the dictionary form is *stół*.'),
    ('pl', 'travel', '## Learn in context

| Target language | English |
| --- | --- |
| Dworzec jest blisko lotniska. | The station is near the airport. |
| Bilet jest w walizce. | The ticket is in the suitcase. |

## Mini dialogue

> **A:** O której wyjeżdża pociąg z dworca?
> *At what time does the train leave from the station?*
>
> **B:** O trzeciej.
> *At three.*
>
> **A:** Gdzie jest mój bilet?
> *Where is my ticket?*
>
> **B:** Twój bilet jest w walizce.
> *Your ticket is in the suitcase.*

## Language note

**Dworzec** is a major station; *stacja* is more often a stop or a station generally.'),
    ('pl', 'directions', '## Learn in context

| Target language | English |
| --- | --- |
| Idź prosto, potem w lewo. | Go straight ahead, then left. |
| Gdzie jest ulica? — Na prawo od hotelu. | Where is the street? — To the right of the hotel. |

## Mini dialogue

> **A:** Przepraszam, gdzie jest ulica do hotelu?
> *Excuse me, where is the street to the hotel?*
>
> **B:** Idź prosto, a potem w lewo.
> *Go straight ahead, and then left.*
>
> **A:** A hotel?
> *And the hotel?*
>
> **B:** Hotel jest po prawej stronie.
> *The hotel is on the right side.*

## Language note

**Idź** is an informal singular command. Add **proszę** to make a request more polite.'),
    ('pl', 'time-calendar', '## Learn in context

| Target language | English |
| --- | --- |
| Dziś jest poniedziałek, a jutro wtorek. | Today is Monday, and tomorrow is Tuesday. |
| Lekcja zaczyna się o ósmej. | The lesson starts at eight. |

## Mini dialogue

> **A:** Jaki dzisiaj jest dzień?
> *What day is today?*
>
> **B:** Dzisiaj jest poniedziałek.
> *Today is Monday.*
>
> **A:** O której zaczyna się lekcja?
> *At what time does the lesson start?*
>
> **B:** Jutro o ósmej.
> *Tomorrow at eight.*

## Language note

Polish says **o ósmej** (“at the eighth [hour]”), using an ordinal feminine form.'),
    ('pl', 'weather', '## Learn in context

| Target language | English |
| --- | --- |
| Dziś jest słonecznie, ale zimno. | Today it is sunny, but cold. |
| Jutro będzie deszczowo i wietrznie. | Tomorrow it will be rainy and windy. |

## Mini dialogue

> **A:** Jaka jest dzisiaj pogoda?
> *What is the weather today?*
>
> **B:** Jest słonecznie, ale zimno.
> *It is sunny, but cold.*
>
> **A:** A jutro?
> *And tomorrow?*
>
> **B:** Jutro będzie deszczowo i bardzo wietrznie.
> *Tomorrow it will be rainy and very windy.*

## Language note

Polish weather often uses adverbs: **jest zimno** and **jest słonecznie**. The adjective forms are **zimny** (“cold”) and **słoneczny** (“sunny”).'),
    ('pl', 'shopping', '## Learn in context

| Target language | English |
| --- | --- |
| Ile kosztuje ta koszula? | How much does this shirt cost? |
| Kosztuje dwadzieścia złotych; rozmiar jest średni. | It costs twenty zloty; the size is medium. |

## Mini dialogue

> **A:** Chcę kupić tę koszulę. Jaka jest cena?
> *I want to buy this shirt. What is the price?*
>
> **B:** Kosztuje dwadzieścia złotych.
> *It costs twenty zloty.*
>
> **A:** To tanio! Czy jest mój rozmiar?
> *That’s cheap! Is my size available?*
>
> **B:** Tak, oczywiście.
> *Yes, of course.*

## Language note

The Polish currency is the **złoty**. Ask **Jaka jest cena?** (“What is the price?”); after *dwadzieścia*, say **złotych**.'),
    ('pl', 'work-school', '## Learn in context

| Target language | English |
| --- | --- |
| Uczę się polskiego w szkole. | I learn Polish at school. |
| Nauczyciel pracuje w biurze. | The teacher works in the office. |

## Mini dialogue

> **A:** Co robisz w szkole?
> *What do you do at school?*
>
> **B:** Uczę się polskiego. Mój nauczyciel jest miły.
> *I learn Polish. My teacher is nice.*
>
> **A:** A gdzie jest twoja praca?
> *And where is your work?*
>
> **B:** Pracuję w biurze.
> *I work in the office.*

## Language note

**Uczyć się** requires genitive: **uczę się polskiego**, not dictionary form *polski*.'),
    ('pl', 'body-health', '## Learn in context

| Target language | English |
| --- | --- |
| Boli mnie głowa; jestem chory. | My head hurts; I am ill. |
| Lekarz ogląda moją rękę. | The doctor examines my hand. |

## Mini dialogue

> **A:** Jak się czujesz?
> *How do you feel?*
>
> **B:** Niedobrze. Boli mnie głowa. Jestem chory.
> *Not well. My head hurts. I am ill.*
>
> **A:** Idź do lekarza!
> *Go to the doctor!*
>
> **B:** Tak, lekarz ogląda też moją rękę.
> *Yes, the doctor is also examining my hand.*

## Language note

Pain uses **boli mnie** (“it hurts me”). *Chory* changes to *chora* for a woman.'),
    ('pl', 'emotions', '## Learn in context

| Target language | English |
| --- | --- |
| Jestem szczęśliwa, ale zmęczona. | I am happy, but tired. |
| On boi się psa. | He is afraid of the dog. |

## Mini dialogue

> **A:** Jak się czujesz?
> *How do you feel?*
>
> **B:** Jestem bardzo szczęśliwa, ale zmęczona.
> *I am very happy, but tired.*
>
> **A:** A dlaczego twój brat jest smutny?
> *And why is your brother sad?*
>
> **B:** Boi się psa.
> *He is afraid of the dog.*

## Language note

**Bać się** takes genitive: **psa**, not dictionary-form *pies*.'),
    ('pl', 'hobbies', '## Learn in context

| Target language | English |
| --- | --- |
| Lubię czytać i słuchać muzyki. | I like reading and listening to music. |
| W weekend uprawiam sport i tańczę. | At the weekend I do sport and dance. |

## Mini dialogue

> **A:** Co lubisz robić w weekend?
> *What do you like to do at the weekend?*
>
> **B:** Lubię czytać i słuchać muzyki. A ty?
> *I like reading and listening to music. And you?*
>
> **A:** Lubię uprawiać sport i tańczyć.
> *I like doing sport and dancing.*

## Language note

After **lubię**, use an infinitive. **Słuchać** takes genitive: *słuchać muzyki*.'),
    ('pl', 'nature-animals', '## Learn in context

| Target language | English |
| --- | --- |
| Pies biegnie przez las. | The dog runs through the forest. |
| Ptak siedzi na drzewie. | A bird sits in the tree. |

## Worked usage

- **Pies biegnie przez las.** — *The dog runs through the forest.*
- **Ptak siedzi na drzewie.** — *A bird sits in the tree.*

## Language note

For movement through, use **przez** + accusative: *przez las*. Location is **na drzewie**.'),
    ('pl', 'long-words', '## Learn in context

| Target language | English |
| --- | --- |
| Konstantynopolitańczykowianeczka to bardzo długie słowo. | Konstantynopolitańczykowianeczka is a very long word. |
| Najnieprawdopodobniej przyjdzie jutro. | Most probably, he or she will come tomorrow. |

## Worked usage

- **Konstantynopolitańczykowianeczka to bardzo długie słowo.** — *Konstantynopolitańczykowianeczka is a very long word.*
- **Najnieprawdopodobniej przyjdzie jutro.** — *Most probably, he or she will come tomorrow.*

## Language note

Polish long words grow through endings. **Konstantynopolitańczykowianeczka** is a playful, highly specific feminine noun built around Constantinople.'),
    ('pl', 'funny-unusual-words', '## Learn in context

| Target language | English |
| --- | --- |
| Chrząszcz brzmi trudno, ale to tylko chrząszcz. | “Chrząszcz” sounds difficult, but it is only a beetle. |
| Żółw idzie powoli po trawie. | A turtle walks slowly on the grass. |

## Worked usage

- **Chrząszcz brzmi trudno, ale to tylko chrząszcz.** — *“Chrząszcz” sounds difficult, but it is only a beetle.*
- **Żółw idzie powoli po trawie.** — *A turtle walks slowly on the grass.*

## Language note

**Chrząszcz** is famous from a Polish tongue twister. Say it in chunks rather than treating its consonants as one sound.')
)
INSERT INTO Lessons (CourseId, Slug, Title, SortOrder, ContentMarkdown)
SELECT c.Id, s.Slug, s.Title, s.SortOrder, content.ContentMarkdown
FROM Courses c
CROSS JOIN LessonSeeds s
INNER JOIN LessonContentSeeds content
    ON content.CourseCode = c.Code AND content.LessonSlug = s.Slug;

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
    ('de', 'funny-unusual-words', 5, 'Kopfkino', 'a vivid imagined scene in your head'),
    ('fr', 'greetings', 1, 'Bonjour', 'Hello'),
    ('fr', 'greetings', 2, 'Salut', 'Good day'),
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
    ('fr', 'time-calendar', 4, 'l’heure', 'clock'),
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
    ('fr', 'funny-unusual-words', 5, 'chouchou', 'teacher’s pet / darling'),
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
    ('it', 'funny-unusual-words', 5, 'passeggiata', 'a leisurely walk'),
    ('nl', 'greetings', 1, 'Hallo', 'Hello'),
    ('nl', 'greetings', 2, 'Goedendag', 'Good day'),
    ('nl', 'greetings', 3, 'Welkom', 'Welcome'),
    ('nl', 'greetings', 4, 'Ja', 'Yes'),
    ('nl', 'greetings', 5, 'Nee', 'No'),
    ('nl', 'introductions', 1, 'Ik heet ...', 'My name is ...'),
    ('nl', 'introductions', 2, 'Hoe heet je?', 'What is your name?'),
    ('nl', 'introductions', 3, 'Ik kom uit ...', 'I come from ...'),
    ('nl', 'introductions', 4, 'Aangenaam', 'Nice to meet you'),
    ('nl', 'introductions', 5, 'Dit is ...', 'This is ...'),
    ('nl', 'politeness', 1, 'Alsjeblieft', 'Please'),
    ('nl', 'politeness', 2, 'Bedankt', 'Thank you'),
    ('nl', 'politeness', 3, 'Sorry', 'Sorry / excuse me'),
    ('nl', 'politeness', 4, 'Graag gedaan', 'You are welcome'),
    ('nl', 'politeness', 5, 'Kun je helpen?', 'Can you help?'),
    ('nl', 'numbers', 1, 'een', 'one'),
    ('nl', 'numbers', 2, 'twee', 'two'),
    ('nl', 'numbers', 3, 'drie', 'three'),
    ('nl', 'numbers', 4, 'tien', 'ten'),
    ('nl', 'numbers', 5, 'honderd', 'one hundred'),
    ('nl', 'family', 1, 'de familie', 'family'),
    ('nl', 'family', 2, 'de moeder', 'mother'),
    ('nl', 'family', 3, 'de vader', 'father'),
    ('nl', 'family', 4, 'de broer', 'brother'),
    ('nl', 'family', 5, 'de zus', 'sister'),
    ('nl', 'food', 1, 'het brood', 'bread'),
    ('nl', 'food', 2, 'de kaas', 'cheese'),
    ('nl', 'food', 3, 'de appel', 'apple'),
    ('nl', 'food', 4, 'het ontbijt', 'breakfast'),
    ('nl', 'food', 5, 'lekker', 'tasty'),
    ('nl', 'drinks', 1, 'het water', 'water'),
    ('nl', 'drinks', 2, 'de koffie', 'coffee'),
    ('nl', 'drinks', 3, 'de thee', 'tea'),
    ('nl', 'drinks', 4, 'het bier', 'beer'),
    ('nl', 'drinks', 5, 'een glas', 'a glass'),
    ('nl', 'home', 1, 'het huis', 'house'),
    ('nl', 'home', 2, 'het appartement', 'apartment'),
    ('nl', 'home', 3, 'de kamer', 'room'),
    ('nl', 'home', 4, 'de keuken', 'kitchen'),
    ('nl', 'home', 5, 'de sleutel', 'key'),
    ('nl', 'travel', 1, 'het station', 'train station'),
    ('nl', 'travel', 2, 'de luchthaven', 'airport'),
    ('nl', 'travel', 3, 'het kaartje', 'ticket'),
    ('nl', 'travel', 4, 'de koffer', 'suitcase'),
    ('nl', 'travel', 5, 'vertrekken', 'to depart'),
    ('nl', 'directions', 1, 'links', 'left'),
    ('nl', 'directions', 2, 'rechts', 'right'),
    ('nl', 'directions', 3, 'rechtdoor', 'straight ahead'),
    ('nl', 'directions', 4, 'de straat', 'street'),
    ('nl', 'directions', 5, 'Waar is ...?', 'Where is ...?'),
    ('nl', 'time-calendar', 1, 'vandaag', 'today'),
    ('nl', 'time-calendar', 2, 'morgen', 'tomorrow'),
    ('nl', 'time-calendar', 3, 'gisteren', 'yesterday'),
    ('nl', 'time-calendar', 4, 'de klok', 'clock'),
    ('nl', 'time-calendar', 5, 'maandag', 'Monday'),
    ('nl', 'weather', 1, 'zonnig', 'sunny'),
    ('nl', 'weather', 2, 'regenachtig', 'rainy'),
    ('nl', 'weather', 3, 'de wind', 'wind'),
    ('nl', 'weather', 4, 'koud', 'cold'),
    ('nl', 'weather', 5, 'warm', 'warm'),
    ('nl', 'shopping', 1, 'kopen', 'to buy'),
    ('nl', 'shopping', 2, 'de prijs', 'price'),
    ('nl', 'shopping', 3, 'duur', 'expensive'),
    ('nl', 'shopping', 4, 'goedkoop', 'cheap'),
    ('nl', 'shopping', 5, 'de maat', 'size'),
    ('nl', 'work-school', 1, 'het werk', 'work'),
    ('nl', 'work-school', 2, 'de school', 'school'),
    ('nl', 'work-school', 3, 'de leraar', 'teacher'),
    ('nl', 'work-school', 4, 'leren', 'to learn'),
    ('nl', 'work-school', 5, 'het kantoor', 'office'),
    ('nl', 'body-health', 1, 'het hoofd', 'head'),
    ('nl', 'body-health', 2, 'de hand', 'hand'),
    ('nl', 'body-health', 3, 'de dokter', 'doctor'),
    ('nl', 'body-health', 4, 'ziek', 'ill'),
    ('nl', 'body-health', 5, 'Het doet pijn', 'It hurts'),
    ('nl', 'emotions', 1, 'blij', 'happy'),
    ('nl', 'emotions', 2, 'verdrietig', 'sad'),
    ('nl', 'emotions', 3, 'moe', 'tired'),
    ('nl', 'emotions', 4, 'opgewonden', 'excited'),
    ('nl', 'emotions', 5, 'bang zijn', 'to be afraid'),
    ('nl', 'hobbies', 1, 'lezen', 'to read'),
    ('nl', 'hobbies', 2, 'muziek luisteren', 'to listen to music'),
    ('nl', 'hobbies', 3, 'koken', 'to cook'),
    ('nl', 'hobbies', 4, 'sporten', 'to do sport'),
    ('nl', 'hobbies', 5, 'dansen', 'to dance'),
    ('nl', 'nature-animals', 1, 'de hond', 'dog'),
    ('nl', 'nature-animals', 2, 'de kat', 'cat'),
    ('nl', 'nature-animals', 3, 'de boom', 'tree'),
    ('nl', 'nature-animals', 4, 'het bos', 'forest'),
    ('nl', 'nature-animals', 5, 'de vogel', 'bird'),
    ('nl', 'long-words', 1, 'meervoudigepersoonlijkheidsstoornis', 'multiple personality disorder'),
    ('nl', 'long-words', 2, 'arbeidsongeschiktheidsverzekering', 'disability insurance'),
    ('nl', 'long-words', 3, 'aansprakelijkheidsverzekering', 'liability insurance'),
    ('nl', 'long-words', 4, 'kindercarnavalsoptocht', 'children’s carnival parade'),
    ('nl', 'long-words', 5, 'hottentottententententoonstelling', 'a playful classic compound'),
    ('nl', 'funny-unusual-words', 1, 'gezellig', 'cozy and sociable'),
    ('nl', 'funny-unusual-words', 2, 'uitwaaien', 'to clear one’s head in the wind'),
    ('nl', 'funny-unusual-words', 3, 'voorpret', 'anticipatory enjoyment'),
    ('nl', 'funny-unusual-words', 4, 'uitbuiken', 'to relax after a big meal'),
    ('nl', 'funny-unusual-words', 5, 'niksen', 'deliberately doing nothing'),
    ('es', 'greetings', 1, 'Hola', 'Hello'),
    ('es', 'greetings', 2, 'Buenos días', 'Good day'),
    ('es', 'greetings', 3, 'Bienvenido', 'Welcome'),
    ('es', 'greetings', 4, 'Sí', 'Yes'),
    ('es', 'greetings', 5, 'No', 'No'),
    ('es', 'introductions', 1, 'Me llamo ...', 'My name is ...'),
    ('es', 'introductions', 2, '¿Cómo te llamas?', 'What is your name?'),
    ('es', 'introductions', 3, 'Vengo de ...', 'I come from ...'),
    ('es', 'introductions', 4, 'Mucho gusto', 'Nice to meet you'),
    ('es', 'introductions', 5, 'Esto es ...', 'This is ...'),
    ('es', 'politeness', 1, 'Por favor', 'Please'),
    ('es', 'politeness', 2, 'Gracias', 'Thank you'),
    ('es', 'politeness', 3, 'Perdón', 'Sorry / excuse me'),
    ('es', 'politeness', 4, 'De nada', 'You are welcome'),
    ('es', 'politeness', 5, '¿Puedes ayudar?', 'Can you help?'),
    ('es', 'numbers', 1, 'uno', 'one'),
    ('es', 'numbers', 2, 'dos', 'two'),
    ('es', 'numbers', 3, 'tres', 'three'),
    ('es', 'numbers', 4, 'diez', 'ten'),
    ('es', 'numbers', 5, 'cien', 'one hundred'),
    ('es', 'family', 1, 'la familia', 'family'),
    ('es', 'family', 2, 'la madre', 'mother'),
    ('es', 'family', 3, 'el padre', 'father'),
    ('es', 'family', 4, 'el hermano', 'brother'),
    ('es', 'family', 5, 'la hermana', 'sister'),
    ('es', 'food', 1, 'el pan', 'bread'),
    ('es', 'food', 2, 'el queso', 'cheese'),
    ('es', 'food', 3, 'la manzana', 'apple'),
    ('es', 'food', 4, 'el desayuno', 'breakfast'),
    ('es', 'food', 5, 'delicioso', 'tasty'),
    ('es', 'drinks', 1, 'el agua', 'water'),
    ('es', 'drinks', 2, 'el café', 'coffee'),
    ('es', 'drinks', 3, 'el té', 'tea'),
    ('es', 'drinks', 4, 'la cerveza', 'beer'),
    ('es', 'drinks', 5, 'un vaso', 'a glass'),
    ('es', 'home', 1, 'la casa', 'house'),
    ('es', 'home', 2, 'el apartamento', 'apartment'),
    ('es', 'home', 3, 'la habitación', 'room'),
    ('es', 'home', 4, 'la cocina', 'kitchen'),
    ('es', 'home', 5, 'la llave', 'key'),
    ('es', 'travel', 1, 'la estación', 'train station'),
    ('es', 'travel', 2, 'el aeropuerto', 'airport'),
    ('es', 'travel', 3, 'el billete', 'ticket'),
    ('es', 'travel', 4, 'la maleta', 'suitcase'),
    ('es', 'travel', 5, 'salir', 'to depart'),
    ('es', 'directions', 1, 'a la izquierda', 'left'),
    ('es', 'directions', 2, 'a la derecha', 'right'),
    ('es', 'directions', 3, 'todo recto', 'straight ahead'),
    ('es', 'directions', 4, 'la calle', 'street'),
    ('es', 'directions', 5, '¿Dónde está ...?', 'Where is ...?'),
    ('es', 'time-calendar', 1, 'hoy', 'today'),
    ('es', 'time-calendar', 2, 'mañana', 'tomorrow'),
    ('es', 'time-calendar', 3, 'ayer', 'yesterday'),
    ('es', 'time-calendar', 4, 'el reloj', 'clock'),
    ('es', 'time-calendar', 5, 'lunes', 'Monday'),
    ('es', 'weather', 1, 'soleado', 'sunny'),
    ('es', 'weather', 2, 'lluvioso', 'rainy'),
    ('es', 'weather', 3, 'el viento', 'wind'),
    ('es', 'weather', 4, 'frío', 'cold'),
    ('es', 'weather', 5, 'caluroso', 'warm'),
    ('es', 'shopping', 1, 'comprar', 'to buy'),
    ('es', 'shopping', 2, 'el precio', 'price'),
    ('es', 'shopping', 3, 'caro', 'expensive'),
    ('es', 'shopping', 4, 'barato', 'cheap'),
    ('es', 'shopping', 5, 'la talla', 'size'),
    ('es', 'work-school', 1, 'el trabajo', 'work'),
    ('es', 'work-school', 2, 'la escuela', 'school'),
    ('es', 'work-school', 3, 'el profesor', 'teacher'),
    ('es', 'work-school', 4, 'aprender', 'to learn'),
    ('es', 'work-school', 5, 'la oficina', 'office'),
    ('es', 'body-health', 1, 'la cabeza', 'head'),
    ('es', 'body-health', 2, 'la mano', 'hand'),
    ('es', 'body-health', 3, 'el médico', 'doctor'),
    ('es', 'body-health', 4, 'enfermo', 'ill'),
    ('es', 'body-health', 5, 'Duele', 'It hurts'),
    ('es', 'emotions', 1, 'feliz', 'happy'),
    ('es', 'emotions', 2, 'triste', 'sad'),
    ('es', 'emotions', 3, 'cansado', 'tired'),
    ('es', 'emotions', 4, 'emocionado', 'excited'),
    ('es', 'emotions', 5, 'tener miedo', 'to be afraid'),
    ('es', 'hobbies', 1, 'leer', 'to read'),
    ('es', 'hobbies', 2, 'escuchar música', 'to listen to music'),
    ('es', 'hobbies', 3, 'cocinar', 'to cook'),
    ('es', 'hobbies', 4, 'hacer deporte', 'to do sport'),
    ('es', 'hobbies', 5, 'bailar', 'to dance'),
    ('es', 'nature-animals', 1, 'el perro', 'dog'),
    ('es', 'nature-animals', 2, 'el gato', 'cat'),
    ('es', 'nature-animals', 3, 'el árbol', 'tree'),
    ('es', 'nature-animals', 4, 'el bosque', 'forest'),
    ('es', 'nature-animals', 5, 'el pájaro', 'bird'),
    ('es', 'long-words', 1, 'electroencefalografista', 'electroencephalograph specialist'),
    ('es', 'long-words', 2, 'otorrinolaringólogo', 'ear, nose, and throat doctor'),
    ('es', 'long-words', 3, 'esternocleidomastoideo', 'sternocleidomastoid muscle'),
    ('es', 'long-words', 4, 'desafortunadamente', 'unfortunately'),
    ('es', 'long-words', 5, 'paralelepípedo', 'parallelepiped'),
    ('es', 'funny-unusual-words', 1, 'sobremesa', 'conversation after a meal'),
    ('es', 'funny-unusual-words', 2, 'madrugar', 'to get up very early'),
    ('es', 'funny-unusual-words', 3, 'empalagoso', 'sickeningly sweet'),
    ('es', 'funny-unusual-words', 4, 'tocayo', 'person with the same first name'),
    ('es', 'funny-unusual-words', 5, 'estrenar', 'to use or wear for the first time'),
    ('pl', 'greetings', 1, 'Cześć', 'Hello'),
    ('pl', 'greetings', 2, 'Dzień dobry', 'Good day'),
    ('pl', 'greetings', 3, 'Witamy', 'Welcome'),
    ('pl', 'greetings', 4, 'Tak', 'Yes'),
    ('pl', 'greetings', 5, 'Nie', 'No'),
    ('pl', 'introductions', 1, 'Mam na imię ...', 'My name is ...'),
    ('pl', 'introductions', 2, 'Jak masz na imię?', 'What is your name?'),
    ('pl', 'introductions', 3, 'Jestem z ...', 'I come from ...'),
    ('pl', 'introductions', 4, 'Miło mi', 'Nice to meet you'),
    ('pl', 'introductions', 5, 'To jest ...', 'This is ...'),
    ('pl', 'politeness', 1, 'Proszę', 'Please'),
    ('pl', 'politeness', 2, 'Dziękuję', 'Thank you'),
    ('pl', 'politeness', 3, 'Przepraszam', 'Sorry / excuse me'),
    ('pl', 'politeness', 4, 'Nie ma za co', 'You are welcome'),
    ('pl', 'politeness', 5, 'Czy możesz pomóc?', 'Can you help?'),
    ('pl', 'numbers', 1, 'jeden', 'one'),
    ('pl', 'numbers', 2, 'dwa', 'two'),
    ('pl', 'numbers', 3, 'trzy', 'three'),
    ('pl', 'numbers', 4, 'dziesięć', 'ten'),
    ('pl', 'numbers', 5, 'sto', 'one hundred'),
    ('pl', 'family', 1, 'rodzina', 'family'),
    ('pl', 'family', 2, 'matka', 'mother'),
    ('pl', 'family', 3, 'ojciec', 'father'),
    ('pl', 'family', 4, 'brat', 'brother'),
    ('pl', 'family', 5, 'siostra', 'sister'),
    ('pl', 'food', 1, 'chleb', 'bread'),
    ('pl', 'food', 2, 'ser', 'cheese'),
    ('pl', 'food', 3, 'jabłko', 'apple'),
    ('pl', 'food', 4, 'śniadanie', 'breakfast'),
    ('pl', 'food', 5, 'smaczny', 'tasty'),
    ('pl', 'drinks', 1, 'woda', 'water'),
    ('pl', 'drinks', 2, 'kawa', 'coffee'),
    ('pl', 'drinks', 3, 'herbata', 'tea'),
    ('pl', 'drinks', 4, 'piwo', 'beer'),
    ('pl', 'drinks', 5, 'szklanka', 'a glass'),
    ('pl', 'home', 1, 'dom', 'house'),
    ('pl', 'home', 2, 'mieszkanie', 'apartment'),
    ('pl', 'home', 3, 'pokój', 'room'),
    ('pl', 'home', 4, 'kuchnia', 'kitchen'),
    ('pl', 'home', 5, 'klucz', 'key'),
    ('pl', 'travel', 1, 'dworzec', 'train station'),
    ('pl', 'travel', 2, 'lotnisko', 'airport'),
    ('pl', 'travel', 3, 'bilet', 'ticket'),
    ('pl', 'travel', 4, 'walizka', 'suitcase'),
    ('pl', 'travel', 5, 'wyjeżdżać', 'to depart'),
    ('pl', 'directions', 1, 'w lewo', 'left'),
    ('pl', 'directions', 2, 'w prawo', 'right'),
    ('pl', 'directions', 3, 'prosto', 'straight ahead'),
    ('pl', 'directions', 4, 'ulica', 'street'),
    ('pl', 'directions', 5, 'Gdzie jest ...?', 'Where is ...?'),
    ('pl', 'time-calendar', 1, 'dzisiaj', 'today'),
    ('pl', 'time-calendar', 2, 'jutro', 'tomorrow'),
    ('pl', 'time-calendar', 3, 'wczoraj', 'yesterday'),
    ('pl', 'time-calendar', 4, 'zegar', 'clock'),
    ('pl', 'time-calendar', 5, 'poniedziałek', 'Monday'),
    ('pl', 'weather', 1, 'słoneczny', 'sunny'),
    ('pl', 'weather', 2, 'deszczowy', 'rainy'),
    ('pl', 'weather', 3, 'wiatr', 'wind'),
    ('pl', 'weather', 4, 'zimny', 'cold'),
    ('pl', 'weather', 5, 'ciepły', 'warm'),
    ('pl', 'shopping', 1, 'kupować', 'to buy'),
    ('pl', 'shopping', 2, 'cena', 'price'),
    ('pl', 'shopping', 3, 'drogi', 'expensive'),
    ('pl', 'shopping', 4, 'tani', 'cheap'),
    ('pl', 'shopping', 5, 'rozmiar', 'size'),
    ('pl', 'work-school', 1, 'praca', 'work'),
    ('pl', 'work-school', 2, 'szkoła', 'school'),
    ('pl', 'work-school', 3, 'nauczyciel', 'teacher'),
    ('pl', 'work-school', 4, 'uczyć się', 'to learn'),
    ('pl', 'work-school', 5, 'biuro', 'office'),
    ('pl', 'body-health', 1, 'głowa', 'head'),
    ('pl', 'body-health', 2, 'ręka', 'hand'),
    ('pl', 'body-health', 3, 'lekarz', 'doctor'),
    ('pl', 'body-health', 4, 'chory', 'ill'),
    ('pl', 'body-health', 5, 'Boli', 'It hurts'),
    ('pl', 'emotions', 1, 'szczęśliwy', 'happy'),
    ('pl', 'emotions', 2, 'smutny', 'sad'),
    ('pl', 'emotions', 3, 'zmęczony', 'tired'),
    ('pl', 'emotions', 4, 'podekscytowany', 'excited'),
    ('pl', 'emotions', 5, 'bać się', 'to be afraid'),
    ('pl', 'hobbies', 1, 'czytać', 'to read'),
    ('pl', 'hobbies', 2, 'słuchać muzyki', 'to listen to music'),
    ('pl', 'hobbies', 3, 'gotować', 'to cook'),
    ('pl', 'hobbies', 4, 'uprawiać sport', 'to do sport'),
    ('pl', 'hobbies', 5, 'tańczyć', 'to dance'),
    ('pl', 'nature-animals', 1, 'pies', 'dog'),
    ('pl', 'nature-animals', 2, 'kot', 'cat'),
    ('pl', 'nature-animals', 3, 'drzewo', 'tree'),
    ('pl', 'nature-animals', 4, 'las', 'forest'),
    ('pl', 'nature-animals', 5, 'ptak', 'bird'),
    ('pl', 'long-words', 1, 'konstantynopolitańczykowianeczka', 'young female resident of Constantinople'),
    ('pl', 'long-words', 2, 'najnieprawdopodobniej', 'most probably'),
    ('pl', 'long-words', 3, 'czterdziestoczterolatek', 'forty-four-year-old man'),
    ('pl', 'long-words', 4, 'niepodległościowy', 'independence-related'),
    ('pl', 'long-words', 5, 'przeintelektualizowany', 'over-intellectualized'),
    ('pl', 'funny-unusual-words', 1, 'chrząszcz', 'beetle; famous for its consonant cluster'),
    ('pl', 'funny-unusual-words', 2, 'źdźbło', 'blade of grass'),
    ('pl', 'funny-unusual-words', 3, 'szczęśliwy', 'happy'),
    ('pl', 'funny-unusual-words', 4, 'przepraszam', 'sorry'),
    ('pl', 'funny-unusual-words', 5, 'żółw', 'turtle')
)
INSERT INTO LessonVocabulary (LessonId, VocabularyJson)
SELECT l.Id, json_object('words', json((
    SELECT json_group_array(json_object('word', ordered.Word, 'meaning', ordered.Meaning))
    FROM (SELECT Word, Meaning FROM WordSeeds WHERE CourseCode = c.Code AND LessonSlug = l.Slug ORDER BY Position) ordered
)))
FROM Lessons l INNER JOIN Courses c ON c.Id = l.CourseId;

INSERT INTO Quizzes (CourseId, Title, IsAi)
SELECT Id, Title || ' Greetings Check', 0 FROM Courses;

INSERT INTO QuizQuestions (QuizId, Content, Type, QuestionData, CorrectAnswer)
SELECT q.Id, 'Choose the greeting that means hello.', 'multiple_choice',
       CASE c.Code WHEN 'de' THEN '{"options":["Hallo","Danke","Bitte"]}' WHEN 'fr' THEN '{"options":["Bonjour","Merci","Oui"]}'
                   WHEN 'it' THEN '{"options":["Ciao","Grazie","Prego"]}' WHEN 'nl' THEN '{"options":["Hallo","Bedankt","Alsjeblieft"]}'
                   WHEN 'es' THEN '{"options":["Hola","Gracias","Por favor"]}' WHEN 'pl' THEN '{"options":["Cześć","Dziękuję","Proszę"]}' END,
       CASE c.Code WHEN 'de' THEN 'Hallo' WHEN 'fr' THEN 'Bonjour' WHEN 'it' THEN 'Ciao' WHEN 'nl' THEN 'Hallo' WHEN 'es' THEN 'Hola' WHEN 'pl' THEN 'Cześć' END
FROM Quizzes q INNER JOIN Courses c ON c.Id = q.CourseId;

INSERT INTO Flashcards (CourseId, FrontText, BackText, IsAi)
SELECT Id, CASE Code WHEN 'de' THEN 'Hallo' WHEN 'fr' THEN 'Bonjour' WHEN 'it' THEN 'Ciao' WHEN 'nl' THEN 'Hallo' WHEN 'es' THEN 'Hola' WHEN 'pl' THEN 'Cześć' END, 'Hello', 0
FROM Courses;
