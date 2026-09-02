namespace LanguageWise.ChatDiscussionService.Db.Data;

/// <summary>
/// Holds the bytes of uploaded images on disk, beside the SQLite file. The Images
/// table is the index; this class only ever deals in storage keys.
/// </summary>
public sealed class ImageStore(string rootPath)
{
    private readonly string root = Path.GetFullPath(rootPath);

    /// <summary>
    /// An opaque name for one stored file. Keys carry no part of the caller's file
    /// name, so nothing an uploader chooses can shape the path that gets written.
    /// </summary>
    public static string NewKey() => Guid.NewGuid().ToString("N");

    /// <summary>
    /// Writes the file and reports how many bytes it holds. Measured rather than taken
    /// from Content-Length, which a chunked upload does not send at all.
    /// </summary>
    public async Task<long> SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(root);

        await using var file = File.Create(PathFor(storageKey));
        await content.CopyToAsync(file, cancellationToken);

        return file.Length;
    }

    public Stream? Open(string storageKey)
    {
        var path = PathFor(storageKey);
        return File.Exists(path) ? File.OpenRead(path) : null;
    }

    public void Delete(string storageKey) => File.Delete(PathFor(storageKey));

    public void DeleteAll(IEnumerable<string> storageKeys)
    {
        foreach (var storageKey in storageKeys)
        {
            Delete(storageKey);
        }
    }

    /// <summary>
    /// Keys come back from the database as plain text, so the resolved path is checked
    /// to be inside the store before any file operation touches it.
    /// </summary>
    private string PathFor(string storageKey)
    {
        var resolved = Path.GetFullPath(Path.Combine(root, storageKey));

        if (Path.GetDirectoryName(resolved) != root)
        {
            throw new ArgumentException($"'{storageKey}' is not a valid storage key.", nameof(storageKey));
        }

        return resolved;
    }
}
