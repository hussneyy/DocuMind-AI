namespace DocuMind.Application.Search;

public interface ISearchService
{
    Task<IReadOnlyList<SearchResultItem>> SearchAsync(Guid ownerUserId, SearchRequest request, CancellationToken ct);
}
