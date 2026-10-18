using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LanguageWise.Shared.Db.Data;
using LanguageWise.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LanguageWise.Shared.Api.Tests;

public sealed class DatabaseHttpTests
{
    [Test]
    public async Task HealthAndSeededLogin_ReturnExistingContracts()
    {
        using var fixture = new DatabaseFixture();
        using var client = fixture.CreateClient();

        var health = await client.GetAsync("/health");
        using var healthBody = JsonDocument.Parse(await health.Content.ReadAsStringAsync());
        var login = await client.PostAsJsonAsync("/api/users/verify", new { username = "amber", password = "test" });
        using var loginBody = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var rejected = await client.PostAsJsonAsync("/api/users/verify", new { username = "amber", password = "wrong" });
        var users = await client.GetFromJsonAsync<UserAccount[]>("/api/users?ids=5&ids=1&ids=5&ids=404");

        Assert.Multiple(() =>
        {
            Assert.That(health.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(healthBody.RootElement.GetProperty("status").GetString(), Is.EqualTo("healthy"));
            Assert.That(healthBody.RootElement.GetProperty("service").GetString(), Is.EqualTo("shared-db"));
            Assert.That(healthBody.RootElement.GetProperty("users").GetInt64(), Is.EqualTo(5));
            Assert.That(login.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(loginBody.RootElement.GetProperty("authenticated").GetBoolean(), Is.True);
            Assert.That(loginBody.RootElement.GetProperty("userId").GetInt32(), Is.EqualTo(1));
            Assert.That(rejected.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(users, Is.EqualTo(new[] { new UserAccount(1, "amber"), new UserAccount(5, "kyan") }));
        });
    }

    [Test]
    public async Task CreateAndUpdate_ReturnIntegerIdsAndConflictWithoutChangingCredentials()
    {
        using var fixture = new DatabaseFixture();
        using var client = fixture.CreateClient();

        var created = await client.PostAsJsonAsync("/api/users", new { username = "new-user", password = "secret" });
        var account = await created.Content.ReadFromJsonAsync<UserAccount>();
        var duplicate = await client.PostAsJsonAsync("/api/users", new { username = "new-user", password = "other" });
        var conflict = await client.PatchAsJsonAsync("/api/users/6", new { username = "amber", password = "changed" });
        var verified = await client.PostAsJsonAsync("/api/users/verify", new { username = "new-user", password = "secret" });
        var updated = await client.PatchAsJsonAsync("/api/users/6", new { password = "changed" });
        var missing = await client.PatchAsJsonAsync("/api/users/404", new { username = "missing" });
        var invalid = await client.PatchAsJsonAsync("/api/users/6", new { });
        var invalidCreate = await client.PostAsJsonAsync("/api/users", new { username = " ", password = "secret" });

        fixture.Services.GetRequiredService<DatabaseInitializer>().Initialise();
        var next = await client.PostAsJsonAsync("/api/users", new { username = "next-user", password = "secret" });
        var nextAccount = await next.Content.ReadFromJsonAsync<UserAccount>();

        Assert.Multiple(() =>
        {
            Assert.That(created.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(created.Headers.Location?.ToString(), Is.EqualTo("/api/users/6"));
            Assert.That(account, Is.EqualTo(new UserAccount(6, "new-user")));
            Assert.That(duplicate.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(conflict.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(verified.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(updated.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(missing.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(invalid.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(invalidCreate.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(next.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(nextAccount!.Id, Is.GreaterThan(6));
        });
    }

    [Test]
    public async Task LoginStreak_ReturnsAbsoluteValueThenNoContentOnSameDay()
    {
        using var fixture = new DatabaseFixture();
        using var client = fixture.CreateClient();
        fixture.Services.GetRequiredService<UserRepository>().RecordLogin(1, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1));

        var login = await client.PostAsync("/api/users/1/login-streak", null);
        using var body = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var repeated = await client.PostAsync("/api/users/1/login-streak", null);
        var missing = await client.PostAsync("/api/users/404/login-streak", null);

        Assert.Multiple(() =>
        {
            Assert.That(login.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body.RootElement.GetProperty("value").GetInt32(), Is.EqualTo(1));
            Assert.That(repeated.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(missing.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        });
    }

    [Test]
    public async Task ProfilePicture_UploadsReadsReplacesAndDeletes()
    {
        using var fixture = new DatabaseFixture();
        using var client = fixture.CreateClient();
        byte[] bytes = [1, 2, 3, 4];
        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        var uploaded = await client.PutAsync("/api/users/1/profile-picture?fileName=avatar.png", content);
        var picture = await uploaded.Content.ReadFromJsonAsync<ProfilePicture>();
        var metadata = await client.GetFromJsonAsync<ProfilePicture>("/api/users/1/profile-picture");
        var downloaded = await client.GetAsync("/api/users/1/profile-picture/content");
        var downloadedBytes = await downloaded.Content.ReadAsByteArrayAsync();
        var replaced = await client.PutAsync("/api/users/1/profile-picture", content);

        Assert.Multiple(() =>
        {
            Assert.That(uploaded.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(picture!.FileName, Is.EqualTo("avatar.png"));
            Assert.That(picture.ContentType, Is.EqualTo("image/png"));
            Assert.That(picture.SizeBytes, Is.EqualTo(4));
            Assert.That(picture.UploadedAt.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(metadata, Is.EqualTo(picture));
            Assert.That(downloaded.Content.Headers.ContentType?.MediaType, Is.EqualTo("image/png"));
            Assert.That(downloadedBytes, Is.EqualTo(bytes));
            Assert.That(replaced.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(Directory.GetFiles(fixture.ImageDirectory), Has.Length.EqualTo(1));
            Assert.That(File.Exists(Path.Combine(fixture.ImageDirectory, picture.StorageKey)), Is.False);
        });

        var deleted = await client.DeleteAsync("/api/users/1/profile-picture");
        var missing = await client.GetAsync("/api/users/1/profile-picture");
        var deletedAgain = await client.DeleteAsync("/api/users/1/profile-picture");
        var unknown = await client.PutAsync("/api/users/404/profile-picture", content);

        Assert.Multiple(() =>
        {
            Assert.That(deleted.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(missing.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(deletedAgain.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(unknown.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(Directory.GetFiles(fixture.ImageDirectory), Is.Empty);
        });
    }

    private sealed class DatabaseFixture : WebApplicationFactory<UserRepository>
    {
        private readonly PostgresTestDatabase database = new();

        internal string ImageDirectory { get; } = Path.Combine(Path.GetTempPath(), $"languagewise-images-{Guid.NewGuid():N}");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Database:ConnectionString", database.ConnectionString);
            builder.UseSetting("Images:Path", ImageDirectory);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:ConnectionString"] = database.ConnectionString,
                    ["Images:Path"] = ImageDirectory
                });
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                database.Dispose();
                if (Directory.Exists(ImageDirectory))
                {
                    Directory.Delete(ImageDirectory, recursive: true);
                }
            }
        }
    }
}