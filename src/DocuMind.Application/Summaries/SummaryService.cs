using DocuMind.Application.Common.Exceptions;
using DocuMind.Application.Documents;
using DocuMind.Application.Llm;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;
using Microsoft.Extensions.Options;

namespace DocuMind.Application.Summaries;

/// <summary>
/// Generates document summaries by feeding a sample of the document's own chunks to the LLM
/// provider — the same grounding principle as RAG chat, applied to "summarize this" rather than
/// "answer this question". Results are cached per (document, type) so re-opening a summary doesn't
/// re-trigger a model call; pass <c>regenerate: true</c> to force a fresh one.
/// </summary>
public class SummaryService(
    IDocumentRepository documentRepository,
    IDocumentChunkRepository chunkRepository,
    ISummaryRepository summaryRepository,
    ILlmProvider llmProvider,
    IOptions<SummaryOptions> options) : ISummaryService
{
    private const string ShortInstruction =
        "Write a concise 2-3 sentence summary of this document, covering only its single most important point.";

    private const string DetailedInstruction =
        "Write a detailed, multi-paragraph summary of this document covering its main topics and key information.";

    private const string KeyPointsInstruction =
        "List the key points of this document as a set of concise, standalone statements.";

    public async Task<SummaryDto> GetOrGenerateAsync(
        Guid ownerUserId, Guid documentId, SummaryType summaryType, bool regenerate, CancellationToken ct)
    {
        var document = await documentRepository.GetByIdAsync(ownerUserId, documentId, ct)
            ?? throw new NotFoundException("Document not found.");

        if (!regenerate)
        {
            var cached = await summaryRepository.GetAsync(documentId, summaryType, ct);
            if (cached is not null)
            {
                return ToDto(cached);
            }
        }

        if (document.ProcessingStatus != DocumentProcessingStatus.Indexed)
        {
            throw new AppValidationException(["The document must finish indexing before it can be summarized."]);
        }

        var ordered = (await chunkRepository.GetByDocumentIdAsync(documentId, ct))
            .OrderBy(c => c.ChunkIndex)
            .ToList();

        if (ordered.Count == 0)
        {
            throw new AppValidationException(["The document has no extracted content to summarize."]);
        }

        var (selected, instruction) = summaryType switch
        {
            SummaryType.Short => (Sample(ordered, options.Value.ShortSampleSize), ShortInstruction),
            SummaryType.KeyPoints => (Sample(ordered, options.Value.KeyPointsSampleSize), KeyPointsInstruction),
            _ => (ordered.Take(options.Value.DetailedMaxChunks).ToList(), DetailedInstruction),
        };

        var contextBlocks = selected
            .Select((c, i) => new LlmContextBlock(i + 1, c.Id, c.DocumentId, document.OriginalFileName, c.PageStart, c.PageEnd, c.TextContent))
            .ToList();

        var content = await llmProvider.GenerateAnswerAsync(new LlmRequest(instruction, contextBlocks, []), ct);

        var summary = new DocumentSummary
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            SummaryType = summaryType,
            Content = content,
            SourceChunkCount = contextBlocks.Count,
            LlmModel = llmProvider.ModelId,
            GeneratedAt = DateTimeOffset.UtcNow,
        };

        await summaryRepository.UpsertAsync(summary, ct);
        return ToDto(summary);
    }

    /// <summary>
    /// Picks up to <paramref name="sampleSize"/> chunks evenly spaced across the whole ordered
    /// list, rather than just the first N — so a short summary still reflects the document's
    /// middle and end, not only its introduction.
    /// </summary>
    private static List<DocumentChunk> Sample(List<DocumentChunk> ordered, int sampleSize)
    {
        if (ordered.Count <= sampleSize)
        {
            return ordered;
        }

        var step = (double)ordered.Count / sampleSize;
        return Enumerable.Range(0, sampleSize)
            .Select(i => ordered[(int)(i * step)])
            .ToList();
    }

    private static SummaryDto ToDto(DocumentSummary summary) => new(
        summary.SummaryType.ToString(), summary.Content, summary.SourceChunkCount, summary.LlmModel, summary.GeneratedAt);
}
