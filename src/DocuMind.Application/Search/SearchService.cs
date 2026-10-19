using DocuMind.Application.Common.Exceptions;
using DocuMind.Application.Documents;
using DocuMind.Application.Embeddings;
using Microsoft.Extensions.Options;

namespace DocuMind.Application.Search;

public class SearchService(
    IDocumentChunkRepository chunkRepository,
    IEmbeddingProvider embeddingProvider,
    IOptions<SearchOptions> options) : ISearchService
{
    private const int RrfConstant = 60;

    public async Task<IReadOnlyList<SearchResultItem>> SearchAsync(Guid ownerUserId, SearchRequest request, CancellationToken ct)
    {
        var candidates = await RetrieveAsync(ownerUserId, request, ct);
        var query = request.Query.Trim();

        return candidates
            .Select(c => new SearchResultItem(
                c.ChunkId, c.DocumentId, c.DocumentFileName, BuildExcerpt(c.TextContent, query), c.PageStart, c.PageEnd, c.Score))
            .ToList();
    }

    public async Task<IReadOnlyList<ChunkSearchCandidate>> RetrieveAsync(Guid ownerUserId, SearchRequest request, CancellationToken ct)
    {
        var query = request.Query.Trim();
        if (query.Length == 0)
        {
            throw new AppValidationException(["A search query is required."]);
        }

        var topK = Math.Clamp(request.TopK ?? options.Value.DefaultTopK, 1, options.Value.MaxTopK);

        IReadOnlyList<ChunkSearchCandidate> semantic = [];
        IReadOnlyList<ChunkSearchCandidate> keyword = [];

        if (request.Mode is SearchMode.Semantic or SearchMode.Hybrid)
        {
            var queryEmbedding = await embeddingProvider.EmbedAsync(query, ct);
            semantic = await chunkRepository.SemanticSearchAsync(
                ownerUserId, queryEmbedding, request.DocumentIds, topK, options.Value.MinSimilarity, ct);
        }

        if (request.Mode is SearchMode.Keyword or SearchMode.Hybrid)
        {
            keyword = await chunkRepository.KeywordSearchAsync(ownerUserId, query, request.DocumentIds, topK, ct);
        }

        var merged = request.Mode switch
        {
            SearchMode.Semantic => semantic,
            SearchMode.Keyword => keyword,
            _ => ReciprocalRankFusion(semantic, keyword, topK),
        };

        return merged.Take(topK).ToList();
    }

    /// <summary>
    /// Combines two independently-ranked candidate lists by rank position (not raw score, which
    /// isn't comparable across a cosine similarity and a text-search rank) using Reciprocal Rank
    /// Fusion: a chunk's combined score is the sum of 1/(k + rank) over every list it appears in.
    /// </summary>
    public static List<ChunkSearchCandidate> ReciprocalRankFusion(
        IReadOnlyList<ChunkSearchCandidate> listA, IReadOnlyList<ChunkSearchCandidate> listB, int topK)
    {
        var combined = new Dictionary<Guid, (ChunkSearchCandidate Candidate, double Score)>();

        void Accumulate(IReadOnlyList<ChunkSearchCandidate> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var contribution = 1.0 / (RrfConstant + i + 1);
                var candidate = list[i];
                combined[candidate.ChunkId] = combined.TryGetValue(candidate.ChunkId, out var existing)
                    ? (existing.Candidate, existing.Score + contribution)
                    : (candidate, contribution);
            }
        }

        Accumulate(listA);
        Accumulate(listB);

        return combined.Values
            .OrderByDescending(v => v.Score)
            .Take(topK)
            .Select(v => v.Candidate with { Score = v.Score })
            .ToList();
    }

    private static string BuildExcerpt(string text, string query, int maxLength = 280)
    {
        if (text.Length <= maxLength) return text;

        var firstWord = query.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        var matchIndex = firstWord is null ? -1 : text.IndexOf(firstWord, StringComparison.OrdinalIgnoreCase);

        if (matchIndex < 0)
        {
            return text[..maxLength] + "…";
        }

        var start = Math.Max(0, matchIndex - (maxLength / 3));
        var length = Math.Min(maxLength, text.Length - start);
        var snippet = text.Substring(start, length);

        return (start > 0 ? "…" : "") + snippet + (start + length < text.Length ? "…" : "");
    }
}
