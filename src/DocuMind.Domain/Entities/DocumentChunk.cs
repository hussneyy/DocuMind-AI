using Pgvector;

namespace DocuMind.Domain.Entities;

/// <summary>
/// A contiguous slice of a document's extracted text, sized for retrieval. A chunk can span more
/// than one source page, so citations resolve against <see cref="PageStart"/>/<see cref="PageEnd"/>
/// rather than a single page number.
/// </summary>
public class DocumentChunk
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int ChunkIndex { get; set; }
    public string TextContent { get; set; } = string.Empty;
    public int PageStart { get; set; }
    public int PageEnd { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Null until the background indexer's embedding step completes for this chunk.</summary>
    public Vector? Embedding { get; set; }

    /// <summary>
    /// Identifier of the embedding model/provider that produced <see cref="Embedding"/> (e.g.
    /// "mock-bow-v1", "text-embedding-3-small"). Lets the app detect stale embeddings if the
    /// configured model ever changes.
    /// </summary>
    public string? EmbeddingModel { get; set; }
}
