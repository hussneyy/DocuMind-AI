using DocuMind.Application.Auditing;
using DocuMind.Application.Common.Exceptions;
using DocuMind.Domain.Entities;

namespace DocuMind.Application.Tags;

public class TagService(ITagRepository repository, IAuditLogger auditLogger) : ITagService
{
    public async Task<IReadOnlyList<TagDto>> ListAsync(Guid ownerUserId, CancellationToken ct)
    {
        var tags = await repository.ListAsync(ownerUserId, ct);
        return tags.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).Select(ToDto).ToList();
    }

    public async Task<TagDto> CreateAsync(Guid ownerUserId, string name, CancellationToken ct)
    {
        var trimmedName = name.Trim();
        if (trimmedName.Length == 0)
        {
            throw new AppValidationException(["Tag name is required."]);
        }

        if (trimmedName.Length > 50)
        {
            throw new AppValidationException(["Tag name must be 50 characters or fewer."]);
        }

        var existing = await repository.GetByNameAsync(ownerUserId, trimmedName, ct);
        if (existing is not null)
        {
            return ToDto(existing);
        }

        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Name = trimmedName,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await repository.AddAsync(tag, ct);
        await repository.SaveChangesAsync(ct);

        auditLogger.Log(AuditEventType.TagCreated, ownerUserId, $"tagId={tag.Id} name={tag.Name}");

        return ToDto(tag);
    }

    public async Task DeleteAsync(Guid ownerUserId, Guid tagId, CancellationToken ct)
    {
        var tag = await repository.GetByIdAsync(ownerUserId, tagId, ct)
            ?? throw new NotFoundException("Tag not found.");

        repository.Remove(tag);
        await repository.SaveChangesAsync(ct);

        auditLogger.Log(AuditEventType.TagDeleted, ownerUserId, $"tagId={tagId} name={tag.Name}");
    }

    private static TagDto ToDto(Tag tag) => new(tag.Id, tag.Name);
}
