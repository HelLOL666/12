using DocArchive.Application.DTOs;

namespace DocArchive.Application.Interfaces;

public interface ICommentService
{
    Task<IEnumerable<CommentDto>> GetByDocumentIdAsync(Guid documentId);
    Task<CommentDto> CreateAsync(Guid documentId, CreateCommentRequest request, Guid userId);
}
