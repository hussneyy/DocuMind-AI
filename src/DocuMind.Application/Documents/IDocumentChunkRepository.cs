using DocuMind.Application.Search;
using DocuMind.Domain.Entities;

namespace DocuMind.Application.Documents;

public interface IDocumentChunkRepository
{
    /// <summary>Deletes any existing chunks for the document and inserts the given ones — safe to call on retry.</summary>
    Task ReplaceAllForDocumentAsync(Guid documentId, IReadOnlyList<DocumentChunk> chunks, CancellationToken ct);

    Task<IReadOnlyList<DocumentChunk>> GetByDocumentIdAsync(Guid documentId, CancellationToken ct);

    /// <summary>
    /// Vector similarity search, ordered by similarity descending (best first), restricted to
    /// <paramref name="ownerUserId"/>'s documents and optionally further restricted to
    /// <paramref name="documentIds"/>. Only chunks with a cosine similarity &gt;= <paramref name="minSimilarity"/> are returned.
    /// </summary>
    Task<IReadOnlyList<ChunkSearchCandidate>> SemanticSearchAsync(
        Guid ownerUserId,
        float[] queryEmbedding,
        IReadOnlyList<Guid>? documentIds,
        int topK,
        double minSimilarity,
        CancellationToken ct);

    /// <summary>Postgres full-text keyword search, ordered by rank descending (best first).</summary>
    Task<IReadOnlyList<ChunkSearchCandidate>> KeywordSearchAsync(
        Guid ownerUserId,
        string query,
        IReadOnlyList<Guid>? documentIds,
        int topK,
        CancellationToken ct);
}
