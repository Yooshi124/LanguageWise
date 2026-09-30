using System.Net;
using System.Text;
using System.Text.Json;
using LanguageWise.McpServer.Security;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace LanguageWise.McpServer.Tests;

public sealed class McpEndpointTests
{
	private McpServerFactory factory = null!;

	[OneTimeSetUp]
	public void StartServer() => factory = new McpServerFactory();

	[OneTimeTearDown]
	public void StopServer() => factory.Dispose();

	[SetUp]
	public void ResetDownstream() => factory.Downstream.Reset();

	[Test]
	public async Task Health_DoesNotRequireApiKey()
	{
		using var client = factory.CreateClient();
		var response = await client.GetAsync("/health");
		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
	}

	[TestCase(null)]
	[TestCase("wrong-key")]
	public async Task McpEndpoint_WithoutValidApiKey_Returns401(string? apiKey)
	{
		using var client = factory.CreateClient();
		using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
		{
			Content = new StringContent("""{"jsonrpc":"2.0","id":1,"method":"tools/list"}""", Encoding.UTF8, "application/json")
		};
		if (apiKey is not null)
		{
			request.Headers.Add(McpHeaders.ApiKey, apiKey);
		}
		var response = await client.SendAsync(request);
		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
	}

	[Test]
	public async Task ListTools_WithCoursesScope_ReturnsOnlyCoursesTools()
	{
		await using var client = await factory.CreateMcpClientAsync("courses");
		var tools = await client.ListToolsAsync();
		Assert.That(tools.Select(t => t.Name), Is.EquivalentTo(new[]
		{
			"courses_list_courses",
			"courses_get_lesson_vocabulary",
			"courses_get_my_progress",
			"courses_list_lessons",
			"courses_list_quizzes",
			"courses_get_flashcards",
			"courses_get_my_vocabulary",
			"courses_get_my_milestones",
			"docs_search"
		}));
		Assert.That(tools.All(t => t.ProtocolTool.Annotations?.ReadOnlyHint == true), Is.True);
	}

	[TestCase("unknown")]
	[TestCase(null)]
	public async Task ListTools_WithOtherOrMissingScope_HidesCoursesTools(string? scope)
	{
		await using var client = await factory.CreateMcpClientAsync(scope);
		var tools = await client.ListToolsAsync();
		Assert.That(tools, Is.Empty);
	}

	[Test]
	public async Task ListTools_WithGamesScope_ReturnsOnlyGamesTools()
	{
		await using var client = await factory.CreateMcpClientAsync("games");
		var tools = await client.ListToolsAsync();
		Assert.That(tools.Select(t => t.Name), Is.EquivalentTo(new[]
		{
			"games_get_completion_stats",
			"games_list_game_languages",
			"docs_search"
		}));
		Assert.That(tools.All(t => t.ProtocolTool.Annotations?.ReadOnlyHint == true), Is.True);
	}

	[Test]
	public async Task ListTools_WithQuestsScope_ReturnsOnlyQuestsToolsWithAccurateHints()
	{
		await using var client = await factory.CreateMcpClientAsync("quests");
		var tools = await client.ListToolsAsync();
		Assert.That(tools.Select(t => t.Name), Is.EquivalentTo(new[]
		{
			"quests_get_my_preferences",
			"quests_set_notifications_enabled",
			"quests_update_my_preferences",
			"quests_get_my_achievements",
			"docs_search"
		}));
		Assert.That(
			tools.Where(t => t.ProtocolTool.Annotations?.ReadOnlyHint == true).Select(t => t.Name),
			Is.EquivalentTo(new[] { "quests_get_my_preferences", "quests_get_my_achievements", "docs_search" }));
	}

	[TestCase("chat")]
	[TestCase("leaderboard")]
	public async Task ListTools_ForScopesWithoutOwnTools_OffersOnlyDocsSearch(string scope)
	{
		await using var client = await factory.CreateMcpClientAsync(scope);
		var tools = await client.ListToolsAsync();
		Assert.That(tools.Select(t => t.Name), Is.EqualTo(new[] { "docs_search" }));
	}

	[Test]
	public async Task DocsSearch_QueriesGeneralRagEndpointAndReturnsPassages()
	{
		factory.Downstream.Body = """
			{"results":[{"source":"quests-achievements-notifications-service","heading":"Notification preferences","relevance":0.7,"text":"A master switch turns all emails on or off."}],"resultCount":1}
			""";
		await using var client = await factory.CreateMcpClientAsync("chat", factory.CreateUserToken());

		var result = await client.CallToolAsync("docs_search", new Dictionary<string, object?> { ["query"] = "notifications page", ["maxResults"] = 9 });

		Assert.That(result.IsError, Is.Not.True);
		var request = factory.Downstream.Requests.Single();
		using var body = JsonDocument.Parse(factory.Downstream.RequestBodies.Single()!);
		var passage = result.StructuredContent!.Value.GetProperty("passages")[0];
		Assert.Multiple(() =>
		{
			Assert.That(request.RequestUri!.AbsolutePath, Is.EqualTo("/query"));
			Assert.That(body.RootElement.GetProperty("query").GetString(), Is.EqualTo("notifications page"));
			Assert.That(body.RootElement.GetProperty("n_results").GetInt32(), Is.EqualTo(5));
			Assert.That(passage.GetProperty("heading").GetString(), Is.EqualTo("Notification preferences"));
			Assert.That(passage.GetProperty("text").GetString(), Does.Contain("master switch"));
		});
	}

	[Test]
	public async Task SetNotificationsEnabled_OnlyFlipsMasterSwitch()
	{
		var token = factory.CreateUserToken();
		factory.Downstream.Body = QuestsPreferencesJson;
		await using var client = await factory.CreateMcpClientAsync("quests", token);

		var result = await client.CallToolAsync("quests_set_notifications_enabled", new Dictionary<string, object?> { ["enabled"] = false });

		Assert.That(result.IsError, Is.Not.True);
		var put = factory.Downstream.Requests.Single(r => r.Method == HttpMethod.Put);
		using var body = JsonDocument.Parse(factory.Downstream.RequestBodies[factory.Downstream.Requests.IndexOf(put)]!);
		Assert.Multiple(() =>
		{
			Assert.That(put.RequestUri!.AbsolutePath, Is.EqualTo("/api/preferences"));
			Assert.That(put.Headers.Authorization?.Parameter, Is.EqualTo(token));
			Assert.That(body.RootElement.GetProperty("notifyAll").GetBoolean(), Is.False);
			Assert.That(body.RootElement.GetProperty("email").GetString(), Is.EqualTo("learner@example.com"));
			Assert.That(body.RootElement.GetProperty("notifyQuizResult").GetBoolean(), Is.False);
			Assert.That(body.RootElement.GetProperty("notifyLoginStreak").GetBoolean(), Is.True);
			Assert.That(result.StructuredContent!.Value.GetProperty("notificationsEnabled").GetBoolean(), Is.False);
		});
	}

	[Test]
	public async Task UpdateMyPreferences_ChangesOnlyRequestedCategories()
	{
		factory.Downstream.Body = QuestsPreferencesJson;
		await using var client = await factory.CreateMcpClientAsync("quests", factory.CreateUserToken());

		var result = await client.CallToolAsync("quests_update_my_preferences", new Dictionary<string, object?>
		{
			["quizResult"] = true,
			["loginStreak"] = false
		});

		Assert.That(result.IsError, Is.Not.True);
		var put = factory.Downstream.Requests.Single(r => r.Method == HttpMethod.Put);
		using var body = JsonDocument.Parse(factory.Downstream.RequestBodies[factory.Downstream.Requests.IndexOf(put)]!);
		Assert.Multiple(() =>
		{
			Assert.That(body.RootElement.GetProperty("notifyQuizResult").GetBoolean(), Is.True);
			Assert.That(body.RootElement.GetProperty("notifyLoginStreak").GetBoolean(), Is.False);
			Assert.That(body.RootElement.GetProperty("notifyAll").GetBoolean(), Is.True);
			Assert.That(body.RootElement.GetProperty("notifyMinigameWin").GetBoolean(), Is.True);
		});
	}

	[Test]
	public async Task UpdateMyPreferences_WithNoCategories_ReturnsErrorWithoutCallingService()
	{
		await using var client = await factory.CreateMcpClientAsync("quests", factory.CreateUserToken());

		var result = await client.CallToolAsync("quests_update_my_preferences");

		Assert.That(result.IsError, Is.True);
		Assert.That(factory.Downstream.Requests, Is.Empty);
	}

	[Test]
	public async Task SetNotificationsEnabled_WithoutEmail_ReturnsErrorWithoutSaving()
	{
		factory.Downstream.Body = QuestsPreferencesJson.Replace("\"learner@example.com\"", "null");
		await using var client = await factory.CreateMcpClientAsync("quests", factory.CreateUserToken());

		var result = await client.CallToolAsync("quests_set_notifications_enabled", new Dictionary<string, object?> { ["enabled"] = true });

		Assert.That(result.IsError, Is.True);
		Assert.That(factory.Downstream.Requests.Any(r => r.Method == HttpMethod.Put), Is.False);
	}

	[Test]
	public async Task GetMyAchievements_MarksEarnedAndRecordAchievements()
	{
		factory.Downstream.Body = """
			[
				{"achievementId":1,"name":"First Lesson","description":"Complete your first lesson","progress":1,"progressNeeded":1},
				{"achievementId":2,"name":"Committed Learner","description":"Complete five lessons","progress":2,"progressNeeded":5},
				{"achievementId":11,"name":"Longest Login Streak","description":"Your longest run","progress":4,"progressNeeded":-1}
			]
			""";
		await using var client = await factory.CreateMcpClientAsync("quests", factory.CreateUserToken());

		var result = await client.CallToolAsync("quests_get_my_achievements");

		Assert.That(result.IsError, Is.Not.True);
		Assert.That(factory.Downstream.Requests.Single().RequestUri!.AbsolutePath, Is.EqualTo("/api/achievements"));
		var achievements = result.StructuredContent!.Value.GetProperty("achievements");
		Assert.Multiple(() =>
		{
			Assert.That(achievements[0].GetProperty("earned").GetBoolean(), Is.True);
			Assert.That(achievements[1].GetProperty("earned").GetBoolean(), Is.False);
			Assert.That(
				achievements[2].TryGetProperty("progressNeeded", out var needed) && needed.ValueKind != JsonValueKind.Null,
				Is.False);
			Assert.That(achievements[2].GetProperty("earned").GetBoolean(), Is.False);
		});
	}

	private const string QuestsPreferencesJson = """
		{"email":"learner@example.com","notifyAll":true,"notifyCommunityContribution":true,"notifyPostEngagement":true,"notifyLessonCompletion":true,"notifyCourseCompletion":true,"notifyQuizResult":false,"notifyMinigameWin":true,"notifyLoginStreak":true,"notifyAchievements":true}
		""";

	[Test]
	public async Task CallTool_OutOfScope_IsRejected()
	{
		await using var client = await factory.CreateMcpClientAsync("games", factory.CreateUserToken());
		Assert.ThrowsAsync<McpProtocolException>(async () => await client.CallToolAsync("courses_list_courses"));
		Assert.That(factory.Downstream.Requests, Is.Empty);
	}

	[Test]
	public async Task GetCompletionStats_ForwardsUserTokenAndReturnsStructuredContent()
	{
		var token = factory.CreateUserToken();
		factory.Downstream.Body = """{"courseCode":"it","guessTheWord":3,"wordSearch":1,"associations":0,"bestGuessTheWordSeconds":42,"bestWordSearchSeconds":null,"bestAssociationsSeconds":null,"currentStreak":2}""";
		await using var client = await factory.CreateMcpClientAsync("games", token);

		var result = await client.CallToolAsync("games_get_completion_stats", new Dictionary<string, object?> { ["courseCode"] = "it" });

		Assert.That(result.IsError, Is.Not.True);
		var request = factory.Downstream.Requests.Single();
		Assert.Multiple(() =>
		{
			Assert.That(request.RequestUri!.AbsolutePath, Is.EqualTo("/api/stats/completions"));
			Assert.That(request.RequestUri!.Query, Is.EqualTo("?courseCode=it"));
			Assert.That(request.Headers.Authorization?.Scheme, Is.EqualTo("Bearer"));
			Assert.That(request.Headers.Authorization?.Parameter, Is.EqualTo(token));
			var content = result.StructuredContent!.Value;
			Assert.That(content.GetProperty("guessTheWordCompletions").GetInt32(), Is.EqualTo(3));
			Assert.That(content.GetProperty("currentStreak").GetInt32(), Is.EqualTo(2));
		});
	}

	[Test]
	public async Task ListGameLanguages_ForwardsUserTokenAndReturnsStructuredContent()
	{
		var token = factory.CreateUserToken();
		factory.Downstream.Body = """[{"code":"it","title":"Italian"}]""";
		await using var client = await factory.CreateMcpClientAsync("games", token);

		var result = await client.CallToolAsync("games_list_game_languages");

		Assert.That(result.IsError, Is.Not.True);
		Assert.That(factory.Downstream.Requests.Single().RequestUri!.AbsolutePath, Is.EqualTo("/api/game-languages"));
		var language = result.StructuredContent!.Value.GetProperty("languages")[0];
		Assert.That(language.GetProperty("code").GetString(), Is.EqualTo("it"));
		Assert.That(language.GetProperty("title").GetString(), Is.EqualTo("Italian"));
	}

	[Test]
	public async Task ListCourses_ForwardsUserTokenAndReturnsStructuredContent()
	{
		var token = factory.CreateUserToken();
		factory.Downstream.Body = """[{"id":1,"code":"it","title":"Italian","description":"Basics"}]""";
		await using var client = await factory.CreateMcpClientAsync("courses", token);

		var result = await client.CallToolAsync("courses_list_courses");

		Assert.That(result.IsError, Is.Not.True);
		var request = factory.Downstream.Requests.Single();
		Assert.Multiple(() =>
		{
			Assert.That(request.RequestUri!.AbsolutePath, Is.EqualTo("/api/courses"));
			Assert.That(request.Headers.Authorization?.Scheme, Is.EqualTo("Bearer"));
			Assert.That(request.Headers.Authorization?.Parameter, Is.EqualTo(token));
			var course = result.StructuredContent!.Value.GetProperty("courses")[0];
			Assert.That(course.GetProperty("code").GetString(), Is.EqualTo("it"));
			Assert.That(course.GetProperty("title").GetString(), Is.EqualTo("Italian"));
		});
	}

	[Test]
	public async Task GetLessonVocabulary_MapsVocabulary()
	{
		factory.Downstream.Body = """
			{"id":3,"course":{"id":1,"code":"it","title":"Italian","description":"d"},"slug":"greetings","title":"Greetings",
			 "sortOrder":1,"contentMarkdown":"# hi","vocabulary":[{"word":"ciao","meaning":"hello"}]}
			""";
		await using var client = await factory.CreateMcpClientAsync("courses", factory.CreateUserToken());

		var result = await client.CallToolAsync("courses_get_lesson_vocabulary", new Dictionary<string, object?>
		{
			["courseCode"] = "it",
			["lessonSlug"] = "greetings"
		});

		Assert.That(result.IsError, Is.Not.True);
		Assert.That(factory.Downstream.Requests.Single().RequestUri!.AbsolutePath, Is.EqualTo("/api/courses/it/lessons/greetings"));
		var content = result.StructuredContent!.Value;
		Assert.That(content.GetProperty("lessonTitle").GetString(), Is.EqualTo("Greetings"));
		Assert.That(content.GetProperty("vocabulary")[0].GetProperty("word").GetString(), Is.EqualTo("ciao"));
	}

	[Test]
	public async Task GetMyProgress_SummarisesLessonsAndQuizzes()
	{
		factory.Downstream.Body = """
			{"courseCompleted":false,"courseEligible":false,
			 "lessons":[{"lessonId":1,"completed":true},{"lessonId":2,"completed":false}],
			 "quizzes":[{"quizId":7,"lessonId":1,"completed":true,"bestScore":8,"totalQuestions":10}]}
			""";
		await using var client = await factory.CreateMcpClientAsync("courses", factory.CreateUserToken());

		var result = await client.CallToolAsync("courses_get_my_progress", new Dictionary<string, object?> { ["courseCode"] = "it" });

		Assert.That(result.IsError, Is.Not.True);
		Assert.That(factory.Downstream.Requests.Single().RequestUri!.AbsolutePath, Is.EqualTo("/api/courses/it/progress"));
		var content = result.StructuredContent!.Value;
		Assert.Multiple(() =>
		{
			Assert.That(content.GetProperty("lessonsCompleted").GetInt32(), Is.EqualTo(1));
			Assert.That(content.GetProperty("lessonsTotal").GetInt32(), Is.EqualTo(2));
			Assert.That(content.GetProperty("quizzes")[0].GetProperty("bestScore").GetInt32(), Is.EqualTo(8));
		});
	}

	[Test]
	public async Task ListLessons_ReturnsLessonsInOrder()
	{
		factory.Downstream.Body = """
			[{"id":2,"slug":"introductions","title":"Introductions","sortOrder":2},
			 {"id":1,"slug":"greetings","title":"Greetings","sortOrder":1}]
			""";
		await using var client = await factory.CreateMcpClientAsync("courses", factory.CreateUserToken());

		var result = await client.CallToolAsync("courses_list_lessons", new Dictionary<string, object?> { ["courseCode"] = "it" });

		Assert.That(result.IsError, Is.Not.True);
		Assert.That(factory.Downstream.Requests.Single().RequestUri!.AbsolutePath, Is.EqualTo("/api/courses/it/lessons"));
		var lessons = result.StructuredContent!.Value.GetProperty("lessons");
		Assert.That(lessons[0].GetProperty("slug").GetString(), Is.EqualTo("greetings"));
		Assert.That(lessons[1].GetProperty("slug").GetString(), Is.EqualTo("introductions"));
	}

	[Test]
	public async Task ListQuizzes_ReturnsQuizzesWithoutQuestions()
	{
		factory.Downstream.Body = """
			[{"id":7,"title":"Greetings quiz","lessonId":1,"lessonSlug":"greetings","lessonTitle":"Greetings","lessonSortOrder":1}]
			""";
		await using var client = await factory.CreateMcpClientAsync("courses", factory.CreateUserToken());

		var result = await client.CallToolAsync("courses_list_quizzes", new Dictionary<string, object?> { ["courseCode"] = "it" });

		Assert.That(result.IsError, Is.Not.True);
		Assert.That(factory.Downstream.Requests.Single().RequestUri!.AbsolutePath, Is.EqualTo("/api/courses/it/quizzes"));
		var quiz = result.StructuredContent!.Value.GetProperty("quizzes")[0];
		Assert.That(quiz.GetProperty("quizId").GetInt32(), Is.EqualTo(7));
		Assert.That(quiz.GetProperty("lessonSlug").GetString(), Is.EqualTo("greetings"));
	}

	[Test]
	public async Task GetFlashcards_MapsCards()
	{
		factory.Downstream.Body = """
			{"lessonId":1,"lessonSlug":"greetings","lessonTitle":"Greetings","lessonSortOrder":1,
			 "cards":[{"id":1,"frontText":"ciao","backText":"hello"}]}
			""";
		await using var client = await factory.CreateMcpClientAsync("courses", factory.CreateUserToken());

		var result = await client.CallToolAsync("courses_get_flashcards", new Dictionary<string, object?>
		{
			["courseCode"] = "it",
			["lessonSlug"] = "greetings"
		});

		Assert.That(result.IsError, Is.Not.True);
		Assert.That(factory.Downstream.Requests.Single().RequestUri!.AbsolutePath, Is.EqualTo("/api/courses/it/flashcard-decks/greetings"));
		var card = result.StructuredContent!.Value.GetProperty("cards")[0];
		Assert.That(card.GetProperty("front").GetString(), Is.EqualTo("ciao"));
		Assert.That(card.GetProperty("back").GetString(), Is.EqualTo("hello"));
	}

	private const string VocabularyBody = """
		{"courses":[
		 {"code":"it","title":"Italian","lessons":[{"lessonId":1,"slug":"greetings","title":"Greetings","vocabulary":[{"word":"ciao","meaning":"hello"},{"word":"grazie","meaning":"thank you"}]}]},
		 {"code":"fr","title":"French","lessons":[{"lessonId":21,"slug":"greetings","title":"Greetings","vocabulary":[{"word":"bonjour","meaning":"hello"}]}]}]}
		""";

	[Test]
	public async Task GetMyVocabulary_ReturnsAllCoursesWithTotal()
	{
		factory.Downstream.Body = VocabularyBody;
		await using var client = await factory.CreateMcpClientAsync("courses", factory.CreateUserToken());

		var result = await client.CallToolAsync("courses_get_my_vocabulary");

		Assert.That(result.IsError, Is.Not.True);
		Assert.That(factory.Downstream.Requests.Single().RequestUri!.AbsolutePath, Is.EqualTo("/api/me/vocabulary"));
		var content = result.StructuredContent!.Value;
		Assert.That(content.GetProperty("courses").GetArrayLength(), Is.EqualTo(2));
		Assert.That(content.GetProperty("totalWords").GetInt32(), Is.EqualTo(3));
	}

	[Test]
	public async Task GetMyVocabulary_FiltersByCourse()
	{
		factory.Downstream.Body = VocabularyBody;
		await using var client = await factory.CreateMcpClientAsync("courses", factory.CreateUserToken());

		var result = await client.CallToolAsync("courses_get_my_vocabulary", new Dictionary<string, object?> { ["courseCode"] = "fr" });

		var content = result.StructuredContent!.Value;
		Assert.That(content.GetProperty("courses").GetArrayLength(), Is.EqualTo(1));
		Assert.That(content.GetProperty("courses")[0].GetProperty("courseCode").GetString(), Is.EqualTo("fr"));
		Assert.That(content.GetProperty("totalWords").GetInt32(), Is.EqualTo(1));
	}

	[Test]
	public async Task GetMyMilestones_ReturnsMostRecentFirstWithNames()
	{
		factory.Downstream.BodiesByPath["/api/me/milestones?limit=200"] = """
			{"items":[
			 {"id":1,"userId":42,"courseId":null,"lessonId":11,"quizId":null,"completedAt":"2026-09-01T10:00:00+00:00"},
			 {"id":2,"userId":42,"courseId":null,"lessonId":null,"quizId":7,"completedAt":"2026-09-02T10:00:00+00:00"},
			 {"id":3,"userId":42,"courseId":3,"lessonId":null,"quizId":null,"completedAt":"2026-09-03T10:00:00+00:00"}],
			 "nextCursor":null}
			""";
		factory.Downstream.BodiesByPath["/api/courses"] = """
			[{"id":3,"code":"it","title":"Italian","description":"Start speaking Italian."}]
			""";
		factory.Downstream.BodiesByPath["/api/courses/it/lessons"] = """
			[{"id":11,"slug":"greetings","title":"Greetings","sortOrder":1}]
			""";
		factory.Downstream.BodiesByPath["/api/courses/it/quizzes"] = """
			[{"id":7,"title":"Greetings quiz","lessonId":11,"lessonSlug":"greetings","lessonTitle":"Greetings","lessonSortOrder":1}]
			""";
		await using var client = await factory.CreateMcpClientAsync("courses", factory.CreateUserToken());

		var result = await client.CallToolAsync("courses_get_my_milestones", new Dictionary<string, object?> { ["limit"] = 3 });

		Assert.That(result.IsError, Is.Not.True);
		Assert.That(factory.Downstream.Requests, Has.Count.EqualTo(4));
		var milestones = result.StructuredContent!.Value.GetProperty("milestones");
		Assert.That(milestones.GetArrayLength(), Is.EqualTo(3));
		Assert.That(milestones[0].GetProperty("kind").GetString(), Is.EqualTo("course"));
		Assert.That(milestones[0].GetProperty("courseTitle").GetString(), Is.EqualTo("Italian"));
		Assert.That(milestones[1].GetProperty("kind").GetString(), Is.EqualTo("quiz"));
		Assert.That(milestones[1].GetProperty("quizTitle").GetString(), Is.EqualTo("Greetings quiz"));
		Assert.That(milestones[1].GetProperty("courseCode").GetString(), Is.EqualTo("it"));
		Assert.That(milestones[2].GetProperty("kind").GetString(), Is.EqualTo("lesson"));
		Assert.That(milestones[2].GetProperty("lessonSlug").GetString(), Is.EqualTo("greetings"));
		Assert.That(result.StructuredContent!.Value.GetRawText(), Does.Not.Contain("userId"));
	}

	[Test]
	public async Task GetMyMilestones_NoMilestones_SkipsNameLookups()
	{
		factory.Downstream.Body = """{"items":[],"nextCursor":null}""";
		await using var client = await factory.CreateMcpClientAsync("courses", factory.CreateUserToken());

		var result = await client.CallToolAsync("courses_get_my_milestones", new Dictionary<string, object?>());

		Assert.That(result.IsError, Is.Not.True);
		Assert.That(factory.Downstream.Requests.Single().RequestUri!.PathAndQuery, Is.EqualTo("/api/me/milestones?limit=200"));
		Assert.That(result.StructuredContent!.Value.GetProperty("milestones").GetArrayLength(), Is.EqualTo(0));
	}

	[TestCase(0)]
	[TestCase(51)]
	public async Task GetMyMilestones_InvalidLimit_ReturnsToolError(int limit)
	{
		await using var client = await factory.CreateMcpClientAsync("courses", factory.CreateUserToken());

		var result = await client.CallToolAsync("courses_get_my_milestones", new Dictionary<string, object?> { ["limit"] = limit });

		Assert.That(result.IsError, Is.True);
		Assert.That(factory.Downstream.Requests, Is.Empty);
	}

	[TestCase("courses_list_lessons")]
	[TestCase("courses_list_quizzes")]
	public async Task CourseTools_InvalidCourseCode_ReturnsToolError(string tool)
	{
		await using var client = await factory.CreateMcpClientAsync("courses", factory.CreateUserToken());

		var result = await client.CallToolAsync(tool, new Dictionary<string, object?> { ["courseCode"] = "../x" });

		Assert.That(result.IsError, Is.True);
		Assert.That(factory.Downstream.Requests, Is.Empty);
	}

	[TestCase("IT", "greetings")]
	[TestCase("ita", "greetings")]
	[TestCase("../admin", "greetings")]
	[TestCase("it", "../../secret")]
	[TestCase("it", "Greetings!")]
	public async Task GetLessonVocabulary_InvalidInput_ReturnsToolErrorWithoutCallingDownstream(string courseCode, string lessonSlug)
	{
		await using var client = await factory.CreateMcpClientAsync("courses", factory.CreateUserToken());

		var result = await client.CallToolAsync("courses_get_lesson_vocabulary", new Dictionary<string, object?>
		{
			["courseCode"] = courseCode,
			["lessonSlug"] = lessonSlug
		});

		Assert.That(result.IsError, Is.True);
		Assert.That(factory.Downstream.Requests, Is.Empty);
	}

	[Test]
	public async Task CallTool_WithoutUserToken_ReturnsSignInError()
	{
		await using var client = await factory.CreateMcpClientAsync("courses");
		var result = await client.CallToolAsync("courses_list_courses");
		Assert.That(result.IsError, Is.True);
		Assert.That(TextOf(result), Does.Contain("signed in"));
		Assert.That(factory.Downstream.Requests, Is.Empty);
	}

	[Test]
	public async Task CallTool_WithExpiredToken_ReturnsSignInError()
	{
		await using var client = await factory.CreateMcpClientAsync("courses", factory.CreateUserToken(TimeSpan.FromMinutes(-1)));
		var result = await client.CallToolAsync("courses_list_courses");
		Assert.That(result.IsError, Is.True);
		Assert.That(factory.Downstream.Requests, Is.Empty);
	}

	[Test]
	public async Task CallTool_WithTokenSignedByOtherKey_ReturnsSignInError()
	{
		using var otherKey = System.Security.Cryptography.RSA.Create(2048);
		await using var client = await factory.CreateMcpClientAsync("courses", factory.CreateUserToken(key: otherKey));
		var result = await client.CallToolAsync("courses_list_courses");
		Assert.That(result.IsError, Is.True);
		Assert.That(factory.Downstream.Requests, Is.Empty);
	}

	[TestCase(HttpStatusCode.NotFound, "not found")]
	[TestCase(HttpStatusCode.Unauthorized, "not allowed")]
	[TestCase(HttpStatusCode.InternalServerError, "unavailable")]
	public async Task CallTool_DownstreamFailure_ReturnsSafeToolError(HttpStatusCode status, string expected)
	{
		factory.Downstream.StatusCode = status;
		factory.Downstream.Body = """{"detail":"stack trace secret"}""";
		await using var client = await factory.CreateMcpClientAsync("courses", factory.CreateUserToken());

		var result = await client.CallToolAsync("courses_get_my_progress", new Dictionary<string, object?> { ["courseCode"] = "it" });

		Assert.That(result.IsError, Is.True);
		Assert.That(TextOf(result), Does.Contain(expected));
		Assert.That(TextOf(result), Does.Not.Contain("secret"));
	}

	[Test]
	public async Task CallTool_DownstreamUnreachable_ReturnsUnavailable()
	{
		factory.Downstream.Throw = true;
		await using var client = await factory.CreateMcpClientAsync("courses", factory.CreateUserToken());
		var result = await client.CallToolAsync("courses_list_courses");
		Assert.That(result.IsError, Is.True);
		Assert.That(TextOf(result), Does.Contain("unavailable"));
	}

	[Test]
	public async Task CallTool_OversizedDownstreamResponse_IsRejected()
	{
		var courses = Enumerable.Range(0, 2000).Select(i => new { code = $"C{i}", title = new string('x', 20), description = new string('y', 20) });
		factory.Downstream.Body = JsonSerializer.Serialize(courses);
		await using var client = await factory.CreateMcpClientAsync("courses", factory.CreateUserToken());

		var result = await client.CallToolAsync("courses_list_courses");

		Assert.That(result.IsError, Is.True);
		Assert.That(TextOf(result), Does.Contain("too large"));
	}

	private static string TextOf(CallToolResult result) =>
		string.Join("\n", result.Content.OfType<TextContentBlock>().Select(c => c.Text));
}
