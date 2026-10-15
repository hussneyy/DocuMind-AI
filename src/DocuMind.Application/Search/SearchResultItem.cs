namespace DocuMind.Application.Search;

public record SearchResultItem(
    Guid ChunkId,
    Guid DocumentId,
    string DocumentFileName,
    string Excerpt,
    int PageStart,
    int PageEnd,
    double Score);
