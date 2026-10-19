namespace DocuMind.Application.Llm;

public record LlmRequest(
    string Question,
    IReadOnlyList<LlmContextBlock> Context,
    IReadOnlyList<LlmChatTurn> History);
