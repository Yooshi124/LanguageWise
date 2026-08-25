using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using LanguageWise.QuizzesCoursesService.Api.Models;

namespace LanguageWise.QuizzesCoursesService.Api.Rendering;

/// <summary>
/// Turns sample items into the HTML fragment that HTMX swaps into the page.
/// HTMX swaps HTML rather than JSON, so the backend renders the markup.
/// </summary>
public static class SampleItemHtmlRenderer
{
    private const int ColumnCount = 4;

    public static string RenderRows(IReadOnlyList<SampleItem> items)
    {
        if (items.Count == 0)
        {
            return $"""<tr><td class="lw-table__empty" colspan="{ColumnCount}">No items found.</td></tr>""";
        }

        var builder = new StringBuilder();

        foreach (var item in items)
        {
            builder.Append("<tr>")
                   .Append("<td>").Append(Encode(item.Id.ToString(CultureInfo.InvariantCulture))).Append("</td>")
                   .Append("<td>").Append(Encode(item.Name)).Append("</td>")
                   .Append("<td>").Append(Encode(item.Description)).Append("</td>")
                   .Append("<td>").Append(Encode(FormatCreatedAt(item.CreatedAt))).Append("</td>")
                   .Append("</tr>");
        }

        return builder.ToString();
    }

    public static string RenderError(string message) =>
        $"""<tr><td class="lw-table__error" colspan="{ColumnCount}">{Encode(message)}</td></tr>""";

    /// <summary>
    /// Renders an ISO-8601 timestamp as a short, human readable date.
    /// Values that cannot be parsed are passed through unchanged.
    /// </summary>
    public static string FormatCreatedAt(string createdAt) =>
        DateTimeOffset.TryParse(
            createdAt,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out var parsed)
            ? parsed.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)
            : createdAt;

    private static string Encode(string value) => HtmlEncoder.Default.Encode(value);
}
