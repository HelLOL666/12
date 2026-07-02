using DocArchive.Domain.Enums;

namespace DocArchive.Application.DTOs;

public record UserDto(Guid Id, string Username, string FullName, string Role, Permission Permissions, bool IsActive, DateTime CreatedAt);
public record CreateUserRequest(string Username, string FullName, string Password, int RoleId, Permission Permissions);
public record UpdateUserRequest(string FullName, int RoleId, Permission Permissions, bool IsActive);
public record ChangePasswordRequest(string NewPassword);
public record UpdateProfileRequest(string FullName);
public record UserListResponse(IEnumerable<UserDto> Items, int TotalCount, int Page, int PageSize);
