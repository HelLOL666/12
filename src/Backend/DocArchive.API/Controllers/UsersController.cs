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
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;

    public UsersController(IUserService userService, IAuditService auditService)
    {
        _userService = userService;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<UserListResponse>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        EnsurePermission(Permission.ManageUsers);
        var result = await _userService.GetAllAsync(page, pageSize);
        return Ok(ApiResponse<UserListResponse>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(Guid id)
    {
        EnsurePermission(Permission.ManageUsers);
        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound(ApiResponse<UserDto>.Fail("User not found"));
        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] CreateUserRequest request)
    {
        EnsurePermission(Permission.ManageUsers);
        var user = await _userService.CreateAsync(request);

        var currentUserId = GetCurrentUserId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await _auditService.LogAsync(currentUserId, User.Identity?.Name ?? "", AuditAction.CreateUser,
            $"Created user: {request.Username}", ip);

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, ApiResponse<UserDto>.Ok(user));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        EnsurePermission(Permission.ManageUsers);
        var user = await _userService.UpdateAsync(id, request);
        if (user == null) return NotFound(ApiResponse<UserDto>.Fail("User not found"));

        var currentUserId = GetCurrentUserId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await _auditService.LogAsync(currentUserId, User.Identity?.Name ?? "", AuditAction.UpdateUser,
            $"Updated user: {user.Username}", ip);

        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    [HttpPut("{id:guid}/password")]
    public async Task<ActionResult<ApiResponse>> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request)
    {
        EnsurePermission(Permission.ManageUsers);
        var result = await _userService.ChangePasswordAsync(id, request);
        if (!result) return NotFound(ApiResponse.Fail("User not found"));
        return Ok(ApiResponse.Ok());
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        EnsurePermission(Permission.ManageUsers);
        var result = await _userService.DeleteAsync(id);
        if (!result) return NotFound(ApiResponse.Fail("User not found"));
        return Ok(ApiResponse.Ok());
    }

    private void EnsurePermission(Permission required)
    {
        var permClaim = User.FindFirstValue("permissions");
        var permissions = (Permission)int.Parse(permClaim ?? "0");
        if (!permissions.HasFlag(required))
            throw new UnauthorizedAccessException();
    }

    private Guid GetCurrentUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
