using DocuMind.Application.Documents;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Persistence;

public class DocumentRepository(AppDbContext dbContext) : IDocumentRepository
{
    public async Task AddAsync(Document document, CancellationToken ct)
    {
        await dbContext.Documents.AddAsync(document, ct);
    }

    public async Task<Document?> GetByIdAsync(Guid ownerUserId, Guid documentId, CancellationToken ct)
    {
        return await dbContext.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId && d.OwnerUserId == ownerUserId, ct);
    }

    public async Task<Document?> GetByIdForProcessingAsync(Guid documentId, CancellationToken ct)
    {
        return await dbContext.Documents.FirstOrDefaultAsync(d => d.Id == documentId, ct);
    }

    public async Task<IReadOnlyList<Document>> GetQueuedForProcessingAsync(int batchSize, CancellationToken ct)
    {
        return await dbContext.Documents
            .Where(d => d.ProcessingStatus == DocumentProcessingStatus.Queued)
            .OrderBy(d => d.UploadedAt)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public async Task<(IReadOnlyList<Document> Items, int TotalCount)> ListAsync(
        Guid ownerUserId, DocumentListQuery query, CancellationToken ct)
    {
        var queryable = dbContext.Documents.Where(d => d.OwnerUserId == ownerUserId);

        if (!string.IsNullOrWhiteSpace(query.FileNameSearch))
        {
            queryable = queryable.Where(d => EF.Functions.ILike(d.OriginalFileName, $"%{query.FileNameSearch}%"));
        }

        if (query.StatusFilter is { } status)
        {
            queryable = queryable.Where(d => d.ProcessingStatus == status);
        }

        if (query.TagId is { } tagId)
        {
            queryable = queryable.Where(d => dbContext.DocumentTags.Any(dt => dt.DocumentId == d.Id && dt.TagId == tagId));
        }

        queryable = (query.SortBy, query.SortDescending) switch
        {
            (DocumentSortField.Name, true) => queryable.OrderByDescending(d => d.OriginalFileName),
            (DocumentSortField.Name, false) => queryable.OrderBy(d => d.OriginalFileName),
            (DocumentSortField.UploadedAt, false) => queryable.OrderBy(d => d.UploadedAt),
            _ => queryable.OrderByDescending(d => d.UploadedAt),
        };

        var totalCount = await queryable.CountAsync(ct);

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var items = await queryable
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Document>> GetAllForOwnerAsync(Guid ownerUserId, CancellationToken ct)
    {
        return await dbContext.Documents.Where(d => d.OwnerUserId == ownerUserId).ToListAsync(ct);
    }

    public void Remove(Document document)
    {
        dbContext.Documents.Remove(document);
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await dbContext.SaveChangesAsync(ct);
    }
}
