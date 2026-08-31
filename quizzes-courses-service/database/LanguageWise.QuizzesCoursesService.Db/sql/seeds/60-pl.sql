-- Polish lessons, vocabulary, quizzes, and questions. Requires schema.sql and seeds/00-courses.sql.
WITH LessonSeeds (Slug, Title, SortOrder, ContentMarkdown) AS (
    VALUES
    ('greetings', 'Greetings', 1, '## Learn in context

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
    ('introductions', 'Introductions', 2, '## Learn in context

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
    ('politeness', 'Politeness', 3, '## Learn in context

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
    ('numbers', 'Numbers', 4, '## Learn in context

| Target language | English |
| --- | --- |
| Mam dwa bilety i dziesięć euro. | I have two tickets and ten euros. |
| Pociąg odjeżdża o trzeciej. | The train leaves at three. |

## Worked usage

- **Mam dwa bilety i dziesięć euro.** — *I have two tickets and ten euros.*
- **Pociąg odjeżdża o trzeciej.** — *The train leaves at three.*

## Language note

Numbers affect nouns: **dwa bilety** but *pięć biletów*. Learn numbers with a noun.'),
    ('family', 'Family', 5, '## Learn in context

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
    ('food', 'Food', 6, '## Learn in context

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
    ('drinks', 'Drinks', 7, '## Learn in context

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
    ('home', 'Home', 8, '## Learn in context

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
    ('travel', 'Travel', 9, '## Learn in context

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
    ('directions', 'Directions', 10, '## Learn in context

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
    ('time-calendar', 'Time and Calendar', 11, '## Learn in context

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
    ('weather', 'Weather', 12, '## Learn in context

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
    ('shopping', 'Shopping', 13, '## Learn in context

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
    ('work-school', 'Work and School', 14, '## Learn in context

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
    ('body-health', 'Body and Health', 15, '## Learn in context

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
    ('emotions', 'Emotions', 16, '## Learn in context

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
    ('hobbies', 'Hobbies', 17, '## Learn in context

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
    ('nature-animals', 'Nature and Animals', 18, '## Learn in context

| Target language | English |
| --- | --- |
| Pies biegnie przez las. | The dog runs through the forest. |
| Ptak siedzi na drzewie. | A bird sits in the tree. |

## Worked usage

- **Pies biegnie przez las.** — *The dog runs through the forest.*
- **Ptak siedzi na drzewie.** — *A bird sits in the tree.*

## Language note

For movement through, use **przez** + accusative: *przez las*. Location is **na drzewie**.'),
    ('long-words', 'Long Words', 19, '## Learn in context

| Target language | English |
| --- | --- |
| Konstantynopolitańczykowianeczka to bardzo długie słowo. | Konstantynopolitańczykowianeczka is a very long word. |
| Najnieprawdopodobniej przyjdzie jutro. | Most probably, he or she will come tomorrow. |

## Worked usage

- **Konstantynopolitańczykowianeczka to bardzo długie słowo.** — *Konstantynopolitańczykowianeczka is a very long word.*
- **Najnieprawdopodobniej przyjdzie jutro.** — *Most probably, he or she will come tomorrow.*

## Language note

Polish long words grow through endings. **Konstantynopolitańczykowianeczka** is a playful, highly specific feminine noun built around Constantinople.'),
    ('funny-unusual-words', 'Funny and Unusual Words', 20, '## Learn in context

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
INSERT OR IGNORE INTO Lessons (CourseId, Slug, Title, SortOrder, ContentMarkdown)
SELECT c.Id, s.Slug, s.Title, s.SortOrder, s.ContentMarkdown
FROM LessonSeeds s
INNER JOIN Courses c ON c.Code = 'pl';

WITH VocabularySeeds (LessonSlug, VocabularyJson) AS (
    VALUES
    ('greetings', '{"words":[{"word":"Cześć","meaning":"Hello"},{"word":"Dzień dobry","meaning":"Good day"},{"word":"Witamy","meaning":"Welcome"},{"word":"Tak","meaning":"Yes"},{"word":"Nie","meaning":"No"}]}'),
    ('introductions', '{"words":[{"word":"Mam na imię ...","meaning":"My name is ..."},{"word":"Jak masz na imię?","meaning":"What is your name?"},{"word":"Jestem z ...","meaning":"I come from ..."},{"word":"Miło mi","meaning":"Nice to meet you"},{"word":"To jest ...","meaning":"This is ..."}]}'),
    ('politeness', '{"words":[{"word":"Proszę","meaning":"Please"},{"word":"Dziękuję","meaning":"Thank you"},{"word":"Przepraszam","meaning":"Sorry / excuse me"},{"word":"Nie ma za co","meaning":"You are welcome"},{"word":"Czy możesz pomóc?","meaning":"Can you help?"}]}'),
    ('numbers', '{"words":[{"word":"jeden","meaning":"one"},{"word":"dwa","meaning":"two"},{"word":"trzy","meaning":"three"},{"word":"dziesięć","meaning":"ten"},{"word":"sto","meaning":"one hundred"}]}'),
    ('family', '{"words":[{"word":"rodzina","meaning":"family"},{"word":"matka","meaning":"mother"},{"word":"ojciec","meaning":"father"},{"word":"brat","meaning":"brother"},{"word":"siostra","meaning":"sister"}]}'),
    ('food', '{"words":[{"word":"chleb","meaning":"bread"},{"word":"ser","meaning":"cheese"},{"word":"jabłko","meaning":"apple"},{"word":"śniadanie","meaning":"breakfast"},{"word":"smaczny","meaning":"tasty"}]}'),
    ('drinks', '{"words":[{"word":"woda","meaning":"water"},{"word":"kawa","meaning":"coffee"},{"word":"herbata","meaning":"tea"},{"word":"piwo","meaning":"beer"},{"word":"szklanka","meaning":"a glass"}]}'),
    ('home', '{"words":[{"word":"dom","meaning":"house"},{"word":"mieszkanie","meaning":"apartment"},{"word":"pokój","meaning":"room"},{"word":"kuchnia","meaning":"kitchen"},{"word":"klucz","meaning":"key"}]}'),
    ('travel', '{"words":[{"word":"dworzec","meaning":"train station"},{"word":"lotnisko","meaning":"airport"},{"word":"bilet","meaning":"ticket"},{"word":"walizka","meaning":"suitcase"},{"word":"wyjeżdżać","meaning":"to depart"}]}'),
    ('directions', '{"words":[{"word":"w lewo","meaning":"left"},{"word":"w prawo","meaning":"right"},{"word":"prosto","meaning":"straight ahead"},{"word":"ulica","meaning":"street"},{"word":"Gdzie jest ...?","meaning":"Where is ...?"}]}'),
    ('time-calendar', '{"words":[{"word":"dzisiaj","meaning":"today"},{"word":"jutro","meaning":"tomorrow"},{"word":"wczoraj","meaning":"yesterday"},{"word":"zegar","meaning":"clock"},{"word":"poniedziałek","meaning":"Monday"}]}'),
    ('weather', '{"words":[{"word":"słoneczny","meaning":"sunny"},{"word":"deszczowy","meaning":"rainy"},{"word":"wiatr","meaning":"wind"},{"word":"zimny","meaning":"cold"},{"word":"ciepły","meaning":"warm"}]}'),
    ('shopping', '{"words":[{"word":"kupować","meaning":"to buy"},{"word":"cena","meaning":"price"},{"word":"drogi","meaning":"expensive"},{"word":"tani","meaning":"cheap"},{"word":"rozmiar","meaning":"size"}]}'),
    ('work-school', '{"words":[{"word":"praca","meaning":"work"},{"word":"szkoła","meaning":"school"},{"word":"nauczyciel","meaning":"teacher"},{"word":"uczyć się","meaning":"to learn"},{"word":"biuro","meaning":"office"}]}'),
    ('body-health', '{"words":[{"word":"głowa","meaning":"head"},{"word":"ręka","meaning":"hand"},{"word":"lekarz","meaning":"doctor"},{"word":"chory","meaning":"ill"},{"word":"Boli","meaning":"It hurts"}]}'),
    ('emotions', '{"words":[{"word":"szczęśliwy","meaning":"happy"},{"word":"smutny","meaning":"sad"},{"word":"zmęczony","meaning":"tired"},{"word":"podekscytowany","meaning":"excited"},{"word":"bać się","meaning":"to be afraid"}]}'),
    ('hobbies', '{"words":[{"word":"czytać","meaning":"to read"},{"word":"słuchać muzyki","meaning":"to listen to music"},{"word":"gotować","meaning":"to cook"},{"word":"uprawiać sport","meaning":"to do sport"},{"word":"tańczyć","meaning":"to dance"}]}'),
    ('nature-animals', '{"words":[{"word":"pies","meaning":"dog"},{"word":"kot","meaning":"cat"},{"word":"drzewo","meaning":"tree"},{"word":"las","meaning":"forest"},{"word":"ptak","meaning":"bird"}]}'),
    ('long-words', '{"words":[{"word":"konstantynopolitańczykowianeczka","meaning":"young female resident of Constantinople"},{"word":"najnieprawdopodobniej","meaning":"most probably"},{"word":"czterdziestoczterolatek","meaning":"forty-four-year-old man"},{"word":"niepodległościowy","meaning":"independence-related"},{"word":"przeintelektualizowany","meaning":"over-intellectualized"}]}'),
    ('funny-unusual-words', '{"words":[{"word":"chrząszcz","meaning":"beetle; famous for its consonant cluster"},{"word":"źdźbło","meaning":"blade of grass"},{"word":"szczęśliwy","meaning":"happy"},{"word":"przepraszam","meaning":"sorry"},{"word":"żółw","meaning":"turtle"}]}')
)
INSERT OR IGNORE INTO LessonVocabulary (LessonId, VocabularyJson)
SELECT l.Id, s.VocabularyJson
FROM VocabularySeeds s
INNER JOIN Courses c ON c.Code = 'pl'
INNER JOIN Lessons l ON l.CourseId = c.Id AND l.Slug = s.LessonSlug;

WITH QuizSeeds (LessonSlug, Title) AS (
    VALUES
    ('greetings', 'Greetings Quiz'),
    ('introductions', 'Introductions Quiz'),
    ('politeness', 'Politeness Quiz'),
    ('numbers', 'Numbers Quiz'),
    ('family', 'Family Quiz'),
    ('food', 'Food Quiz'),
    ('drinks', 'Drinks Quiz'),
    ('home', 'Home Quiz'),
    ('travel', 'Travel Quiz'),
    ('directions', 'Directions Quiz'),
    ('time-calendar', 'Time and Calendar Quiz'),
    ('weather', 'Weather Quiz'),
    ('shopping', 'Shopping Quiz'),
    ('work-school', 'Work and School Quiz'),
    ('body-health', 'Body and Health Quiz'),
    ('emotions', 'Emotions Quiz'),
    ('hobbies', 'Hobbies Quiz'),
    ('nature-animals', 'Nature and Animals Quiz'),
    ('long-words', 'Long Words Quiz'),
    ('funny-unusual-words', 'Funny and Unusual Words Quiz')
)
INSERT OR IGNORE INTO Quizzes (LessonId, Title)
SELECT l.Id, s.Title
FROM QuizSeeds s
INNER JOIN Courses c ON c.Code = 'pl'
INNER JOIN Lessons l ON l.CourseId = c.Id AND l.Slug = s.LessonSlug;

WITH QuestionSeeds (LessonSlug, SortOrder, Content, Type, QuestionData, CorrectAnswer) AS (
    VALUES
    ('greetings', 1, 'Which Polish term means “Hello”?', 'multiple_choice', '{"options":["Cześć","Dzień dobry","Witamy"]}', 'Cześć'),
    ('greetings', 2, 'Which Polish term means “Good day”?', 'multiple_choice', '{"options":["Witamy","Dzień dobry","Tak"]}', 'Dzień dobry'),
    ('greetings', 3, 'Which Polish term means “Welcome”?', 'multiple_choice', '{"options":["Tak","Nie","Witamy"]}', 'Witamy'),
    ('greetings', 4, 'Which Polish term means “Yes”?', 'multiple_choice', '{"options":["Tak","Nie","Cześć"]}', 'Tak'),
    ('greetings', 5, 'Put “Hi, Ania” in Polish order.', 'word_ordering', '{"tokens":["Aniu","Cześć"]}', '["Cześć","Aniu"]'),
    ('greetings', 6, 'Put “Welcome to Warsaw” in Polish order.', 'word_ordering', '{"tokens":["Warszawie","Witaj","w"]}', '["Witaj","w","Warszawie"]'),
    ('greetings', 7, 'Put “Good day, madam” in Polish order.', 'word_ordering', '{"tokens":["pani","dobry","Dzień"]}', '["Dzień","dobry","pani"]'),
    ('greetings', 8, 'Type the Polish term for “Welcome”.', 'free_text', '{}', 'Witamy'),
    ('greetings', 9, 'Type the Polish term for “Yes”.', 'free_text', '{}', 'Tak'),
    ('greetings', 10, 'Type the Polish term for “No”.', 'free_text', '{}', 'Nie'),
    ('introductions', 1, 'Which Polish term means “My name is ...”?', 'multiple_choice', '{"options":["Mam na imię ...","Jak masz na imię?","Jestem z ..."]}', 'Mam na imię ...'),
    ('introductions', 2, 'Which Polish term means “What is your name?”?', 'multiple_choice', '{"options":["Jestem z ...","Jak masz na imię?","Miło mi"]}', 'Jak masz na imię?'),
    ('introductions', 3, 'Which Polish term means “I come from ...”?', 'multiple_choice', '{"options":["Miło mi","To jest ...","Jestem z ..."]}', 'Jestem z ...'),
    ('introductions', 4, 'Which Polish term means “Nice to meet you”?', 'multiple_choice', '{"options":["Miło mi","To jest ...","Mam na imię ..."]}', 'Miło mi'),
    ('introductions', 5, 'Put “My name is Ola” in Polish order.', 'word_ordering', '{"tokens":["Ola","imię","Mam","na"]}', '["Mam","na","imię","Ola"]'),
    ('introductions', 6, 'Put “I am from Canada” in Polish order.', 'word_ordering', '{"tokens":["Kanady","Jestem","z"]}', '["Jestem","z","Kanady"]'),
    ('introductions', 7, 'Put “My name is Tomek” in Polish order.', 'word_ordering', '{"tokens":["Tomek","na","Mam","imię"]}', '["Mam","na","imię","Tomek"]'),
    ('introductions', 8, 'Type the Polish term for “I come from ...”.', 'free_text', '{}', 'Jestem z ...'),
    ('introductions', 9, 'Type the Polish term for “Nice to meet you”.', 'free_text', '{}', 'Miło mi'),
    ('introductions', 10, 'Type the Polish term for “This is ...”.', 'free_text', '{}', 'To jest ...'),
    ('politeness', 1, 'Which Polish term means “Please”?', 'multiple_choice', '{"options":["Proszę","Dziękuję","Przepraszam"]}', 'Proszę'),
    ('politeness', 2, 'Which Polish term means “Thank you”?', 'multiple_choice', '{"options":["Przepraszam","Dziękuję","Nie ma za co"]}', 'Dziękuję'),
    ('politeness', 3, 'Which Polish term means “Sorry / excuse me”?', 'multiple_choice', '{"options":["Nie ma za co","Czy możesz pomóc?","Przepraszam"]}', 'Przepraszam'),
    ('politeness', 4, 'Which Polish term means “You are welcome”?', 'multiple_choice', '{"options":["Nie ma za co","Czy możesz pomóc?","Proszę"]}', 'Nie ma za co'),
    ('politeness', 5, 'Put “A coffee, please” in Polish order.', 'word_ordering', '{"tokens":["kawę","Poproszę"]}', '["Poproszę","kawę"]'),
    ('politeness', 6, 'Put “Thank you very much” in Polish order.', 'word_ordering', '{"tokens":["dziękuję","Bardzo"]}', '["Bardzo","dziękuję"]'),
    ('politeness', 7, 'Put “You are welcome” in Polish order.', 'word_ordering', '{"tokens":["co","ma","za","Nie"]}', '["Nie","ma","za","co"]'),
    ('politeness', 8, 'Type the Polish term for “Sorry / excuse me”.', 'free_text', '{}', 'Przepraszam'),
    ('politeness', 9, 'Type the Polish term for “You are welcome”.', 'free_text', '{}', 'Nie ma za co'),
    ('politeness', 10, 'Type the Polish term for “Can you help?”.', 'free_text', '{}', 'Czy możesz pomóc?'),
    ('numbers', 1, 'Which Polish term means “one”?', 'multiple_choice', '{"options":["jeden","dwa","trzy"]}', 'jeden'),
    ('numbers', 2, 'Which Polish term means “two”?', 'multiple_choice', '{"options":["trzy","dwa","dziesięć"]}', 'dwa'),
    ('numbers', 3, 'Which Polish term means “three”?', 'multiple_choice', '{"options":["dziesięć","sto","trzy"]}', 'trzy'),
    ('numbers', 4, 'Which Polish term means “ten”?', 'multiple_choice', '{"options":["dziesięć","sto","jeden"]}', 'dziesięć'),
    ('numbers', 5, 'Put “I have two tickets and ten euros” in Polish order.', 'word_ordering', '{"tokens":["euro","bilety","Mam","dziesięć","dwa","i"]}', '["Mam","dwa","bilety","i","dziesięć","euro"]'),
    ('numbers', 6, 'Put “The train leaves at three” in Polish order.', 'word_ordering', '{"tokens":["trzeciej","odjeżdża","Pociąg","o"]}', '["Pociąg","odjeżdża","o","trzeciej"]'),
    ('numbers', 7, 'Put “two tickets” in Polish order.', 'word_ordering', '{"tokens":["bilety","dwa"]}', '["dwa","bilety"]'),
    ('numbers', 8, 'Type the Polish term for “three”.', 'free_text', '{}', 'trzy'),
    ('numbers', 9, 'Type the Polish term for “ten”.', 'free_text', '{}', 'dziesięć'),
    ('numbers', 10, 'Type the Polish term for “one hundred”.', 'free_text', '{}', 'sto'),
    ('family', 1, 'Which Polish term means “family”?', 'multiple_choice', '{"options":["rodzina","matka","ojciec"]}', 'rodzina'),
    ('family', 2, 'Which Polish term means “mother”?', 'multiple_choice', '{"options":["ojciec","matka","brat"]}', 'matka'),
    ('family', 3, 'Which Polish term means “father”?', 'multiple_choice', '{"options":["brat","siostra","ojciec"]}', 'ojciec'),
    ('family', 4, 'Which Polish term means “brother”?', 'multiple_choice', '{"options":["brat","siostra","rodzina"]}', 'brat'),
    ('family', 5, 'Put “This is my mum and my dad” in Polish order.', 'word_ordering', '{"tokens":["tata","moja","jest","i","To","mama","mój"]}', '["To","jest","moja","mama","i","mój","tata"]'),
    ('family', 6, 'Put “My sister has a brother” in Polish order.', 'word_ordering', '{"tokens":["brata","Moja","ma","siostra"]}', '["Moja","siostra","ma","brata"]'),
    ('family', 7, 'Put “No, but I have a sister” in Polish order.', 'word_ordering', '{"tokens":["siostrę","ale","mam","Nie"]}', '["Nie","ale","mam","siostrę"]'),
    ('family', 8, 'Type the Polish term for “father”.', 'free_text', '{}', 'ojciec'),
    ('family', 9, 'Type the Polish term for “brother”.', 'free_text', '{}', 'brat'),
    ('family', 10, 'Type the Polish term for “sister”.', 'free_text', '{}', 'siostra'),
    ('food', 1, 'Which Polish term means “bread”?', 'multiple_choice', '{"options":["chleb","ser","jabłko"]}', 'chleb'),
    ('food', 2, 'Which Polish term means “cheese”?', 'multiple_choice', '{"options":["jabłko","ser","śniadanie"]}', 'ser'),
    ('food', 3, 'Which Polish term means “apple”?', 'multiple_choice', '{"options":["śniadanie","smaczny","jabłko"]}', 'jabłko'),
    ('food', 4, 'Which Polish term means “breakfast”?', 'multiple_choice', '{"options":["śniadanie","smaczny","chleb"]}', 'śniadanie'),
    ('food', 5, 'Put “For breakfast I eat bread and cheese” in Polish order.', 'word_ordering', '{"tokens":["ser","śniadanie","chleb","jem","Na","i"]}', '["Na","śniadanie","jem","chleb","i","ser"]'),
    ('food', 6, 'Put “The apple is delicious” in Polish order.', 'word_ordering', '{"tokens":["pyszne","Jabłko","jest"]}', '["Jabłko","jest","pyszne"]'),
    ('food', 7, 'Put “I eat bread and cheese” in Polish order.', 'word_ordering', '{"tokens":["ser","Jem","chleb","i"]}', '["Jem","chleb","i","ser"]'),
    ('food', 8, 'Type the Polish term for “apple”.', 'free_text', '{}', 'jabłko'),
    ('food', 9, 'Type the Polish term for “breakfast”.', 'free_text', '{}', 'śniadanie'),
    ('food', 10, 'Type the Polish term for “tasty”.', 'free_text', '{}', 'smaczny'),
    ('drinks', 1, 'Which Polish term means “water”?', 'multiple_choice', '{"options":["woda","kawa","herbata"]}', 'woda'),
    ('drinks', 2, 'Which Polish term means “coffee”?', 'multiple_choice', '{"options":["herbata","kawa","piwo"]}', 'kawa'),
    ('drinks', 3, 'Which Polish term means “tea”?', 'multiple_choice', '{"options":["piwo","szklanka","herbata"]}', 'herbata'),
    ('drinks', 4, 'Which Polish term means “beer”?', 'multiple_choice', '{"options":["piwo","szklanka","woda"]}', 'piwo'),
    ('drinks', 5, 'Put “A glass of water, please” in Polish order.', 'word_ordering', '{"tokens":["wody","Poproszę","szklankę"]}', '["Poproszę","szklankę","wody"]'),
    ('drinks', 6, 'Put “Coffee is hot, and tea is warm” in Polish order.', 'word_ordering', '{"tokens":["ciepła","gorąca","herbata","jest","Kawa","a"]}', '["Kawa","jest","gorąca","a","herbata","ciepła"]'),
    ('drinks', 7, 'Put “The coffee is too hot” in Polish order.', 'word_ordering', '{"tokens":["gorąca","za","jest","Kawa"]}', '["Kawa","jest","za","gorąca"]'),
    ('drinks', 8, 'Type the Polish term for “tea”.', 'free_text', '{}', 'herbata'),
    ('drinks', 9, 'Type the Polish term for “beer”.', 'free_text', '{}', 'piwo'),
    ('drinks', 10, 'Type the Polish term for “a glass”.', 'free_text', '{}', 'szklanka'),
    ('home', 1, 'Which Polish term means “house”?', 'multiple_choice', '{"options":["dom","mieszkanie","pokój"]}', 'dom'),
    ('home', 2, 'Which Polish term means “apartment”?', 'multiple_choice', '{"options":["pokój","mieszkanie","kuchnia"]}', 'mieszkanie'),
    ('home', 3, 'Which Polish term means “room”?', 'multiple_choice', '{"options":["kuchnia","klucz","pokój"]}', 'pokój'),
    ('home', 4, 'Which Polish term means “kitchen”?', 'multiple_choice', '{"options":["kuchnia","klucz","dom"]}', 'kuchnia'),
    ('home', 5, 'Put “The kitchen is in the apartment” in Polish order.', 'word_ordering', '{"tokens":["mieszkaniu","jest","Kuchnia","w"]}', '["Kuchnia","jest","w","mieszkaniu"]'),
    ('home', 6, 'Put “Where is the key?” in Polish order.', 'word_ordering', '{"tokens":["klucz","jest","Gdzie"]}', '["Gdzie","jest","klucz"]'),
    ('home', 7, 'Put “The key is in the kitchen” in Polish order.', 'word_ordering', '{"tokens":["kuchni","jest","Klucz","w"]}', '["Klucz","jest","w","kuchni"]'),
    ('home', 8, 'Type the Polish term for “room”.', 'free_text', '{}', 'pokój'),
    ('home', 9, 'Type the Polish term for “kitchen”.', 'free_text', '{}', 'kuchnia'),
    ('home', 10, 'Type the Polish term for “key”.', 'free_text', '{}', 'klucz'),
    ('travel', 1, 'Which Polish term means “train station”?', 'multiple_choice', '{"options":["dworzec","lotnisko","bilet"]}', 'dworzec'),
    ('travel', 2, 'Which Polish term means “airport”?', 'multiple_choice', '{"options":["bilet","lotnisko","walizka"]}', 'lotnisko'),
    ('travel', 3, 'Which Polish term means “ticket”?', 'multiple_choice', '{"options":["walizka","wyjeżdżać","bilet"]}', 'bilet'),
    ('travel', 4, 'Which Polish term means “suitcase”?', 'multiple_choice', '{"options":["walizka","wyjeżdżać","dworzec"]}', 'walizka'),
    ('travel', 5, 'Put “The station is near the airport” in Polish order.', 'word_ordering', '{"tokens":["lotniska","jest","Dworzec","blisko"]}', '["Dworzec","jest","blisko","lotniska"]'),
    ('travel', 6, 'Put “The ticket is in the suitcase” in Polish order.', 'word_ordering', '{"tokens":["walizce","jest","Bilet","w"]}', '["Bilet","jest","w","walizce"]'),
    ('travel', 7, 'Put “Your ticket is in the suitcase” in Polish order.', 'word_ordering', '{"tokens":["walizce","bilet","w","Twój","jest"]}', '["Twój","bilet","jest","w","walizce"]'),
    ('travel', 8, 'Type the Polish term for “ticket”.', 'free_text', '{}', 'bilet'),
    ('travel', 9, 'Type the Polish term for “suitcase”.', 'free_text', '{}', 'walizka'),
    ('travel', 10, 'Type the Polish term for “to depart”.', 'free_text', '{}', 'wyjeżdżać'),
    ('directions', 1, 'Which Polish term means “left”?', 'multiple_choice', '{"options":["w lewo","w prawo","prosto"]}', 'w lewo'),
    ('directions', 2, 'Which Polish term means “right”?', 'multiple_choice', '{"options":["prosto","w prawo","ulica"]}', 'w prawo'),
    ('directions', 3, 'Which Polish term means “straight ahead”?', 'multiple_choice', '{"options":["ulica","Gdzie jest ...?","prosto"]}', 'prosto'),
    ('directions', 4, 'Which Polish term means “street”?', 'multiple_choice', '{"options":["ulica","Gdzie jest ...?","w lewo"]}', 'ulica'),
    ('directions', 5, 'Put “Go straight ahead, then left” in Polish order.', 'word_ordering', '{"tokens":["lewo","prosto","potem","Idź","w"]}', '["Idź","prosto","potem","w","lewo"]'),
    ('directions', 6, 'Put “to the right of the hotel” in Polish order.', 'word_ordering', '{"tokens":["hotelu","prawo","od","Na"]}', '["Na","prawo","od","hotelu"]'),
    ('directions', 7, 'Put “The hotel is on the right side” in Polish order.', 'word_ordering', '{"tokens":["stronie","jest","Hotel","prawej","po"]}', '["Hotel","jest","po","prawej","stronie"]'),
    ('directions', 8, 'Type the Polish term for “straight ahead”.', 'free_text', '{}', 'prosto'),
    ('directions', 9, 'Type the Polish term for “street”.', 'free_text', '{}', 'ulica'),
    ('directions', 10, 'Type the Polish term for “Where is ...?”.', 'free_text', '{}', 'Gdzie jest ...?'),
    ('time-calendar', 1, 'Which Polish term means “today”?', 'multiple_choice', '{"options":["dzisiaj","jutro","wczoraj"]}', 'dzisiaj'),
    ('time-calendar', 2, 'Which Polish term means “tomorrow”?', 'multiple_choice', '{"options":["wczoraj","jutro","zegar"]}', 'jutro'),
    ('time-calendar', 3, 'Which Polish term means “yesterday”?', 'multiple_choice', '{"options":["zegar","poniedziałek","wczoraj"]}', 'wczoraj'),
    ('time-calendar', 4, 'Which Polish term means “clock”?', 'multiple_choice', '{"options":["zegar","poniedziałek","dzisiaj"]}', 'zegar'),
    ('time-calendar', 5, 'Put “Today is Monday, and tomorrow is Tuesday” in Polish order.', 'word_ordering', '{"tokens":["wtorek","poniedziałek","jutro","jest","Dziś","a"]}', '["Dziś","jest","poniedziałek","a","jutro","wtorek"]'),
    ('time-calendar', 6, 'Put “The lesson starts at eight” in Polish order.', 'word_ordering', '{"tokens":["ósmej","się","Lekcja","o","zaczyna"]}', '["Lekcja","zaczyna","się","o","ósmej"]'),
    ('time-calendar', 7, 'Put “Tomorrow at eight” in Polish order.', 'word_ordering', '{"tokens":["ósmej","Jutro","o"]}', '["Jutro","o","ósmej"]'),
    ('time-calendar', 8, 'Type the Polish term for “yesterday”.', 'free_text', '{}', 'wczoraj'),
    ('time-calendar', 9, 'Type the Polish term for “clock”.', 'free_text', '{}', 'zegar'),
    ('time-calendar', 10, 'Type the Polish term for “Monday”.', 'free_text', '{}', 'poniedziałek'),
    ('weather', 1, 'Which Polish term means “sunny”?', 'multiple_choice', '{"options":["słoneczny","deszczowy","wiatr"]}', 'słoneczny'),
    ('weather', 2, 'Which Polish term means “rainy”?', 'multiple_choice', '{"options":["wiatr","deszczowy","zimny"]}', 'deszczowy'),
    ('weather', 3, 'Which Polish term means “wind”?', 'multiple_choice', '{"options":["zimny","ciepły","wiatr"]}', 'wiatr'),
    ('weather', 4, 'Which Polish term means “cold”?', 'multiple_choice', '{"options":["zimny","ciepły","słoneczny"]}', 'zimny'),
    ('weather', 5, 'Put “Today it is sunny, but cold” in Polish order.', 'word_ordering', '{"tokens":["zimno","słonecznie","ale","Dziś","jest"]}', '["Dziś","jest","słonecznie","ale","zimno"]'),
    ('weather', 6, 'Put “Tomorrow it will be rainy and windy” in Polish order.', 'word_ordering', '{"tokens":["wietrznie","deszczowo","Jutro","i","będzie"]}', '["Jutro","będzie","deszczowo","i","wietrznie"]'),
    ('weather', 7, 'Put “It is sunny, but cold” in Polish order.', 'word_ordering', '{"tokens":["zimno","słonecznie","ale","Jest"]}', '["Jest","słonecznie","ale","zimno"]'),
    ('weather', 8, 'Type the Polish term for “wind”.', 'free_text', '{}', 'wiatr'),
    ('weather', 9, 'Type the Polish term for “cold”.', 'free_text', '{}', 'zimny'),
    ('weather', 10, 'Type the Polish term for “warm”.', 'free_text', '{}', 'ciepły'),
    ('shopping', 1, 'Which Polish term means “to buy”?', 'multiple_choice', '{"options":["kupować","cena","drogi"]}', 'kupować'),
    ('shopping', 2, 'Which Polish term means “price”?', 'multiple_choice', '{"options":["drogi","cena","tani"]}', 'cena'),
    ('shopping', 3, 'Which Polish term means “expensive”?', 'multiple_choice', '{"options":["tani","rozmiar","drogi"]}', 'drogi'),
    ('shopping', 4, 'Which Polish term means “cheap”?', 'multiple_choice', '{"options":["tani","rozmiar","kupować"]}', 'tani'),
    ('shopping', 5, 'Put “How much does this shirt cost?” in Polish order.', 'word_ordering', '{"tokens":["koszula","kosztuje","Ile","ta"]}', '["Ile","kosztuje","ta","koszula"]'),
    ('shopping', 6, 'Put “It costs twenty zloty” in Polish order.', 'word_ordering', '{"tokens":["złotych","Kosztuje","dwadzieścia"]}', '["Kosztuje","dwadzieścia","złotych"]'),
    ('shopping', 7, 'Put “I want to buy this shirt” in Polish order.', 'word_ordering', '{"tokens":["koszulę","kupić","Chcę","tę"]}', '["Chcę","kupić","tę","koszulę"]'),
    ('shopping', 8, 'Type the Polish term for “expensive”.', 'free_text', '{}', 'drogi'),
    ('shopping', 9, 'Type the Polish term for “cheap”.', 'free_text', '{}', 'tani'),
    ('shopping', 10, 'Type the Polish term for “size”.', 'free_text', '{}', 'rozmiar'),
    ('work-school', 1, 'Which Polish term means “work”?', 'multiple_choice', '{"options":["praca","szkoła","nauczyciel"]}', 'praca'),
    ('work-school', 2, 'Which Polish term means “school”?', 'multiple_choice', '{"options":["nauczyciel","szkoła","uczyć się"]}', 'szkoła'),
    ('work-school', 3, 'Which Polish term means “teacher”?', 'multiple_choice', '{"options":["uczyć się","biuro","nauczyciel"]}', 'nauczyciel'),
    ('work-school', 4, 'Which Polish term means “to learn”?', 'multiple_choice', '{"options":["uczyć się","biuro","praca"]}', 'uczyć się'),
    ('work-school', 5, 'Put “I learn Polish at school” in Polish order.', 'word_ordering', '{"tokens":["szkole","polskiego","się","Uczę","w"]}', '["Uczę","się","polskiego","w","szkole"]'),
    ('work-school', 6, 'Put “The teacher works in the office” in Polish order.', 'word_ordering', '{"tokens":["biurze","Nauczyciel","pracuje","w"]}', '["Nauczyciel","pracuje","w","biurze"]'),
    ('work-school', 7, 'Put “I work in the office” in Polish order.', 'word_ordering', '{"tokens":["biurze","Pracuję","w"]}', '["Pracuję","w","biurze"]'),
    ('work-school', 8, 'Type the Polish term for “teacher”.', 'free_text', '{}', 'nauczyciel'),
    ('work-school', 9, 'Type the Polish term for “to learn”.', 'free_text', '{}', 'uczyć się'),
    ('work-school', 10, 'Type the Polish term for “office”.', 'free_text', '{}', 'biuro'),
    ('body-health', 1, 'Which Polish term means “head”?', 'multiple_choice', '{"options":["głowa","ręka","lekarz"]}', 'głowa'),
    ('body-health', 2, 'Which Polish term means “hand”?', 'multiple_choice', '{"options":["lekarz","ręka","chory"]}', 'ręka'),
    ('body-health', 3, 'Which Polish term means “doctor”?', 'multiple_choice', '{"options":["chory","Boli","lekarz"]}', 'lekarz'),
    ('body-health', 4, 'Which Polish term means “ill”?', 'multiple_choice', '{"options":["chory","Boli","głowa"]}', 'chory'),
    ('body-health', 5, 'Put “My head hurts” in Polish order.', 'word_ordering', '{"tokens":["głowa","mnie","Boli"]}', '["Boli","mnie","głowa"]'),
    ('body-health', 6, 'Put “I am ill” in Polish order.', 'word_ordering', '{"tokens":["chory","Jestem"]}', '["Jestem","chory"]'),
    ('body-health', 7, 'Put “The doctor examines my hand” in Polish order.', 'word_ordering', '{"tokens":["rękę","Lekarz","moją","ogląda"]}', '["Lekarz","ogląda","moją","rękę"]'),
    ('body-health', 8, 'Type the Polish term for “doctor”.', 'free_text', '{}', 'lekarz'),
    ('body-health', 9, 'Type the Polish term for “ill”.', 'free_text', '{}', 'chory'),
    ('body-health', 10, 'Type the Polish term for “It hurts”.', 'free_text', '{}', 'Boli'),
    ('emotions', 1, 'Which Polish term means “happy”?', 'multiple_choice', '{"options":["szczęśliwy","smutny","zmęczony"]}', 'szczęśliwy'),
    ('emotions', 2, 'Which Polish term means “sad”?', 'multiple_choice', '{"options":["zmęczony","smutny","podekscytowany"]}', 'smutny'),
    ('emotions', 3, 'Which Polish term means “tired”?', 'multiple_choice', '{"options":["podekscytowany","bać się","zmęczony"]}', 'zmęczony'),
    ('emotions', 4, 'Which Polish term means “excited”?', 'multiple_choice', '{"options":["podekscytowany","bać się","szczęśliwy"]}', 'podekscytowany'),
    ('emotions', 5, 'Put “I am happy, but tired” in Polish order.', 'word_ordering', '{"tokens":["zmęczona","szczęśliwa","ale","Jestem"]}', '["Jestem","szczęśliwa","ale","zmęczona"]'),
    ('emotions', 6, 'Put “He is afraid of the dog” in Polish order.', 'word_ordering', '{"tokens":["psa","się","On","boi"]}', '["On","boi","się","psa"]'),
    ('emotions', 7, 'Put “Your brother is sad” in Polish order.', 'word_ordering', '{"tokens":["smutny","brat","jest","Twój"]}', '["Twój","brat","jest","smutny"]'),
    ('emotions', 8, 'Type the Polish term for “tired”.', 'free_text', '{}', 'zmęczony'),
    ('emotions', 9, 'Type the Polish term for “excited”.', 'free_text', '{}', 'podekscytowany'),
    ('emotions', 10, 'Type the Polish term for “to be afraid”.', 'free_text', '{}', 'bać się'),
    ('hobbies', 1, 'Which Polish term means “to read”?', 'multiple_choice', '{"options":["czytać","słuchać muzyki","gotować"]}', 'czytać'),
    ('hobbies', 2, 'Which Polish term means “to listen to music”?', 'multiple_choice', '{"options":["gotować","słuchać muzyki","uprawiać sport"]}', 'słuchać muzyki'),
    ('hobbies', 3, 'Which Polish term means “to cook”?', 'multiple_choice', '{"options":["uprawiać sport","tańczyć","gotować"]}', 'gotować'),
    ('hobbies', 4, 'Which Polish term means “to do sport”?', 'multiple_choice', '{"options":["uprawiać sport","tańczyć","czytać"]}', 'uprawiać sport'),
    ('hobbies', 5, 'Put “I like reading and listening to music” in Polish order.', 'word_ordering', '{"tokens":["muzyki","czytać","Lubię","słuchać","i"]}', '["Lubię","czytać","i","słuchać","muzyki"]'),
    ('hobbies', 6, 'Put “At the weekend I do sport and dance” in Polish order.', 'word_ordering', '{"tokens":["sport","tańczę","weekend","uprawiam","W","i"]}', '["W","weekend","uprawiam","sport","i","tańczę"]'),
    ('hobbies', 7, 'Put “I like doing sport and dancing” in Polish order.', 'word_ordering', '{"tokens":["tańczyć","sport","Lubię","uprawiać","i"]}', '["Lubię","uprawiać","sport","i","tańczyć"]'),
    ('hobbies', 8, 'Type the Polish term for “to cook”.', 'free_text', '{}', 'gotować'),
    ('hobbies', 9, 'Type the Polish term for “to do sport”.', 'free_text', '{}', 'uprawiać sport'),
    ('hobbies', 10, 'Type the Polish term for “to dance”.', 'free_text', '{}', 'tańczyć'),
    ('nature-animals', 1, 'Which Polish term means “dog”?', 'multiple_choice', '{"options":["pies","kot","drzewo"]}', 'pies'),
    ('nature-animals', 2, 'Which Polish term means “cat”?', 'multiple_choice', '{"options":["drzewo","kot","las"]}', 'kot'),
    ('nature-animals', 3, 'Which Polish term means “tree”?', 'multiple_choice', '{"options":["las","ptak","drzewo"]}', 'drzewo'),
    ('nature-animals', 4, 'Which Polish term means “forest”?', 'multiple_choice', '{"options":["las","ptak","pies"]}', 'las'),
    ('nature-animals', 5, 'Put “The dog runs through the forest” in Polish order.', 'word_ordering', '{"tokens":["las","Pies","przez","biegnie"]}', '["Pies","biegnie","przez","las"]'),
    ('nature-animals', 6, 'Put “A bird sits in the tree” in Polish order.', 'word_ordering', '{"tokens":["drzewie","Ptak","na","siedzi"]}', '["Ptak","siedzi","na","drzewie"]'),
    ('nature-animals', 7, 'Put “The cat sits in the tree” in Polish order.', 'word_ordering', '{"tokens":["drzewie","Kot","na","siedzi"]}', '["Kot","siedzi","na","drzewie"]'),
    ('nature-animals', 8, 'Type the Polish term for “tree”.', 'free_text', '{}', 'drzewo'),
    ('nature-animals', 9, 'Type the Polish term for “forest”.', 'free_text', '{}', 'las'),
    ('nature-animals', 10, 'Type the Polish term for “bird”.', 'free_text', '{}', 'ptak'),
    ('long-words', 1, 'Which Polish term means “young female resident of Constantinople”?', 'multiple_choice', '{"options":["konstantynopolitańczykowianeczka","najnieprawdopodobniej","czterdziestoczterolatek"]}', 'konstantynopolitańczykowianeczka'),
    ('long-words', 2, 'Which Polish term means “most probably”?', 'multiple_choice', '{"options":["czterdziestoczterolatek","najnieprawdopodobniej","niepodległościowy"]}', 'najnieprawdopodobniej'),
    ('long-words', 3, 'Which Polish term means “forty-four-year-old man”?', 'multiple_choice', '{"options":["niepodległościowy","przeintelektualizowany","czterdziestoczterolatek"]}', 'czterdziestoczterolatek'),
    ('long-words', 4, 'Which Polish term means “independence-related”?', 'multiple_choice', '{"options":["niepodległościowy","przeintelektualizowany","konstantynopolitańczykowianeczka"]}', 'niepodległościowy'),
    ('long-words', 5, 'Put “Konstantynopolitańczykowianeczka is a very long word” in Polish order.', 'word_ordering', '{"tokens":["słowo","bardzo","to","Konstantynopolitańczykowianeczka","długie"]}', '["Konstantynopolitańczykowianeczka","to","bardzo","długie","słowo"]'),
    ('long-words', 6, 'Put “Most probably, he or she will come tomorrow” in Polish order.', 'word_ordering', '{"tokens":["jutro","przyjdzie","Najnieprawdopodobniej"]}', '["Najnieprawdopodobniej","przyjdzie","jutro"]'),
    ('long-words', 7, 'Put “A forty-four-year-old man is a long word” in Polish order.', 'word_ordering', '{"tokens":["słowo","długie","to","Czterdziestoczterolatek"]}', '["Czterdziestoczterolatek","to","długie","słowo"]'),
    ('long-words', 8, 'Type the Polish term for “forty-four-year-old man”.', 'free_text', '{}', 'czterdziestoczterolatek'),
    ('long-words', 9, 'Type the Polish term for “independence-related”.', 'free_text', '{}', 'niepodległościowy'),
    ('long-words', 10, 'Type the Polish term for “over-intellectualized”.', 'free_text', '{}', 'przeintelektualizowany'),
    ('funny-unusual-words', 1, 'Which Polish term means “beetle; famous for its consonant cluster”?', 'multiple_choice', '{"options":["chrząszcz","źdźbło","szczęśliwy"]}', 'chrząszcz'),
    ('funny-unusual-words', 2, 'Which Polish term means “blade of grass”?', 'multiple_choice', '{"options":["szczęśliwy","źdźbło","przepraszam"]}', 'źdźbło'),
    ('funny-unusual-words', 3, 'Which Polish term means “happy”?', 'multiple_choice', '{"options":["przepraszam","żółw","szczęśliwy"]}', 'szczęśliwy'),
    ('funny-unusual-words', 4, 'Which Polish term means “sorry”?', 'multiple_choice', '{"options":["przepraszam","żółw","chrząszcz"]}', 'przepraszam'),
    ('funny-unusual-words', 5, 'Put “Beetle sounds difficult” in Polish order.', 'word_ordering', '{"tokens":["trudno","brzmi","Chrząszcz"]}', '["Chrząszcz","brzmi","trudno"]'),
    ('funny-unusual-words', 6, 'Put “A turtle walks slowly on the grass” in Polish order.', 'word_ordering', '{"tokens":["trawie","powoli","Żółw","po","idzie"]}', '["Żółw","idzie","powoli","po","trawie"]'),
    ('funny-unusual-words', 7, 'Put “It is only a beetle” in Polish order.', 'word_ordering', '{"tokens":["chrząszcz","tylko","To"]}', '["To","tylko","chrząszcz"]'),
    ('funny-unusual-words', 8, 'Type the Polish term for “happy”.', 'free_text', '{}', 'szczęśliwy'),
    ('funny-unusual-words', 9, 'Type the Polish term for “sorry”.', 'free_text', '{}', 'przepraszam'),
    ('funny-unusual-words', 10, 'Type the Polish term for “turtle”.', 'free_text', '{}', 'żółw')
)
INSERT OR IGNORE INTO QuizQuestions (QuizId, SortOrder, Content, Type, QuestionData, CorrectAnswer)
SELECT q.Id, s.SortOrder, s.Content, s.Type, s.QuestionData, s.CorrectAnswer
FROM QuestionSeeds s
INNER JOIN Courses c ON c.Code = 'pl'
INNER JOIN Lessons l ON l.CourseId = c.Id AND l.Slug = s.LessonSlug
INNER JOIN Quizzes q ON q.LessonId = l.Id;
