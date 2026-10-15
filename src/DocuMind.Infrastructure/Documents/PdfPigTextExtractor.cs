using DocuMind.Application.Documents;
using UglyToad.PdfPig;

namespace DocuMind.Infrastructure.Documents;

public class PdfPigTextExtractor : IPdfTextExtractor
{
    public IReadOnlyList<PageText> ExtractPages(Stream pdfContent)
    {
        using var document = PdfDocument.Open(pdfContent);
        var pages = new List<PageText>(document.NumberOfPages);

        foreach (var page in document.GetPages())
        {
            pages.Add(new PageText(page.Number, page.Text ?? string.Empty));
        }

        return pages;
    }
}
