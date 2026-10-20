using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;

namespace DocuMind.Application.Summaries;

public interface ISummaryRepository
{
    Task<DocumentSummary?> GetAsync(Guid documentId, SummaryType summaryType, CancellationToken ct);

    /// <summary>Inserts or replaces the cached summary for this (DocumentId, SummaryType) pair.</summary>
    Task UpsertAsync(DocumentSummary summary, CancellationToken ct);
}
