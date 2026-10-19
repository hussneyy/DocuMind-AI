using System.Text.RegularExpressions;
using DocuMind.Application.Llm;
using Microsoft.Extensions.Options;

namespace DocuMind.Infrastructure.Llm;

/// <summary>
/// Deterministic, dependency-free stand-in for a real chat model: picks the sentence within each
/// retrieved context block that shares the most vocabulary with the question (the same "shared
/// vocabulary implies relevance" heuristic as <see cref="Embeddings.MockEmbeddingProvider"/>) and
/// composes an answer that cites each block it drew from with a [n] marker. It has no real
/// language understanding — it cannot paraphrase, infer, or resolve follow-up questions the way a
/// real model would — but it is enough to exercise the full grounded-answer-with-citations
/// pipeline, including the "say so instead of guessing" behavior, without a paid API key.
/// </summary>
public partial class MockLlmProvider(IOptions<LlmOptions> options) : ILlmProvider
{
    private const int MaxCitedBlocks = 3;

    public string ModelId => options.Value.Model;

    public Task<string> GenerateAnswerAsync(LlmRequest request, CancellationToken ct)
    {
        if (request.Context.Count == 0)
        {
            return Task.FromResult(
                "I don't have enough information in your documents to answer that question.");
        }

        var questionWords = WordsOf(request.Question);
        var sentences = new List<(int BlockIndex, string Sentence, int Overlap)>();

        foreach (var block in request.Context.Take(MaxCitedBlocks))
        {
            var best = BestMatchingSentence(block.Text, questionWords);
            if (best is not null)
            {
                sentences.Add((block.Index, best.Value.Sentence, best.Value.Overlap));
            }
        }

        if (sentences.Count == 0)
        {
            // Every block failed to yield a usable sentence (e.g. empty text) — still answer from
            // the single best-ranked block rather than claiming total ignorance, but make clear
            // it's a direct quote rather than a synthesized answer.
            var fallbackBlock = request.Context[0];
            return Task.FromResult($"{Truncate(fallbackBlock.Text, 300)} [{fallbackBlock.Index}]");
        }

        var answer = "Based on your documents: " + string.Join(" ", sentences.Select(s => $"{s.Sentence} [{s.BlockIndex}]"));
        return Task.FromResult(answer);
    }

    private static (string Sentence, int Overlap)? BestMatchingSentence(string text, HashSet<string> questionWords)
    {
        var sentences = SentenceSplitRegex().Split(text).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        if (sentences.Count == 0) return null;

        (string Sentence, int Overlap) best = (sentences[0], -1);
        foreach (var sentence in sentences)
        {
            var overlap = WordsOf(sentence).Intersect(questionWords).Count();
            if (overlap > best.Overlap)
            {
                best = (sentence, overlap);
            }
        }

        return best;
    }

    private static HashSet<string> WordsOf(string text) =>
        WordSplitRegex().Split(text.ToLowerInvariant())
            .Where(w => w.Length > 2)
            .ToHashSet();

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "…";

    [GeneratedRegex(@"(?<=[.!?])\s+")]
    private static partial Regex SentenceSplitRegex();

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex WordSplitRegex();
}
