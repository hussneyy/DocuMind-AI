namespace DocuMind.Application.Search;

/// <summary>A chunk match returned by the repository, already ranked (best first) within its own search mode.</summary>
public record ChunkSearchCandidate(
    Guid ChunkId,
    Guid DocumentId,
    string DocumentFileName,
    string TextContent,
    int PageStart,
    int PageEnd,
    double Score);
