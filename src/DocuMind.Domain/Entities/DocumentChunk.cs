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
}
