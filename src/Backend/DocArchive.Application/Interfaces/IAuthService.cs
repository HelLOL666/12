using DocArchive.Application.DTOs;

namespace DocArchive.Application.Interfaces;

public interface IAuthService
{
    Task<TokenResponse?> LoginAsync(LoginRequest request, string ipAddress);
    Task<TokenResponse?> RefreshTokenAsync(RefreshTokenRequest request, string ipAddress);
    Task LogoutAsync(Guid userId, string ipAddress);
}
