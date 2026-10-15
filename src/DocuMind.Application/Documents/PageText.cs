namespace DocuMind.Application.Documents;

/// <summary>Raw extracted text for one page, 1-indexed to match how pages are cited to users.</summary>
public record PageText(int PageNumber, string Text);
