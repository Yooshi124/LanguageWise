using System.Net;
using LanguageWise.Student5.Api.Clients;

namespace LanguageWise.Student5.Api.Tests;

[TestFixture]
public class SampleItemsClientTests
{
    private const string ItemsJson =
        """
        [
          { "id": 1, "name": "First",  "description": "One", "createdAt": "2026-01-05T09:00:00Z" },
          { "id": 2, "name": "Second", "description": "Two", "createdAt": "2026-01-06T09:00:00Z" }
        ]
        """;

    [Test]
    public async Task GetAllAsync_DeserialisesEveryItemReturnedByTheDatabaseService()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, ItemsJson);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://student-5-db:8080/") };
        var client = new SampleItemsClient(httpClient);

        var items = await client.GetAllAsync();

        Assert.That(items, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(items[0].Id, Is.EqualTo(1));
            Assert.That(items[0].Name, Is.EqualTo("First"));
            Assert.That(items[1].Description, Is.EqualTo("Two"));
        });
    }

    [Test]
    public async Task GetAllAsync_RequestsTheItemsEndpointOnTheDatabaseService()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "[]");
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://student-5-db:8080/") };
        var client = new SampleItemsClient(httpClient);

        await client.GetAllAsync();

        Assert.That(handler.LastRequestUri, Is.EqualTo(new Uri("http://student-5-db:8080/api/items")));
    }

    [Test]
    public async Task GetAllAsync_ReturnsAnEmptyListWhenTheDatabaseHasNoRows()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "[]");
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://student-5-db:8080/") };
        var client = new SampleItemsClient(httpClient);

        var items = await client.GetAllAsync();

        Assert.That(items, Is.Empty);
    }

    [Test]
    public void GetAllAsync_ThrowsWhenTheDatabaseServiceFails()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.ServiceUnavailable, "{}");
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://student-5-db:8080/") };
        var client = new SampleItemsClient(httpClient);

        Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetAllAsync());
    }
}
