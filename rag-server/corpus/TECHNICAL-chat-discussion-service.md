# TECHNICAL-chat-discussion: Chat Discussion Service (Technical Overview)

Internal reference for the agentic loop. Not for end-user access.

## Backend

- ASP.NET Core (.NET 10) project `LanguageWise.ChatDiscussionService.Api`.
- Key routes: `GET /api/forums`, `GET /api/posts`, `GET /api/posts/{id}`,
  `POST /api/posts`, `POST /api/posts/{id}/comments`,
  `DELETE /api/posts/{id}`, `DELETE /api/comments/{id}`,
  `POST /api/posts/{id}/likes`, `POST /api/comments/{id}/likes`,
  `POST /api/posts/{id}/images`, `POST /api/comments/{id}/images`,
  `POST /api/assistant/messages`.
- Domain entities: `Post`, `PostDetail`, `Comment`, `CommentDetail`, `Forum`,
  `AttachedImage`.
- Depends on quests-achievements-notifications-service (community contribution
  events) and garry-ai-service (discussion topic suggestions).

## Database

- SQLite, file `chat-discussion-service.db`, plus an `/data/images` folder for
  uploaded attachments. Container `chat-discussion-service-db`, host port `6002`.

## Frontend

- Vue 3 (JavaScript), package `languagewise-chat-discussion-frontend`, Module
  Federation remote.
- Key views: `ForumIndexView`, `ForumView`, `PostView`, `PostCreateView`,
  `PostEditView`, `MyPostsView`.

## Deployment

- Backend service `chat-discussion-service-backend`, host port `5002`.
- Env vars: `Services__Database`, `Services__Achievements`, `Services__Garry`.
