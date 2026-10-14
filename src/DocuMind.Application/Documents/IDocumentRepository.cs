using DocuMind.Domain.Entities;

namespace DocuMind.Application.Documents;

public interface IDocumentRepository
{
    Task AddAsync(Document document, CancellationToken ct);

    /// <summary>Returns the document only if it belongs to <paramref name="ownerUserId"/>; otherwise null.</summary>
    Task<Document?> GetByIdAsync(Guid ownerUserId, Guid documentId, CancellationToken ct);

    Task<(IReadOnlyList<Document> Items, int TotalCount)> ListAsync(
        Guid ownerUserId, DocumentListQuery query, CancellationToken ct);

    void Remove(Document document);

    Task SaveChangesAsync(CancellationToken ct);
}
