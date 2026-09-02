namespace LanguageWise.ChatDiscussionService.Api;

/// <summary>
/// What counts as an acceptable image upload. Kept free of HTTP and file system
/// dependencies so the limits can be unit tested directly.
/// </summary>
internal static class ImageRules
{
    internal const long MaxBytes = 5 * 1024 * 1024;

    internal const int MaxPerPost = 6;

    internal const int MaxFileNameLength = 120;

    private const string FallbackFileName = "image";

    /// <summary>
    /// SVG is excluded on purpose: it is a document that can carry script, and it
    /// would be served back from this service's own origin.
    /// </summary>
    internal static readonly IReadOnlyList<string> AllowedContentTypes =
    [
        "image/png",
        "image/jpeg",
        "image/gif",
        "image/webp"
    ];

    internal static Dictionary<string, string[]> ValidateUpload(
        string? contentType,
        long sizeBytes,
        int existingCount)
    {
        var errors = new Dictionary<string, string[]>();

        if (sizeBytes <= 0)
        {
            errors["file"] = ["Choose an image to upload."];
            return errors;
        }

        if (sizeBytes > MaxBytes)
        {
            errors["file"] = [$"An image must be {MaxBytes / (1024 * 1024)} MB or smaller."];
        }

        if (!IsAllowedContentType(contentType))
        {
            errors["contentType"] = [$"Images must be one of: {string.Join(", ", AllowedContentTypes)}."];
        }

        if (existingCount >= MaxPerPost)
        {
            errors["images"] = [$"A post can have at most {MaxPerPost} images."];
        }

        return errors;
    }

    internal static bool IsAllowedContentType(string? contentType) =>
        contentType is not null
        && AllowedContentTypes.Contains(Normalise(contentType), StringComparer.OrdinalIgnoreCase);

    /// <summary>Strips parameters and casing, so the stored type is exactly what is served back.</summary>
    internal static string Normalise(string contentType)
    {
        var separator = contentType.IndexOf(';');
        var mediaType = separator < 0 ? contentType : contentType[..separator];
        return mediaType.Trim().ToLowerInvariant();
    }

    /// <summary>The file name is kept only as a label, so any directory part is dropped.</summary>
    internal static string SafeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return FallbackFileName;
        }

        var trimmed = fileName.Trim();
        var lastSeparator = trimmed.LastIndexOfAny(['/', '\\']);
        var leaf = lastSeparator < 0 ? trimmed : trimmed[(lastSeparator + 1)..];
        var cleaned = new string(leaf.Where(character => !char.IsControl(character)).ToArray()).Trim();

        if (cleaned.Length == 0)
        {
            return FallbackFileName;
        }

        return cleaned.Length <= MaxFileNameLength ? cleaned : cleaned[..MaxFileNameLength];
    }

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];

    /// <summary>
    /// A browser's content type is only a claim, and this service serves the stored type
    /// straight back, so the bytes are checked to agree with it before anything is written.
    /// </summary>
    internal static bool MatchesContentType(string contentType, ReadOnlySpan<byte> header) =>
        Normalise(contentType) switch
        {
            "image/png" => header.StartsWith(PngSignature),
            "image/jpeg" => header.StartsWith(JpegSignature),
            "image/gif" => header.StartsWith("GIF87a"u8) || header.StartsWith("GIF89a"u8),
            // RIFF....WEBP: the four bytes in between are the file length.
            "image/webp" => header.Length >= 12 && header.StartsWith("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8),
            _ => false
        };

    /// <summary>Enough bytes for the longest signature above.</summary>
    internal const int SignatureLength = 12;
}
