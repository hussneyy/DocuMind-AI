using DocuMind.Domain.Enums;

namespace DocuMind.Application.Summaries;

public interface ISummaryService
{
    /// <summary>
    /// Returns the cached summary if one exists and <paramref name="regenerate"/> is false;
    /// otherwise generates, persists, and returns a fresh one. Throws <see cref="Common.Exceptions.AppValidationException"/>
    /// if the document hasn't finished indexing yet (there would be nothing grounded to summarize).
    /// </summary>
    Task<SummaryDto> GetOrGenerateAsync(Guid ownerUserId, Guid documentId, SummaryType summaryType, bool regenerate, CancellationToken ct);
}
