using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Clients;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Tests;

[NonParallelizable]
[Category("Integration")]
public sealed class EventProcessingIntegrationTests
{
    private const int UserId = 1;
    private static readonly string ComposeFile = FindComposeFile();
    private static readonly string ComposeProject = $"languagewise-qan-tests-{Guid.NewGuid():N}";
    private static string databaseUrl = string.Empty;

    [OneTimeSetUp]
    public async Task StartDatabaseAsync()
    {
        await RunDockerComposeAsync("up", "-d", "--build", "--wait");
        var port = (await RunDockerComposeAsync("port", "postgrest", "3000")).Trim();
        databaseUrl = $"http://{port}";

        using var client = new HttpClient { BaseAddress = new Uri(databaseUrl) };
        for (var attempt = 0; attempt < 30; attempt++)
        {
            try
            {
                using var response = await client.GetAsync("achievements?limit=1");
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }

            await Task.Delay(500);
        }

        Assert.Fail("The Docker PostgREST service did not become ready.");
    }

    [OneTimeTearDown]
    public async Task StopDatabaseAsync()
    {
        await RunDockerComposeAsync("down", "--volumes", "--remove-orphans");
    }

    [Test]
    public async Task IdenticalEvents_AreProcessedAsSeparateOccurrences()
    {
        using var fixture = new ApiFixture(databaseUrl);
        using var apiClient = fixture.CreateClient();
        apiClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", fixture.CreateToken());

        var request = new EventRequest(
            "lesson-completion",
            "Introduction to Spanish",
            UserId,
            "Amber");

        using var firstResponse = await apiClient.PostAsJsonAsync("/api/events", request);
        using var secondResponse = await apiClient.PostAsJsonAsync("/api/events", request);
        using var responseBody = JsonDocument.Parse(await firstResponse.Content.ReadAsStringAsync());
        using var secondResponseBody = JsonDocument.Parse(await secondResponse.Content.ReadAsStringAsync());

        using var databaseClient = new HttpClient { BaseAddress = new Uri(databaseUrl) };
        var progress = await databaseClient.GetFromJsonAsync<List<UserAchievement>>(
            $"user_achievements?user_id=eq.{UserId}&achievement_id=in.(1,2,3)&order=achievement_id.asc");
        var notifications = await databaseClient.GetFromJsonAsync<List<NotificationInput>>(
            $"notifications?user_id=eq.{UserId}&trigger=eq.lesson-completion");
        var generatedNotifications = notifications!
            .Where(item => item.EmailSubject == "Course achievement progress")
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(firstResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(secondResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(
                responseBody.RootElement.GetProperty("achievements").EnumerateArray()
                    .Select(item => item.GetProperty("progress").GetInt32()),
                Is.EqualTo(new[] { 1, 1, 1 }));
            Assert.That(
                responseBody.RootElement.GetProperty("achievements").EnumerateArray()
                    .Select(item => item.GetProperty("newlyAttained").GetBoolean()),
                Is.EqualTo(new[] { true, false, false }));
            Assert.That(
                secondResponseBody.RootElement.GetProperty("achievements").EnumerateArray()
                    .Select(item => item.GetProperty("newlyAttained").GetBoolean()),
                Is.EqualTo(new[] { false, false, false }));
            Assert.That(progress, Has.Count.EqualTo(3));
            Assert.That(progress!.Select(item => item.Progress), Is.EqualTo(new[] { 1, 2, 2 }));
            Assert.That(generatedNotifications, Has.Count.EqualTo(2));
            Assert.That(generatedNotifications[0].EmailBody, Does.Contain("Complete your first lesson"));
            Assert.That(generatedNotifications[0].EmailBody, Does.Contain("Complete five lessons"));
            Assert.That(generatedNotifications[0].EmailBody, Does.Contain("Complete twenty lessons"));
        });
    }

    [Test]
    public async Task Event_WithoutPreferences_StillStoresNotificationAndProgress()
    {
        const int recipientUserId = 97;
        var sender = new RecordingEmailSender();
        using var fixture = new ApiFixture(databaseUrl, sender);
        using var apiClient = fixture.CreateClient();
        apiClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", fixture.CreateToken());
        var request = new EventRequest(
            "community-contribution",
            "Welcome to LanguageWise",
            recipientUserId,
            "Learner");

        var beforeRequest = DateTimeOffset.UtcNow;
        using var response = await apiClient.PostAsJsonAsync("/api/events", request);
        var afterRequest = DateTimeOffset.UtcNow;
        using var databaseClient = new HttpClient { BaseAddress = new Uri(databaseUrl) };
        var progress = await databaseClient.GetFromJsonAsync<List<UserAchievement>>(
            $"user_achievements?user_id=eq.{recipientUserId}&order=achievement_id.asc");
        var notifications = await databaseClient.GetFromJsonAsync<List<NotificationInput>>(
            $"notifications?user_id=eq.{recipientUserId}&trigger=eq.community-contribution");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(progress!.Select(item => item.Progress), Is.EqualTo(new[] { 1, 1, 1 }));
            Assert.That(notifications, Has.Count.EqualTo(1));
            Assert.That(notifications![0].EmailSubject, Is.Not.Empty);
            Assert.That(notifications[0].EmailBody, Is.Not.Empty);
            Assert.That(notifications[0].Time, Is.InRange(beforeRequest, afterRequest));
            Assert.That(sender.SentCount, Is.Zero);
        });
    }

    [Test]
    public async Task Event_WhenPreferencesAllowDelivery_StoresAndSendsSameContent()
    {
        var sender = new RecordingEmailSender();
        using var fixture = new ApiFixture(databaseUrl, sender);
        using var apiClient = fixture.CreateClient();
        apiClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", fixture.CreateToken());
        var request = new EventRequest(
            "login-streak",
            "Daily learning streak",
            UserId,
            "Amber");

        using var response = await apiClient.PostAsJsonAsync("/api/events", request);
        using var databaseClient = new HttpClient { BaseAddress = new Uri(databaseUrl) };
        var notifications = await databaseClient.GetFromJsonAsync<List<NotificationInput>>(
            $"notifications?user_id=eq.{UserId}&trigger=eq.login-streak");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(notifications, Has.Count.EqualTo(1));
            Assert.That(sender.SentCount, Is.EqualTo(1));
            Assert.That(sender.LastContent, Is.Not.Null);
            Assert.That(sender.LastContent!.Subject, Is.EqualTo(notifications![0].EmailSubject));
            Assert.That(sender.LastContent.Body, Is.EqualTo(notifications[0].EmailBody));
        });
    }

    [Test]
    public async Task LoginStreak_WithAbsoluteValue_OnlyRaisesStoredProgress()
    {
        const int recipientUserId = 95;
        using var fixture = new ApiFixture(databaseUrl);
        using var apiClient = fixture.CreateClient();
        apiClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", fixture.CreateToken(recipientUserId));

        using var firstResponse = await apiClient.PostAsJsonAsync("/api/events", new EventRequest(
            "login-streak", "Continued a daily login streak for five consecutive days", recipientUserId, "Amber", 5));
        using var secondResponse = await apiClient.PostAsJsonAsync("/api/events", new EventRequest(
            "login-streak", "Started a new daily login streak", recipientUserId, "Amber", 0));

        using var databaseClient = new HttpClient { BaseAddress = new Uri(databaseUrl) };
        var progress = await databaseClient.GetFromJsonAsync<List<UserAchievement>>(
            $"user_achievements?user_id=eq.{recipientUserId}&achievement_id=in.(9,10,11)&order=achievement_id.asc");

        Assert.Multiple(() =>
        {
            Assert.That(firstResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(secondResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(progress!.Select(item => item.Progress), Is.EqualTo(new[] { 3, 5, 5 }));
        });
    }

    [Test]
    public async Task Event_WithoutEmail_StillStoresNotificationAndProgress()
    {
        const int recipientUserId = 96;
        using var databaseHttpClient = new HttpClient { BaseAddress = new Uri(databaseUrl) };
        var databaseClient = new AppDataClient(databaseHttpClient);
        await databaseClient.UpsertPreferencesAsync(new UserPreferences(
            recipientUserId, null, true, true, true, true, true, true, true, true, true));

        var sender = new RecordingEmailSender();
        using var fixture = new ApiFixture(databaseUrl, sender);
        using var apiClient = fixture.CreateClient();
        apiClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", fixture.CreateToken());
        var request = new EventRequest(
            "minigame-win",
            "Spanish vocabulary quiz",
            recipientUserId,
            "Learner");

        using var response = await apiClient.PostAsJsonAsync("/api/events", request);
        var progress = await databaseHttpClient.GetFromJsonAsync<List<UserAchievement>>(
            $"user_achievements?user_id=eq.{recipientUserId}&order=achievement_id.asc");
        var notifications = await databaseHttpClient.GetFromJsonAsync<List<NotificationInput>>(
            $"notifications?user_id=eq.{recipientUserId}&trigger=eq.minigame-win");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(progress!.Select(item => item.Progress), Is.EqualTo(new[] { 1, 1, 1 }));
            Assert.That(notifications, Has.Count.EqualTo(1));
            Assert.That(notifications![0].EmailSubject, Is.Not.Empty);
            Assert.That(notifications[0].EmailBody, Is.Not.Empty);
            Assert.That(sender.SentCount, Is.Zero);
        });
    }

    [Test]
    public async Task Profile_ReturnsOnlyAuthenticatedUsersNotifications_NewestFirst()
    {
        const int profileUserId = 98;
        using var fixture = new ApiFixture(databaseUrl);
        using var databaseHttpClient = new HttpClient { BaseAddress = new Uri(databaseUrl) };
        var databaseClient = new AppDataClient(databaseHttpClient);
        var suffix = Guid.NewGuid().ToString("N");
        await databaseClient.CreateNotificationAsync(new NotificationInput(
            profileUserId, "login-streak",
            new DateTimeOffset(2030, 1, 1, 9, 0, 0, TimeSpan.Zero), $"Old {suffix}", "Old body"));
        await databaseClient.CreateNotificationAsync(new NotificationInput(
            profileUserId, "minigame-win",
            new DateTimeOffset(2030, 1, 2, 9, 0, 0, TimeSpan.Zero), $"New {suffix}", "New body"));
        await databaseClient.CreateNotificationAsync(new NotificationInput(
            99, "lesson-completion",
            new DateTimeOffset(2030, 1, 3, 9, 0, 0, TimeSpan.Zero), $"Other {suffix}", "Other body"));

        using var apiClient = fixture.CreateClient();
        apiClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", fixture.CreateToken(profileUserId));
        using var response = await apiClient.GetAsync("/api/profile");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var subjects = body.RootElement.GetProperty("notifications").EnumerateArray()
            .Select(item => item.GetProperty("emailSubject").GetString())
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(subjects, Is.EqualTo(new[] { $"New {suffix}", $"Old {suffix}" }));
            Assert.That(subjects, Does.Not.Contain($"Other {suffix}"));
        });
    }

    private static async Task<string> RunDockerComposeAsync(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("docker")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("compose");
        startInfo.ArgumentList.Add("--project-name");
        startInfo.ArgumentList.Add(ComposeProject);
        startInfo.ArgumentList.Add("--file");
        startInfo.ArgumentList.Add(ComposeFile);
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Docker Compose could not be started.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var output = await outputTask;
        var error = await errorTask;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Docker Compose failed: {error}");
        }

        return output;
    }

    private static string FindComposeFile()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "quests-achievements-notifications-service",
                "tests",
                "LanguageWise.QuestsAchievementsNotificationsService.Api.Tests",
                "docker-compose.integration.yml");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate docker-compose.integration.yml.");
    }

    private sealed class ApiFixture : WebApplicationFactory<Program>
    {
        private readonly RSA rsa = RSA.Create(2048);
        private readonly string publicKeyPath = Path.GetTempFileName();
        private readonly string databaseServiceUrl;
        private readonly IEmailSender emailSender;

        internal ApiFixture(string databaseServiceUrl, IEmailSender? emailSender = null)
        {
            this.databaseServiceUrl = databaseServiceUrl;
            this.emailSender = emailSender ?? new DisabledEmailSender();
            File.WriteAllText(publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem());
        }

        internal string CreateToken(int userId = UserId)
        {
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([
                    new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                    new Claim(JwtRegisteredClaimNames.Name, "integration-test")
                ]),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = new SigningCredentials(
                    new RsaSecurityKey(rsa),
                    SecurityAlgorithms.RsaSha256)
            };
            return new JwtSecurityTokenHandler().WriteToken(
                new JwtSecurityTokenHandler().CreateToken(descriptor));
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Auth:VerificationKeyPath", publicKeyPath);
            builder.UseSetting("Services:Database", databaseServiceUrl);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:VerificationKeyPath"] = publicKeyPath,
                    ["Services:Database"] = databaseServiceUrl
                });
            });
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEmailSender>();
                services.AddSingleton(emailSender);
                services.RemoveAll<IEmailContentGenerator>();
                services.AddSingleton<IEmailContentGenerator, StubEmailGenerator>();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                rsa.Dispose();
                File.Delete(publicKeyPath);
            }
        }
    }

    private sealed class DisabledEmailSender : IEmailSender
    {
        public bool IsConfigured => false;

        public Task SendAsync(
            string recipient,
            EmailContent content,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public bool IsConfigured => true;
        public int SentCount { get; private set; }
        public EmailContent? LastContent { get; private set; }

        public Task SendAsync(
            string recipient,
            EmailContent content,
            CancellationToken cancellationToken = default)
        {
            SentCount++;
            LastContent = content;
            return Task.CompletedTask;
        }
    }

    private sealed class StubEmailGenerator : IEmailContentGenerator
    {
        public Task<EmailContent> GenerateAsync(
            EmailContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new EmailContent(
                "Course achievement progress",
                string.Join(", ", context.Achievements.Select(item => item.Description)),
                false));
    }
}