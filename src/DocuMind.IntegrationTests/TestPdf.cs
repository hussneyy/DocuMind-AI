using System.Text;

namespace DocuMind.IntegrationTests;

/// <summary>Builds minimal, structurally valid PDFs with correctly computed xref offsets, for tests.</summary>
public static class TestPdf
{
    public static byte[] BuildOnePagePdfBytes() => BuildMultiPagePdfBytes("Sample page text for testing.");

    /// <summary>A structurally valid single-page PDF with no content stream — zero extractable text, the same shape as a scanned/image-only page.</summary>
    public static byte[] BuildBlankPageBytes()
    {
        var objects = new List<string>
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n",
            "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 300] /Resources << >> >>\nendobj\n",
        };

        return Assemble(objects);
    }

    /// <summary>
    /// Builds a PDF with one page per string, each page's content stream drawing that exact text
    /// via a base-14 font (no embedding needed), so PdfPig can extract real, known text per page.
    /// </summary>
    public static byte[] BuildMultiPagePdfBytes(params string[] pageTexts)
    {
        var objects = new List<string>
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
            BuildPagesObject(pageTexts.Length),
            "3 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n",
        };

        for (var i = 0; i < pageTexts.Length; i++)
        {
            var pageId = 4 + (2 * i);
            var contentId = 5 + (2 * i);

            objects.Add(
                $"{pageId} 0 obj\n" +
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 400 400] " +
                "/Resources << /Font << /F1 3 0 R >> >> " +
                $"/Contents {contentId} 0 R >>\nendobj\n");

            var escaped = pageTexts[i].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
            var contentStream = $"BT /F1 12 Tf 50 350 Td ({escaped}) Tj ET";
            var streamByteCount = Encoding.ASCII.GetByteCount(contentStream);

            objects.Add(
                $"{contentId} 0 obj\n<< /Length {streamByteCount} >>\nstream\n{contentStream}\nendstream\nendobj\n");
        }

        return Assemble(objects);
    }

    private static string BuildPagesObject(int pageCount)
    {
        var kids = string.Join(" ", Enumerable.Range(0, pageCount).Select(i => $"{4 + (2 * i)} 0 R"));
        return $"2 0 obj\n<< /Type /Pages /Kids [{kids}] /Count {pageCount} >>\nendobj\n";
    }

    private static byte[] Assemble(List<string> objects)
    {
        var body = new StringBuilder("%PDF-1.4\n");
        var offsets = new int[objects.Count];

        for (var i = 0; i < objects.Count; i++)
        {
            offsets[i] = Encoding.ASCII.GetByteCount(body.ToString());
            body.Append(objects[i]);
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(body.ToString());
        var totalEntries = objects.Count + 1;

        body.Append("xref\n");
        body.Append($"0 {totalEntries}\n");
        body.Append("0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            body.Append($"{offset:D10} 00000 n \n");
        }

        body.Append("trailer\n");
        body.Append($"<< /Size {totalEntries} /Root 1 0 R >>\n");
        body.Append("startxref\n");
        body.Append($"{xrefOffset}\n");
        body.Append("%%EOF");

        return Encoding.ASCII.GetBytes(body.ToString());
    }
}
