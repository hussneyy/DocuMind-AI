using DocuMind.Application.Documents;
using DocuMind.Application.Search;
using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

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

    public async Task<IReadOnlyList<ChunkSearchCandidate>> SemanticSearchAsync(
        Guid ownerUserId,
        float[] queryEmbedding,
        IReadOnlyList<Guid>? documentIds,
        int topK,
        double minSimilarity,
        CancellationToken ct)
    {
        var queryVector = new Vector(queryEmbedding);
        var maxDistance = 1.0 - minSimilarity;

        var baseQuery =
            from chunk in dbContext.DocumentChunks
            join document in dbContext.Documents on chunk.DocumentId equals document.Id
            where document.OwnerUserId == ownerUserId && chunk.Embedding != null
            select new { chunk, document.OriginalFileName };

        if (documentIds is { Count: > 0 })
        {
            baseQuery = baseQuery.Where(x => documentIds.Contains(x.chunk.DocumentId));
        }

        var ranked = baseQuery
            .Select(x => new
            {
                x.chunk,
                x.OriginalFileName,
                Distance = x.chunk.Embedding!.CosineDistance(queryVector),
            })
            .Where(x => x.Distance <= maxDistance)
            .OrderBy(x => x.Distance)
            .Take(topK);

        var rows = await ranked.ToListAsync(ct);

        return rows
            .Select(r => new ChunkSearchCandidate(
                r.chunk.Id, r.chunk.DocumentId, r.OriginalFileName, r.chunk.TextContent, r.chunk.PageStart, r.chunk.PageEnd, 1.0 - r.Distance))
            .ToList();
    }

    public async Task<IReadOnlyList<ChunkSearchCandidate>> KeywordSearchAsync(
        Guid ownerUserId, string query, IReadOnlyList<Guid>? documentIds, int topK, CancellationToken ct)
    {
        var baseQuery =
            from chunk in dbContext.DocumentChunks
            join document in dbContext.Documents on chunk.DocumentId equals document.Id
            where document.OwnerUserId == ownerUserId
            select new { chunk, document.OriginalFileName };

        if (documentIds is { Count: > 0 })
        {
            baseQuery = baseQuery.Where(x => documentIds.Contains(x.chunk.DocumentId));
        }

        // EF.Functions calls must appear inside the query expression itself to be translated to
        // SQL — hoisting EF.Functions.PlainToTsQuery(...) into a local variable first causes EF
        // Core to fall back to (unsupported) client evaluation and throw at runtime.
        var ranked = baseQuery
            .Where(x => EF.Functions.ToTsVector("english", x.chunk.TextContent)
                .Matches(EF.Functions.PlainToTsQuery("english", query)))
            .Select(x => new
            {
                x.chunk,
                x.OriginalFileName,
                Rank = EF.Functions.ToTsVector("english", x.chunk.TextContent)
                    .RankCoverDensity(EF.Functions.PlainToTsQuery("english", query)),
            })
            .OrderByDescending(x => x.Rank)
            .Take(topK);

        var rows = await ranked.ToListAsync(ct);

        return rows
            .Select(r => new ChunkSearchCandidate(
                r.chunk.Id, r.chunk.DocumentId, r.OriginalFileName, r.chunk.TextContent, r.chunk.PageStart, r.chunk.PageEnd, r.Rank))
            .ToList();
    }
}
