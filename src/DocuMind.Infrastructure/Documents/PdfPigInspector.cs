using DocuMind.Application.Documents;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace DocuMind.Infrastructure.Documents;

public class PdfPigInspector(ILogger<PdfPigInspector> logger) : IPdfInspector
{
    public PdfInspectionResult Inspect(Stream pdfContent)
    {
        try
        {
            using var document = PdfDocument.Open(pdfContent);
            return new PdfInspectionResult(true, document.NumberOfPages, false, null);
        }
        catch (Exception ex) when (LooksLikeEncryptionFailure(ex))
        {
            return new PdfInspectionResult(false, null, true, "The PDF is password-protected and cannot be processed.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse uploaded PDF");
            return new PdfInspectionResult(false, null, false,
                "The file could not be read as a PDF. It may be corrupted or use an unsupported format.");
        }
    }

    private static bool LooksLikeEncryptionFailure(Exception ex) =>
        ex.GetType().Name.Contains("Encrypt", StringComparison.OrdinalIgnoreCase)
        || (ex.Message?.Contains("password", StringComparison.OrdinalIgnoreCase) ?? false)
        || (ex.Message?.Contains("encrypt", StringComparison.OrdinalIgnoreCase) ?? false);
}
