namespace DocuMind.Application.Llm;

/// <summary>
/// Generates a grounded chat answer from retrieved context. Implementations are swappable via
/// configuration (see <see cref="LlmOptions"/>) — a deterministic mock implementation lets the app
/// and its tests run without a paid API key, mirroring <c>IEmbeddingProvider</c>.
/// </summary>
public interface ILlmProvider
{
    string ModelId { get; }

    /// <summary>
    /// Returns the model's answer text. The caller (not this interface) is responsible for
    /// validating that any [n] citation markers in the returned text refer to context blocks that
    /// were actually passed in — never trust the model's own claim of what it cited.
    /// </summary>
    Task<string> GenerateAnswerAsync(LlmRequest request, CancellationToken ct);
}
