namespace DocuMind.Application.Search;

public interface ISearchService
{
    Task<IReadOnlyList<SearchResultItem>> SearchAsync(Guid ownerUserId, SearchRequest request, CancellationToken ct);

    /// <summary>
    /// Same retrieval and ranking as <see cref="SearchAsync"/> (embedding, semantic/keyword/hybrid
    /// fusion, the minimum-similarity floor, ownership scoping) but returns the full chunk text
    /// rather than a truncated excerpt — used by RAG answer generation, which needs the complete
    /// passage rather than a UI-sized snippet.
    /// </summary>
    Task<IReadOnlyList<ChunkSearchCandidate>> RetrieveAsync(Guid ownerUserId, SearchRequest request, CancellationToken ct);
}
