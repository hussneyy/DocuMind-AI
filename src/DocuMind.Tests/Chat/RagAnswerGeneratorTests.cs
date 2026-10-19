using DocuMind.Application.Chat;
using DocuMind.Application.Llm;
using DocuMind.Application.Search;
using Microsoft.Extensions.Options;

namespace DocuMind.Tests.Chat;

public class RagAnswerGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_WithNoRetrievedChunks_RefusesWithoutCallingTheLlm()
    {
        var search = new FakeSearchService([]);
        var llm = new FakeLlmProvider(_ => "should never be called");
        var generator = new RagAnswerGenerator(search, llm, Options.Create(new RagOptions()));

        var answer = await generator.GenerateAsync(Guid.NewGuid(), "What is the deadline?", null, [], CancellationToken.None);

        Assert.False(answer.ContextWasAvailable);
        Assert.Empty(answer.Citations);
        Assert.Contains("don't have enough information", answer.AnswerText, StringComparison.OrdinalIgnoreCase);
        Assert.False(llm.WasCalled);
    }

    [Fact]
    public async Task GenerateAsync_WithRetrievedChunks_ResolvesCitationMarkersToTheMatchingChunk()
    {
        var chunkId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var candidate = new ChunkSearchCandidate(chunkId, documentId, "handbook.pdf", "The deadline is March 15th.", 2, 2, 0.9);
        var search = new FakeSearchService([candidate]);
        var llm = new FakeLlmProvider(_ => "The deadline is March 15th [1].");
        var generator = new RagAnswerGenerator(search, llm, Options.Create(new RagOptions()));

        var answer = await generator.GenerateAsync(Guid.NewGuid(), "When is the deadline?", null, [], CancellationToken.None);

        Assert.True(answer.ContextWasAvailable);
        Assert.Single(answer.Citations);
        Assert.Equal(chunkId, answer.Citations[0].ChunkId);
        Assert.Equal(documentId, answer.Citations[0].DocumentId);
        Assert.Equal("handbook.pdf", answer.Citations[0].DocumentFileName);
    }

    [Fact]
    public async Task GenerateAsync_IgnoresACitationMarkerThatDoesNotMatchAnyProvidedContextBlock()
    {
        var candidate = new ChunkSearchCandidate(Guid.NewGuid(), Guid.NewGuid(), "handbook.pdf", "Some text.", 1, 1, 0.9);
        var search = new FakeSearchService([candidate]);
        // The model hallucinates a marker [7] for which no context block was ever provided.
        var llm = new FakeLlmProvider(_ => "Some text [7].");
        var generator = new RagAnswerGenerator(search, llm, Options.Create(new RagOptions()));

        var answer = await generator.GenerateAsync(Guid.NewGuid(), "question", null, [], CancellationToken.None);

        Assert.Empty(answer.Citations);
    }

    [Fact]
    public async Task GenerateAsync_DeduplicatesARepeatedCitationMarker()
    {
        var candidate = new ChunkSearchCandidate(Guid.NewGuid(), Guid.NewGuid(), "handbook.pdf", "Some text.", 1, 1, 0.9);
        var search = new FakeSearchService([candidate]);
        var llm = new FakeLlmProvider(_ => "Some text [1]. More about it [1].");
        var generator = new RagAnswerGenerator(search, llm, Options.Create(new RagOptions()));

        var answer = await generator.GenerateAsync(Guid.NewGuid(), "question", null, [], CancellationToken.None);

        Assert.Single(answer.Citations);
    }

    [Fact]
    public async Task GenerateAsync_PassesPriorHistoryToTheLlmRequest()
    {
        var candidate = new ChunkSearchCandidate(Guid.NewGuid(), Guid.NewGuid(), "handbook.pdf", "Some text.", 1, 1, 0.9);
        var search = new FakeSearchService([candidate]);
        var llm = new FakeLlmProvider(_ => "answer [1]");
        var generator = new RagAnswerGenerator(search, llm, Options.Create(new RagOptions()));

        var history = new List<(string Role, string Content)> { ("user", "earlier question"), ("assistant", "earlier answer") };
        await generator.GenerateAsync(Guid.NewGuid(), "follow-up question", null, history, CancellationToken.None);

        Assert.NotNull(llm.LastRequest);
        Assert.Equal(2, llm.LastRequest!.History.Count);
        Assert.Equal("earlier question", llm.LastRequest.History[0].Content);
    }

    private sealed class FakeSearchService(IReadOnlyList<ChunkSearchCandidate> candidates) : ISearchService
    {
        public Task<IReadOnlyList<SearchResultItem>> SearchAsync(Guid ownerUserId, SearchRequest request, CancellationToken ct) =>
            throw new NotSupportedException("RagAnswerGenerator should call RetrieveAsync, not SearchAsync.");

        public Task<IReadOnlyList<ChunkSearchCandidate>> RetrieveAsync(Guid ownerUserId, SearchRequest request, CancellationToken ct) =>
            Task.FromResult(candidates);
    }

    private sealed class FakeLlmProvider(Func<LlmRequest, string> answer) : ILlmProvider
    {
        public bool WasCalled { get; private set; }
        public LlmRequest? LastRequest { get; private set; }

        public string ModelId => "fake";

        public Task<string> GenerateAnswerAsync(LlmRequest request, CancellationToken ct)
        {
            WasCalled = true;
            LastRequest = request;
            return Task.FromResult(answer(request));
        }
    }
}
