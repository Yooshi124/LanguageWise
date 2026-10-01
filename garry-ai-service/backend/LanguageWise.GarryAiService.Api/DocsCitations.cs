using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace LanguageWise.GarryAiService.Api;

/// <summary>
/// Numbers docs_search passages across one tool loop so the model can cite them as [n], then
/// appends a server-built sources list and confidence category to the answer.
/// </summary>
public sealed partial class DocsCitations
{
	private static readonly string[] ConfidenceOrder = ["insufficient", "low", "medium", "high"];
	private readonly List<(string Source, string Heading)> sources = [];
	private int bestConfidence = -1;

	public JsonElement Number(JsonElement result)
	{
		if (JsonNode.Parse(result.GetRawText()) is not JsonObject root)
		{
			return result;
		}

		var confidence = Array.IndexOf(ConfidenceOrder, root["confidence"]?.GetValue<string>() ?? "insufficient");
		bestConfidence = Math.Max(bestConfidence, Math.Max(confidence, 0));
		if (root["passages"] is JsonArray passages)
		{
			foreach (var passage in passages.OfType<JsonObject>())
			{
				var source = (passage["source"]?.GetValue<string>() ?? "").Trim();
				var heading = (passage["heading"]?.GetValue<string>() ?? "").Trim();
				var index = sources.IndexOf((source, heading));
				if (index < 0)
				{
					sources.Add((source, heading));
					index = sources.Count - 1;
				}
				passage["cite"] = index + 1;
			}
		}
		return JsonSerializer.SerializeToElement(root);
	}

	public string? AppendTo(string? answer)
	{
		if (bestConfidence < 0 || string.IsNullOrWhiteSpace(answer))
		{
			return answer;
		}

		var cited = new SortedSet<int>();
		var text = CitationMarker().Replace(answer, match =>
		{
			var number = int.Parse(match.Groups[2].Value);
			if (number < 1 || number > sources.Count)
			{
				return "";
			}
			cited.Add(number);
			return match.Value;
		}).TrimEnd();

		var footer = new StringBuilder("\n\n---\n");
		if (sources.Count == 0)
		{
			footer.Append("**Confidence:** Insufficient context. The LanguageWise docs don't cover this.");
			return text + footer;
		}

		footer.Append("**Sources**\n\n");
		foreach (var number in cited.Count > 0 ? cited : Enumerable.Range(1, sources.Count))
		{
			var (source, heading) = sources[number - 1];
			footer.Append($"- [{number}] {source} — {heading}\n");
		}
		var confidence = ConfidenceOrder[bestConfidence];
		footer.Append($"\n**Confidence:** {char.ToUpperInvariant(confidence[0])}{confidence[1..]}");
		return text + footer;
	}

	[GeneratedRegex(@"([ \t]*)\[(\d{1,3})\]")]
	private static partial Regex CitationMarker();
}
