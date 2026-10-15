using DocuMind.Application.Common;

namespace DocuMind.Application.Documents;

public interface IDocumentService
{
    Task<DocumentDto> UploadAsync(
        Guid ownerUserId, Stream fileContent, string originalFileName, long declaredLength, CancellationToken ct);

    Task<PagedResult<DocumentDto>> ListAsync(Guid ownerUserId, DocumentListQuery query, CancellationToken ct);

    Task<DocumentDto> GetAsync(Guid ownerUserId, Guid documentId, CancellationToken ct);

    Task DeleteAsync(Guid ownerUserId, Guid documentId, CancellationToken ct);

    /// <summary>Re-queues a failed document for indexing. Throws if the document isn't currently Failed.</summary>
    Task<DocumentDto> RetryAsync(Guid ownerUserId, Guid documentId, CancellationToken ct);

    Task<IReadOnlyList<DocumentChunkDto>> ListChunksAsync(Guid ownerUserId, Guid documentId, CancellationToken ct);
}
