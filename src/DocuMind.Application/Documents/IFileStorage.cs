namespace DocuMind.Application.Documents;

public interface IFileStorage
{
    /// <summary>Persists the stream under a new internally-generated key and returns that key.</summary>
    Task<string> SaveAsync(Stream content, string fileExtension, CancellationToken ct);

    Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct);

    Task DeleteAsync(string storageKey, CancellationToken ct);
}
