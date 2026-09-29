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
			"courses_get_my_milestones"
		}));
		Assert.That(tools.All(t => t.ProtocolTool.Annotations?.ReadOnlyHint == true), Is.True);
	}

	[TestCase("games")]
	[TestCase("unknown")]
	[TestCase(null)]
	public async Task ListTools_WithOtherOrMissingScope_HidesCoursesTools(string? scope)
	{
		await using var client = await factory.CreateMcpClientAsync(scope);
		var tools = await client.ListToolsAsync();
		Assert.That(tools, Is.Empty);
	}

	[Test]
	public async Task CallTool_OutOfScope_IsRejected()
	{
		await using var client = await factory.CreateMcpClientAsync("games", factory.CreateUserToken());
		Assert.ThrowsAsync<McpProtocolException>(async () => await client.CallToolAsync("courses_list_courses"));
		Assert.That(factory.Downstream.Requests, Is.Empty);
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
	public async Task GetMyMilestones_ReturnsMostRecentFirstWithKind()
	{
		factory.Downstream.Body = """
			{"items":[
			 {"id":1,"userId":42,"courseId":null,"lessonId":1,"quizId":null,"completedAt":"2026-09-01T10:00:00+00:00"},
			 {"id":2,"userId":42,"courseId":null,"lessonId":null,"quizId":7,"completedAt":"2026-09-02T10:00:00+00:00"},
			 {"id":3,"userId":42,"courseId":3,"lessonId":null,"quizId":null,"completedAt":"2026-09-03T10:00:00+00:00"}],
			 "nextCursor":null}
			""";
		await using var client = await factory.CreateMcpClientAsync("courses", factory.CreateUserToken());

		var result = await client.CallToolAsync("courses_get_my_milestones", new Dictionary<string, object?> { ["limit"] = 2 });

		Assert.That(result.IsError, Is.Not.True);
		Assert.That(factory.Downstream.Requests.Single().RequestUri!.PathAndQuery, Is.EqualTo("/api/me/milestones?limit=200"));
		var milestones = result.StructuredContent!.Value.GetProperty("milestones");
		Assert.That(milestones.GetArrayLength(), Is.EqualTo(2));
		Assert.That(milestones[0].GetProperty("kind").GetString(), Is.EqualTo("course"));
		Assert.That(milestones[1].GetProperty("kind").GetString(), Is.EqualTo("quiz"));
		Assert.That(result.StructuredContent!.Value.GetRawText(), Does.Not.Contain("userId"));
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
