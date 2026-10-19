namespace DocuMind.Application.Chat;

public class RagOptions
{
    public const string SectionName = "Rag";

    /// <summary>How many retrieved chunks to offer the language model as context for one question.</summary>
    public int ContextTopK { get; set; } = 5;

    /// <summary>How many prior messages from the same conversation to replay to the model for follow-up questions.</summary>
    public int MaxHistoryMessages { get; set; } = 10;
}
