using System.Linq.Expressions;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Clients;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;
using Newtonsoft.Json;
using NSubstitute;
using Supabase.Postgrest;
using Supabase.Postgrest.Interfaces;
using Supabase.Postgrest.Responses;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Tests;

[TestFixture]
public class SampleItemsClientTests
{
    [Test]
    public async Task CreateAsync_Create_InsertsAndReturnsTheCreatedItem()
    {
        var input = new SampleItem(0, "New", "Description", "2026-08-26T00:00:00Z");
        var created = new SampleItem(11, input.Name, input.Description, input.CreatedAt);
        var (client, table) = CreateClient();
        table.Insert(Arg.Any<SampleItemEntity>(), null, Arg.Any<CancellationToken>()).Returns(ResponseWith(created));

        var result = await client.CreateAsync(input);

        Assert.That(result, Is.EqualTo(created));
        await table.Received(1).Insert(
            Arg.Is<SampleItemEntity>(item => item.Name == input.Name && item.Description == input.Description),
            null,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetAllAsync_Read_ReturnsItemsOrderedByIdAscending()
    {
        var expectedItems = new[]
        {
            new SampleItem(1, "First", "One", "2026-01-05T09:00:00Z"),
            new SampleItem(2, "Second", "Two", "2026-01-06T09:00:00Z")
        };
        var (client, table) = CreateClient();
        table.Get(Arg.Any<CancellationToken>()).Returns(ResponseWith(expectedItems));

        var items = await client.GetAllAsync();

        Assert.That(items, Is.EqualTo(expectedItems));
        table.Received(1).Order(Arg.Any<Expression<Func<SampleItemEntity, object>>>(), Constants.Ordering.Ascending);
    }

    [Test]
    public async Task UpdateAsync_Update_UpdatesAndReturnsTheItem()
    {
        var item = new SampleItem(1, "Updated", "Changed", "2026-01-05T09:00:00Z");
        var (client, table) = CreateClient();
        table.Update(Arg.Any<SampleItemEntity>(), null, Arg.Any<CancellationToken>()).Returns(ResponseWith(item));

        var result = await client.UpdateAsync(item);

        Assert.That(result, Is.EqualTo(item));
        await table.Received(1).Update(
            Arg.Is<SampleItemEntity>(entity => entity.Id == item.Id && entity.Name == item.Name),
            null,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteAsync_Delete_DeletesTheItem()
    {
        var item = new SampleItem(1, "Delete", "Me", "2026-01-05T09:00:00Z");
        var (client, table) = CreateClient();
        table.Delete(Arg.Any<SampleItemEntity>(), null, Arg.Any<CancellationToken>()).Returns(ResponseWith(item));

        await client.DeleteAsync(item);

        await table.Received(1).Delete(
            Arg.Is<SampleItemEntity>(entity => entity.Id == item.Id),
            null,
            Arg.Any<CancellationToken>());
    }

    private static (SampleItemsClient Client, IPostgrestTable<SampleItemEntity> Table) CreateClient()
    {
        var postgrestClient = Substitute.For<IPostgrestClient>();
        var table = Substitute.For<IPostgrestTable<SampleItemEntity>>();

        postgrestClient.Table<SampleItemEntity>().Returns(table);
        table.Order(Arg.Any<Expression<Func<SampleItemEntity, object>>>(), Constants.Ordering.Ascending).Returns(table);

        return (new SampleItemsClient(postgrestClient), table);
    }

    private static ModeledResponse<SampleItemEntity> ResponseWith(params IEnumerable<SampleItem> items)
    {
        var response = new ModeledResponse<SampleItemEntity>(
            new BaseResponse(new ClientOptions(), null, null),
            new JsonSerializerSettings(),
            shouldParse: false);
        response.Models.AddRange(items.Select(SampleItemEntity.FromModel));
        return response;
    }
}
