# TECHNICAL-quizzes-courses: Quizzes & Courses Service (Technical Overview)

Internal reference for the agentic loop. Not for end-user access.

## Backend

- ASP.NET Core (.NET 10) project `LanguageWise.QuizzesCoursesService.Api`.
- Key routes: `GET /api/courses`, `GET /api/courses/{code}`, `GET /api/courses/{code}/lessons`,
  `GET /api/courses/{code}/quizzes`, `POST /api/quizzes/{quizId}/attempts`,
  `POST /api/quiz-attempts/{attemptId}/submit`, `GET /api/courses/{code}/flashcard-decks`,
  `GET /api/milestones`, `POST /api/assistant/messages`.
- Domain entities: `Course`, `Lesson`, `Quiz`, `QuizQuestion`, `QuizAttempt`,
  `QuizAnswerReview`, `Flashcard`, `VocabularyWord`, `Milestone`.
- Calls out to the quests-achievements-notifications-service (achievement events),
  garry-ai-service (AI course assistant), and the MCP server for tool invocations.

## Database

- SQLite, file `quizzes-service.db`, mounted at `/data` in the
  `quizzes-courses-service-db` container, exposed on host port `6003`.

## Frontend

- Vue 3 + TypeScript, package `languagewise-quizzes-courses-frontend`, uses
  Vuetify 3 and Module Federation to expose remote components to the shell.
- Key views: `LanguageSelectionView`, `CourseView`, `LessonDetail`,
  `QuizRunnerView`, `FlashcardDecksView`, `FlashcardRevisionView`,
  `CourseCompletionView`.

## Deployment

- Backend service `quizzes-courses-service-backend`, host port `5003`.
- Env vars: `Services__Database`, `Services__Achievements`, `Services__Garry`.
