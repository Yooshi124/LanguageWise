-- Spanish lessons, vocabulary, quizzes, and questions. Requires schema.sql and seeds/00-courses.sql.
WITH LessonSeeds (Slug, Title, SortOrder, ContentMarkdown) AS (
    VALUES
    ('greetings', 'Greetings', 1, '## Learn in context

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
    ('introductions', 'Introductions', 2, '## Learn in context

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
    ('politeness', 'Politeness', 3, '## Learn in context

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
    ('numbers', 'Numbers', 4, '## Learn in context

| Target language | English |
| --- | --- |
| Tengo dos billetes y diez euros. | I have two tickets and ten euros. |
| El tren sale a las tres. | The train leaves at three. |

## Worked usage

- **Tengo dos billetes y diez euros.** — *I have two tickets and ten euros.*
- **El tren sale a las tres.** — *The train leaves at three.*

## Language note

Use **a la una** for one o’clock but **a las** for every other hour.'),
    ('family', 'Family', 5, '## Learn in context

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
    ('food', 'Food', 6, '## Learn in context

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
    ('drinks', 'Drinks', 7, '## Learn in context

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
    ('home', 'Home', 8, '## Learn in context

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
    ('travel', 'Travel', 9, '## Learn in context

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
    ('directions', 'Directions', 10, '## Learn in context

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
    ('time-calendar', 'Time and Calendar', 11, '## Learn in context

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
    ('weather', 'Weather', 12, '## Learn in context

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
    ('shopping', 'Shopping', 13, '## Learn in context

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
    ('work-school', 'Work and School', 14, '## Learn in context

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
    ('body-health', 'Body and Health', 15, '## Learn in context

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
    ('emotions', 'Emotions', 16, '## Learn in context

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
    ('hobbies', 'Hobbies', 17, '## Learn in context

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
    ('nature-animals', 'Nature and Animals', 18, '## Learn in context

| Target language | English |
| --- | --- |
| El perro corre por el bosque. | The dog runs through the forest. |
| Un pájaro está en el árbol. | A bird is in the tree. |

## Worked usage

- **El perro corre por el bosque.** — *The dog runs through the forest.*
- **Un pájaro está en el árbol.** — *A bird is in the tree.*

## Language note

Use **por** for movement through an area. **Árbol** has a written stress accent.'),
    ('long-words', 'Long Words', 19, '## Learn in context

| Target language | English |
| --- | --- |
| El electroencefalografista analiza el informe. | The electroencephalograph specialist analyses the report. |
| La anticonstitucionalidad es un tema jurídico. | Unconstitutionality is a legal topic. |

## Worked usage

- **El electroencefalografista analiza el informe.** — *The electroencephalograph specialist analyses the report.*
- **La anticonstitucionalidad es un tema jurídico.** — *Unconstitutionality is a legal topic.*

## Language note

Spanish technical words use Greek and Latin roots: **electro + encefalo + grafista**. Look for familiar chunks. **Otorrinolaringólogo** is another technical word: an ear, nose, and throat doctor.'),
    ('funny-unusual-words', 'Funny and Unusual Words', 20, '## Learn in context

| Target language | English |
| --- | --- |
| Después de comer, seguimos hablando durante la sobremesa. | After eating, we keep talking during the post-meal conversation. |
| Mañana tengo que madrugar. | Tomorrow I have to get up very early. |

## Worked usage

- **Después de comer, seguimos hablando durante la sobremesa.** — *After eating, we keep talking during the post-meal conversation.*
- **Mañana tengo que madrugar.** — *Tomorrow I have to get up very early.*

## Language note

**Sobremesa** is an unhurried post-meal conversation, valued in many Spanish-speaking places. **Madrugar** means getting up early.')
)
INSERT OR IGNORE INTO Lessons (CourseId, Slug, Title, SortOrder, ContentMarkdown)
SELECT c.Id, s.Slug, s.Title, s.SortOrder, s.ContentMarkdown
FROM Courses c
CROSS JOIN LessonSeeds s
WHERE c.Code = 'es';

WITH VocabularySeeds (LessonSlug, VocabularyJson) AS (
    VALUES
    ('greetings', '{"words":[{"word":"Hola","meaning":"Hello"},{"word":"Buenos días","meaning":"Good day"},{"word":"Bienvenido","meaning":"Welcome"},{"word":"Sí","meaning":"Yes"},{"word":"No","meaning":"No"}]}'),
    ('introductions', '{"words":[{"word":"Me llamo ...","meaning":"My name is ..."},{"word":"¿Cómo te llamas?","meaning":"What is your name?"},{"word":"Vengo de ...","meaning":"I come from ..."},{"word":"Mucho gusto","meaning":"Nice to meet you"},{"word":"Esto es ...","meaning":"This is ..."}]}'),
    ('politeness', '{"words":[{"word":"Por favor","meaning":"Please"},{"word":"Gracias","meaning":"Thank you"},{"word":"Perdón","meaning":"Sorry / excuse me"},{"word":"De nada","meaning":"You are welcome"},{"word":"¿Puedes ayudar?","meaning":"Can you help?"}]}'),
    ('numbers', '{"words":[{"word":"uno","meaning":"one"},{"word":"dos","meaning":"two"},{"word":"tres","meaning":"three"},{"word":"diez","meaning":"ten"},{"word":"cien","meaning":"one hundred"}]}'),
    ('family', '{"words":[{"word":"la familia","meaning":"family"},{"word":"la madre","meaning":"mother"},{"word":"el padre","meaning":"father"},{"word":"el hermano","meaning":"brother"},{"word":"la hermana","meaning":"sister"}]}'),
    ('food', '{"words":[{"word":"el pan","meaning":"bread"},{"word":"el queso","meaning":"cheese"},{"word":"la manzana","meaning":"apple"},{"word":"el desayuno","meaning":"breakfast"},{"word":"delicioso","meaning":"tasty"}]}'),
    ('drinks', '{"words":[{"word":"el agua","meaning":"water"},{"word":"el café","meaning":"coffee"},{"word":"el té","meaning":"tea"},{"word":"la cerveza","meaning":"beer"},{"word":"un vaso","meaning":"a glass"}]}'),
    ('home', '{"words":[{"word":"la casa","meaning":"house"},{"word":"el apartamento","meaning":"apartment"},{"word":"la habitación","meaning":"room"},{"word":"la cocina","meaning":"kitchen"},{"word":"la llave","meaning":"key"}]}'),
    ('travel', '{"words":[{"word":"la estación","meaning":"train station"},{"word":"el aeropuerto","meaning":"airport"},{"word":"el billete","meaning":"ticket"},{"word":"la maleta","meaning":"suitcase"},{"word":"salir","meaning":"to depart"}]}'),
    ('directions', '{"words":[{"word":"a la izquierda","meaning":"left"},{"word":"a la derecha","meaning":"right"},{"word":"todo recto","meaning":"straight ahead"},{"word":"la calle","meaning":"street"},{"word":"¿Dónde está ...?","meaning":"Where is ...?"}]}'),
    ('time-calendar', '{"words":[{"word":"hoy","meaning":"today"},{"word":"mañana","meaning":"tomorrow"},{"word":"ayer","meaning":"yesterday"},{"word":"el reloj","meaning":"clock"},{"word":"lunes","meaning":"Monday"}]}'),
    ('weather', '{"words":[{"word":"soleado","meaning":"sunny"},{"word":"lluvioso","meaning":"rainy"},{"word":"el viento","meaning":"wind"},{"word":"frío","meaning":"cold"},{"word":"caluroso","meaning":"warm"}]}'),
    ('shopping', '{"words":[{"word":"comprar","meaning":"to buy"},{"word":"el precio","meaning":"price"},{"word":"caro","meaning":"expensive"},{"word":"barato","meaning":"cheap"},{"word":"la talla","meaning":"size"}]}'),
    ('work-school', '{"words":[{"word":"el trabajo","meaning":"work"},{"word":"la escuela","meaning":"school"},{"word":"el profesor","meaning":"teacher"},{"word":"aprender","meaning":"to learn"},{"word":"la oficina","meaning":"office"}]}'),
    ('body-health', '{"words":[{"word":"la cabeza","meaning":"head"},{"word":"la mano","meaning":"hand"},{"word":"el médico","meaning":"doctor"},{"word":"enfermo","meaning":"ill"},{"word":"Duele","meaning":"It hurts"}]}'),
    ('emotions', '{"words":[{"word":"feliz","meaning":"happy"},{"word":"triste","meaning":"sad"},{"word":"cansado","meaning":"tired"},{"word":"emocionado","meaning":"excited"},{"word":"tener miedo","meaning":"to be afraid"}]}'),
    ('hobbies', '{"words":[{"word":"leer","meaning":"to read"},{"word":"escuchar música","meaning":"to listen to music"},{"word":"cocinar","meaning":"to cook"},{"word":"hacer deporte","meaning":"to do sport"},{"word":"bailar","meaning":"to dance"}]}'),
    ('nature-animals', '{"words":[{"word":"el perro","meaning":"dog"},{"word":"el gato","meaning":"cat"},{"word":"el árbol","meaning":"tree"},{"word":"el bosque","meaning":"forest"},{"word":"el pájaro","meaning":"bird"}]}'),
    ('long-words', '{"words":[{"word":"electroencefalografista","meaning":"electroencephalograph specialist"},{"word":"otorrinolaringólogo","meaning":"ear, nose, and throat doctor"},{"word":"esternocleidomastoideo","meaning":"sternocleidomastoid muscle"},{"word":"desafortunadamente","meaning":"unfortunately"},{"word":"paralelepípedo","meaning":"parallelepiped"}]}'),
    ('funny-unusual-words', '{"words":[{"word":"sobremesa","meaning":"conversation after a meal"},{"word":"madrugar","meaning":"to get up very early"},{"word":"empalagoso","meaning":"sickeningly sweet"},{"word":"tocayo","meaning":"person with the same first name"},{"word":"estrenar","meaning":"to use or wear for the first time"}]}')
)
INSERT OR IGNORE INTO LessonVocabulary (LessonId, VocabularyJson)
SELECT l.Id, s.VocabularyJson
FROM VocabularySeeds s
INNER JOIN Courses c ON c.Code = 'es'
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
INNER JOIN Courses c ON c.Code = 'es'
INNER JOIN Lessons l ON l.CourseId = c.Id AND l.Slug = s.LessonSlug;

WITH QuestionSeeds (LessonSlug, SortOrder, Content, Type, QuestionData, CorrectAnswer) AS (
    VALUES
    ('greetings', 1, 'Which Spanish term means “Hello”?', 'multiple_choice', '{"options":["Hola","Buenos días","Bienvenido"]}', 'Hola'),
    ('greetings', 2, 'Which Spanish term means “Good day”?', 'multiple_choice', '{"options":["Bienvenido","Buenos días","Sí"]}', 'Buenos días'),
    ('greetings', 3, 'Which Spanish term means “Welcome”?', 'multiple_choice', '{"options":["Sí","No","Bienvenido"]}', 'Bienvenido'),
    ('greetings', 4, 'Which Spanish term means “Yes”?', 'multiple_choice', '{"options":["Sí","No","Hola"]}', 'Sí'),
    ('greetings', 5, 'Put “Hello, Ana” in Spanish order.', 'word_ordering', '{"tokens":["Ana","Hola"]}', '["Hola","Ana"]'),
    ('greetings', 6, 'Put “Good morning, sir” in Spanish order.', 'word_ordering', '{"tokens":["señor","días","Buenos"]}', '["Buenos","días","señor"]'),
    ('greetings', 7, 'Put “Welcome to Madrid” in Spanish order.', 'word_ordering', '{"tokens":["Madrid","a","Bienvenida"]}', '["Bienvenida","a","Madrid"]'),
    ('greetings', 8, 'Type the Spanish term for “Welcome”.', 'free_text', '{}', 'Bienvenido'),
    ('greetings', 9, 'Type the Spanish term for “Yes”.', 'free_text', '{}', 'Sí'),
    ('greetings', 10, 'Type the Spanish term for “No”.', 'free_text', '{}', 'No'),
    ('introductions', 1, 'Which Spanish term means “My name is ...”?', 'multiple_choice', '{"options":["Me llamo ...","¿Cómo te llamas?","Vengo de ..."]}', 'Me llamo ...'),
    ('introductions', 2, 'Which Spanish term means “What is your name?”?', 'multiple_choice', '{"options":["Vengo de ...","¿Cómo te llamas?","Mucho gusto"]}', '¿Cómo te llamas?'),
    ('introductions', 3, 'Which Spanish term means “I come from ...”?', 'multiple_choice', '{"options":["Mucho gusto","Esto es ...","Vengo de ..."]}', 'Vengo de ...'),
    ('introductions', 4, 'Which Spanish term means “Nice to meet you”?', 'multiple_choice', '{"options":["Mucho gusto","Esto es ...","Me llamo ..."]}', 'Mucho gusto'),
    ('introductions', 5, 'Put “My name is Sofía” in Spanish order.', 'word_ordering', '{"tokens":["Sofía","llamo","Me"]}', '["Me","llamo","Sofía"]'),
    ('introductions', 6, 'Put “I come from Canada” in Spanish order.', 'word_ordering', '{"tokens":["Canadá","de","Vengo"]}', '["Vengo","de","Canadá"]'),
    ('introductions', 7, 'Put “My name is Diego” in Spanish order.', 'word_ordering', '{"tokens":["Diego","Me","llamo"]}', '["Me","llamo","Diego"]'),
    ('introductions', 8, 'Type the Spanish term for “I come from ...”.', 'free_text', '{}', 'Vengo de ...'),
    ('introductions', 9, 'Type the Spanish term for “Nice to meet you”.', 'free_text', '{}', 'Mucho gusto'),
    ('introductions', 10, 'Type the Spanish term for “This is ...”.', 'free_text', '{}', 'Esto es ...'),
    ('politeness', 1, 'Which Spanish term means “Please”?', 'multiple_choice', '{"options":["Por favor","Gracias","Perdón"]}', 'Por favor'),
    ('politeness', 2, 'Which Spanish term means “Thank you”?', 'multiple_choice', '{"options":["Perdón","Gracias","De nada"]}', 'Gracias'),
    ('politeness', 3, 'Which Spanish term means “Sorry / excuse me”?', 'multiple_choice', '{"options":["De nada","¿Puedes ayudar?","Perdón"]}', 'Perdón'),
    ('politeness', 4, 'Which Spanish term means “You are welcome”?', 'multiple_choice', '{"options":["De nada","¿Puedes ayudar?","Por favor"]}', 'De nada'),
    ('politeness', 5, 'Put “A coffee, please” in Spanish order.', 'word_ordering', '{"tokens":["favor","café","por","Un"]}', '["Un","café","por","favor"]'),
    ('politeness', 6, 'Put “Thank you very much” in Spanish order.', 'word_ordering', '{"tokens":["gracias","Muchas"]}', '["Muchas","gracias"]'),
    ('politeness', 7, 'Put “Excuse me, can you help me?” in Spanish order.', 'word_ordering', '{"tokens":["ayudarme","puede","Perdón"]}', '["Perdón","puede","ayudarme"]'),
    ('politeness', 8, 'Type the Spanish term for “Sorry / excuse me”.', 'free_text', '{}', 'Perdón'),
    ('politeness', 9, 'Type the Spanish term for “You are welcome”.', 'free_text', '{}', 'De nada'),
    ('politeness', 10, 'Type the Spanish term for “Can you help?”.', 'free_text', '{}', '¿Puedes ayudar?'),
    ('numbers', 1, 'Which Spanish term means “one”?', 'multiple_choice', '{"options":["uno","dos","tres"]}', 'uno'),
    ('numbers', 2, 'Which Spanish term means “two”?', 'multiple_choice', '{"options":["tres","dos","diez"]}', 'dos'),
    ('numbers', 3, 'Which Spanish term means “three”?', 'multiple_choice', '{"options":["diez","cien","tres"]}', 'tres'),
    ('numbers', 4, 'Which Spanish term means “ten”?', 'multiple_choice', '{"options":["diez","cien","uno"]}', 'diez'),
    ('numbers', 5, 'Put “I have two tickets and ten euros” in Spanish order.', 'word_ordering', '{"tokens":["euros","billetes","Tengo","diez","dos","y"]}', '["Tengo","dos","billetes","y","diez","euros"]'),
    ('numbers', 6, 'Put “The train leaves at three” in Spanish order.', 'word_ordering', '{"tokens":["tres","sale","las","tren","a","El"]}', '["El","tren","sale","a","las","tres"]'),
    ('numbers', 7, 'Put “at one o’clock” in Spanish order.', 'word_ordering', '{"tokens":["una","a","la"]}', '["a","la","una"]'),
    ('numbers', 8, 'Type the Spanish term for “three”.', 'free_text', '{}', 'tres'),
    ('numbers', 9, 'Type the Spanish term for “ten”.', 'free_text', '{}', 'diez'),
    ('numbers', 10, 'Type the Spanish term for “one hundred”.', 'free_text', '{}', 'cien'),
    ('family', 1, 'Which Spanish term means “family”?', 'multiple_choice', '{"options":["la familia","la madre","el padre"]}', 'la familia'),
    ('family', 2, 'Which Spanish term means “mother”?', 'multiple_choice', '{"options":["el padre","la madre","el hermano"]}', 'la madre'),
    ('family', 3, 'Which Spanish term means “father”?', 'multiple_choice', '{"options":["el hermano","la hermana","el padre"]}', 'el padre'),
    ('family', 4, 'Which Spanish term means “brother”?', 'multiple_choice', '{"options":["el hermano","la hermana","la familia"]}', 'el hermano'),
    ('family', 5, 'Put “This is my mother and this is my father” in Spanish order.', 'word_ordering', '{"tokens":["padre","mi","es","Esta","madre","este","y","mi","es"]}', '["Esta","es","mi","madre","y","este","es","mi","padre"]'),
    ('family', 6, 'Put “My sister has a brother” in Spanish order.', 'word_ordering', '{"tokens":["hermano","Mi","un","tiene","hermana"]}', '["Mi","hermana","tiene","un","hermano"]'),
    ('family', 7, 'Put “No, but I have a sister” in Spanish order.', 'word_ordering', '{"tokens":["hermana","pero","tengo","No","una"]}', '["No","pero","tengo","una","hermana"]'),
    ('family', 8, 'Type the Spanish term for “father”.', 'free_text', '{}', 'el padre'),
    ('family', 9, 'Type the Spanish term for “brother”.', 'free_text', '{}', 'el hermano'),
    ('family', 10, 'Type the Spanish term for “sister”.', 'free_text', '{}', 'la hermana'),
    ('food', 1, 'Which Spanish term means “bread”?', 'multiple_choice', '{"options":["el pan","el queso","la manzana"]}', 'el pan'),
    ('food', 2, 'Which Spanish term means “cheese”?', 'multiple_choice', '{"options":["la manzana","el queso","el desayuno"]}', 'el queso'),
    ('food', 3, 'Which Spanish term means “apple”?', 'multiple_choice', '{"options":["el desayuno","delicioso","la manzana"]}', 'la manzana'),
    ('food', 4, 'Which Spanish term means “breakfast”?', 'multiple_choice', '{"options":["el desayuno","delicioso","el pan"]}', 'el desayuno'),
    ('food', 5, 'Put “I eat bread and cheese” in Spanish order.', 'word_ordering', '{"tokens":["queso","Como","y","pan"]}', '["Como","pan","y","queso"]'),
    ('food', 6, 'Put “The apple is delicious” in Spanish order.', 'word_ordering', '{"tokens":["deliciosa","manzana","está","La"]}', '["La","manzana","está","deliciosa"]'),
    ('food', 7, 'Put “What do you eat for breakfast?” in Spanish order.', 'word_ordering', '{"tokens":["desayuno","comes","el","Qué","en"]}', '["Qué","comes","en","el","desayuno"]'),
    ('food', 8, 'Type the Spanish term for “apple”.', 'free_text', '{}', 'la manzana'),
    ('food', 9, 'Type the Spanish term for “breakfast”.', 'free_text', '{}', 'el desayuno'),
    ('food', 10, 'Type the Spanish term for “tasty”.', 'free_text', '{}', 'delicioso'),
    ('drinks', 1, 'Which Spanish term means “water”?', 'multiple_choice', '{"options":["el agua","el café","el té"]}', 'el agua'),
    ('drinks', 2, 'Which Spanish term means “coffee”?', 'multiple_choice', '{"options":["el té","el café","la cerveza"]}', 'el café'),
    ('drinks', 3, 'Which Spanish term means “tea”?', 'multiple_choice', '{"options":["la cerveza","un vaso","el té"]}', 'el té'),
    ('drinks', 4, 'Which Spanish term means “beer”?', 'multiple_choice', '{"options":["la cerveza","un vaso","el agua"]}', 'la cerveza'),
    ('drinks', 5, 'Put “I would like a glass of water, please” in Spanish order.', 'word_ordering', '{"tokens":["agua","favor","vaso","Quisiera","por","de","un"]}', '["Quisiera","un","vaso","de","agua","por","favor"]'),
    ('drinks', 6, 'Put “The coffee is hot and the tea is lukewarm” in Spanish order.', 'word_ordering', '{"tokens":["tibio","está","café","el","caliente","té","El","y","está"]}', '["El","café","está","caliente","y","el","té","está","tibio"]'),
    ('drinks', 7, 'Put “The coffee is very hot” in Spanish order.', 'word_ordering', '{"tokens":["caliente","muy","está","café","El"]}', '["El","café","está","muy","caliente"]'),
    ('drinks', 8, 'Type the Spanish term for “tea”.', 'free_text', '{}', 'el té'),
    ('drinks', 9, 'Type the Spanish term for “beer”.', 'free_text', '{}', 'la cerveza'),
    ('drinks', 10, 'Type the Spanish term for “a glass”.', 'free_text', '{}', 'un vaso'),
    ('home', 1, 'Which Spanish term means “house”?', 'multiple_choice', '{"options":["la casa","el apartamento","la habitación"]}', 'la casa'),
    ('home', 2, 'Which Spanish term means “apartment”?', 'multiple_choice', '{"options":["la habitación","el apartamento","la cocina"]}', 'el apartamento'),
    ('home', 3, 'Which Spanish term means “room”?', 'multiple_choice', '{"options":["la cocina","la llave","la habitación"]}', 'la habitación'),
    ('home', 4, 'Which Spanish term means “kitchen”?', 'multiple_choice', '{"options":["la cocina","la llave","la casa"]}', 'la cocina'),
    ('home', 5, 'Put “The kitchen is in the apartment” in Spanish order.', 'word_ordering', '{"tokens":["apartamento","está","cocina","el","en","La"]}', '["La","cocina","está","en","el","apartamento"]'),
    ('home', 6, 'Put “Where is the key?” in Spanish order.', 'word_ordering', '{"tokens":["llave","está","Dónde","la"]}', '["Dónde","está","la","llave"]'),
    ('home', 7, 'Put “The key is in the kitchen” in Spanish order.', 'word_ordering', '{"tokens":["cocina","está","llave","la","en","La"]}', '["La","llave","está","en","la","cocina"]'),
    ('home', 8, 'Type the Spanish term for “room”.', 'free_text', '{}', 'la habitación'),
    ('home', 9, 'Type the Spanish term for “kitchen”.', 'free_text', '{}', 'la cocina'),
    ('home', 10, 'Type the Spanish term for “key”.', 'free_text', '{}', 'la llave'),
    ('travel', 1, 'Which Spanish term means “train station”?', 'multiple_choice', '{"options":["la estación","el aeropuerto","el billete"]}', 'la estación'),
    ('travel', 2, 'Which Spanish term means “airport”?', 'multiple_choice', '{"options":["el billete","el aeropuerto","la maleta"]}', 'el aeropuerto'),
    ('travel', 3, 'Which Spanish term means “ticket”?', 'multiple_choice', '{"options":["la maleta","salir","el billete"]}', 'el billete'),
    ('travel', 4, 'Which Spanish term means “suitcase”?', 'multiple_choice', '{"options":["la maleta","salir","la estación"]}', 'la maleta'),
    ('travel', 5, 'Put “The station is near the airport” in Spanish order.', 'word_ordering', '{"tokens":["aeropuerto","cerca","está","del","estación","La"]}', '["La","estación","está","cerca","del","aeropuerto"]'),
    ('travel', 6, 'Put “The ticket is in the suitcase” in Spanish order.', 'word_ordering', '{"tokens":["maleta","está","billete","la","en","El"]}', '["El","billete","está","en","la","maleta"]'),
    ('travel', 7, 'Put “Your ticket is in the suitcase” in Spanish order.', 'word_ordering', '{"tokens":["maleta","billete","la","Tu","en","está"]}', '["Tu","billete","está","en","la","maleta"]'),
    ('travel', 8, 'Type the Spanish term for “ticket”.', 'free_text', '{}', 'el billete'),
    ('travel', 9, 'Type the Spanish term for “suitcase”.', 'free_text', '{}', 'la maleta'),
    ('travel', 10, 'Type the Spanish term for “to depart”.', 'free_text', '{}', 'salir'),
    ('directions', 1, 'Which Spanish term means “left”?', 'multiple_choice', '{"options":["a la izquierda","a la derecha","todo recto"]}', 'a la izquierda'),
    ('directions', 2, 'Which Spanish term means “right”?', 'multiple_choice', '{"options":["todo recto","a la derecha","la calle"]}', 'a la derecha'),
    ('directions', 3, 'Which Spanish term means “straight ahead”?', 'multiple_choice', '{"options":["la calle","¿Dónde está ...?","todo recto"]}', 'todo recto'),
    ('directions', 4, 'Which Spanish term means “street”?', 'multiple_choice', '{"options":["la calle","¿Dónde está ...?","a la izquierda"]}', 'la calle'),
    ('directions', 5, 'Put “Go straight ahead and turn left” in Spanish order.', 'word_ordering', '{"tokens":["izquierda","recto","gire","todo","Siga","la","a","y"]}', '["Siga","todo","recto","y","gire","a","la","izquierda"]'),
    ('directions', 6, 'Put “to the right of the bank” in Spanish order.', 'word_ordering', '{"tokens":["banco","derecha","del","la","A"]}', '["A","la","derecha","del","banco"]'),
    ('directions', 7, 'Put “It is on the right” in Spanish order.', 'word_ordering', '{"tokens":["derecha","Está","la","a"]}', '["Está","a","la","derecha"]'),
    ('directions', 8, 'Type the Spanish term for “straight ahead”.', 'free_text', '{}', 'todo recto'),
    ('directions', 9, 'Type the Spanish term for “street”.', 'free_text', '{}', 'la calle'),
    ('directions', 10, 'Type the Spanish term for “Where is ...?”.', 'free_text', '{}', '¿Dónde está ...?'),
    ('time-calendar', 1, 'Which Spanish term means “today”?', 'multiple_choice', '{"options":["hoy","mañana","ayer"]}', 'hoy'),
    ('time-calendar', 2, 'Which Spanish term means “tomorrow”?', 'multiple_choice', '{"options":["ayer","mañana","el reloj"]}', 'mañana'),
    ('time-calendar', 3, 'Which Spanish term means “yesterday”?', 'multiple_choice', '{"options":["el reloj","lunes","ayer"]}', 'ayer'),
    ('time-calendar', 4, 'Which Spanish term means “clock”?', 'multiple_choice', '{"options":["el reloj","lunes","hoy"]}', 'el reloj'),
    ('time-calendar', 5, 'Put “Today is Monday” in Spanish order.', 'word_ordering', '{"tokens":["lunes","Hoy","es"]}', '["Hoy","es","lunes"]'),
    ('time-calendar', 6, 'Put “Tomorrow I have class” in Spanish order.', 'word_ordering', '{"tokens":["clase","tengo","Mañana"]}', '["Mañana","tengo","clase"]'),
    ('time-calendar', 7, 'Put “The class starts at eight” in Spanish order.', 'word_ordering', '{"tokens":["ocho","clase","las","empieza","La","a"]}', '["La","clase","empieza","a","las","ocho"]'),
    ('time-calendar', 8, 'Type the Spanish term for “yesterday”.', 'free_text', '{}', 'ayer'),
    ('time-calendar', 9, 'Type the Spanish term for “clock”.', 'free_text', '{}', 'el reloj'),
    ('time-calendar', 10, 'Type the Spanish term for “Monday”.', 'free_text', '{}', 'lunes'),
    ('weather', 1, 'Which Spanish term means “sunny”?', 'multiple_choice', '{"options":["soleado","lluvioso","el viento"]}', 'soleado'),
    ('weather', 2, 'Which Spanish term means “rainy”?', 'multiple_choice', '{"options":["el viento","lluvioso","frío"]}', 'lluvioso'),
    ('weather', 3, 'Which Spanish term means “wind”?', 'multiple_choice', '{"options":["frío","caluroso","el viento"]}', 'el viento'),
    ('weather', 4, 'Which Spanish term means “cold”?', 'multiple_choice', '{"options":["frío","caluroso","soleado"]}', 'frío'),
    ('weather', 5, 'Put “Today it is sunny, but cold” in Spanish order.', 'word_ordering', '{"tokens":["frío","sol","pero","Hoy","hace","hace"]}', '["Hoy","hace","sol","pero","hace","frío"]'),
    ('weather', 6, 'Put “Tomorrow it will be rainy and windy” in Spanish order.', 'word_ordering', '{"tokens":["viento","lluvioso","Mañana","habrá","y","estará"]}', '["Mañana","estará","lluvioso","y","habrá","viento"]'),
    ('weather', 7, 'Put “There is wind” in Spanish order.', 'word_ordering', '{"tokens":["viento","Hay"]}', '["Hay","viento"]'),
    ('weather', 8, 'Type the Spanish term for “wind”.', 'free_text', '{}', 'el viento'),
    ('weather', 9, 'Type the Spanish term for “cold”.', 'free_text', '{}', 'frío'),
    ('weather', 10, 'Type the Spanish term for “warm”.', 'free_text', '{}', 'caluroso'),
    ('shopping', 1, 'Which Spanish term means “to buy”?', 'multiple_choice', '{"options":["comprar","el precio","caro"]}', 'comprar'),
    ('shopping', 2, 'Which Spanish term means “price”?', 'multiple_choice', '{"options":["caro","el precio","barato"]}', 'el precio'),
    ('shopping', 3, 'Which Spanish term means “expensive”?', 'multiple_choice', '{"options":["barato","la talla","caro"]}', 'caro'),
    ('shopping', 4, 'Which Spanish term means “cheap”?', 'multiple_choice', '{"options":["barato","la talla","comprar"]}', 'barato'),
    ('shopping', 5, 'Put “How much does this shirt cost?” in Spanish order.', 'word_ordering', '{"tokens":["camisa","cuesta","Cuánto","esta"]}', '["Cuánto","cuesta","esta","camisa"]'),
    ('shopping', 6, 'Put “It costs twenty euros” in Spanish order.', 'word_ordering', '{"tokens":["euros","Cuesta","veinte"]}', '["Cuesta","veinte","euros"]'),
    ('shopping', 7, 'Put “I want to buy this shirt” in Spanish order.', 'word_ordering', '{"tokens":["camisa","comprar","Quiero","esta"]}', '["Quiero","comprar","esta","camisa"]'),
    ('shopping', 8, 'Type the Spanish term for “expensive”.', 'free_text', '{}', 'caro'),
    ('shopping', 9, 'Type the Spanish term for “cheap”.', 'free_text', '{}', 'barato'),
    ('shopping', 10, 'Type the Spanish term for “size”.', 'free_text', '{}', 'la talla'),
    ('work-school', 1, 'Which Spanish term means “work”?', 'multiple_choice', '{"options":["el trabajo","la escuela","el profesor"]}', 'el trabajo'),
    ('work-school', 2, 'Which Spanish term means “school”?', 'multiple_choice', '{"options":["el profesor","la escuela","aprender"]}', 'la escuela'),
    ('work-school', 3, 'Which Spanish term means “teacher”?', 'multiple_choice', '{"options":["aprender","la oficina","el profesor"]}', 'el profesor'),
    ('work-school', 4, 'Which Spanish term means “to learn”?', 'multiple_choice', '{"options":["aprender","la oficina","el trabajo"]}', 'aprender'),
    ('work-school', 5, 'Put “I learn Spanish at school” in Spanish order.', 'word_ordering', '{"tokens":["escuela","español","la","Aprendo","en"]}', '["Aprendo","español","en","la","escuela"]'),
    ('work-school', 6, 'Put “The teacher works in the office” in Spanish order.', 'word_ordering', '{"tokens":["oficina","profesora","la","trabaja","La","en"]}', '["La","profesora","trabaja","en","la","oficina"]'),
    ('work-school', 7, 'Put “I work in the office” in Spanish order.', 'word_ordering', '{"tokens":["oficina","Trabajo","la","en"]}', '["Trabajo","en","la","oficina"]'),
    ('work-school', 8, 'Type the Spanish term for “teacher”.', 'free_text', '{}', 'el profesor'),
    ('work-school', 9, 'Type the Spanish term for “to learn”.', 'free_text', '{}', 'aprender'),
    ('work-school', 10, 'Type the Spanish term for “office”.', 'free_text', '{}', 'la oficina'),
    ('body-health', 1, 'Which Spanish term means “head”?', 'multiple_choice', '{"options":["la cabeza","la mano","el médico"]}', 'la cabeza'),
    ('body-health', 2, 'Which Spanish term means “hand”?', 'multiple_choice', '{"options":["el médico","la mano","enfermo"]}', 'la mano'),
    ('body-health', 3, 'Which Spanish term means “doctor”?', 'multiple_choice', '{"options":["enfermo","Duele","el médico"]}', 'el médico'),
    ('body-health', 4, 'Which Spanish term means “ill”?', 'multiple_choice', '{"options":["enfermo","Duele","la cabeza"]}', 'enfermo'),
    ('body-health', 5, 'Put “My head hurts” in Spanish order.', 'word_ordering', '{"tokens":["cabeza","duele","la","Me"]}', '["Me","duele","la","cabeza"]'),
    ('body-health', 6, 'Put “I am ill” in Spanish order.', 'word_ordering', '{"tokens":["enfermo","Estoy"]}', '["Estoy","enfermo"]'),
    ('body-health', 7, 'Put “The doctor looks at my hand” in Spanish order.', 'word_ordering', '{"tokens":["mano","mira","médico","mi","El"]}', '["El","médico","mira","mi","mano"]'),
    ('body-health', 8, 'Type the Spanish term for “doctor”.', 'free_text', '{}', 'el médico'),
    ('body-health', 9, 'Type the Spanish term for “ill”.', 'free_text', '{}', 'enfermo'),
    ('body-health', 10, 'Type the Spanish term for “It hurts”.', 'free_text', '{}', 'Duele'),
    ('emotions', 1, 'Which Spanish term means “happy”?', 'multiple_choice', '{"options":["feliz","triste","cansado"]}', 'feliz'),
    ('emotions', 2, 'Which Spanish term means “sad”?', 'multiple_choice', '{"options":["cansado","triste","emocionado"]}', 'triste'),
    ('emotions', 3, 'Which Spanish term means “tired”?', 'multiple_choice', '{"options":["emocionado","tener miedo","cansado"]}', 'cansado'),
    ('emotions', 4, 'Which Spanish term means “excited”?', 'multiple_choice', '{"options":["emocionado","tener miedo","feliz"]}', 'emocionado'),
    ('emotions', 5, 'Put “I am happy, but tired” in Spanish order.', 'word_ordering', '{"tokens":["cansada","feliz","pero","Estoy"]}', '["Estoy","feliz","pero","cansada"]'),
    ('emotions', 6, 'Put “He is afraid of the dog” in Spanish order.', 'word_ordering', '{"tokens":["perro","miedo","Él","del","tiene"]}', '["Él","tiene","miedo","del","perro"]'),
    ('emotions', 7, 'Put “Why is your brother sad?” in Spanish order.', 'word_ordering', '{"tokens":["hermano","triste","qué","tu","Por","está"]}', '["Por","qué","está","triste","tu","hermano"]'),
    ('emotions', 8, 'Type the Spanish term for “tired”.', 'free_text', '{}', 'cansado'),
    ('emotions', 9, 'Type the Spanish term for “excited”.', 'free_text', '{}', 'emocionado'),
    ('emotions', 10, 'Type the Spanish term for “to be afraid”.', 'free_text', '{}', 'tener miedo'),
    ('hobbies', 1, 'Which Spanish term means “to read”?', 'multiple_choice', '{"options":["leer","escuchar música","cocinar"]}', 'leer'),
    ('hobbies', 2, 'Which Spanish term means “to listen to music”?', 'multiple_choice', '{"options":["cocinar","escuchar música","hacer deporte"]}', 'escuchar música'),
    ('hobbies', 3, 'Which Spanish term means “to cook”?', 'multiple_choice', '{"options":["hacer deporte","bailar","cocinar"]}', 'cocinar'),
    ('hobbies', 4, 'Which Spanish term means “to do sport”?', 'multiple_choice', '{"options":["hacer deporte","bailar","leer"]}', 'hacer deporte'),
    ('hobbies', 5, 'Put “I like reading and listening to music” in Spanish order.', 'word_ordering', '{"tokens":["música","leer","Me","escuchar","y","gusta"]}', '["Me","gusta","leer","y","escuchar","música"]'),
    ('hobbies', 6, 'Put “On Saturdays I do sport and dance” in Spanish order.', 'word_ordering', '{"tokens":["deporte","bailo","sábados","hago","Los","y"]}', '["Los","sábados","hago","deporte","y","bailo"]'),
    ('hobbies', 7, 'Put “I like doing sport and dancing” in Spanish order.', 'word_ordering', '{"tokens":["bailar","deporte","Me","hacer","y","gusta"]}', '["Me","gusta","hacer","deporte","y","bailar"]'),
    ('hobbies', 8, 'Type the Spanish term for “to cook”.', 'free_text', '{}', 'cocinar'),
    ('hobbies', 9, 'Type the Spanish term for “to do sport”.', 'free_text', '{}', 'hacer deporte'),
    ('hobbies', 10, 'Type the Spanish term for “to dance”.', 'free_text', '{}', 'bailar'),
    ('nature-animals', 1, 'Which Spanish term means “dog”?', 'multiple_choice', '{"options":["el perro","el gato","el árbol"]}', 'el perro'),
    ('nature-animals', 2, 'Which Spanish term means “cat”?', 'multiple_choice', '{"options":["el árbol","el gato","el bosque"]}', 'el gato'),
    ('nature-animals', 3, 'Which Spanish term means “tree”?', 'multiple_choice', '{"options":["el bosque","el pájaro","el árbol"]}', 'el árbol'),
    ('nature-animals', 4, 'Which Spanish term means “forest”?', 'multiple_choice', '{"options":["el bosque","el pájaro","el perro"]}', 'el bosque'),
    ('nature-animals', 5, 'Put “The dog runs through the forest” in Spanish order.', 'word_ordering', '{"tokens":["bosque","perro","el","corre","El","por"]}', '["El","perro","corre","por","el","bosque"]'),
    ('nature-animals', 6, 'Put “A bird is in the tree” in Spanish order.', 'word_ordering', '{"tokens":["árbol","pájaro","el","está","Un","en"]}', '["Un","pájaro","está","en","el","árbol"]'),
    ('nature-animals', 7, 'Put “The cat is in the tree” in Spanish order.', 'word_ordering', '{"tokens":["árbol","gato","el","está","El","en"]}', '["El","gato","está","en","el","árbol"]'),
    ('nature-animals', 8, 'Type the Spanish term for “tree”.', 'free_text', '{}', 'el árbol'),
    ('nature-animals', 9, 'Type the Spanish term for “forest”.', 'free_text', '{}', 'el bosque'),
    ('nature-animals', 10, 'Type the Spanish term for “bird”.', 'free_text', '{}', 'el pájaro'),
    ('long-words', 1, 'Which Spanish term means “electroencephalograph specialist”?', 'multiple_choice', '{"options":["electroencefalografista","otorrinolaringólogo","esternocleidomastoideo"]}', 'electroencefalografista'),
    ('long-words', 2, 'Which Spanish term means “ear, nose, and throat doctor”?', 'multiple_choice', '{"options":["esternocleidomastoideo","otorrinolaringólogo","desafortunadamente"]}', 'otorrinolaringólogo'),
    ('long-words', 3, 'Which Spanish term means “sternocleidomastoid muscle”?', 'multiple_choice', '{"options":["desafortunadamente","paralelepípedo","esternocleidomastoideo"]}', 'esternocleidomastoideo'),
    ('long-words', 4, 'Which Spanish term means “unfortunately”?', 'multiple_choice', '{"options":["desafortunadamente","paralelepípedo","electroencefalografista"]}', 'desafortunadamente'),
    ('long-words', 5, 'Put “The electroencephalograph specialist analyses the report” in Spanish order.', 'word_ordering', '{"tokens":["informe","electroencefalografista","el","analiza","El"]}', '["El","electroencefalografista","analiza","el","informe"]'),
    ('long-words', 6, 'Put “Unconstitutionality is a legal topic” in Spanish order.', 'word_ordering', '{"tokens":["jurídico","anticonstitucionalidad","tema","La","un","es"]}', '["La","anticonstitucionalidad","es","un","tema","jurídico"]'),
    ('long-words', 7, 'Put “The ear, nose and throat doctor is a doctor” in Spanish order.', 'word_ordering', '{"tokens":["médico","otorrinolaringólogo","un","es","El"]}', '["El","otorrinolaringólogo","es","un","médico"]'),
    ('long-words', 8, 'Type the Spanish term for “sternocleidomastoid muscle”.', 'free_text', '{}', 'esternocleidomastoideo'),
    ('long-words', 9, 'Type the Spanish term for “unfortunately”.', 'free_text', '{}', 'desafortunadamente'),
    ('long-words', 10, 'Type the Spanish term for “parallelepiped”.', 'free_text', '{}', 'paralelepípedo'),
    ('funny-unusual-words', 1, 'Which Spanish term means “conversation after a meal”?', 'multiple_choice', '{"options":["sobremesa","madrugar","empalagoso"]}', 'sobremesa'),
    ('funny-unusual-words', 2, 'Which Spanish term means “to get up very early”?', 'multiple_choice', '{"options":["empalagoso","madrugar","tocayo"]}', 'madrugar'),
    ('funny-unusual-words', 3, 'Which Spanish term means “sickeningly sweet”?', 'multiple_choice', '{"options":["tocayo","estrenar","empalagoso"]}', 'empalagoso'),
    ('funny-unusual-words', 4, 'Which Spanish term means “person with the same first name”?', 'multiple_choice', '{"options":["tocayo","estrenar","sobremesa"]}', 'tocayo'),
    ('funny-unusual-words', 5, 'Put “After eating, we keep talking during the post-meal conversation” in Spanish order.', 'word_ordering', '{"tokens":["sobremesa","hablando","comer","la","seguimos","durante","Después","de"]}', '["Después","de","comer","seguimos","hablando","durante","la","sobremesa"]'),
    ('funny-unusual-words', 6, 'Put “Tomorrow I have to get up very early” in Spanish order.', 'word_ordering', '{"tokens":["madrugar","Mañana","que","tengo"]}', '["Mañana","tengo","que","madrugar"]'),
    ('funny-unusual-words', 7, 'Put “The post-meal conversation is a conversation” in Spanish order.', 'word_ordering', '{"tokens":["conversación","sobremesa","una","es","La"]}', '["La","sobremesa","es","una","conversación"]'),
    ('funny-unusual-words', 8, 'Type the Spanish term for “sickeningly sweet”.', 'free_text', '{}', 'empalagoso'),
    ('funny-unusual-words', 9, 'Type the Spanish term for “person with the same first name”.', 'free_text', '{}', 'tocayo'),
    ('funny-unusual-words', 10, 'Type the Spanish term for “to use or wear for the first time”.', 'free_text', '{}', 'estrenar')
)
INSERT OR IGNORE INTO QuizQuestions (
    QuizId,
    SortOrder,
    Content,
    Type,
    QuestionData,
    CorrectAnswer
)
SELECT
    q.Id,
    s.SortOrder,
    s.Content,
    s.Type,
    s.QuestionData,
    s.CorrectAnswer
FROM QuestionSeeds s
INNER JOIN Courses c ON c.Code = 'es'
INNER JOIN Lessons l ON l.CourseId = c.Id AND l.Slug = s.LessonSlug
INNER JOIN Quizzes q ON q.LessonId = l.Id;
