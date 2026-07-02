using DocArchive.Application.DTOs;
using DocArchive.Application.Interfaces;
using DocArchive.Domain.Entities;
using DocArchive.Domain.Enums;
using DocArchive.Domain.Interfaces;

namespace DocArchive.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly IAuditService _auditService;

    public AuthService(IUnitOfWork unitOfWork, IPasswordService passwordService,
        ITokenService tokenService, IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _auditService = auditService;
    }

    public async Task<TokenResponse?> LoginAsync(LoginRequest request, string ipAddress)
    {
        var user = await _unitOfWork.Users.GetByUsernameAsync(request.Username);
        if (user == null || !user.IsActive)
            return null;

        if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            return null;

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = expiresAt,
            CreatedByIp = ipAddress
        };

        await _unitOfWork.RefreshTokens.AddAsync(token);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAsync(user.Id, user.Username, AuditAction.Login, "User logged in", ipAddress);

        return new TokenResponse(accessToken, refreshToken, expiresAt);
    }

    public async Task<TokenResponse?> RefreshTokenAsync(RefreshTokenRequest request, string ipAddress)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
            return null;

        var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            return null;

        var tokens = await _unitOfWork.RefreshTokens.FindAsync(
            t => t.UserId == userId && t.Token == request.RefreshToken);
        var existingToken = tokens.FirstOrDefault();

        if (existingToken == null || !existingToken.IsActive)
            return null;

        existingToken.RevokedAt = DateTime.UtcNow;
        _unitOfWork.RefreshTokens.Update(existingToken);

        var user = await _unitOfWork.Users.GetByIdWithRoleAsync(userId);
        if (user == null || !user.IsActive)
            return null;

        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var newToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiresAt = expiresAt,
            CreatedByIp = ipAddress
        };

        await _unitOfWork.RefreshTokens.AddAsync(newToken);
        await _unitOfWork.SaveChangesAsync();

        return new TokenResponse(newAccessToken, newRefreshToken, expiresAt);
    }

    public async Task LogoutAsync(Guid userId, string ipAddress)
    {
        var tokens = await _unitOfWork.RefreshTokens.FindAsync(
            t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow);

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            _unitOfWork.RefreshTokens.Update(token);
        }

        await _unitOfWork.SaveChangesAsync();

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user != null)
            await _auditService.LogAsync(userId, user.Username, AuditAction.Logout, "User logged out", ipAddress);
    }
}
