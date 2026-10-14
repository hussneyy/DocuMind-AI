using DocuMind.Application.Documents;

namespace DocuMind.Tests.Documents;

public class DocumentUploadValidatorTests
{
    private static readonly UploadOptions DefaultOptions = new() { MaxFileSizeBytes = 1024, MaxPageCount = 10 };

    [Fact]
    public void ValidateMetadata_AcceptsAWellFormedPdfUpload()
    {
        var errors = DocumentUploadValidator.ValidateMetadata("handbook.pdf", 512, DefaultOptions);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateMetadata_RejectsMissingFileName(string fileName)
    {
        var errors = DocumentUploadValidator.ValidateMetadata(fileName, 512, DefaultOptions);

        Assert.Contains(errors, e => e.Contains("file name", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("handbook.docx")]
    [InlineData("handbook.exe")]
    [InlineData("handbook")]
    public void ValidateMetadata_RejectsNonPdfExtensions(string fileName)
    {
        var errors = DocumentUploadValidator.ValidateMetadata(fileName, 512, DefaultOptions);

        Assert.Contains(errors, e => e.Contains("PDF", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateMetadata_RejectsEmptyFile()
    {
        var errors = DocumentUploadValidator.ValidateMetadata("handbook.pdf", 0, DefaultOptions);

        Assert.Contains(errors, e => e.Contains("empty", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateMetadata_RejectsFilesLargerThanConfiguredLimit()
    {
        var errors = DocumentUploadValidator.ValidateMetadata(
            "handbook.pdf", DefaultOptions.MaxFileSizeBytes + 1, DefaultOptions);

        Assert.Contains(errors, e => e.Contains("exceeds", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateMetadata_AcceptsFileExactlyAtTheSizeLimit()
    {
        var errors = DocumentUploadValidator.ValidateMetadata(
            "handbook.pdf", DefaultOptions.MaxFileSizeBytes, DefaultOptions);

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateMetadata_RejectsExcessivelyLongFileNames()
    {
        var longName = new string('a', 300) + ".pdf";

        var errors = DocumentUploadValidator.ValidateMetadata(longName, 512, DefaultOptions);

        Assert.Contains(errors, e => e.Contains("255", StringComparison.Ordinal));
    }

    [Fact]
    public void HasValidPdfSignature_AcceptsRealPdfMagicNumber()
    {
        var header = "%PDF-1.7\n..."u8.ToArray();

        Assert.True(DocumentUploadValidator.HasValidPdfSignature(header));
    }

    [Theory]
    [InlineData("")]
    [InlineData("PK\x03\x04")] // zip/docx signature
    [InlineData("GIF89a")]
    public void HasValidPdfSignature_RejectsNonPdfSignatures(string content)
    {
        var header = System.Text.Encoding.ASCII.GetBytes(content);

        Assert.False(DocumentUploadValidator.HasValidPdfSignature(header));
    }

    [Fact]
    public void HasValidPdfSignature_RejectsHeaderShorterThanTheSignature()
    {
        var header = "%PD"u8.ToArray();

        Assert.False(DocumentUploadValidator.HasValidPdfSignature(header));
    }
}
