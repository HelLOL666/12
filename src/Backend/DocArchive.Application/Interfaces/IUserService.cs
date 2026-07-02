using DocArchive.Application.DTOs;

namespace DocArchive.Application.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<UserListResponse> GetAllAsync(int page, int pageSize);
    Task<UserDto> CreateAsync(CreateUserRequest request);
    Task<UserDto?> UpdateAsync(Guid id, UpdateUserRequest request);
    Task<bool> ChangePasswordAsync(Guid id, ChangePasswordRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<UserDto?> UpdateProfileAsync(Guid id, UpdateProfileRequest request);
}
