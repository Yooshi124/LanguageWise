# TECHNICAL-chat-discussion: Chat Discussion Service (Technical Overview)

Internal reference for the agentic loop. Not for end-user access.

## Backend

- ASP.NET Core (.NET 10) project `LanguageWise.ChatDiscussionService.Api`.
- Key routes: `GET /api/forums`, `GET /api/posts`, `GET /api/posts/{id}`,
  `POST /api/posts`, `POST /api/posts/{id}/comments`,
  `DELETE /api/posts/{id}`, `DELETE /api/comments/{id}`,
  `POST /api/posts/{id}/likes`, `POST /api/comments/{id}/likes`,
  `POST /api/posts/{id}/images`, `POST /api/comments/{id}/images`,
  `GET /api/assistant/topics`, `POST /api/assistant/messages` (SSE),
  `GET /api/assistant/tools`, `POST /api/assistant/tools/{name}`.
- Domain entities: `Post`, `PostDetail`, `Comment`, `CommentDetail`, `Forum`,
  `AttachedImage`.
- Depends on quests-achievements-notifications-service (community contribution
  events), garry-ai-service (assistant completions) and the shared MCP server
  (assistant tools).

## TECHNICAL-chat-discussion-assistant: Garry, MCP and RAG

- `AssistantContextService` builds the canonical context: page, forum code, open
  post ID, forum list and the best-matching `HelpKnowledgeBase` articles, biased
  by the route name. Post content is never injected; Garry reads it through
  `chat_get_post`. The same articles form the fallback answer when Garry is
  unavailable.
- `GarryCompletionClient` posts to Garry's `api/completions` with the user's
  token and `toolScope = chat` when `Mcp:Enabled` is true. Garry runs the tool
  loop against the MCP server.
- MCP tools in the `chat` scope: `chat_list_forums`, `chat_search_posts`,
  `chat_get_post`, plus the shared `docs_search` (RAG `POST /query`, general
  passages only). The MCP server hides `docs_search` when `Rag:Enabled` is false.
- When Garry calls `docs_search`, it appends a numbered sources and confidence
  footer to the streamed answer; the backend relays it unchanged.
- The shell's "Ask the docs" button uses the shared backend's
  `POST /api/rag/answer`, not this service.
- Assistant tool chips call `/api/assistant/tools/{name}`; names must match
  `^chat_[a-z_]{1,58}$` and arguments are limited to 2048 bytes of JSON.

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
- Env vars: `Services__Database`, `Services__Achievements`, `Services__Garry`,
  `Mcp__Enabled`, `Mcp__Endpoint`, `Mcp__ApiKeyPath`.
- CI sets `Mcp__Enabled=false` and `Rag__Enabled=false`; tests use fakes and
  never contact the MCP or RAG server.
