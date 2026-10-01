using System.Security.Cryptography;
using System.Text;

namespace LanguageWise.McpServer.Security;

public sealed class ApiKeyMiddleware(RequestDelegate next, ApiKeyOptions options)
{
	private readonly byte[] expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(options.Key));

	public async Task InvokeAsync(HttpContext context)
	{
		if (!context.Request.Path.StartsWithSegments(options.ProtectedPath))
		{
			await next(context);
			return;
		}

		var supplied = context.Request.Headers[McpHeaders.ApiKey].ToString();
		var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(supplied));
		if (supplied.Length == 0 || !CryptographicOperations.FixedTimeEquals(suppliedHash, expectedHash))
		{
			context.Response.StatusCode = StatusCodes.Status401Unauthorized;
			return;
		}

		await next(context);
	}
}

public sealed record ApiKeyOptions(string Key, PathString ProtectedPath)
{
	public static ApiKeyOptions Load(IConfiguration configuration, string contentRoot, PathString protectedPath)
	{
		var key = configuration["Mcp:ApiKey"];
		if (string.IsNullOrWhiteSpace(key))
		{
			var path = Path.GetFullPath(configuration["Mcp:ApiKeyPath"] ?? "../../.mcp-api-key", contentRoot);
			if (!File.Exists(path))
			{
				throw new InvalidOperationException(
					$"MCP API key file not found at '{path}'. Run mcp-server/scripts/New-McpApiKey.ps1 first.");
			}
			key = File.ReadAllText(path);
		}

		key = key.Trim();
		if (key.Length < 32)
		{
			throw new InvalidOperationException("MCP API key must be at least 32 characters.");
		}
		return new ApiKeyOptions(key, protectedPath);
	}
}
