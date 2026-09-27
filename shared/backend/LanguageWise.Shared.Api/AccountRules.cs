namespace LanguageWise.Shared.Api;

internal static class AccountRules
{
    internal const int MinUsernameLength = 3;
    internal const int MaxUsernameLength = 32;
    internal const int MinPasswordLength = 8;
    internal const int MaxPasswordLength = 128;

    internal static void ValidateUsername(string? username, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrEmpty(username))
        {
            errors["username"] = ["Enter a username."];
        }
        else if (username.Length is < MinUsernameLength or > MaxUsernameLength)
        {
            errors["username"] = [$"Usernames must be {MinUsernameLength}-{MaxUsernameLength} characters."];
        }
        else if (!username.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-'))
        {
            errors["username"] = ["Usernames may only contain letters, numbers, '.', '_' and '-'."];
        }
    }

    internal static void ValidatePassword(string? password, string field, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrEmpty(password))
        {
            errors[field] = ["Enter a password."];
        }
        else if (password.Length is < MinPasswordLength or > MaxPasswordLength)
        {
            errors[field] = [$"Passwords must be {MinPasswordLength}-{MaxPasswordLength} characters."];
        }
    }
}
