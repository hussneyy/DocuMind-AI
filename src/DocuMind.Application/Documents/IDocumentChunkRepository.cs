using DocuMind.Domain.Entities;

namespace DocuMind.Application.Documents;

public interface IDocumentChunkRepository
{
    /// <summary>Deletes any existing chunks for the document and inserts the given ones — safe to call on retry.</summary>
    Task ReplaceAllForDocumentAsync(Guid documentId, IReadOnlyList<DocumentChunk> chunks, CancellationToken ct);

    Task<IReadOnlyList<DocumentChunk>> GetByDocumentIdAsync(Guid documentId, CancellationToken ct);
}
