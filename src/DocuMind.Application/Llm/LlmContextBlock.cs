namespace DocuMind.Application.Llm;

/// <summary>One retrieved chunk offered to the language model as grounding context, numbered so the model can cite it.</summary>
public record LlmContextBlock(
    int Index,
    Guid ChunkId,
    Guid DocumentId,
    string DocumentFileName,
    int PageStart,
    int PageEnd,
    string Text);
