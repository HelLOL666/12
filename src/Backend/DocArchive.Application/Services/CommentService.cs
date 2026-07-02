using AutoMapper;
using DocArchive.Application.DTOs;
using DocArchive.Application.Interfaces;
using DocArchive.Domain.Entities;
using DocArchive.Domain.Interfaces;

namespace DocArchive.Application.Services;

public class CommentService : ICommentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CommentService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CommentDto>> GetByDocumentIdAsync(Guid documentId)
    {
        var document = await _unitOfWork.Documents.GetWithCommentsAsync(documentId);
        if (document == null) return Enumerable.Empty<CommentDto>();
        return _mapper.Map<IEnumerable<CommentDto>>(document.Comments);
    }

    public async Task<CommentDto> CreateAsync(Guid documentId, CreateCommentRequest request, Guid userId)
    {
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            AuthorId = userId,
            Text = request.Text,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Comments.AddAsync(comment);
        await _unitOfWork.SaveChangesAsync();

        var comments = await _unitOfWork.Comments.FindAsync(c => c.Id == comment.Id);
        var saved = comments.First();
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        return new CommentDto(saved.Id, user!.FullName, saved.Text, saved.CreatedAt);
    }
}
