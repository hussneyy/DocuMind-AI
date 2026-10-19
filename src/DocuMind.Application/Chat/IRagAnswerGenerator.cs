namespace DocuMind.Application.Chat;

public interface IRagAnswerGenerator
{
    Task<RagAnswer> GenerateAsync(
        Guid ownerUserId,
        string question,
        IReadOnlyList<Guid>? documentIds,
        IReadOnlyList<(string Role, string Content)> history,
        CancellationToken ct);
}
