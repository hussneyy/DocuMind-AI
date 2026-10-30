using DocuMind.Domain.Entities;

namespace DocuMind.Application.Tags;

public interface ITagRepository
{
    Task AddAsync(Tag tag, CancellationToken ct);

    /// <summary>Returns the tag only if it belongs to <paramref name="ownerUserId"/>; otherwise null.</summary>
    Task<Tag?> GetByIdAsync(Guid ownerUserId, Guid tagId, CancellationToken ct);

    Task<Tag?> GetByNameAsync(Guid ownerUserId, string name, CancellationToken ct);

    Task<IReadOnlyList<Tag>> ListAsync(Guid ownerUserId, CancellationToken ct);

    void Remove(Tag tag);

    Task<bool> IsAttachedAsync(Guid documentId, Guid tagId, CancellationToken ct);

    Task AttachAsync(Guid documentId, Guid tagId, CancellationToken ct);

    Task DetachAsync(Guid documentId, Guid tagId, CancellationToken ct);

    /// <summary>Batch-loads tags for several documents at once, keyed by document id.</summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<TagDto>>> GetTagsForDocumentsAsync(
        IReadOnlyList<Guid> documentIds, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
