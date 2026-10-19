namespace DocuMind.Application.Llm;

/// <summary>A prior turn in the conversation, replayed so the model can resolve follow-up questions.</summary>
public record LlmChatTurn(string Role, string Content);
