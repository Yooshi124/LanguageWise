namespace LanguageWise.ChatDiscussionService.Api.Tests;

/// <summary>
/// Test uploads. Only the leading signature is ever inspected — the backend checks
/// that a file is the format it claims to be and then passes the bytes straight on —
/// so these are headers followed by padding rather than decodable images.
/// </summary>
internal static class ImageBytes
{
    internal static byte[] Png() => WithPadding([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

    internal static byte[] Gif() => WithPadding("GIF89a"u8.ToArray());

    /// <summary>RIFF, then a length this test never has to make truthful, then WEBP.</summary>
    internal static byte[] Webp() =>
        WithPadding([.. "RIFF"u8.ToArray(), 0x00, 0x00, 0x00, 0x00, .. "WEBP"u8.ToArray()]);

    /// <summary>Bytes that match no image signature at all.</summary>
    internal static byte[] NotAnImage() => "<!doctype html><script>alert(1)</script>"u8.ToArray();

    private static byte[] WithPadding(byte[] signature) => [.. signature, .. new byte[32]];
}
