using DocuMind.Application.Summaries;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Persistence;

public class SummaryRepository(AppDbContext dbContext) : ISummaryRepository
{
    public Task<DocumentSummary?> GetAsync(Guid documentId, SummaryType summaryType, CancellationToken ct) =>
        dbContext.DocumentSummaries
            .FirstOrDefaultAsync(s => s.DocumentId == documentId && s.SummaryType == summaryType, ct);

    public async Task UpsertAsync(DocumentSummary summary, CancellationToken ct)
    {
        var existing = await dbContext.DocumentSummaries
            .FirstOrDefaultAsync(s => s.DocumentId == summary.DocumentId && s.SummaryType == summary.SummaryType, ct);

        if (existing is null)
        {
            dbContext.DocumentSummaries.Add(summary);
        }
        else
        {
            existing.Content = summary.Content;
            existing.SourceChunkCount = summary.SourceChunkCount;
            existing.LlmModel = summary.LlmModel;
            existing.GeneratedAt = summary.GeneratedAt;
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
