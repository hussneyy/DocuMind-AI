namespace DocuMind.Application.Summaries;

public record SummaryDto(
    string SummaryType,
    string Content,
    int SourceChunkCount,
    string GeneratedByModel,
    DateTimeOffset GeneratedAt);
