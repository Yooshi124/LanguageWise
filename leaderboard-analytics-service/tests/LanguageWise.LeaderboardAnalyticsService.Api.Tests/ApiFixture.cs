using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using LanguageWise.LeaderboardAnalyticsService.Api.Clients;
using LanguageWise.LeaderboardAnalyticsService.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace LanguageWise.LeaderboardAnalyticsService.Api.Tests;

internal sealed class ApiFixture : WebApplicationFactory<Program>
{
    private readonly RSA rsa = RSA.Create(2048);
    private readonly string publicKeyPath = Path.Combine(
        AppContext.BaseDirectory,
        $"auth-test-key-{Guid.NewGuid():N}.pem");

    internal ApiFixture()
    {
        File.WriteAllText(publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem());
    }

    internal ISummaryGenerator SummaryGenerator { get; set; } = new FakeSummaryGenerator();
    internal HttpMessageHandler? QuizzesCoursesHandler { get; set; }

    internal string CreateToken(int userId = 7, string username = "justin")
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(5);
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, username)
            ]),
            NotBefore = expiresAt.AddMinutes(-5),
            IssuedAt = expiresAt.AddMinutes(-5),
            Expires = expiresAt,
            SigningCredentials =
                new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256)
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
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ISummaryGenerator>();
            services.AddSingleton(SummaryGenerator);
            if (QuizzesCoursesHandler is not null)
            {
                services.RemoveAll<QuizzesCoursesClient>();
                var httpClient = new HttpClient(QuizzesCoursesHandler)
                {
                    BaseAddress = new Uri("http://quizzes-courses/")
                };
                services.AddSingleton(new QuizzesCoursesClient(
                    httpClient,
                    new MemoryCache(new MemoryCacheOptions())));
            }
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

internal sealed class FakeSummaryGenerator : ISummaryGenerator
{
    public LessonsCompletedResponse? LastChartData { get; private set; }

    public LessonsCompletedSummaryResponse Response { get; set; } =
        new("You made steady progress across your courses.", "up", "German");

    public Task<LessonsCompletedSummaryResponse> GenerateAsync(
        LessonsCompletedResponse chartData,
        CancellationToken cancellationToken = default)
    {
        LastChartData = chartData;
        return Task.FromResult(Response);
    }
}
