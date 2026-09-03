using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Clients;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Tests;

public sealed class AuthorizationTests
{
    [Test]
    public async Task Health_AllowsAnonymousRequests()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Profile_WithoutToken_ReturnsUnauthorized()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/profile");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Profile_WithBearerToken_ReturnsProfile()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fixture.CreateToken());

        var response = await client.GetAsync("/api/profile");

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var notifications = body.RootElement.GetProperty("notifications");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(notifications.GetArrayLength(), Is.EqualTo(1));
            Assert.That(notifications[0].GetProperty("notificationId").GetInt64(), Is.EqualTo(12));
            Assert.That(notifications[0].GetProperty("trigger").GetString(), Is.EqualTo("lesson-completion"));
            Assert.That(notifications[0].GetProperty("emailSubject").GetString(), Is.EqualTo("Lesson complete"));
            Assert.That(notifications[0].GetProperty("emailBody").GetString(), Is.EqualTo("You completed a lesson."));
            Assert.That(notifications[0].TryGetProperty("userId", out _), Is.False);
        });
    }

    [Test]
    public async Task Profile_WithTokenCookie_ReturnsProfile()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("Cookie", $"token={fixture.CreateToken()}");

        var response = await client.GetAsync("/api/profile");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Preferences_WithMalformedJson_ReturnsBadRequest()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fixture.CreateToken());
        using var content = new StringContent("{", Encoding.UTF8, "application/json");

        var response = await client.PutAsync("/api/preferences", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Preferences_WhenAllNotificationsAreEnabled_StoresAndSendsWelcome()
    {
        using var fixture = new ApiFixture(notifyAll: false);
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fixture.CreateToken());

        var response = await client.PutAsJsonAsync("/api/preferences", EnabledPreferences());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(fixture.DataHandler.NotificationBodies, Has.Count.EqualTo(1));
        Assert.That(fixture.DataHandler.NotificationBodies[0], Does.Contain("\"trigger\":\"notifications-enabled\""));
        await fixture.EmailGenerator.Received(1).GenerateAsync(
            Arg.Is<EmailContext>(context =>
                context.UserName == "amber"
                && context.IsNotificationsWelcome
                && context.Achievements.Count == 0),
            Arg.Any<CancellationToken>());
        await fixture.EmailSender.Received(1).SendAsync(
            "learner@example.com",
            Arg.Any<EmailContent>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Preferences_WhenAllNotificationsAreAlreadyEnabled_DoesNotSendWelcomeAgain()
    {
        using var fixture = new ApiFixture(notifyAll: true);
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fixture.CreateToken());

        var response = await client.PutAsJsonAsync("/api/preferences", EnabledPreferences());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(fixture.DataHandler.NotificationBodies, Is.Empty);
        await fixture.EmailGenerator.DidNotReceiveWithAnyArgs().GenerateAsync(default!, default);
        await fixture.EmailSender.DidNotReceiveWithAnyArgs().SendAsync(default!, default!, default);
    }

    private static PreferenceUpdateRequest EnabledPreferences() => new(
        "learner@example.com", true, true, true, true, true, true);

    private sealed class ApiFixture : WebApplicationFactory<Program>
    {
        private readonly RSA rsa = RSA.Create(2048);
        private readonly string publicKeyPath = Path.GetTempFileName();
        private readonly bool notifyAll;

        internal ApiFixture(bool notifyAll = true)
        {
            this.notifyAll = notifyAll;
            File.WriteAllText(publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem());
            EmailGenerator.GenerateAsync(Arg.Any<EmailContext>(), Arg.Any<CancellationToken>())
                .Returns(new EmailContent("Welcome", "Welcome body", false));
            EmailSender.IsConfigured.Returns(true);
        }

        internal ProfileDataHandler DataHandler { get; private set; } = null!;
        internal IEmailContentGenerator EmailGenerator { get; } = Substitute.For<IEmailContentGenerator>();
        internal IEmailSender EmailSender { get; } = Substitute.For<IEmailSender>();

        internal string CreateToken()
        {
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([
                    new Claim(JwtRegisteredClaimNames.Sub, "1"),
                    new Claim(JwtRegisteredClaimNames.Name, "amber")
                ]),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256)
            };
            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(handler.CreateToken(descriptor));
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Auth:VerificationKeyPath", publicKeyPath);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:VerificationKeyPath"] = publicKeyPath
                });
            });
            builder.ConfigureServices(services =>
            {
                DataHandler = new ProfileDataHandler(notifyAll);
                services.RemoveAll<AppDataClient>();
                services.AddSingleton(new AppDataClient(new HttpClient(DataHandler)
                {
                    BaseAddress = new Uri("http://database/")
                }));
                services.RemoveAll<IEmailContentGenerator>();
                services.AddSingleton(EmailGenerator);
                services.RemoveAll<IEmailSender>();
                services.AddSingleton(EmailSender);
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

    private sealed class ProfileDataHandler(bool notifyAll) : HttpMessageHandler
    {
        internal List<string> NotificationBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Post
                && request.RequestUri?.AbsolutePath == "/notifications")
            {
                NotificationBodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            }

            var body = request.RequestUri?.AbsolutePath switch
            {
                "/user_preferences" when request.Method == HttpMethod.Get => $"[{{\"user_id\":1,\"email\":\"learner@example.com\",\"notify_all\":{notifyAll.ToString().ToLowerInvariant()},\"notify_post_engagement\":true,\"notify_course_completion\":true,\"notify_quiz_results\":true,\"notify_streaks\":true,\"notify_achievements\":true}}]",
                "/notifications" when request.RequestUri.Query.Contains("user_id=eq.1") => "[{\"notification_id\":12,\"user_id\":1,\"trigger\":\"lesson-completion\",\"time\":\"2026-08-29T10:00:00Z\",\"email_subject\":\"Lesson complete\",\"email_body\":\"You completed a lesson.\"}]",
                _ => "[]"
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
                RequestMessage = request
            };
        }
    }
}