using DocArchive.Application.DTOs;

namespace DocArchive.Application.Interfaces;

public interface IAuditLogService
{
    Task<AuditLogListResponse> GetAllAsync(int page, int pageSize);
}
