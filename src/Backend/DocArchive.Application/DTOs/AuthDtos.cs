namespace DocArchive.Application.DTOs;

public record LoginRequest(string Username, string Password);
public record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
public record RefreshTokenRequest(string AccessToken, string RefreshToken);
