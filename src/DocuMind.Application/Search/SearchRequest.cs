namespace DocuMind.Application.Search;

public record SearchRequest(string Query, IReadOnlyList<Guid>? DocumentIds, SearchMode Mode, int? TopK);
