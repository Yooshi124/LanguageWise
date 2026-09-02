using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
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
