using LanguageWise.McpServer.Security;

namespace LanguageWise.McpServer.Tests;

public sealed class StubAdminSwitches : IAdminSwitches
{
	public AdminSwitchState State { get; set; } = new(AiMode: true, Mcp: true, Rag: true);

	public Task<AdminSwitchState> GetAsync(CancellationToken cancellationToken) => Task.FromResult(State);
}
