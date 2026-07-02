namespace DocArchive.Application.DTOs;

public record CommentDto(Guid Id, string AuthorName, string Text, DateTime CreatedAt);
public record CreateCommentRequest(string Text);
