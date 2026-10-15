namespace DocuMind.Application.Embeddings;

public class EmbeddingOptions
{
    public const string SectionName = "Embedding";

    /// <summary>"Mock" (no external calls, deterministic — for dev/test) or "OpenAI".</summary>
    public string Provider { get; set; } = "Mock";

    public string? ApiKey { get; set; }

    /// <summary>Overrides the default provider endpoint — e.g. for Azure OpenAI or a self-hosted OpenAI-compatible API.</summary>
    public string? Endpoint { get; set; }

    public string Model { get; set; } = "mock-bow-v1";

    public int Dimensions { get; set; } = 256;

    public int TimeoutSeconds { get; set; } = 30;

    public int MaxRetries { get; set; } = 2;
}
