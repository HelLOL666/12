using DocArchive.Application.DTOs;
using DocArchive.Application.Interfaces;
using DocArchive.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DocArchive.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ICommentService _commentService;
    private readonly IAuditService _auditService;

    public DocumentsController(IDocumentService documentService, ICommentService commentService, IAuditService auditService)
    {
        _documentService = documentService;
        _commentService = commentService;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<DocumentListResponse>>> GetAll(
        [FromQuery] string? search, [FromQuery] string? sortBy,
        [FromQuery] bool descending = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        EnsurePermission(Permission.View);
        var result = await _documentService.GetAllAsync(search, sortBy, descending, page, pageSize);
        return Ok(ApiResponse<DocumentListResponse>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DocumentDetailDto>>> GetById(Guid id)
    {
        EnsurePermission(Permission.View);
        var document = await _documentService.GetByIdAsync(id);
        if (document == null) return NotFound(ApiResponse<DocumentDetailDto>.Fail("Document not found"));

        var userId = GetCurrentUserId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await _auditService.LogAsync(userId, User.Identity?.Name ?? "", AuditAction.ViewDocument,
            $"Viewed document: {document.Number}", ip);

        return Ok(ApiResponse<DocumentDetailDto>.Ok(document));
    }

    [HttpPost("upload")]
    [RequestSizeLimit(200 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<DocumentDto>>> Upload(
        [FromForm] string number, [FromForm] string title, IFormFile file)
    {
        EnsurePermission(Permission.Upload);

        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<DocumentDto>.Fail("File is required"));

        var userId = GetCurrentUserId();
        var request = new UploadDocumentRequest(number, title);

        await using var stream = file.OpenReadStream();
        var result = await _documentService.UploadAsync(request, stream, file.FileName, file.ContentType, file.Length, userId);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await _auditService.LogAsync(userId, User.Identity?.Name ?? "", AuditAction.UploadDocument,
            $"Uploaded document: {number}", ip);

        return Ok(ApiResponse<DocumentDto>.Ok(result));
    }

    [HttpGet("versions/{versionId:guid}/download")]
    public async Task<IActionResult> Download(Guid versionId)
    {
        EnsurePermission(Permission.Download);

        var result = await _documentService.DownloadAsync(versionId);
        if (result == null) return NotFound(ApiResponse.Fail("Version not found"));

        var (stream, fileName, contentType) = result.Value;

        var userId = GetCurrentUserId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await _auditService.LogAsync(userId, User.Identity?.Name ?? "", AuditAction.DownloadDocument,
            $"Downloaded version: {versionId}", ip);

        return File(stream, contentType, fileName);
    }

    [HttpGet("versions/{versionId:guid}/preview")]
    public async Task<IActionResult> Preview(Guid versionId)
    {
        EnsurePermission(Permission.View);

        var result = await _documentService.GetPdfPreviewAsync(versionId);
        if (result == null) return NotFound(ApiResponse.Fail("Preview not available"));

        var (stream, contentType) = result.Value;
        return File(stream, contentType);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        EnsurePermission(Permission.Delete);

        var document = await _documentService.GetByIdAsync(id);
        var success = await _documentService.DeleteAsync(id);
        if (!success) return NotFound(ApiResponse.Fail("Document not found"));

        var userId = GetCurrentUserId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await _auditService.LogAsync(userId, User.Identity?.Name ?? "", AuditAction.DeleteDocument,
            $"Deleted document: {document?.Number}", ip);

        return Ok(ApiResponse.Ok());
    }

    [HttpGet("{id:guid}/comments")]
    public async Task<ActionResult<ApiResponse<IEnumerable<CommentDto>>>> GetComments(Guid id)
    {
        EnsurePermission(Permission.View);
        var comments = await _commentService.GetByDocumentIdAsync(id);
        return Ok(ApiResponse<IEnumerable<CommentDto>>.Ok(comments));
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<ApiResponse<CommentDto>>> AddComment(Guid id, [FromBody] CreateCommentRequest request)
    {
        EnsurePermission(Permission.View);
        var userId = GetCurrentUserId();
        var comment = await _commentService.CreateAsync(id, request, userId);
        return Ok(ApiResponse<CommentDto>.Ok(comment));
    }

    private void EnsurePermission(Permission required)
    {
        var permClaim = User.FindFirstValue("permissions");
        var permissions = (Permission)int.Parse(permClaim ?? "0");
        if (!permissions.HasFlag(required))
            throw new UnauthorizedAccessException();
    }

    private Guid GetCurrentUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
