namespace DocuMind.Application.Tags;

public interface ITagService
{
    Task<IReadOnlyList<TagDto>> ListAsync(Guid ownerUserId, CancellationToken ct);

    Task<TagDto> CreateAsync(Guid ownerUserId, string name, CancellationToken ct);

    Task DeleteAsync(Guid ownerUserId, Guid tagId, CancellationToken ct);
}
