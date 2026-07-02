using DocArchive.Domain.Enums;

namespace DocArchive.Application.DTOs;

public record AuditLogDto(Guid Id, string Username, AuditAction Action, string Details, string IpAddress, DateTime Timestamp);
public record AuditLogListResponse(IEnumerable<AuditLogDto> Items, int TotalCount, int Page, int PageSize);
