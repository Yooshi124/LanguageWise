using LanguageWise.McpServer.Tools;

namespace LanguageWise.McpServer.Security;

public sealed class McpCallerContext(IHttpContextAccessor accessor, UserTokenValidator validator)
{
	private bool tokenChecked;
	private string? validatedToken;

	public string? Scope
	{
		get
		{
			var scope = accessor.HttpContext?.Request.Headers[McpHeaders.ToolScope].ToString().Trim();
			return ToolScopes.IsKnown(scope) ? scope : null;
		}
	}

	public async Task<string?> GetUserTokenAsync()
	{
		if (!tokenChecked)
		{
			var token = accessor.HttpContext?.Request.Headers[McpHeaders.UserToken].ToString().Trim();
			validatedToken = await validator.IsValidAsync(token) ? token : null;
			tokenChecked = true;
		}
		return validatedToken;
	}
}
