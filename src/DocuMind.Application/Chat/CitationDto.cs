namespace DocuMind.Application.Chat;

public record CitationDto(
    Guid ChunkId,
    Guid DocumentId,
    string DocumentFileName,
    int PageStart,
    int PageEnd,
    string Excerpt);
