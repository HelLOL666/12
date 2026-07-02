namespace DocArchive.Application.DTOs;

public record DocumentDto(
    Guid Id, string Number, string Title, string Author, int CurrentVersion, DateTime UpdatedAt);

public record DocumentDetailDto(
    Guid Id, string Number, string Title, string AuthorName, Guid AuthorId,
    DateTime CreatedAt, DateTime UpdatedAt, IEnumerable<DocumentVersionDto> Versions);

public record DocumentVersionDto(
    Guid Id, int VersionNumber, string OriginalFileName, string ContentType,
    long FileSize, string UploadedBy, DateTime UploadedAt, bool HasPdfPreview);

public record DocumentListResponse(
    IEnumerable<DocumentDto> Items, int TotalCount, int Page, int PageSize);

public record UploadDocumentRequest(string Number, string Title);
