using DocuMind.Application.Tags;
using DocuMind.Domain.Enums;

namespace DocuMind.Application.Documents;

public record DocumentDto(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    int? PageCount,
    DocumentProcessingStatus ProcessingStatus,
    string? ProcessingError,
    DateTimeOffset UploadedAt,
    DateTimeOffset? IndexedAt,
    IReadOnlyList<TagDto> Tags);
