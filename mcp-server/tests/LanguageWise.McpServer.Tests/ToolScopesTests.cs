using LanguageWise.McpServer.Tools;

namespace LanguageWise.McpServer.Tests;

public sealed class ToolScopesTests
{
	[TestCase("courses", "courses_list_courses", true)]
	[TestCase("courses", "coursesx_list", false)]
	[TestCase("games", "courses_list_courses", false)]
	[TestCase("COURSES", "courses_list_courses", false)]
	[TestCase("", "courses_list_courses", false)]
	[TestCase("admin", "admin_delete", false)]
	[TestCase("chat", "docs_search", true)]
	[TestCase("quests", "docs_search", true)]
	[TestCase("admin", "docs_search", false)]
	[TestCase("", "docs_search", false)]
	public void Allows_OnlyMatchesRegisteredScopePrefix(string scope, string toolName, bool expected) =>
		Assert.That(ToolScopes.Allows(scope, toolName), Is.EqualTo(expected));
}
