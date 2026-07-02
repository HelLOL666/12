using DocArchive.Domain.Enums;

namespace DocArchive.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(Guid? userId, string username, AuditAction action, string details, string ipAddress);
}
