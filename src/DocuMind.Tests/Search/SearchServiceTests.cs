using DocuMind.Application.Search;

namespace DocuMind.Tests.Search;

public class SearchServiceTests
{
    [Fact]
    public void ReciprocalRankFusion_RanksAChunkFoundByBothListsAboveOneFoundByOnlyOne()
    {
        var sharedChunk = Candidate("shared", docScore: 0.9);
        var semanticOnly = Candidate("semantic-only", docScore: 0.95);
        var keywordOnly = Candidate("keyword-only", docScore: 0.99);

        var semantic = new List<ChunkSearchCandidate> { sharedChunk, semanticOnly };
        var keyword = new List<ChunkSearchCandidate> { sharedChunk, keywordOnly };

        var fused = SearchService.ReciprocalRankFusion(semantic, keyword, topK: 10);

        Assert.Equal(sharedChunk.ChunkId, fused[0].ChunkId);
    }

    [Fact]
    public void ReciprocalRankFusion_PrefersHigherRankedItemsWithinEachList()
    {
        var first = Candidate("first", 0.9);
        var second = Candidate("second", 0.8);
        var third = Candidate("third", 0.7);

        var semantic = new List<ChunkSearchCandidate> { first, second, third };
        var keyword = new List<ChunkSearchCandidate>();

        var fused = SearchService.ReciprocalRankFusion(semantic, keyword, topK: 10);

        Assert.Equal(["first", "second", "third"], fused.Select(f => f.DocumentFileName));
    }

    [Fact]
    public void ReciprocalRankFusion_RespectsTopK()
    {
        var candidates = Enumerable.Range(0, 5).Select(i => Candidate($"doc-{i}", 1.0 - i * 0.1)).ToList();

        var fused = SearchService.ReciprocalRankFusion(candidates, [], topK: 2);

        Assert.Equal(2, fused.Count);
    }

    [Fact]
    public void ReciprocalRankFusion_WithBothListsEmpty_ReturnsEmpty()
    {
        var fused = SearchService.ReciprocalRankFusion([], [], topK: 10);

        Assert.Empty(fused);
    }

    [Fact]
    public void ReciprocalRankFusion_DoesNotDuplicateAChunkThatAppearsInBothLists()
    {
        var shared = Candidate("shared", 0.9);

        var fused = SearchService.ReciprocalRankFusion([shared], [shared], topK: 10);

        Assert.Single(fused);
    }

    private static ChunkSearchCandidate Candidate(string fileName, double docScore) =>
        new(Guid.NewGuid(), Guid.NewGuid(), fileName, $"Text for {fileName}", 1, 1, docScore);
}
