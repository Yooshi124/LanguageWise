using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;

namespace LanguageWise.GarryAiService.Api.Tests;

public sealed class AdminSwitchesTests
{
	[Test]
	public async Task GetAsync_TurnsDependentsOffWhenAiModeIsOff()
	{
		var switches = Create(HttpStatusCode.OK, """[{"name":"AIMode","isEnabled":false},{"name":"MCP","isEnabled":true},{"name":"RAG","isEnabled":true}]""");

		var state = await switches.GetAsync(CancellationToken.None);

		Assert.That(state, Is.EqualTo(AdminSwitchState.AllOff));
	}

	[Test]
	public async Task GetAsync_ReadsEachSwitchWhenAiModeIsOn()
	{
		var switches = Create(HttpStatusCode.OK, """[{"name":"AIMode","isEnabled":true},{"name":"MCP","isEnabled":false},{"name":"RAG","isEnabled":true}]""");

		var state = await switches.GetAsync(CancellationToken.None);

		Assert.That(state, Is.EqualTo(new AdminSwitchState(AiMode: true, Mcp: false, Rag: true)));
	}

	[Test]
	public async Task GetAsync_WhenSettingsCannotBeRead_TurnsEverythingOff()
	{
		var switches = Create(HttpStatusCode.InternalServerError, "");

		var state = await switches.GetAsync(CancellationToken.None);

		Assert.That(state, Is.EqualTo(AdminSwitchState.AllOff));
	}

	private static AdminSwitches Create(HttpStatusCode status, string body) =>
		new(new Factory(status, body), NullLogger<AdminSwitches>.Instance);

	private sealed class Factory(HttpStatusCode status, string body) : IHttpClientFactory
	{
		public HttpClient CreateClient(string name) =>
			new(new Handler(status, body)) { BaseAddress = new Uri("http://shared-db-api/") };
	}

	private sealed class Handler(HttpStatusCode status, string body) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
	}
}

public sealed class StubAdminSwitches : IAdminSwitches
{
	public AdminSwitchState State { get; set; } = new(AiMode: true, Mcp: true, Rag: true);

	public Task<AdminSwitchState> GetAsync(CancellationToken cancellationToken) => Task.FromResult(State);
}
