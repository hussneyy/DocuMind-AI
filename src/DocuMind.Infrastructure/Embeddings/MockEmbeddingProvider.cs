using DocuMind.Application.Embeddings;
using Microsoft.Extensions.Options;

namespace DocuMind.Infrastructure.Embeddings;

/// <summary>
/// Deterministic, dependency-free embedding approximation for development and automated testing:
/// a hashed bag-of-words vector, L2-normalized. Documents sharing more vocabulary end up with
/// higher cosine similarity, which is enough to exercise the real retrieval pipeline (ranking,
/// thresholds, hybrid fusion) without a paid API key. It has no real semantic understanding —
/// synonyms and paraphrases won't match the way a real embedding model's would.
/// </summary>
public class MockEmbeddingProvider(IOptions<EmbeddingOptions> options) : IEmbeddingProvider
{
    private static readonly char[] WordSeparators =
        [' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?', '"', '\'', '(', ')', '[', ']'];

    public string ModelId => options.Value.Model;

    public int Dimensions => options.Value.Dimensions;

    public Task<float[]> EmbedAsync(string text, CancellationToken ct) => Task.FromResult(Embed(text));

    public Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<float[]>>(texts.Select(Embed).ToList());

    private float[] Embed(string text)
    {
        var vector = new float[Dimensions];
        var words = text.ToLowerInvariant().Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            var bucket = (int)(Fnv1aHash(word) % (uint)Dimensions);
            vector[bucket] += 1f;
        }

        Normalize(vector);
        return vector;
    }

    private static uint Fnv1aHash(string value)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        var hash = offsetBasis;
        foreach (var ch in value)
        {
            hash ^= ch;
            hash *= prime;
        }

        return hash;
    }

    private static void Normalize(float[] vector)
    {
        var magnitude = MathF.Sqrt(vector.Sum(v => v * v));
        if (magnitude <= float.Epsilon) return;

        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] /= magnitude;
        }
    }
}
