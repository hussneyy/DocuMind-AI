using System.Text.RegularExpressions;
using DocuMind.Application.Llm;
using DocuMind.Application.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocuMind.Application.Chat;

/// <summary>
/// Orchestrates one RAG turn: retrieve, ground, generate, and validate citations against what was
/// actually retrieved. Never asks the model to answer from nothing, and never trusts a citation
/// marker the model emits unless it maps back to a context block this generator itself provided.
/// </summary>
public class RagAnswerGenerator(
    ISearchService searchService,
    ILlmProvider llmProvider,
    IOptions<RagOptions> options) : IRagAnswerGenerator
{
    private static readonly Regex CitationMarkerPattern = new(@"\[(\d+)\]", RegexOptions.Compiled);

    private const string NoContextAnswer =
        "I don't have enough information in your documents to answer that question. " +
        "Try rephrasing, uploading a relevant document, or asking something else.";

    public async Task<RagAnswer> GenerateAsync(
        Guid ownerUserId,
        string question,
        IReadOnlyList<Guid>? documentIds,
        IReadOnlyList<(string Role, string Content)> history,
        CancellationToken ct)
    {
        var searchRequest = new SearchRequest(question, documentIds, SearchMode.Hybrid, options.Value.ContextTopK);
        var candidates = await searchService.RetrieveAsync(ownerUserId, searchRequest, ct);

        if (candidates.Count == 0)
        {
            // Same "say so instead of guessing" principle already applied at the search layer:
            // with no relevant retrieved passage, there is nothing grounded to ask the model to
            // answer from, so don't call it at all.
            return new RagAnswer(NoContextAnswer, [], ContextWasAvailable: false);
        }

        var contextBlocks = candidates
            .Select((c, i) => new LlmContextBlock(i + 1, c.ChunkId, c.DocumentId, c.DocumentFileName, c.PageStart, c.PageEnd, c.TextContent))
            .ToList();

        var replayedHistory = history
            .TakeLast(options.Value.MaxHistoryMessages)
            .Select(h => new LlmChatTurn(h.Role, h.Content))
            .ToList();

        var llmRequest = new LlmRequest(question, contextBlocks, replayedHistory);
        var answerText = await llmProvider.GenerateAnswerAsync(llmRequest, ct);

        var citations = ResolveCitations(answerText, contextBlocks);

        return new RagAnswer(answerText, citations, ContextWasAvailable: true);
    }

    /// <summary>
    /// Extracts [n] markers from the model's answer and maps each distinct one to the context
    /// block it actually refers to — markers outside the range of context blocks that were
    /// provided (a hallucinated or out-of-range citation) are silently dropped rather than
    /// resolved to some other chunk.
    /// </summary>
    private static List<RagCitation> ResolveCitations(string answerText, IReadOnlyList<LlmContextBlock> contextBlocks)
    {
        var citations = new List<RagCitation>();
        var seen = new HashSet<int>();

        foreach (Match match in CitationMarkerPattern.Matches(answerText))
        {
            if (!int.TryParse(match.Groups[1].Value, out var index) || !seen.Add(index))
            {
                continue;
            }

            var block = contextBlocks.FirstOrDefault(b => b.Index == index);
            if (block is null)
            {
                continue;
            }

            citations.Add(new RagCitation(block.ChunkId, block.DocumentId, block.DocumentFileName, block.PageStart, block.PageEnd, Excerpt(block.Text)));
        }

        return citations;
    }

    private static string Excerpt(string text, int maxLength = 280) =>
        text.Length <= maxLength ? text : text[..maxLength] + "…";
}
