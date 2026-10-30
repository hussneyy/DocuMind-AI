using DocuMind.Application.Tags;
using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Persistence;

public class TagRepository(AppDbContext dbContext) : ITagRepository
{
    public async Task AddAsync(Tag tag, CancellationToken ct)
    {
        await dbContext.Tags.AddAsync(tag, ct);
    }

    public async Task<Tag?> GetByIdAsync(Guid ownerUserId, Guid tagId, CancellationToken ct)
    {
        return await dbContext.Tags
            .FirstOrDefaultAsync(t => t.Id == tagId && t.OwnerUserId == ownerUserId, ct);
    }

    public async Task<Tag?> GetByNameAsync(Guid ownerUserId, string name, CancellationToken ct)
    {
        return await dbContext.Tags
            .FirstOrDefaultAsync(t => t.OwnerUserId == ownerUserId && t.Name == name, ct);
    }

    public async Task<IReadOnlyList<Tag>> ListAsync(Guid ownerUserId, CancellationToken ct)
    {
        return await dbContext.Tags.Where(t => t.OwnerUserId == ownerUserId).ToListAsync(ct);
    }

    public void Remove(Tag tag)
    {
        dbContext.Tags.Remove(tag);
    }

    public async Task<bool> IsAttachedAsync(Guid documentId, Guid tagId, CancellationToken ct)
    {
        return await dbContext.DocumentTags
            .AnyAsync(dt => dt.DocumentId == documentId && dt.TagId == tagId, ct);
    }

    public async Task AttachAsync(Guid documentId, Guid tagId, CancellationToken ct)
    {
        await dbContext.DocumentTags.AddAsync(new DocumentTag { DocumentId = documentId, TagId = tagId }, ct);
    }

    public async Task DetachAsync(Guid documentId, Guid tagId, CancellationToken ct)
    {
        var link = await dbContext.DocumentTags
            .FirstOrDefaultAsync(dt => dt.DocumentId == documentId && dt.TagId == tagId, ct);
        if (link is not null)
        {
            dbContext.DocumentTags.Remove(link);
        }
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<TagDto>>> GetTagsForDocumentsAsync(
        IReadOnlyList<Guid> documentIds, CancellationToken ct)
    {
        if (documentIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<TagDto>>();
        }

        var rows = await dbContext.DocumentTags
            .Where(dt => documentIds.Contains(dt.DocumentId))
            .Join(dbContext.Tags, dt => dt.TagId, t => t.Id, (dt, t) => new { dt.DocumentId, Tag = new TagDto(t.Id, t.Name) })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.DocumentId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<TagDto>)g.Select(r => r.Tag).OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList());
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await dbContext.SaveChangesAsync(ct);
    }
}
