using DocuMind.Domain.Enums;

namespace DocuMind.Domain.Entities;

/// <summary>
/// A PDF document uploaded by a user. Text extraction and chunking (Milestone 3) populate
/// <see cref="DocumentChunk"/> records separately; this entity tracks ownership, storage,
/// and processing status only.
/// </summary>
public class Document
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>Internal storage key (never derived from user input) used to locate the file on disk.</summary>
    public string StorageKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    /// <summary>SHA-256 hash of the file content, for integrity checks and future dedup.</summary>
    public string FileHash { get; set; } = string.Empty;
    public int? PageCount { get; set; }
    public DocumentProcessingStatus ProcessingStatus { get; set; } = DocumentProcessingStatus.Uploaded;
    public string? ProcessingError { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public DateTimeOffset? IndexedAt { get; set; }
}
