using DocArchive.Domain.Entities;

namespace DocArchive.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdWithRoleAsync(Guid id);
    Task<IEnumerable<User>> GetAllWithRolesAsync(int page, int pageSize);
    Task<int> GetTotalCountAsync();
}
