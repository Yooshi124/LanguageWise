namespace LanguageWise.Shared.Db.Data;

public sealed class ImageStore(string rootPath)
{
    private readonly string root = Path.GetFullPath(rootPath);

    // Keys carry no part of the uploader's file name, so callers cannot shape the path written.
    public static string NewKey() => Guid.NewGuid().ToString("N");

    // Measured rather than taken from Content-Length, which a chunked upload does not send.
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
