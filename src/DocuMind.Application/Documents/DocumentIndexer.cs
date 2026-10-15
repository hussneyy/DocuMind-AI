using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocuMind.Application.Documents;

public class DocumentIndexer(
    IDocumentRepository documentRepository,
    IDocumentChunkRepository chunkRepository,
    IFileStorage fileStorage,
    IPdfTextExtractor textExtractor,
    ITextChunker textChunker,
    IOptions<ChunkingOptions> chunkingOptions,
    ILogger<DocumentIndexer> logger) : IDocumentIndexer
{
    public async Task IndexAsync(Guid documentId, CancellationToken ct)
    {
        var document = await documentRepository.GetByIdForProcessingAsync(documentId, ct);
        if (document is null)
        {
            logger.LogWarning("Indexing requested for unknown document {DocumentId}", documentId);
            return;
        }

        document.ProcessingStatus = DocumentProcessingStatus.Processing;
        await documentRepository.SaveChangesAsync(ct);

        try
        {
            await using var stream = await fileStorage.OpenReadAsync(document.StorageKey, ct);
            var pages = textExtractor.ExtractPages(stream);

            var hasText = pages.Any(p => !string.IsNullOrWhiteSpace(p.Text));
            if (!hasText)
            {
                document.ProcessingStatus = DocumentProcessingStatus.Failed;
                document.ProcessingError =
                    "No extractable text was found. This looks like a scanned or image-only PDF — OCR support is not yet implemented.";
                await documentRepository.SaveChangesAsync(ct);
                return;
            }

            var draftChunks = textChunker.Chunk(pages, chunkingOptions.Value);
            var now = DateTimeOffset.UtcNow;
            var chunks = draftChunks.Select(d => new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                ChunkIndex = d.ChunkIndex,
                TextContent = d.Text,
                PageStart = d.PageStart,
                PageEnd = d.PageEnd,
                CreatedAt = now,
            }).ToList();

            await chunkRepository.ReplaceAllForDocumentAsync(document.Id, chunks, ct);

            document.ProcessingStatus = DocumentProcessingStatus.Indexed;
            document.ProcessingError = null;
            document.IndexedAt = now;
            await documentRepository.SaveChangesAsync(ct);

            logger.LogInformation("Indexed document {DocumentId}: {ChunkCount} chunks from {PageCount} pages",
                document.Id, chunks.Count, pages.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to index document {DocumentId}", documentId);
            document.ProcessingStatus = DocumentProcessingStatus.Failed;
            document.ProcessingError = "Indexing failed due to an internal error. You can retry.";
            await documentRepository.SaveChangesAsync(ct);
        }
    }
}
