using AutoMapper;
using DocArchive.Application.DTOs;
using DocArchive.Domain.Entities;

namespace DocArchive.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForCtorParam("Role", opt => opt.MapFrom(src => src.Role.Name));

        CreateMap<Document, DocumentDto>()
            .ForCtorParam("Author", opt => opt.MapFrom(src => src.Author.FullName))
            .ForCtorParam("CurrentVersion", opt => opt.MapFrom(src =>
                src.Versions.Any() ? src.Versions.Max(v => v.VersionNumber) : 0));

        CreateMap<Document, DocumentDetailDto>()
            .ForCtorParam("AuthorName", opt => opt.MapFrom(src => src.Author.FullName));

        CreateMap<DocumentVersion, DocumentVersionDto>()
            .ForCtorParam("UploadedBy", opt => opt.MapFrom(src => src.UploadedBy.FullName))
            .ForCtorParam("HasPdfPreview", opt => opt.MapFrom(src => src.PdfPreviewPath != null));

        CreateMap<Comment, CommentDto>()
            .ForCtorParam("AuthorName", opt => opt.MapFrom(src => src.Author.FullName));

        CreateMap<AuditLog, AuditLogDto>();
    }
}
