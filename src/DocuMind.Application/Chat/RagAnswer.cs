namespace DocuMind.Application.Chat;

public record RagAnswer(string AnswerText, IReadOnlyList<RagCitation> Citations, bool ContextWasAvailable);

public record RagCitation(
    Guid ChunkId,
    Guid DocumentId,
    string DocumentFileName,
    int PageStart,
    int PageEnd,
    string Excerpt);
