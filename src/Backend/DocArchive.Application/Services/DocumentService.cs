using AutoMapper;
using DocArchive.Application.DTOs;
using DocArchive.Application.Interfaces;
using DocArchive.Domain.Entities;
using DocArchive.Domain.Interfaces;

namespace DocArchive.Application.Services;

public class DocumentService : IDocumentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorage;
    private readonly IMapper _mapper;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".cdw", ".spw", ".m3d", ".dxf"
    };

    private const long MaxFileSize = 200 * 1024 * 1024; // 200 MB

    public DocumentService(IUnitOfWork unitOfWork, IFileStorageService fileStorage, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
        _mapper = mapper;
    }

    public async Task<DocumentListResponse> GetAllAsync(string? search, string? sortBy, bool descending, int page, int pageSize)
    {
        var (items, totalCount) = await _unitOfWork.Documents.GetPagedAsync(search, sortBy, descending, page, pageSize);
        var dtos = _mapper.Map<IEnumerable<DocumentDto>>(items);
        return new DocumentListResponse(dtos, totalCount, page, pageSize);
    }

    public async Task<DocumentDetailDto?> GetByIdAsync(Guid id)
    {
        var document = await _unitOfWork.Documents.GetWithVersionsAsync(id);
        return document == null ? null : _mapper.Map<DocumentDetailDto>(document);
    }

    public async Task<DocumentDto> UploadAsync(UploadDocumentRequest request, Stream fileStream,
        string fileName, string contentType, long fileSize, Guid userId)
    {
        var extension = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(extension))
            throw new InvalidOperationException($"File extension '{extension}' is not allowed. Allowed: {string.Join(", ", AllowedExtensions)}");

        if (fileSize > MaxFileSize)
            throw new InvalidOperationException($"File size exceeds maximum allowed size of {MaxFileSize / 1024 / 1024} MB");

        var existingDoc = await _unitOfWork.Documents.GetByNumberAsync(request.Number);
        Document document;
        int versionNumber;

        if (existingDoc != null)
        {
            document = existingDoc;
            versionNumber = existingDoc.Versions.Any() ? existingDoc.Versions.Max(v => v.VersionNumber) + 1 : 1;
            document.UpdatedAt = DateTime.UtcNow;
            document.Title = request.Title;
            _unitOfWork.Documents.Update(document);
        }
        else
        {
            document = new Document
            {
                Id = Guid.NewGuid(),
                Number = request.Number,
                Title = request.Title,
                AuthorId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            versionNumber = 1;
            await _unitOfWork.Documents.AddAsync(document);
        }

        var storagePath = await _fileStorage.SaveFileAsync(fileStream, fileName, $"documents/{document.Id}");

        var version = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            VersionNumber = versionNumber,
            FileName = Path.GetFileName(storagePath),
            OriginalFileName = fileName,
            ContentType = contentType,
            FileSize = fileSize,
            StoragePath = storagePath,
            UploadedById = userId,
            UploadedAt = DateTime.UtcNow
        };

        if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            version.PdfPreviewPath = storagePath;

        await _unitOfWork.DocumentVersions.AddAsync(version);
        await _unitOfWork.SaveChangesAsync();

        var result = await _unitOfWork.Documents.GetWithVersionsAsync(document.Id);
        return _mapper.Map<DocumentDto>(result!);
    }

    public async Task<(Stream Stream, string FileName, string ContentType)?> DownloadAsync(Guid versionId)
    {
        var versions = await _unitOfWork.DocumentVersions.FindAsync(v => v.Id == versionId);
        var version = versions.FirstOrDefault();
        if (version == null) return null;

        var stream = await _fileStorage.GetFileAsync(version.StoragePath);
        return (stream, version.OriginalFileName, version.ContentType);
    }

    public async Task<(Stream Stream, string ContentType)?> GetPdfPreviewAsync(Guid versionId)
    {
        var versions = await _unitOfWork.DocumentVersions.FindAsync(v => v.Id == versionId);
        var version = versions.FirstOrDefault();
        if (version?.PdfPreviewPath == null) return null;

        var stream = await _fileStorage.GetFileAsync(version.PdfPreviewPath);
        return (stream, "application/pdf");
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var document = await _unitOfWork.Documents.GetWithVersionsAsync(id);
        if (document == null) return false;

        foreach (var version in document.Versions)
        {
            await _fileStorage.DeleteFileAsync(version.StoragePath);
            if (version.PdfPreviewPath != null && version.PdfPreviewPath != version.StoragePath)
                await _fileStorage.DeleteFileAsync(version.PdfPreviewPath);
        }

        _unitOfWork.Documents.Remove(document);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
