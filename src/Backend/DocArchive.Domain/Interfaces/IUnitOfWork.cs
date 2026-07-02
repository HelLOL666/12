namespace DocArchive.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IDocumentRepository Documents { get; }
    IRepository<Entities.DocumentVersion> DocumentVersions { get; }
    IRepository<Entities.Comment> Comments { get; }
    IRepository<Entities.AuditLog> AuditLogs { get; }
    IRepository<Entities.RefreshToken> RefreshTokens { get; }
    IRepository<Entities.Role> Roles { get; }
    Task<int> SaveChangesAsync();
}
