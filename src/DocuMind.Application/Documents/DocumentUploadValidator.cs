namespace DocuMind.Application.Documents;

/// <summary>
/// Pure, I/O-free validation of an upload candidate. Deliberately does not trust the
/// browser-supplied content type for anything except a quick early rejection — the real
/// gatekeeping is the PDF file signature check and the subsequent parse via <see cref="IPdfInspector"/>.
/// </summary>
public static class DocumentUploadValidator
{
    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();
    private const int MaxFileNameLength = 255;

    public static IReadOnlyList<string> ValidateMetadata(
        string originalFileName, long declaredLength, UploadOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            errors.Add("A file name is required.");
        }
        else if (originalFileName.Length > MaxFileNameLength)
        {
            errors.Add($"File name must be {MaxFileNameLength} characters or fewer.");
        }
        else if (!originalFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Only PDF files (.pdf) are supported.");
        }

        if (declaredLength <= 0)
        {
            errors.Add("The file is empty.");
        }
        else if (declaredLength > options.MaxFileSizeBytes)
        {
            var maxMb = options.MaxFileSizeBytes / (1024.0 * 1024.0);
            errors.Add($"The file exceeds the maximum allowed size of {maxMb:0.#} MB.");
        }

        return errors;
    }

    /// <summary>Checks the leading bytes of the file against the PDF magic number ("%PDF-").</summary>
    public static bool HasValidPdfSignature(ReadOnlySpan<byte> header)
    {
        return header.Length >= PdfSignature.Length
            && header[..PdfSignature.Length].SequenceEqual(PdfSignature);
    }
}
