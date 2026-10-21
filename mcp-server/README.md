# LanguageWise MCP Server

The shared [Model Context Protocol](https://modelcontextprotocol.io) server for LanguageWise. It exposes scoped **tools** that let Garry (and feature backends) fetch live LanguageWise data and manage notification preferences on behalf of the signed-in user.

- .NET 10 + the official C# SDK (`ModelContextProtocol.AspNetCore` 2.2.0)
- Streamable HTTP, stateless, at `http://localhost:8200/mcp`
- Runs in Compose; containers reach it at `http://mcp-server:8080/mcp`. Host access is loopback-only on port 8200.
- Isolated tests use fakes; the separate `ai-e2e` GitHub Actions job exercises the real containers.

## What MCP is

MCP is a standard JSON-RPC protocol between an AI application (the **host/client**, here Garry) and a **server** that offers tools. The client asks `tools/list` to discover tools (name, description, JSON input schema) and `tools/call` to run one. The LLM never talks to the server directly: Garry passes the tool list to the model, the model asks for a tool, Garry calls it over MCP, and feeds the result back to the model (the "tool loop", max 3 rounds / 8 calls).

```
Browser → feature backend → Garry ──MCP──► mcp-server:8080 ──HTTP + user JWT──► feature API
```

## Running it

```powershell
# From the repository root; do not regenerate an existing signing key pair.
pwsh mcp-server\scripts\New-McpApiKey.ps1
docker compose up -d --build
```

Compose mounts the existing signing public key and MCP key as secrets. Generate
the signing pair with `tools\gen-signing-key\gen-signing-key.ps1` only if it does
not already exist. RAG indexes its corpus before MCP becomes healthy.

For host-only development instead (stop the Compose MCP service first):

```powershell
cd mcp-server\src\LanguageWise.McpServer
dotnet run
```

The following are host-development defaults; Compose overrides listen/key paths
and every downstream URL with internal DNS names.

| Setting | Default | Purpose |
|---|---|---|
| `Urls` | `http://0.0.0.0:8200` | Listen address |
| `Mcp:ApiKeyPath` / `Mcp:ApiKey` | `../../.mcp-api-key` | Shared API key (server refuses to start without one) |
| `Auth:VerificationKeyPath` | `../../../signing_public_key.pem` | Validates user JWTs |
| `Mcp:MaxResultBytes` | `32768` | Downstream response size cap |
| `Downstream:TimeoutSeconds` | `10` | Downstream API timeout |
| `Services:QuizzesCourses` | `http://localhost:5003` | quizzes-courses-service API |
| `Services:MiniGames` | `http://localhost:5001` | mini-games-service API |
| `Services:ChatDiscussion` | `http://localhost:5002` | chat-discussion-service API |
| `Services:QuestsAchievements` | `http://localhost:5004` | quests-achievements-notifications-service API |
| `Services:LeaderboardAnalytics` | `http://localhost:5005` | leaderboard-analytics-service API |
| `Services:Rag` | `http://localhost:8100` | RAG server; `docs_search` (offered in every scope) uses its general `POST /query` |
| `Services:Database` | `http://localhost:6000` | shared-db-api; the admin **MCP** switch (off → `/mcp` returns 503 `mcp_disabled`) and **RAG** switch (off → `docs_*` tools hidden) are read from `GET /api/admin-settings` (cached 5 s, treated as off if unreadable) |

## Security model

Every request to `/mcp` must send these headers:

| Header | Purpose |
|---|---|
| `X-LanguageWise-Mcp-Key` | Shared API key. Missing/wrong → `401` |
| `X-LanguageWise-Tool-Scope` | Caller's scope, e.g. `courses`. Only `<scope>_*` tools and shared `docs_search` are listed or callable |
| `X-LanguageWise-User-Token` | The user's JWT. Validated (RS256) and forwarded as `Bearer` to the feature API, so a tool can never do more than the user can |

Domain calls validate their inputs, time out after 10 s, cap downstream results at
32 KB and return safe failures. Most tools are read-only; the two quests preference
write tools require an explicit user request and retain normal backend validation.

## Tools

| Tool | Inputs | Calls |
|---|---|---|
| `courses_list_courses` | – | `GET /api/courses` |
| `courses_get_lesson_vocabulary` | `courseCode`, `lessonSlug` | `GET /api/courses/{code}/lessons/{slug}` |
| `courses_get_my_progress` | `courseCode` | `GET /api/courses/{code}/progress` |
| `courses_list_lessons` | `courseCode` | `GET /api/courses/{code}/lessons` |
| `courses_list_quizzes` | `courseCode` | `GET /api/courses/{code}/quizzes` (no questions or answers) |
| `courses_get_flashcards` | `courseCode`, `lessonSlug` | `GET /api/courses/{code}/flashcard-decks/{slug}` |
| `courses_get_my_vocabulary` | `courseCode` (optional) | `GET /api/me/vocabulary` |
| `courses_get_my_milestones` | `limit` (1-50, default 10) | `GET /api/me/milestones`, plus `/api/courses`, `/lessons` and `/quizzes` to add names |
| `chat_list_forums` | – | `GET /api/forums` |
| `chat_search_posts` | `query`, `forumCode` (optional), `limit` (1-10) | `GET /api/posts` |
| `chat_get_post` | `postId` | `GET /api/posts/{id}` including its comment preview |
| `games_get_completion_stats` | `courseCode` (optional) | `GET /api/stats/completions` |
| `games_list_game_languages` | - | `GET /api/game-languages` |
| `quests_get_my_preferences` | - | `GET /api/preferences` |
| `quests_set_notifications_enabled` | `enabled` | Read/update `/api/preferences` |
| `quests_update_my_preferences` | Optional category booleans | Read/update `/api/preferences` |
| `quests_get_my_achievements` | - | `GET /api/achievements` |
| `leaderboard_get_my_language_rankings` | - | `GET /api/my-language-rankings` |
| `leaderboard_get_my_lessons_completed_over_time` | - | `GET /api/lessons-completed-over-time` |
| `docs_search` | `query`, `maxResults` (1-5) | RAG `POST /query`, available in all five scopes |

Quiz details (`GET /api/quizzes/{id}`) are deliberately not exposed because they
can include answers. The only exposed writes are the explicit quests preference tools.

Course codes are the seeded two-letter codes (`de`, `fr`, `it`, `nl`, `es`, `pl`); lesson slugs are kebab-case, e.g. `greetings` or `time-calendar`.

## Validating from the terminal

```powershell
pwsh mcp-server/scripts/Invoke-McpSmokeTest.ps1 -UserToken "<jwt copied from the browser>"
```

Runs `initialize`, `tools/list`, one `tools/call` per tool, and shows that a missing key is rejected (401) and that another scope can neither see nor call `courses_*` tools. Without `-UserToken` the tools return a "signed in" error.

Or use the MCP Inspector: `npx @modelcontextprotocol/inspector`, choose **Streamable HTTP**, URL `http://localhost:8200/mcp`, and add the three headers above.

## Tests

```powershell
dotnet test mcp-server/LanguageWise.McpServer.slnx -c Release
```

For real cross-service tests without paid APIs, see `tests\e2e\README.md`.

## Adding tools for your service

Using quizzes-courses as the example:

1. **Scope** – add your scope to `Tools/ToolScopes.cs` (`games`, `chat`, `quests`, `leaderboard` already exist).
2. **Downstream URL** – add `Services:<YourService>` to `appsettings.json` and register a named `HttpClient` in `Program.cs` (copy the `QuizzesCourses` one).
3. **Tools** – create `Tools/<YourService>/<YourService>Tools.cs`:

   ```csharp
   [McpServerToolType]
   public sealed class GamesTools(DownstreamClient downstream)
   {
   	[McpServerTool(Name = "games_list_games", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
   	[Description("Lists the mini games the user can play.")]
   	public async Task<GameListResult> ListGamesAsync(CancellationToken cancellationToken) =>
   		new(await downstream.GetAsync<List<GameItem>>("MiniGames", "api/games", cancellationToken));
   }
   ```

   - Name every tool `<scope>_<verb>_<noun>`; the scope filter relies on the prefix.
   - Prefer read-only tools and call your service's **public API** via `DownstreamClient` (it forwards the user JWT and maps errors). Writes need accurate annotations and explicit user intent.
   - Validate inputs and throw `McpException("safe message")` for bad input.
   - Return records; they become the tool's structured output.
4. **Tests** – add cases to `tests/` using `McpServerFactory` and its stub downstream handler.
5. **Use it from Garry** – send `"toolScope": "<scope>"` in your backend's `POST /api/completions` body. Garry will then run the tool loop and stream `event: tool` (`{name, arguments, isError, result}`) before the `delta` events. Only send `toolScope` once your SSE parser handles the `tool` event.
6. **Wire your backend and frontend** – `quizzes-courses-service` is the reference implementation:
   - Backend: `Clients/McpToolClient.cs` (MCP client with your scope), `GET /api/assistant/tools` and `POST /api/assistant/tools/{name}` in `Program.cs` (503 `mcp_disabled` when `Mcp:Enabled` is false), `toolScope` in `GarryCompletionClient`, and the `tool` event relay in `AssistantSseResult`.
   - Frontend: export an `assistant.tools` config (chips + result `view`) from your feature module – see `quizzes-courses-service/frontend/src/federation/assistant.ts`. The shared host's `GarryAssistant.vue` renders the Tools toggle, chips, and result cards, and handles the `tool` SSE event.
   - Compose: add `Mcp__Enabled`, `Mcp__Endpoint: http://mcp-server:8080/mcp`, `Mcp__ApiKeyPath` and the `mcp_api_key` secret to your backend. Isolated CI tests keep `Mcp__Enabled: false`; integrated E2E uses the real server.
