namespace LanguageWise.QuizzesCoursesService.Api.Clients;

public static class UserTokenReader
{
	public static string? Read(HttpRequest request)
	{
		var authorization = request.Headers.Authorization.ToString();
		return authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
			? authorization["Bearer ".Length..].Trim()
			: request.Cookies["token"];
	}
}
