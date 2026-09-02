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

    internal const int CommentPreviewLimit = 20;

    internal static bool IsKnownForum(string? code, IReadOnlyCollection<Forum> forums) =>
        code is not null
        && forums.Any(forum => string.Equals(forum.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The signed-in user's ID, or null when the caller is anonymous. Reading the
    /// raw 'sub' claim only works because the JWT bearer options set
    /// MapInboundClaims to false; otherwise it is renamed to NameIdentifier.
    /// </summary>
    internal static int? GetUserId(ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId) ? userId : null;

    internal static string GetUserName(ClaimsPrincipal user) =>
        user.FindFirstValue(JwtRegisteredClaimNames.Name) ?? string.Empty;

    internal static Dictionary<string, string[]> ValidateCreatePost(
        CreatePostRequest? request,
        IReadOnlyCollection<Forum> forums)
    {
        var errors = new Dictionary<string, string[]>();

        if (request is null)
        {
            errors["body"] = ["A request body is required."];
            return errors;
        }

        Require(errors, "title", request.Title, "A title is required.");
        Require(errors, "content", request.Content, "Content is required.");
        Require(errors, "forumCode", request.ForumCode, "A forum is required.");

        if (!errors.ContainsKey("forumCode") && !IsKnownForum(request.ForumCode, forums))
        {
            errors["forumCode"] = [UnknownForumMessage(forums)];
        }

        return errors;
    }

    internal static Dictionary<string, string[]> ValidatePatchPost(
        PatchPostRequest? request,
        IReadOnlyCollection<Forum> forums)
    {
        var errors = new Dictionary<string, string[]>();

        if (request is null)
        {
            errors["body"] = ["A request body is required."];
            return errors;
        }

        if (request.Title is null && request.Content is null && request.ForumCode is null)
        {
            errors["body"] = ["Supply at least one of title, content or forumCode."];
            return errors;
        }

        RejectBlank(errors, "title", request.Title, "A title cannot be blank.");
        RejectBlank(errors, "content", request.Content, "Content cannot be blank.");
        RejectBlank(errors, "forumCode", request.ForumCode, "A forum cannot be blank.");

        if (request.ForumCode is not null
            && !errors.ContainsKey("forumCode")
            && !IsKnownForum(request.ForumCode, forums))
        {
            errors["forumCode"] = [UnknownForumMessage(forums)];
        }

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
    internal static (string Title, string Content, string ForumCode) MergePost(
        PostSummary current,
        PatchPostRequest patch) =>
        (patch.Title?.Trim() ?? current.Title,
         patch.Content?.Trim() ?? current.Content,
         patch.ForumCode?.Trim() ?? current.ForumCode);

    internal static string MergeComment(Comment current, PatchCommentRequest patch) =>
        patch.Content?.Trim() ?? current.Content;

    internal static string UnknownForumMessage(IReadOnlyCollection<Forum> forums) =>
        $"Unknown forum. Valid values are: {string.Join(", ", forums.Select(forum => forum.Code))}.";

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
