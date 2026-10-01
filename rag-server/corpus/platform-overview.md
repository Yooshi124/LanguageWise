# Platform Overview

LanguageWise is a language-learning platform built as independently deployed
microservices with one federated Vue 3 browser application, orchestrated with
Docker Compose. Open the application at http://localhost:3000.

## For users

The shared frontend owns navigation, authentication, and routing. From one
sign-in you can reach every feature: mini games, the discussion forum, quizzes
and courses, achievements and notifications, and the leaderboard and analytics.
Each feature is a lazy-loaded remote; if one fails to load you see an isolated
retry view while the rest of the app keeps working.

## For agents

Each feature is a separate backend service with its own database.

The Garry AI assistant retrieves grounding context from this RAG server by
calling the `retrieve_context` MCP tool. Ask a natural-language question and it
returns the most relevant passages, each labelled with its source service and
heading. No other agent tools have been implemented for the feature services
yet.

## Services at a glance

- **mini-games-service** — vocabulary practice games (Guess the Word, Word
  Search, Associations).
- **chat-discussion-service** — student discussion forum with posts, likes,
  replies, and comments.
- **quizzes-courses-service** — static quizzes and course content plus
  AI-generated questions and flashcards.
- **quests-achievements-notifications-service** — event-driven notifications,
  emails, achievements, and completion certificates.
- **leaderboard-analytics-service** — global analytics, rankings, and
  customisable visualisations.
