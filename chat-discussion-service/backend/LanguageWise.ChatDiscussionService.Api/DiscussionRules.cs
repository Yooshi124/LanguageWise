using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LanguageWise.ChatDiscussionService.Api.Models;

namespace LanguageWise.ChatDiscussionService.Api;

/// <summary>
/// Validation and merge rules with no HTTP or database dependencies, so they can
/// be unit tested directly rather than through the request pipeline.
/// </summary>
internal static class DiscussionRules
{
    internal const int DefaultLimit = 20;
    internal const int MaxLimit = 100;

    /// <summary>
    /// The signed-in user's ID, or null when the caller is anonymous. Reading the
    /// raw 'sub' claim only works because the JWT bearer options set
    /// MapInboundClaims to false; otherwise it is renamed to NameIdentifier.
    /// </summary>
    internal static int? GetUserId(ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId) ? userId : null;

    internal static Dictionary<string, string[]> ValidateCreatePost(CreatePostRequest? request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request is null)
        {
            errors["body"] = ["A request body is required."];
            return errors;
        }

        Require(errors, "title", request.Title, "A title is required.");
        Require(errors, "content", request.Content, "Content is required.");
        Require(errors, "category", request.Category, "A category is required.");

        return errors;
    }

    internal static Dictionary<string, string[]> ValidatePatchPost(PatchPostRequest? request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request is null)
        {
            errors["body"] = ["A request body is required."];
            return errors;
        }

        if (request.Title is null && request.Content is null && request.Category is null)
        {
            errors["body"] = ["Supply at least one of title, content or category."];
            return errors;
        }

        RejectBlank(errors, "title", request.Title, "A title cannot be blank.");
        RejectBlank(errors, "content", request.Content, "Content cannot be blank.");
        RejectBlank(errors, "category", request.Category, "A category cannot be blank.");

        return errors;
    }

    internal static Dictionary<string, string[]> ValidateCreateComment(CreateCommentRequest? request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request is null)
        {
            errors["body"] = ["A request body is required."];
            return errors;
        }

        Require(errors, "content", request.Content, "Content is required.");

        return errors;
    }

    internal static Dictionary<string, string[]> ValidatePatchComment(PatchCommentRequest? request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request is null)
        {
            errors["body"] = ["A request body is required."];
            return errors;
        }

        if (request.Content is null)
        {
            errors["body"] = ["Supply content to update."];
            return errors;
        }

        RejectBlank(errors, "content", request.Content, "Content cannot be blank.");

        return errors;
    }

    internal static Dictionary<string, string[]> ValidatePaging(int limit, int offset)
    {
        var errors = new Dictionary<string, string[]>();

        if (limit is < 1 or > MaxLimit)
        {
            errors["limit"] = [$"Limit must be between 1 and {MaxLimit}."];
        }

        if (offset < 0)
        {
            errors["offset"] = ["Offset cannot be negative."];
        }

        return errors;
    }

    /// <summary>
    /// Folds a partial update over the post as it stands. The backend has already
    /// loaded the post to check ownership, so the merge happens here and the
    /// database service still receives a complete replacement.
    /// </summary>
    internal static (string Title, string Content, string Category) MergePost(
        PostSummary current,
        PatchPostRequest patch) =>
        (patch.Title?.Trim() ?? current.Title,
         patch.Content?.Trim() ?? current.Content,
         patch.Category?.Trim() ?? current.Category);

    internal static string MergeComment(Comment current, PatchCommentRequest patch) =>
        patch.Content?.Trim() ?? current.Content;

    private static void Require(
        Dictionary<string, string[]> errors,
        string key,
        string? value,
        string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[key] = [message];
        }
    }

    private static void RejectBlank(
        Dictionary<string, string[]> errors,
        string key,
        string? value,
        string message)
    {
        if (value is not null && string.IsNullOrWhiteSpace(value))
        {
            errors[key] = [message];
        }
    }
}
