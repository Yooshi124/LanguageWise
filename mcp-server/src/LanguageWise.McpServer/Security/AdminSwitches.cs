using System.Text.Json;

namespace LanguageWise.McpServer.Security;

public sealed record AdminSwitchState(bool AiMode, bool Mcp, bool Rag)
{
	public static readonly AdminSwitchState AllOff = new(false, false, false);
}

public interface IAdminSwitches
{
	Task<AdminSwitchState> GetAsync(CancellationToken cancellationToken);
}

public sealed class AdminSwitches(IHttpClientFactory clients, ILogger<AdminSwitches> logger) : IAdminSwitches
{
	public const string HttpClientName = "admin-settings";
	private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(5);
	private CachedState? cached;

	public async Task<AdminSwitchState> GetAsync(CancellationToken cancellationToken)
	{
		var current = cached;
		if (current is not null && current.ExpiresAt > DateTime.UtcNow)
		{
			return current.State;
		}

		AdminSwitchState state;
		try
		{
			var settings = await clients.CreateClient(HttpClientName)
				.GetFromJsonAsync<List<AdminSetting>>("api/admin-settings", cancellationToken) ?? [];
			state = FromSettings(settings);
		}
		catch (Exception exception) when (exception is HttpRequestException or JsonException
			|| (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
		{
			// Fail closed: MCP stays off until the switches can be read.
			logger.LogWarning("Could not read admin settings: {ErrorType}", exception.GetType().Name);
			state = AdminSwitchState.AllOff;
		}

		cached = new CachedState(state, DateTime.UtcNow + CacheDuration);
		return state;
	}

	private static AdminSwitchState FromSettings(IReadOnlyList<AdminSetting> settings)
	{
		bool IsOn(string name) => settings.Any(setting => setting.Name == name && setting.IsEnabled);
		var aiMode = IsOn("AIMode");
		return new AdminSwitchState(aiMode, aiMode && IsOn("MCP"), aiMode && IsOn("RAG"));
	}

	private sealed record AdminSetting(string Name, bool IsEnabled);

	private sealed record CachedState(AdminSwitchState State, DateTime ExpiresAt);
}
