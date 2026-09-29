using System.ComponentModel;
using System.Text.RegularExpressions;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace LanguageWise.McpServer.Tools.QuizzesCourses;

[McpServerToolType]
public sealed partial class QuizzesCoursesTools(DownstreamClient downstream)
{
	public const string ServiceName = "QuizzesCourses";

	[McpServerTool(Name = "courses_list_courses", Title = "List courses", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
	[Description("Lists every LanguageWise course with its code, title and description. Use this to find a course code.")]
	public async Task<CourseListResult> ListCoursesAsync(CancellationToken cancellationToken)
	{
		var courses = await downstream.GetAsync<List<CourseDto>>(ServiceName, "api/courses", cancellationToken);
		return new CourseListResult(courses.Select(c => new CourseItem(c.Code, c.Title, c.Description)).ToList());
	}

	[McpServerTool(Name = "courses_get_lesson_vocabulary", Title = "Get lesson vocabulary", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
	[Description("Gets the vocabulary words and their meanings taught in one lesson of a course.")]
	public async Task<LessonVocabularyResult> GetLessonVocabularyAsync(
		[Description("Two-letter course code: de, fr, it, nl, es or pl.")] string courseCode,
		[Description("Lesson slug, for example greetings or time-calendar.")] string lessonSlug,
		CancellationToken cancellationToken)
	{
		ValidateCourseCode(courseCode);
		ValidateLessonSlug(lessonSlug);

		var lesson = await downstream.GetAsync<LessonDetailDto>(
			ServiceName,
			$"api/courses/{Uri.EscapeDataString(courseCode)}/lessons/{Uri.EscapeDataString(lessonSlug)}",
			cancellationToken);
		return new LessonVocabularyResult(
			lesson.Course?.Code ?? courseCode,
			lesson.Slug,
			lesson.Title,
			(lesson.Vocabulary ?? []).Select(v => new VocabularyItem(v.Word, v.Meaning)).ToList());
	}

	[McpServerTool(Name = "courses_get_my_progress", Title = "Get my course progress", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
	[Description("Gets the signed-in user's progress in one course: completed lessons and best quiz scores.")]
	public async Task<CourseProgressResult> GetMyProgressAsync(
		[Description("Two-letter course code: de, fr, it, nl, es or pl.")] string courseCode,
		CancellationToken cancellationToken)
	{
		ValidateCourseCode(courseCode);
		var progress = await downstream.GetAsync<CourseProgressDto>(
			ServiceName,
			$"api/courses/{Uri.EscapeDataString(courseCode)}/progress",
			cancellationToken);
		var lessons = progress.Lessons ?? [];
		return new CourseProgressResult(
			courseCode,
			progress.CourseCompleted,
			lessons.Count(l => l.Completed),
			lessons.Count,
			(progress.Quizzes ?? []).Select(q => new QuizProgressItem(q.QuizId, q.Completed, q.BestScore, q.TotalQuestions)).ToList());
	}

	[McpServerTool(Name = "courses_list_lessons", Title = "List course lessons", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
	[Description("Lists the lessons in a course in order, with each lesson's slug and title. Use this to find a lesson slug.")]
	public async Task<LessonListResult> ListLessonsAsync(
		[Description("Two-letter course code: de, fr, it, nl, es or pl.")] string courseCode,
		CancellationToken cancellationToken)
	{
		ValidateCourseCode(courseCode);
		var lessons = await downstream.GetAsync<List<LessonSummaryDto>>(
			ServiceName,
			$"api/courses/{Uri.EscapeDataString(courseCode)}/lessons",
			cancellationToken);
		return new LessonListResult(
			courseCode,
			lessons.OrderBy(l => l.SortOrder).Select(l => new LessonItem(l.Id, l.Slug, l.Title, l.SortOrder)).ToList());
	}

	[McpServerTool(Name = "courses_list_quizzes", Title = "List course quizzes", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
	[Description("Lists the quizzes in a course and the lesson each quiz belongs to. Does not include questions or answers.")]
	public async Task<QuizListResult> ListQuizzesAsync(
		[Description("Two-letter course code: de, fr, it, nl, es or pl.")] string courseCode,
		CancellationToken cancellationToken)
	{
		ValidateCourseCode(courseCode);
		var quizzes = await downstream.GetAsync<List<QuizSummaryDto>>(
			ServiceName,
			$"api/courses/{Uri.EscapeDataString(courseCode)}/quizzes",
			cancellationToken);
		return new QuizListResult(
			courseCode,
			quizzes.OrderBy(q => q.LessonSortOrder).Select(q => new QuizItem(q.Id, q.Title, q.LessonSlug, q.LessonTitle)).ToList());
	}

	[McpServerTool(Name = "courses_get_flashcards", Title = "Get lesson flashcards", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
	[Description("Gets the flashcards (front and back text) for one lesson of a course. Useful for revision.")]
	public async Task<FlashcardDeckResult> GetFlashcardsAsync(
		[Description("Two-letter course code: de, fr, it, nl, es or pl.")] string courseCode,
		[Description("Lesson slug, for example greetings or time-calendar.")] string lessonSlug,
		CancellationToken cancellationToken)
	{
		ValidateCourseCode(courseCode);
		ValidateLessonSlug(lessonSlug);
		var deck = await downstream.GetAsync<FlashcardDeckDto>(
			ServiceName,
			$"api/courses/{Uri.EscapeDataString(courseCode)}/flashcard-decks/{Uri.EscapeDataString(lessonSlug)}",
			cancellationToken);
		return new FlashcardDeckResult(
			courseCode,
			deck.LessonSlug,
			deck.LessonTitle,
			(deck.Cards ?? []).Select(c => new FlashcardItem(c.FrontText, c.BackText)).ToList());
	}

	[McpServerTool(Name = "courses_get_my_vocabulary", Title = "Get my learnt vocabulary", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
	[Description("Gets the vocabulary the signed-in user has unlocked by completing lessons, grouped by course and lesson.")]
	public async Task<MyVocabularyResult> GetMyVocabularyAsync(
		[Description("Optional two-letter course code to limit results to one course.")] string? courseCode = null,
		CancellationToken cancellationToken = default)
	{
		if (courseCode is not null)
		{
			ValidateCourseCode(courseCode);
		}
		var vocabulary = await downstream.GetAsync<UserVocabularyDto>(ServiceName, "api/me/vocabulary", cancellationToken);
		var courses = (vocabulary.Courses ?? [])
			.Where(c => courseCode is null || c.Code == courseCode)
			.Select(c => new CourseVocabularyItem(
				c.Code,
				c.Title,
				(c.Lessons ?? []).Select(l => new LessonVocabularyItem(
					l.Slug,
					l.Title,
					(l.Vocabulary ?? []).Select(v => new VocabularyItem(v.Word, v.Meaning)).ToList())).ToList()))
			.ToList();
		return new MyVocabularyResult(courses, courses.Sum(c => c.Lessons.Sum(l => l.Vocabulary.Count)));
	}

	[McpServerTool(Name = "courses_get_my_milestones", Title = "Get my milestones", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
	[Description("Gets the signed-in user's most recent milestones (completed courses, lessons and quizzes) with completion dates. Use courses_list_lessons or courses_list_quizzes to turn ids into names.")]
	public async Task<MyMilestonesResult> GetMyMilestonesAsync(
		[Description("How many recent milestones to return, 1-50. Defaults to 10.")] int limit = 10,
		CancellationToken cancellationToken = default)
	{
		if (limit is < 1 or > 50)
		{
			throw new McpException("limit must be between 1 and 50.");
		}
		var page = await downstream.GetAsync<MilestonePageDto>(ServiceName, "api/me/milestones?limit=200", cancellationToken);
		var items = (page.Items ?? [])
			.OrderByDescending(m => m.CompletedAt)
			.Take(limit)
			.Select(m => new MilestoneItem(
				m.QuizId is not null ? "quiz" : m.LessonId is not null ? "lesson" : "course",
				m.CourseId,
				m.LessonId,
				m.QuizId,
				m.CompletedAt))
			.ToList();
		return new MyMilestonesResult(items);
	}

	private static void ValidateCourseCode(string courseCode)
	{
		if (string.IsNullOrWhiteSpace(courseCode) || !CourseCodePattern().IsMatch(courseCode))
		{
			throw new McpException("courseCode must be a two-letter lowercase code such as it or fr.");
		}
	}

	private static void ValidateLessonSlug(string lessonSlug)
	{
		if (string.IsNullOrWhiteSpace(lessonSlug) || !LessonSlugPattern().IsMatch(lessonSlug))
		{
			throw new McpException("lessonSlug must be 1-64 lowercase letters, digits or hyphens.");
		}
	}

	[GeneratedRegex("^[a-z]{2}$")]
	private static partial Regex CourseCodePattern();

	[GeneratedRegex("^[a-z0-9](?:[a-z0-9-]{0,62}[a-z0-9])?$")]
	private static partial Regex LessonSlugPattern();
}
