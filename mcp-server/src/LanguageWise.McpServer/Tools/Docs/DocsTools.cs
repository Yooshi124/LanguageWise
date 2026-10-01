using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace LanguageWise.McpServer.Tools.Docs;

/// <param name="Confidence">high, medium, low, or insufficient (no passage is relevant enough to answer from).</param>
public sealed record DocsSearchResult(string Confidence, IReadOnlyList<DocsPassage> Passages);

public sealed record DocsPassage(string Source, string Heading, double Relevance, string Text);

/// <summary>General LanguageWise documentation search, available in every tool scope.</summary>
[McpServerToolType]
public sealed class DocsTools(IHttpClientFactory httpClientFactory, ILogger<DocsTools> logger)
{
	public const string ServiceName = "Rag";
	public const string InsufficientConfidence = "insufficient";
	private const int MaxQueryLength = 500;
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	[McpServerTool(Name = "docs_search", Title = "Search the LanguageWise docs", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
	[Description("Searches LanguageWise's user documentation. Use it when the learner asks how a LanguageWise page, feature or service works (for example the notifications page, achievements, mini games, courses, quizzes, the forum or the leaderboard) and the answer is not already in your context. Returns a confidence category; when it is 'insufficient' there are no passages and the docs do not cover the question.")]
	public async Task<DocsSearchResult> SearchAsync(
		[Description("A short natural-language question or topic, for example \"how do notification preferences work\".")] string query,
		[Description("How many passages to return, from 1 to 5. Defaults to 3.")] int maxResults = 3,
		CancellationToken cancellationToken = default)
	{
		query = query?.Trim() ?? "";
		if (query.Length is 0 or > MaxQueryLength)
		{
			throw new McpException($"query must be 1 to {MaxQueryLength} characters.");
		}

		var client = httpClientFactory.CreateClient(ServiceName);
		try
		{
			// The general endpoint never returns TECHNICAL- (internal) passages.
			using var response = await client.PostAsJsonAsync(
				"query",
				new RagQuery(query, Math.Clamp(maxResults, 1, 5)),
				JsonOptions,
				cancellationToken);
			response.EnsureSuccessStatusCode();
			var body = await response.Content.ReadFromJsonAsync<RagResponse>(JsonOptions, cancellationToken);
			var passages = (body?.Results ?? [])
				.Select(result => new DocsPassage(result.Source, result.Heading, result.Relevance, result.Text))
				.ToList();
			var confidence = passages.Count == 0 ? InsufficientConfidence : body?.Confidence ?? InsufficientConfidence;
			return new DocsSearchResult(confidence, confidence == InsufficientConfidence ? [] : passages);
		}
		catch (Exception exception) when (exception is HttpRequestException or JsonException
			|| (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
		{
			logger.LogWarning("Docs search failed: {ErrorType}", exception.GetType().Name);
			throw new McpException("The documentation search is unavailable right now.");
		}
	}

	private sealed record RagQuery(string Query, [property: JsonPropertyName("n_results")] int NResults);

	private sealed record RagResponse(IReadOnlyList<RagResult>? Results, string? Confidence);

	private sealed record RagResult(string Source, string Heading, double Relevance, string Text);
}
