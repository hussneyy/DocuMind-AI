using DocuMind.Application.Documents;
using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Persistence;

public class DocumentChunkRepository(AppDbContext dbContext) : IDocumentChunkRepository
{
    public async Task ReplaceAllForDocumentAsync(Guid documentId, IReadOnlyList<DocumentChunk> chunks, CancellationToken ct)
    {
        await dbContext.DocumentChunks
            .Where(c => c.DocumentId == documentId)
            .ExecuteDeleteAsync(ct);

        await dbContext.DocumentChunks.AddRangeAsync(chunks, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DocumentChunk>> GetByDocumentIdAsync(Guid documentId, CancellationToken ct)
    {
        return await dbContext.DocumentChunks
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.ChunkIndex)
            .ToListAsync(ct);
    }
}
