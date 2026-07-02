using DocArchive.Application.DTOs;
using DocArchive.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DocArchive.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;

    public AuthController(IAuthService authService, IUserService userService)
    {
        _authService = authService;
        _userService = userService;
    }

    /// <summary>
    /// Authenticate user and get tokens
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> Login([FromBody] LoginRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _authService.LoginAsync(request, ipAddress);

        if (result == null)
            return Unauthorized(ApiResponse<TokenResponse>.Fail("Invalid username or password"));

        SetRefreshTokenCookie(result.RefreshToken, result.ExpiresAt);
        return Ok(ApiResponse<TokenResponse>.Ok(result));
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> Refresh([FromBody] RefreshTokenRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _authService.RefreshTokenAsync(request, ipAddress);

        if (result == null)
            return Unauthorized(ApiResponse<TokenResponse>.Fail("Invalid token"));

        SetRefreshTokenCookie(result.RefreshToken, result.ExpiresAt);
        return Ok(ApiResponse<TokenResponse>.Ok(result));
    }

    /// <summary>
    /// Logout and revoke tokens
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> Logout()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await _authService.LogoutAsync(userId, ipAddress);

        Response.Cookies.Delete("refreshToken");
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Get current user info
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public ActionResult<ApiResponse<object>> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var username = User.FindFirstValue(ClaimTypes.Name);
        var role = User.FindFirstValue(ClaimTypes.Role);
        var fullName = User.FindFirstValue("fullName");
        var permissions = User.FindFirstValue("permissions");

        return Ok(ApiResponse<object>.Ok(new
        {
            Id = userId,
            Username = username,
            FullName = fullName,
            Role = role,
            Permissions = int.Parse(permissions ?? "0")
        }));
    }

    /// <summary>
    /// Update current user's profile (full name only)
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _userService.UpdateProfileAsync(userId, request);
        if (result == null)
            return NotFound(ApiResponse<object>.Fail("User not found"));

        return Ok(ApiResponse<object>.Ok(new
        {
            result.Id,
            result.Username,
            result.FullName,
            result.Role,
            Permissions = (int)result.Permissions
        }));
    }

    private void SetRefreshTokenCookie(string token, DateTime expires)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expires
        };
        Response.Cookies.Append("refreshToken", token, cookieOptions);
    }
}
