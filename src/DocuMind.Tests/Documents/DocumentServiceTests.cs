using DocuMind.Application.Auditing;
using DocuMind.Application.Common.Exceptions;
using DocuMind.Application.Documents;
using DocuMind.Application.Search;
using DocuMind.Application.Tags;
using DocuMind.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DocuMind.Tests.Documents;

public class DocumentServiceTests
{
    private static readonly Guid OwnerUserId = Guid.NewGuid();

    [Fact]
    public async Task UploadAsync_WithAValidPdf_PersistsTheDocumentAndAudits()
    {
        var repository = new FakeDocumentRepository();
        var auditLogger = new FakeAuditLogger();
        var service = BuildService(repository, auditLogger, inspection: new PdfInspectionResult(true, 1, false, null));

        var bytes = BuildMinimalPdfBytes();
        await using var stream = new MemoryStream(bytes);

        var dto = await service.UploadAsync(OwnerUserId, stream, "handbook.pdf", bytes.Length, CancellationToken.None);

        Assert.Equal("handbook.pdf", dto.OriginalFileName);
        Assert.Single(repository.Added);
        Assert.Contains(auditLogger.Entries, e => e.EventType == AuditEventType.DocumentUploaded && e.UserId == OwnerUserId);
    }

    [Fact]
    public async Task UploadAsync_WhenThePdfFailsToParse_ThrowsAndDoesNotAudit()
    {
        var repository = new FakeDocumentRepository();
        var auditLogger = new FakeAuditLogger();
        var service = BuildService(repository, auditLogger, inspection: new PdfInspectionResult(false, null, false, "corrupt"));

        var bytes = BuildMinimalPdfBytes();
        await using var stream = new MemoryStream(bytes);

        await Assert.ThrowsAsync<AppValidationException>(() =>
            service.UploadAsync(OwnerUserId, stream, "handbook.pdf", bytes.Length, CancellationToken.None));

        Assert.Empty(repository.Added);
        Assert.DoesNotContain(auditLogger.Entries, e => e.EventType == AuditEventType.DocumentUploaded);
    }

    [Fact]
    public async Task DeleteAsync_ForAnOwnedDocument_DeletesAndAudits()
    {
        var document = new Document { Id = Guid.NewGuid(), OwnerUserId = OwnerUserId, OriginalFileName = "handbook.pdf", StorageKey = "key123" };
        var repository = new FakeDocumentRepository { ExistingDocument = document };
        var auditLogger = new FakeAuditLogger();
        var service = BuildService(repository, auditLogger, inspection: null);

        await service.DeleteAsync(OwnerUserId, document.Id, CancellationToken.None);

        Assert.Contains(document, repository.Removed);
        Assert.Contains(auditLogger.Entries, e => e.EventType == AuditEventType.DocumentDeleted && e.UserId == OwnerUserId);
    }

    [Fact]
    public async Task DeleteAsync_ForAnUnknownDocument_ThrowsNotFoundAndDoesNotAudit()
    {
        var repository = new FakeDocumentRepository();
        var auditLogger = new FakeAuditLogger();
        var service = BuildService(repository, auditLogger, inspection: null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.DeleteAsync(OwnerUserId, Guid.NewGuid(), CancellationToken.None));

        Assert.Empty(auditLogger.Entries);
    }

    private static DocumentService BuildService(FakeDocumentRepository repository, FakeAuditLogger auditLogger, PdfInspectionResult? inspection) =>
        new(repository,
            new FakeDocumentChunkRepository(),
            new FakeTagRepository(),
            new FakeFileStorage(),
            new FakePdfInspector(inspection),
            Options.Create(new UploadOptions()),
            NullLogger<DocumentService>.Instance,
            auditLogger);

    private static byte[] BuildMinimalPdfBytes() => "%PDF-1.4\n%%EOF"u8.ToArray();

    private sealed class FakeAuditLogger : IAuditLogger
    {
        public List<(AuditEventType EventType, Guid? UserId, string Details)> Entries { get; } = [];

        public void Log(AuditEventType eventType, Guid? userId, string details) =>
            Entries.Add((eventType, userId, details));
    }

    private sealed class FakePdfInspector(PdfInspectionResult? result) : IPdfInspector
    {
        public PdfInspectionResult Inspect(Stream pdfContent) =>
            result ?? throw new InvalidOperationException("Not configured for this test.");
    }

    private sealed class FakeFileStorage : IFileStorage
    {
        public Task<string> SaveAsync(Stream content, string fileExtension, CancellationToken ct) => Task.FromResult("key123");

        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct) => throw new NotSupportedException();

        public Task DeleteAsync(string storageKey, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeDocumentChunkRepository : IDocumentChunkRepository
    {
        public Task ReplaceAllForDocumentAsync(Guid documentId, IReadOnlyList<DocumentChunk> chunks, CancellationToken ct) => throw new NotSupportedException();

        public Task<IReadOnlyList<DocumentChunk>> GetByDocumentIdAsync(Guid documentId, CancellationToken ct) => throw new NotSupportedException();

        public Task<IReadOnlyList<ChunkSearchCandidate>> SemanticSearchAsync(
            Guid ownerUserId, float[] queryEmbedding, IReadOnlyList<Guid>? documentIds, int topK, double minSimilarity, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ChunkSearchCandidate>> KeywordSearchAsync(
            Guid ownerUserId, string query, IReadOnlyList<Guid>? documentIds, int topK, CancellationToken ct) =>
            throw new NotSupportedException();
    }

    private sealed class FakeDocumentRepository : IDocumentRepository
    {
        public Document? ExistingDocument { get; set; }
        public List<Document> Added { get; } = [];
        public List<Document> Removed { get; } = [];

        public Task AddAsync(Document document, CancellationToken ct)
        {
            Added.Add(document);
            return Task.CompletedTask;
        }

        public Task<Document?> GetByIdAsync(Guid ownerUserId, Guid documentId, CancellationToken ct) =>
            Task.FromResult(ExistingDocument is not null && ExistingDocument.Id == documentId && ExistingDocument.OwnerUserId == ownerUserId
                ? ExistingDocument
                : null);

        public Task<Document?> GetByIdForProcessingAsync(Guid documentId, CancellationToken ct) => throw new NotSupportedException();

        public Task<IReadOnlyList<Document>> GetQueuedForProcessingAsync(int batchSize, CancellationToken ct) => throw new NotSupportedException();

        public Task<(IReadOnlyList<Document> Items, int TotalCount)> ListAsync(Guid ownerUserId, DocumentListQuery query, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Document>> GetAllForOwnerAsync(Guid ownerUserId, CancellationToken ct) => throw new NotSupportedException();

        public void Remove(Document document) => Removed.Add(document);

        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeTagRepository : ITagRepository
    {
        public Task AddAsync(Tag tag, CancellationToken ct) => throw new NotSupportedException();

        public Task<Tag?> GetByIdAsync(Guid ownerUserId, Guid tagId, CancellationToken ct) => throw new NotSupportedException();

        public Task<Tag?> GetByNameAsync(Guid ownerUserId, string name, CancellationToken ct) => throw new NotSupportedException();

        public Task<IReadOnlyList<Tag>> ListAsync(Guid ownerUserId, CancellationToken ct) => throw new NotSupportedException();

        public void Remove(Tag tag) => throw new NotSupportedException();

        public Task<bool> IsAttachedAsync(Guid documentId, Guid tagId, CancellationToken ct) => throw new NotSupportedException();

        public Task AttachAsync(Guid documentId, Guid tagId, CancellationToken ct) => throw new NotSupportedException();

        public Task DetachAsync(Guid documentId, Guid tagId, CancellationToken ct) => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<TagDto>>> GetTagsForDocumentsAsync(IReadOnlyList<Guid> documentIds, CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<TagDto>>>(new Dictionary<Guid, IReadOnlyList<TagDto>>());

        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
