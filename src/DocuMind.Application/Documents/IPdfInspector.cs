namespace DocuMind.Application.Documents;

public record PdfInspectionResult(bool IsValid, int? PageCount, bool IsEncrypted, string? Error);

/// <summary>
/// Opens a candidate PDF file to confirm it actually parses, and to read basic structural facts
/// (page count, whether it's encrypted) needed at upload time. Full text extraction happens later,
/// in the indexing pipeline (Milestone 3).
/// </summary>
public interface IPdfInspector
{
    PdfInspectionResult Inspect(Stream pdfContent);
}
