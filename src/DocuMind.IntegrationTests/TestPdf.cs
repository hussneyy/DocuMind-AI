using System.Text;

namespace DocuMind.IntegrationTests;

/// <summary>Builds a minimal, structurally valid one-page PDF with correctly computed xref offsets.</summary>
public static class TestPdf
{
    public static byte[] BuildOnePagePdfBytes()
    {
        string[] objects =
        [
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n",
            "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 300] /Resources << >> >>\nendobj\n",
        ];

        var body = new StringBuilder("%PDF-1.4\n");
        var offsets = new int[objects.Length];

        for (var i = 0; i < objects.Length; i++)
        {
            offsets[i] = Encoding.ASCII.GetByteCount(body.ToString());
            body.Append(objects[i]);
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(body.ToString());

        body.Append("xref\n");
        body.Append($"0 {objects.Length + 1}\n");
        body.Append("0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            body.Append($"{offset:D10} 00000 n \n");
        }

        body.Append("trailer\n");
        body.Append($"<< /Size {objects.Length + 1} /Root 1 0 R >>\n");
        body.Append("startxref\n");
        body.Append($"{xrefOffset}\n");
        body.Append("%%EOF");

        return Encoding.ASCII.GetBytes(body.ToString());
    }
}
