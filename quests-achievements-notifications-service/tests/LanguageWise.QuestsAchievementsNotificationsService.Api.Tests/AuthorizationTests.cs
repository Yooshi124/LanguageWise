using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Clients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

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
            Assert.That(notifications[0].GetProperty("eventId").GetString(), Is.EqualTo("event-12"));
            Assert.That(notifications[0].GetProperty("trigger").GetString(), Is.EqualTo("course-completion"));
            Assert.That(notifications[0].GetProperty("emailSubject").GetString(), Is.EqualTo("Course complete"));
            Assert.That(notifications[0].GetProperty("emailBody").GetString(), Is.EqualTo("You completed a course."));
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

    private sealed class ApiFixture : WebApplicationFactory<Program>
    {
        private readonly RSA rsa = RSA.Create(2048);
        private readonly string publicKeyPath = Path.GetTempFileName();

        internal ApiFixture()
        {
            File.WriteAllText(publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem());
        }

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
                services.RemoveAll<AppDataClient>();
                services.AddSingleton(new AppDataClient(new HttpClient(new ProfileDataHandler())
                {
                    BaseAddress = new Uri("http://database/")
                }));
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

    private sealed class ProfileDataHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.RequestUri?.AbsolutePath switch
            {
                "/user_preferences" => "[{\"user_id\":1,\"email\":\"learner@example.com\",\"notify_all\":true,\"notify_post_engagement\":true,\"notify_course_completion\":true,\"notify_quiz_results\":true,\"notify_streaks\":true,\"notify_achievements\":true}]",
                "/notifications" when request.RequestUri.Query.Contains("user_id=eq.1") => "[{\"notification_id\":12,\"event_id\":\"event-12\",\"user_id\":1,\"trigger\":\"course-completion\",\"time\":\"2026-08-29T10:00:00Z\",\"email_subject\":\"Course complete\",\"email_body\":\"You completed a course.\"}]",
                _ => "[]"
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
                RequestMessage = request
            });
        }
    }
}