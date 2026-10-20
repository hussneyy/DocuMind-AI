using DocuMind.Domain.Enums;

namespace DocuMind.Domain.Entities;

/// <summary>
/// A cached, generated summary of one document, one row per (DocumentId, SummaryType). Regenerated
/// (upserted) on request rather than automatically kept in sync with re-indexing, since generation
/// costs a real LLM call.
/// </summary>
public class DocumentSummary
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public SummaryType SummaryType { get; set; }
    public string Content { get; set; } = string.Empty;
    public int SourceChunkCount { get; set; }
    public string LlmModel { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; set; }
}
