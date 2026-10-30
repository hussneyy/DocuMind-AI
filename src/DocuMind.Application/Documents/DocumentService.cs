using System.Security.Cryptography;
using DocuMind.Application.Auditing;
using DocuMind.Application.Common;
using DocuMind.Application.Common.Exceptions;
using DocuMind.Application.Tags;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocuMind.Application.Documents;

public class DocumentService(
    IDocumentRepository repository,
    IDocumentChunkRepository chunkRepository,
    ITagRepository tagRepository,
    IFileStorage fileStorage,
    IPdfInspector pdfInspector,
    IOptions<UploadOptions> options,
    ILogger<DocumentService> logger,
    IAuditLogger auditLogger) : IDocumentService
{
    private readonly UploadOptions _options = options.Value;

    public async Task<DocumentDto> UploadAsync(
        Guid ownerUserId, Stream fileContent, string originalFileName, long declaredLength, CancellationToken ct)
    {
        var metadataErrors = DocumentUploadValidator.ValidateMetadata(originalFileName, declaredLength, _options);
        if (metadataErrors.Count > 0)
        {
            throw new AppValidationException(metadataErrors);
        }

        await using var buffer = new MemoryStream();
        await fileContent.CopyToAsync(buffer, ct);

        if (buffer.Length == 0)
        {
            throw new AppValidationException(["The file is empty."]);
        }

        if (buffer.Length > _options.MaxFileSizeBytes)
        {
            var maxMb = _options.MaxFileSizeBytes / (1024.0 * 1024.0);
            throw new AppValidationException([$"The file exceeds the maximum allowed size of {maxMb:0.#} MB."]);
        }

        buffer.Position = 0;
        var header = new byte[Math.Min(8, buffer.Length)];
        var read = await buffer.ReadAsync(header, ct);
        buffer.Position = 0;

        if (read < header.Length || !DocumentUploadValidator.HasValidPdfSignature(header))
        {
            throw new AppValidationException(["The file is not a valid PDF."]);
        }

        var inspection = pdfInspector.Inspect(buffer);
        buffer.Position = 0;

        if (!inspection.IsValid)
        {
            logger.LogWarning("Rejected upload {FileName}: PDF failed to parse ({Error})", originalFileName, inspection.Error);
            throw new AppValidationException([inspection.Error ?? "The PDF could not be read. It may be corrupted or unsupported."]);
        }

        if (inspection.IsEncrypted)
        {
            throw new AppValidationException(["Encrypted PDFs are not supported. Please upload an unencrypted PDF."]);
        }

        if (inspection.PageCount is { } pageCount && pageCount > _options.MaxPageCount)
        {
            throw new AppValidationException(
                [$"The document has {pageCount} pages, exceeding the maximum of {_options.MaxPageCount}."]);
        }

        var hashBytes = await SHA256.HashDataAsync(buffer, ct);
        buffer.Position = 0;

        var storageKey = await fileStorage.SaveAsync(buffer, ct);

        var document = new Document
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            OriginalFileName = Path.GetFileName(originalFileName),
            StorageKey = storageKey,
            ContentType = "application/pdf",
            FileSizeBytes = buffer.Length,
            FileHash = Convert.ToHexString(hashBytes),
            PageCount = inspection.PageCount,
            ProcessingStatus = DocumentProcessingStatus.Queued,
            UploadedAt = DateTimeOffset.UtcNow,
        };

        await repository.AddAsync(document, ct);
        await repository.SaveChangesAsync(ct);

        logger.LogInformation("Document {DocumentId} uploaded by user {UserId} ({PageCount} pages), queued for indexing",
            document.Id, ownerUserId, document.PageCount);
        auditLogger.Log(AuditEventType.DocumentUploaded, ownerUserId, $"documentId={document.Id} fileName={document.OriginalFileName}");

        return ToDto(document, []);
    }

    public async Task<PagedResult<DocumentDto>> ListAsync(Guid ownerUserId, DocumentListQuery query, CancellationToken ct)
    {
        var (items, total) = await repository.ListAsync(ownerUserId, query, ct);
        var tagsByDocument = await tagRepository.GetTagsForDocumentsAsync(items.Select(d => d.Id).ToList(), ct);
        return new PagedResult<DocumentDto>
        {
            Items = items.Select(d => ToDto(d, tagsByDocument.GetValueOrDefault(d.Id, []))).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total,
        };
    }

    public async Task<DocumentDto> GetAsync(Guid ownerUserId, Guid documentId, CancellationToken ct)
    {
        var document = await repository.GetByIdAsync(ownerUserId, documentId, ct)
            ?? throw new NotFoundException("Document not found.");
        return ToDto(document, await GetTagsForDocumentAsync(documentId, ct));
    }

    public async Task DeleteAsync(Guid ownerUserId, Guid documentId, CancellationToken ct)
    {
        var document = await repository.GetByIdAsync(ownerUserId, documentId, ct)
            ?? throw new NotFoundException("Document not found.");

        await fileStorage.DeleteAsync(document.StorageKey, ct);
        repository.Remove(document);
        await repository.SaveChangesAsync(ct);

        auditLogger.Log(AuditEventType.DocumentDeleted, ownerUserId, $"documentId={documentId} fileName={document.OriginalFileName}");
    }

    public async Task<DocumentDto> RetryAsync(Guid ownerUserId, Guid documentId, CancellationToken ct)
    {
        var document = await repository.GetByIdAsync(ownerUserId, documentId, ct)
            ?? throw new NotFoundException("Document not found.");

        if (document.ProcessingStatus != DocumentProcessingStatus.Failed)
        {
            throw new AppValidationException(["Only documents in the Failed state can be retried."]);
        }

        document.ProcessingStatus = DocumentProcessingStatus.Queued;
        document.ProcessingError = null;
        await repository.SaveChangesAsync(ct);

        logger.LogInformation("Document {DocumentId} re-queued for indexing by user {UserId}", document.Id, ownerUserId);

        return ToDto(document, await GetTagsForDocumentAsync(documentId, ct));
    }

    public async Task<IReadOnlyList<DocumentChunkDto>> ListChunksAsync(Guid ownerUserId, Guid documentId, CancellationToken ct)
    {
        _ = await repository.GetByIdAsync(ownerUserId, documentId, ct)
            ?? throw new NotFoundException("Document not found.");

        var chunks = await chunkRepository.GetByDocumentIdAsync(documentId, ct);
        return chunks
            .OrderBy(c => c.ChunkIndex)
            .Select(c => new DocumentChunkDto(c.Id, c.ChunkIndex, c.TextContent, c.PageStart, c.PageEnd))
            .ToList();
    }

    public async Task<(Stream Content, string FileName, string ContentType)> OpenFileAsync(Guid ownerUserId, Guid documentId, CancellationToken ct)
    {
        var document = await repository.GetByIdAsync(ownerUserId, documentId, ct)
            ?? throw new NotFoundException("Document not found.");

        var stream = await fileStorage.OpenReadAsync(document.StorageKey, ct);
        return (stream, document.OriginalFileName, document.ContentType);
    }

    public async Task<DocumentDto> AttachTagAsync(Guid ownerUserId, Guid documentId, Guid tagId, CancellationToken ct)
    {
        var document = await repository.GetByIdAsync(ownerUserId, documentId, ct)
            ?? throw new NotFoundException("Document not found.");
        _ = await tagRepository.GetByIdAsync(ownerUserId, tagId, ct)
            ?? throw new NotFoundException("Tag not found.");

        if (!await tagRepository.IsAttachedAsync(documentId, tagId, ct))
        {
            await tagRepository.AttachAsync(documentId, tagId, ct);
            await tagRepository.SaveChangesAsync(ct);
        }

        return ToDto(document, await GetTagsForDocumentAsync(documentId, ct));
    }

    public async Task<DocumentDto> DetachTagAsync(Guid ownerUserId, Guid documentId, Guid tagId, CancellationToken ct)
    {
        var document = await repository.GetByIdAsync(ownerUserId, documentId, ct)
            ?? throw new NotFoundException("Document not found.");

        await tagRepository.DetachAsync(documentId, tagId, ct);
        await tagRepository.SaveChangesAsync(ct);

        return ToDto(document, await GetTagsForDocumentAsync(documentId, ct));
    }

    private async Task<IReadOnlyList<TagDto>> GetTagsForDocumentAsync(Guid documentId, CancellationToken ct)
    {
        var tagsByDocument = await tagRepository.GetTagsForDocumentsAsync([documentId], ct);
        return tagsByDocument.GetValueOrDefault(documentId, []);
    }

    private static DocumentDto ToDto(Document document, IReadOnlyList<TagDto> tags) => new(
        document.Id,
        document.OriginalFileName,
        document.ContentType,
        document.FileSizeBytes,
        document.PageCount,
        document.ProcessingStatus,
        document.ProcessingError,
        document.UploadedAt,
        document.IndexedAt,
        tags);
}
