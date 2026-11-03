using DocuMind.Domain.Entities;

namespace DocuMind.Application.Documents;

public interface IDocumentRepository
{
    Task AddAsync(Document document, CancellationToken ct);

    /// <summary>Returns the document only if it belongs to <paramref name="ownerUserId"/>; otherwise null.</summary>
    Task<Document?> GetByIdAsync(Guid ownerUserId, Guid documentId, CancellationToken ct);

    /// <summary>
    /// Returns the document regardless of owner. For internal background processing only — never
    /// call this on behalf of a user request.
    /// </summary>
    Task<Document?> GetByIdForProcessingAsync(Guid documentId, CancellationToken ct);

    /// <summary>Documents queued for indexing, oldest first, for the background indexer to claim.</summary>
    Task<IReadOnlyList<Document>> GetQueuedForProcessingAsync(int batchSize, CancellationToken ct);

    Task<(IReadOnlyList<Document> Items, int TotalCount)> ListAsync(
        Guid ownerUserId, DocumentListQuery query, CancellationToken ct);

    /// <summary>All of a user's documents, unpaginated — for account deletion cleanup, never for a listing UI.</summary>
    Task<IReadOnlyList<Document>> GetAllForOwnerAsync(Guid ownerUserId, CancellationToken ct);

    void Remove(Document document);

    Task SaveChangesAsync(CancellationToken ct);
}
