using System.ComponentModel;
using System.Text.RegularExpressions;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace LanguageWise.McpServer.Tools.MiniGames;

[McpServerToolType]
public sealed partial class MiniGamesTools(DownstreamClient downstream)
{
	public const string ServiceName = "MiniGames";

	[McpServerTool(Name = "games_get_completion_stats", Title = "Get my mini games completion stats", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
	[Description("Gets the signed-in user's successful completion counts, best times and current daily streak for the mini games (Guess the Word, Word Search, Associations), optionally scoped to one course/language.")]
	public async Task<CompletionStatsResult> GetCompletionStatsAsync(
		[Description("Optional two-letter course code to scope the stats to one language, for example it or fr.")] string? courseCode = null,
		CancellationToken cancellationToken = default)
	{
		if (courseCode is not null)
		{
			ValidateCourseCode(courseCode);
		}

		var path = courseCode is null
			? "api/stats/completions"
			: $"api/stats/completions?courseCode={Uri.EscapeDataString(courseCode)}";
		var stats = await downstream.GetAsync<CompletionStatsDto>(ServiceName, path, cancellationToken);
		return new CompletionStatsResult(
			stats.CourseCode,
			stats.GuessTheWord,
			stats.WordSearch,
			stats.Associations,
			stats.BestGuessTheWordSeconds,
			stats.BestWordSearchSeconds,
			stats.BestAssociationsSeconds,
			stats.CurrentStreak);
	}

	[McpServerTool(Name = "games_list_game_languages", Title = "List my mini games languages", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
	[Description("Lists the languages the signed-in user has unlocked vocabulary for and can play the mini games in.")]
	public async Task<GameLanguageListResult> ListGameLanguagesAsync(CancellationToken cancellationToken)
	{
		var languages = await downstream.GetAsync<List<GameLanguageDto>>(ServiceName, "api/game-languages", cancellationToken);
		return new GameLanguageListResult(languages.Select(l => new GameLanguageItem(l.Code, l.Title)).ToList());
	}

	private static void ValidateCourseCode(string courseCode)
	{
		if (string.IsNullOrWhiteSpace(courseCode) || !CourseCodePattern().IsMatch(courseCode))
		{
			throw new McpException("courseCode must be a two-letter lowercase code such as it or fr.");
		}
	}

	[GeneratedRegex("^[a-z]{2}$")]
	private static partial Regex CourseCodePattern();
}
