namespace DocuMind.Application.Chat;

public record AskResultDto(Guid ConversationId, string Title, MessageDto UserMessage, MessageDto AssistantMessage);
