namespace DocuMind.Domain.Entities;

/// <summary>
/// A citation attached to an assistant <see cref="Message"/>. Denormalized (document file name,
/// page range, excerpt are copied at answer time rather than joined later) so a citation still
/// makes sense in conversation history even if the source document is later deleted or re-indexed.
/// </summary>
public class MessageCitation
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Guid ChunkId { get; set; }
    public Guid DocumentId { get; set; }
    public string DocumentFileName { get; set; } = string.Empty;
    public int PageStart { get; set; }
    public int PageEnd { get; set; }
    public string Excerpt { get; set; } = string.Empty;
}
