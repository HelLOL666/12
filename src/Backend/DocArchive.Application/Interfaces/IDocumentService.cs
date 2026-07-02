using DocArchive.Application.DTOs;

namespace DocArchive.Application.Interfaces;

public interface IDocumentService
{
    Task<DocumentListResponse> GetAllAsync(string? search, string? sortBy, bool descending, int page, int pageSize);
    Task<DocumentDetailDto?> GetByIdAsync(Guid id);
    Task<DocumentDto> UploadAsync(UploadDocumentRequest request, Stream fileStream, string fileName, string contentType, long fileSize, Guid userId);
    Task<(Stream Stream, string FileName, string ContentType)?> DownloadAsync(Guid versionId);
    Task<(Stream Stream, string ContentType)?> GetPdfPreviewAsync(Guid versionId);
    Task<bool> DeleteAsync(Guid id);
}
