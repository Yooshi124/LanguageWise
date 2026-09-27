using System.Net;
using System.Net.Http.Headers;

namespace LanguageWise.Shared.Api.Clients;

/// <summary>
/// Talks to the database microservice over HTTP. The backend never opens the SQLite file
/// itself; the database service is the only owner of that file.
/// </summary>
public sealed class UsersClient(HttpClient httpClient)
{
    internal async Task<VerifyResponse> VerifyAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/users/verify", new { Username = username, Password = password }, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new VerifyResponse(false, 0);
        }

        var result = await response.Content.ReadFromJsonAsync<VerifyResponse>(cancellationToken: cancellationToken);
        return result ?? new VerifyResponse(false, 0);
    }

    internal async Task<int?> RecordLoginAsync(int userId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync($"api/users/{userId}/login-streak", null, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginStreakResponse>(cancellationToken: cancellationToken))?.Value;
    }

    internal async Task<ProfilePicture?> GetProfilePictureAsync(int userId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/users/{userId}/profile-picture", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProfilePicture>(cancellationToken);
    }

    // Buffered rather than streamed on; ImageRules.MaxBytes keeps that small.
    internal async Task<ImageContent?> DownloadProfilePictureAsync(int userId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/users/{userId}/profile-picture/content", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return new ImageContent(
            await response.Content.ReadAsByteArrayAsync(cancellationToken),
            response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
    }

    // Raw body: the browser's multipart form is already parsed and validated here.
    internal async Task<ProfilePicture?> UploadProfilePictureAsync(
        int userId,
        Stream content,
        string contentType,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        using var body = new StreamContent(content);
        body.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        using var response = await httpClient.PutAsync(
            $"api/users/{userId}/profile-picture?fileName={Uri.EscapeDataString(fileName)}",
            body,
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProfilePicture>(cancellationToken);
    }

    internal async Task<bool> DeleteProfilePictureAsync(int userId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"api/users/{userId}/profile-picture", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }
}

internal sealed record LoginStreakResponse(int Value);

internal sealed record ProfilePicture(
    string StorageKey,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime UploadedAt);

internal sealed record ImageContent(byte[] Bytes, string ContentType);
