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
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Get audit logs (admin only)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<AuditLogListResponse>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        EnsurePermission(Permission.ManageUsers);
        var result = await _auditLogService.GetAllAsync(page, pageSize);
        return Ok(ApiResponse<AuditLogListResponse>.Ok(result));
    }

    private void EnsurePermission(Permission required)
    {
        var permClaim = User.FindFirstValue("permissions");
        var permissions = (Permission)int.Parse(permClaim ?? "0");
        if (!permissions.HasFlag(required))
            throw new UnauthorizedAccessException();
    }
}
