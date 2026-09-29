using System.Text.RegularExpressions;

namespace LanguageWise.McpServer.Tools;

public static partial class ToolScopes
{
	public const string Courses = "courses";
	public const string Games = "games";
	public const string Chat = "chat";
	public const string Quests = "quests";
	public const string Leaderboard = "leaderboard";

	private static readonly HashSet<string> Known = [Courses, Games, Chat, Quests, Leaderboard];

	public static bool IsKnown(string? scope) =>
		!string.IsNullOrEmpty(scope) && ScopePattern().IsMatch(scope) && Known.Contains(scope);

	public static bool Allows(string scope, string toolName) =>
		IsKnown(scope) && toolName.StartsWith(scope + "_", StringComparison.Ordinal);

	[GeneratedRegex("^[a-z]{1,32}$")]
	private static partial Regex ScopePattern();
}
