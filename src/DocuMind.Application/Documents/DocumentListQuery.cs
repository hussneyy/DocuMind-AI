using DocuMind.Domain.Enums;

namespace DocuMind.Application.Documents;

public enum DocumentSortField
{
    UploadedAt,
    Name,
}

public record DocumentListQuery(
    int Page = 1,
    int PageSize = 20,
    string? FileNameSearch = null,
    DocumentProcessingStatus? StatusFilter = null,
    DocumentSortField SortBy = DocumentSortField.UploadedAt,
    bool SortDescending = true);
