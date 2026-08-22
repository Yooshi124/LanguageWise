using LanguageWise.MiniGamesService.Api.Models;
using LanguageWise.MiniGamesService.Api.Rendering;

namespace LanguageWise.MiniGamesService.Api.Tests;

[TestFixture]
public class SampleItemHtmlRendererTests
{
    [Test]
    public void RenderRows_RendersOneTableRowPerItem()
    {
        var items = new List<SampleItem>
        {
            new(1, "First", "One", "2026-01-05T09:00:00Z"),
            new(2, "Second", "Two", "2026-01-06T09:00:00Z")
        };

        var html = SampleItemHtmlRenderer.RenderRows(items);

        Assert.That(CountOccurrences(html, "<tr>"), Is.EqualTo(2));
        Assert.That(html, Does.Contain("First").And.Contain("Second"));
    }

    [Test]
    public void RenderRows_ShowsAPlaceholderRowWhenThereAreNoItems()
    {
        var html = SampleItemHtmlRenderer.RenderRows([]);

        Assert.That(html, Does.Contain("No items found."));
        Assert.That(CountOccurrences(html, "<tr>"), Is.EqualTo(1));
    }

    [Test]
    public void RenderRows_EncodesMarkupSoUserDataCannotInjectHtml()
    {
        var items = new List<SampleItem>
        {
            new(1, "<script>alert('xss')</script>", "safe", "2026-01-05T09:00:00Z")
        };

        var html = SampleItemHtmlRenderer.RenderRows(items);

        Assert.That(html, Does.Not.Contain("<script>"));
        Assert.That(html, Does.Contain("&lt;script&gt;"));
    }

    [Test]
    public void FormatCreatedAt_FormatsAnIsoTimestampAsAShortDate()
    {
        Assert.That(SampleItemHtmlRenderer.FormatCreatedAt("2026-01-05T09:00:00Z"), Is.EqualTo("05 Jan 2026"));
    }

    [Test]
    public void FormatCreatedAt_PassesUnparseableValuesThroughUnchanged()
    {
        Assert.That(SampleItemHtmlRenderer.FormatCreatedAt("not-a-date"), Is.EqualTo("not-a-date"));
    }

    private static int CountOccurrences(string haystack, string needle) =>
        (haystack.Length - haystack.Replace(needle, string.Empty, StringComparison.Ordinal).Length) / needle.Length;
}
