namespace DocuMind.Application.Llm;

public class LlmOptions
{
    public const string SectionName = "Llm";

    /// <summary>"Mock" (default, no API key) or "OpenAI".</summary>
    public string Provider { get; set; } = "Mock";

    public string ApiKey { get; set; } = string.Empty;

    public string Endpoint { get; set; } = string.Empty;

    public string Model { get; set; } = "mock-grounded-v1";

    public int TimeoutSeconds { get; set; } = 60;

    public int MaxRetries { get; set; } = 2;

    public double Temperature { get; set; } = 0.2;

    public int MaxOutputTokens { get; set; } = 800;
}
