using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LanguageWise.McpServer.Security;

public sealed class UserTokenValidator
{
	private readonly JsonWebTokenHandler handler = new() { MapInboundClaims = false };
	private readonly TokenValidationParameters parameters;

	public UserTokenValidator(RSA publicKey)
	{
		parameters = new TokenValidationParameters
		{
			ValidateIssuer = false,
			ValidateAudience = false,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			IssuerSigningKey = new RsaSecurityKey(publicKey),
			ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
			ClockSkew = TimeSpan.Zero
		};
	}

	public static UserTokenValidator FromConfiguration(IConfiguration configuration, string contentRoot)
	{
		var path = Path.GetFullPath(configuration["Auth:VerificationKeyPath"] ?? "../../../signing_public_key.pem", contentRoot);
		var rsa = RSA.Create();
		rsa.ImportFromPem(File.ReadAllText(path));
		return new UserTokenValidator(rsa);
	}

	public async Task<bool> IsValidAsync(string? token)
	{
		if (string.IsNullOrWhiteSpace(token) || token.Length > 8192)
		{
			return false;
		}
		var result = await handler.ValidateTokenAsync(token, parameters);
		return result.IsValid;
	}
}
