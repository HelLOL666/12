using DocArchive.Domain.Entities;
using DocArchive.Domain.Interfaces;
using DocArchive.Infrastructure.Data;

namespace DocArchive.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IUserRepository? _users;
    private IDocumentRepository? _documents;
    private IRepository<DocumentVersion>? _documentVersions;
    private IRepository<Comment>? _comments;
    private IRepository<AuditLog>? _auditLogs;
    private IRepository<RefreshToken>? _refreshTokens;
    private IRepository<Role>? _roles;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IDocumentRepository Documents => _documents ??= new DocumentRepository(_context);
    public IRepository<DocumentVersion> DocumentVersions => _documentVersions ??= new Repository<DocumentVersion>(_context);
    public IRepository<Comment> Comments => _comments ??= new Repository<Comment>(_context);
    public IRepository<AuditLog> AuditLogs => _auditLogs ??= new Repository<AuditLog>(_context);
    public IRepository<RefreshToken> RefreshTokens => _refreshTokens ??= new Repository<RefreshToken>(_context);
    public IRepository<Role> Roles => _roles ??= new Repository<Role>(_context);

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}
