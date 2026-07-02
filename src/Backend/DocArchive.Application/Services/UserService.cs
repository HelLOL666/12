using AutoMapper;
using DocArchive.Application.DTOs;
using DocArchive.Application.Interfaces;
using DocArchive.Domain.Entities;
using DocArchive.Domain.Interfaces;

namespace DocArchive.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly IMapper _mapper;

    public UserService(IUnitOfWork unitOfWork, IPasswordService passwordService, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _mapper = mapper;
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await _unitOfWork.Users.GetByIdWithRoleAsync(id);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }

    public async Task<UserListResponse> GetAllAsync(int page, int pageSize)
    {
        var users = await _unitOfWork.Users.GetAllWithRolesAsync(page, pageSize);
        var totalCount = await _unitOfWork.Users.GetTotalCountAsync();
        var items = _mapper.Map<IEnumerable<UserDto>>(users);
        return new UserListResponse(items, totalCount, page, pageSize);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            FullName = request.FullName,
            PasswordHash = _passwordService.HashPassword(request.Password),
            RoleId = request.RoleId,
            Permissions = request.Permissions,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var created = await _unitOfWork.Users.GetByIdWithRoleAsync(user.Id);
        return _mapper.Map<UserDto>(created!);
    }

    public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdWithRoleAsync(id);
        if (user == null) return null;

        user.FullName = request.FullName;
        user.RoleId = request.RoleId;
        user.Permissions = request.Permissions;
        user.IsActive = request.IsActive;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        var updated = await _unitOfWork.Users.GetByIdWithRoleAsync(id);
        return _mapper.Map<UserDto>(updated!);
    }

    public async Task<bool> ChangePasswordAsync(Guid id, ChangePasswordRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null) return false;

        user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null) return false;

        user.IsActive = false;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<UserDto?> UpdateProfileAsync(Guid id, UpdateProfileRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdWithRoleAsync(id);
        if (user == null) return null;

        user.FullName = request.FullName;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        var updated = await _unitOfWork.Users.GetByIdWithRoleAsync(id);
        return _mapper.Map<UserDto>(updated!);
    }
}
