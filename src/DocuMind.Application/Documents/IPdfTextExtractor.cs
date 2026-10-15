namespace DocuMind.Application.Documents;

/// <summary>
/// Extracts page-by-page text from an already-validated PDF (see <see cref="IPdfInspector"/> for
/// the fast upload-time check). Used only by the background indexing pipeline, where a slower,
/// full-document parse is acceptable.
/// </summary>
public interface IPdfTextExtractor
{
    IReadOnlyList<PageText> ExtractPages(Stream pdfContent);
}
