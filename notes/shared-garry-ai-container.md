# Common Aspects for a Shared Garry AI Container

Compared the five implementations: chat discussion, leaderboard analytics, mini-games, quests/achievements, and quizzes/courses.

## Shared Capabilities

- **One chat-completion API:** Each backend exposes `POST /api/assistant/messages`. Requests share the basic shape of a current `message`, prior `history`, and page-specific `context`.
- **Request validation and bounded history:** All five enforce a 4,000-character message limit, up to 12 earlier turns, and a 12,000-character total conversation limit. History roles are restricted to `user` and `assistant`.
- **Per-user throttling:** Each backend configures a limit of 10 assistant requests per minute per user.
- **Prompt assembly:** Each implementation builds model messages from a system prompt, server-supplied canonical context, conversation history, and the current user message.
- **Grounding and prompt-injection protections:** Shared prompt rules include treating conversation and context as data rather than instructions, avoiding disclosure of system prompts or raw context, and acknowledging when the supplied facts do not answer a question.
- **Streaming responses:** The model response is streamed to the client as server-sent events. The common event contract is `delta`, `done`, and `error`; clients can cancel an in-progress request.
- **Provider integration:** The backends support streaming chat completions, provider error handling, cancellation, and configurable model settings. Provider selection and fallback policy should be configurable in a shared service.
- **Operational controls:** The shared service would need configuration for provider URLs, model names, token limits and credentials, plus structured logging and clear client-safe error responses.

## Keep Domain-Specific

- **Context retrieval and authorization:** Context comes from different sources: game rules, course/catalog data, learner profiles, analytics, or forum help content. These sources are owned by the relevant service today. A shared container should receive validated canonical context or use explicitly defined domain adapters; it should not assume one universal context schema.
- **System prompt content and domain rules:** Garry’s role, allowed topics, and restrictions differ by domain. For example, the learning assistant must not answer an in-progress quiz, and the mini-games assistant must not reveal hidden answers. These should remain domain-configurable.
- **Route-context schemas:** `context` is not identical across services; it can include route names and domain identifiers such as course, lesson, game, or forum/post details.
- **Transcript persistence:** The frontends keep bounded conversation transcripts in user-scoped `sessionStorage`; the backends do not share a conversation database. Persistent server-side chat storage is not a current common requirement.

## Differences to Resolve

- **Provider strategy varies:** Chat discussion uses Ollama; quizzes/courses and mini-games use OpenRouter; leaderboard and quests/achievements use OpenRouter with Ollama fallback. Chat discussion also has a help-text fallback when the model is unavailable.
- **The model runtime is already partly shared:** Docker Compose defines a shared `ollama` container, while Garry’s request handling, prompts, context loading, and stream relaying remain in the individual backend services. A shared Garry service could centralize that orchestration while domain services retain responsibility for supplying authorized context.

## Code References

- Request handling and validation: [chat discussion validator](../chat-discussion-service/backend/LanguageWise.ChatDiscussionService.Api/Services/AssistantRequestValidator.cs), [mini-games validator](../mini-games-service/backend/LanguageWise.MiniGamesService.Api/Services/AssistantRequestValidator.cs)
- Prompt and context patterns: [discussion prompt](../chat-discussion-service/backend/LanguageWise.ChatDiscussionService.Api/Services/AssistantPromptBuilder.cs), [mini-games context](../mini-games-service/backend/LanguageWise.MiniGamesService.Api/Services/AssistantContextService.cs), [course context](../quizzes-courses-service/backend/LanguageWise.QuizzesCoursesService.Api/Services/AssistantContextService.cs)
- Streaming and provider handling: [SSE relay](../leaderboard-analytics-service/backend/LanguageWise.LeaderboardAnalyticsService.Api/Services/AssistantSseResult.cs), [OpenRouter client](../mini-games-service/backend/LanguageWise.MiniGamesService.Api/Clients/OpenRouterAssistantClient.cs)
- Existing Ollama deployment and Garry configuration: [Docker Compose](../docker-compose.yml), [README configuration](../README.md)
