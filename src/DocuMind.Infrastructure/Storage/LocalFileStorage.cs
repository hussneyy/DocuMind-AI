using DocuMind.Application.Documents;
using Microsoft.Extensions.Options;

namespace DocuMind.Infrastructure.Storage;

/// <summary>
/// Stores files on the local filesystem under a private, non-web-accessible directory.
/// Storage keys are server-generated GUIDs — never derived from user-supplied file names — so
/// there is no path-traversal surface from untrusted input.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly string _rootDirectory;

    public LocalFileStorage(IOptions<StorageOptions> options)
    {
        _rootDirectory = Path.GetFullPath(options.Value.Directory);
        System.IO.Directory.CreateDirectory(_rootDirectory);
    }

    public async Task<string> SaveAsync(Stream content, string fileExtension, CancellationToken ct)
    {
        var storageKey = $"{Guid.NewGuid():N}{fileExtension}";
        var path = ResolvePath(storageKey);

        await using var fileStream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fileStream, ct);

        return storageKey;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct)
    {
        var path = ResolvePath(storageKey);
        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct)
    {
        var path = ResolvePath(storageKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        var fileName = Path.GetFileName(storageKey);
        var fullPath = Path.GetFullPath(Path.Combine(_rootDirectory, fileName));

        if (!fullPath.StartsWith(_rootDirectory, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Resolved storage path escapes the storage root.");
        }

        return fullPath;
    }
}
