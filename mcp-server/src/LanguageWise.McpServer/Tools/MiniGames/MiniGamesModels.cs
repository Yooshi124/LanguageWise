namespace LanguageWise.McpServer.Tools.MiniGames;

public sealed record CompletionStatsResult(
	string? CourseCode,
	int GuessTheWordCompletions,
	int WordSearchCompletions,
	int AssociationsCompletions,
	int? BestGuessTheWordSeconds,
	int? BestWordSearchSeconds,
	int? BestAssociationsSeconds,
	int CurrentStreak);

public sealed record GameLanguageListResult(IReadOnlyList<GameLanguageItem> Languages);
public sealed record GameLanguageItem(string Code, string Title);

internal sealed record CompletionStatsDto(
	string? CourseCode,
	int GuessTheWord,
	int WordSearch,
	int Associations,
	int? BestGuessTheWordSeconds,
	int? BestWordSearchSeconds,
	int? BestAssociationsSeconds,
	int CurrentStreak);

internal sealed record GameLanguageDto(string Code, string Title);
