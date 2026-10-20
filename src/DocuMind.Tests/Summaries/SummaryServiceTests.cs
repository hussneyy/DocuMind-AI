using DocuMind.Application.Common;
using DocuMind.Application.Common.Exceptions;
using DocuMind.Application.Documents;
using DocuMind.Application.Llm;
using DocuMind.Application.Search;
using DocuMind.Application.Summaries;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using Microsoft.Extensions.Options;

namespace DocuMind.Tests.Summaries;

public class SummaryServiceTests
{
    private static readonly Guid OwnerUserId = Guid.NewGuid();
    private static readonly Guid DocumentId = Guid.NewGuid();

    [Fact]
    public async Task GetOrGenerateAsync_WhenNothingCached_GeneratesAndPersistsASummary()
    {
        var documents = new FakeDocumentRepository(IndexedDocument());
        var chunks = new FakeDocumentChunkRepository(BuildChunks(5));
        var summaries = new FakeSummaryRepository();
        var llm = new FakeLlmProvider(_ => "a generated summary");
        var service = new SummaryService(documents, chunks, summaries, llm, Options(new SummaryOptions()));

        var dto = await service.GetOrGenerateAsync(OwnerUserId, DocumentId, SummaryType.Short, regenerate: false, CancellationToken.None);

        Assert.Equal("a generated summary", dto.Content);
        Assert.Equal("Short", dto.SummaryType);
        Assert.True(llm.WasCalled);
        Assert.NotNull(summaries.Saved);
    }

    [Fact]
    public async Task GetOrGenerateAsync_WhenAlreadyCached_ReturnsItWithoutCallingTheLlm()
    {
        var documents = new FakeDocumentRepository(IndexedDocument());
        var chunks = new FakeDocumentChunkRepository(BuildChunks(5));
        var summaries = new FakeSummaryRepository();
        summaries.Saved = new DocumentSummary
        {
            Id = Guid.NewGuid(), DocumentId = DocumentId, SummaryType = SummaryType.Short,
            Content = "cached content", SourceChunkCount = 5, LlmModel = "mock", GeneratedAt = DateTimeOffset.UtcNow,
        };
        var llm = new FakeLlmProvider(_ => "should not be called");
        var service = new SummaryService(documents, chunks, summaries, llm, Options(new SummaryOptions()));

        var dto = await service.GetOrGenerateAsync(OwnerUserId, DocumentId, SummaryType.Short, regenerate: false, CancellationToken.None);

        Assert.Equal("cached content", dto.Content);
        Assert.False(llm.WasCalled);
    }

    [Fact]
    public async Task GetOrGenerateAsync_WithRegenerateTrue_CallsTheLlmEvenWhenCached()
    {
        var documents = new FakeDocumentRepository(IndexedDocument());
        var chunks = new FakeDocumentChunkRepository(BuildChunks(5));
        var summaries = new FakeSummaryRepository();
        summaries.Saved = new DocumentSummary
        {
            Id = Guid.NewGuid(), DocumentId = DocumentId, SummaryType = SummaryType.Short,
            Content = "stale content", SourceChunkCount = 5, LlmModel = "mock", GeneratedAt = DateTimeOffset.UtcNow.AddDays(-1),
        };
        var llm = new FakeLlmProvider(_ => "fresh content");
        var service = new SummaryService(documents, chunks, summaries, llm, Options(new SummaryOptions()));

        var dto = await service.GetOrGenerateAsync(OwnerUserId, DocumentId, SummaryType.Short, regenerate: true, CancellationToken.None);

        Assert.Equal("fresh content", dto.Content);
        Assert.True(llm.WasCalled);
    }

    [Fact]
    public async Task GetOrGenerateAsync_WhenDocumentIsNotYetIndexed_ThrowsValidation()
    {
        var documents = new FakeDocumentRepository(new Document { Id = DocumentId, ProcessingStatus = DocumentProcessingStatus.Processing });
        var chunks = new FakeDocumentChunkRepository([]);
        var service = new SummaryService(documents, chunks, new FakeSummaryRepository(), new FakeLlmProvider(_ => "x"), Options(new SummaryOptions()));

        await Assert.ThrowsAsync<AppValidationException>(() =>
            service.GetOrGenerateAsync(OwnerUserId, DocumentId, SummaryType.Short, regenerate: false, CancellationToken.None));
    }

    [Fact]
    public async Task GetOrGenerateAsync_WhenDocumentDoesNotExistForThisOwner_ThrowsNotFound()
    {
        var documents = new FakeDocumentRepository(null);
        var service = new SummaryService(documents, new FakeDocumentChunkRepository([]), new FakeSummaryRepository(), new FakeLlmProvider(_ => "x"), Options(new SummaryOptions()));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetOrGenerateAsync(OwnerUserId, DocumentId, SummaryType.Short, regenerate: false, CancellationToken.None));
    }

    [Fact]
    public async Task GetOrGenerateAsync_ForDetailedType_CapsContextAtTheConfiguredMaxChunks()
    {
        var documents = new FakeDocumentRepository(IndexedDocument());
        var chunks = new FakeDocumentChunkRepository(BuildChunks(100));
        var llm = new FakeLlmProvider(_ => "detailed summary");
        var service = new SummaryService(
            documents, chunks, new FakeSummaryRepository(), llm, Options(new SummaryOptions { DetailedMaxChunks = 10 }));

        await service.GetOrGenerateAsync(OwnerUserId, DocumentId, SummaryType.Detailed, regenerate: false, CancellationToken.None);

        Assert.Equal(10, llm.LastRequest!.Context.Count);
    }

    [Fact]
    public async Task GetOrGenerateAsync_ForShortType_SamplesAcrossTheWholeDocument_NotJustTheStart()
    {
        var documents = new FakeDocumentRepository(IndexedDocument());
        var chunks = new FakeDocumentChunkRepository(BuildChunks(30));
        var llm = new FakeLlmProvider(_ => "short summary");
        var service = new SummaryService(
            documents, chunks, new FakeSummaryRepository(), llm, Options(new SummaryOptions { ShortSampleSize = 5 }));

        await service.GetOrGenerateAsync(OwnerUserId, DocumentId, SummaryType.Short, regenerate: false, CancellationToken.None);

        Assert.Equal(5, llm.LastRequest!.Context.Count);
        // With 30 chunks sampled down to 5, a purely-first-N sample would never reach chunk text
        // from the back half of the document — evenly-spaced sampling should.
        Assert.Contains(llm.LastRequest.Context, block => block.Text.Contains("chunk-20") || block.Text.Contains("chunk-24") || block.Text.Contains("chunk-29"));
    }

    private static Document IndexedDocument() => new()
    {
        Id = DocumentId, OwnerUserId = OwnerUserId, OriginalFileName = "handbook.pdf",
        ProcessingStatus = DocumentProcessingStatus.Indexed,
    };

    private static List<DocumentChunk> BuildChunks(int count) =>
        Enumerable.Range(0, count)
            .Select(i => new DocumentChunk { Id = Guid.NewGuid(), DocumentId = DocumentId, ChunkIndex = i, TextContent = $"Text for chunk-{i}.", PageStart = i + 1, PageEnd = i + 1 })
            .ToList();

    private static IOptions<SummaryOptions> Options(SummaryOptions options) => Microsoft.Extensions.Options.Options.Create(options);

    private sealed class FakeDocumentRepository(Document? document) : IDocumentRepository
    {
        public Task AddAsync(Document document, CancellationToken ct) => throw new NotSupportedException();

        public Task<Document?> GetByIdAsync(Guid ownerUserId, Guid documentId, CancellationToken ct) =>
            Task.FromResult(document is not null && documentId == document.Id ? document : null);

        public Task<Document?> GetByIdForProcessingAsync(Guid documentId, CancellationToken ct) => throw new NotSupportedException();

        public Task<IReadOnlyList<Document>> GetQueuedForProcessingAsync(int batchSize, CancellationToken ct) => throw new NotSupportedException();

        public Task<(IReadOnlyList<Document> Items, int TotalCount)> ListAsync(Guid ownerUserId, DocumentListQuery query, CancellationToken ct) =>
            throw new NotSupportedException();

        public void Remove(Document document) => throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class FakeDocumentChunkRepository(IReadOnlyList<DocumentChunk> chunks) : IDocumentChunkRepository
    {
        public Task ReplaceAllForDocumentAsync(Guid documentId, IReadOnlyList<DocumentChunk> chunks, CancellationToken ct) => throw new NotSupportedException();

        public Task<IReadOnlyList<DocumentChunk>> GetByDocumentIdAsync(Guid documentId, CancellationToken ct) => Task.FromResult(chunks);

        public Task<IReadOnlyList<ChunkSearchCandidate>> SemanticSearchAsync(
            Guid ownerUserId, float[] queryEmbedding, IReadOnlyList<Guid>? documentIds, int topK, double minSimilarity, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ChunkSearchCandidate>> KeywordSearchAsync(
            Guid ownerUserId, string query, IReadOnlyList<Guid>? documentIds, int topK, CancellationToken ct) =>
            throw new NotSupportedException();
    }

    private sealed class FakeSummaryRepository : ISummaryRepository
    {
        public DocumentSummary? Saved { get; set; }

        public Task<DocumentSummary?> GetAsync(Guid documentId, SummaryType summaryType, CancellationToken ct) =>
            Task.FromResult(Saved is not null && Saved.DocumentId == documentId && Saved.SummaryType == summaryType ? Saved : null);

        public Task UpsertAsync(DocumentSummary summary, CancellationToken ct)
        {
            Saved = summary;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeLlmProvider(Func<LlmRequest, string> answer) : ILlmProvider
    {
        public bool WasCalled { get; private set; }
        public LlmRequest? LastRequest { get; private set; }
        public string ModelId => "fake-mock";

        public Task<string> GenerateAnswerAsync(LlmRequest request, CancellationToken ct)
        {
            WasCalled = true;
            LastRequest = request;
            return Task.FromResult(answer(request));
        }
    }
}
