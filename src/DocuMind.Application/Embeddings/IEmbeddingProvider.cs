namespace DocuMind.Application.Embeddings;

/// <summary>
/// Generates vector embeddings for text. Implementations are swappable via configuration (see
/// <see cref="EmbeddingOptions"/>) — a deterministic mock implementation lets the app and its
/// tests run without a paid API key.
/// </summary>
public interface IEmbeddingProvider
{
    /// <summary>Identifier stored alongside each embedding so stale vectors can be detected if this changes.</summary>
    string ModelId { get; }

    int Dimensions { get; }

    Task<float[]> EmbedAsync(string text, CancellationToken ct);

    Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct);
}
