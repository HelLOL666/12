using DocArchive.Domain.Entities;
using DocArchive.Domain.Interfaces;
using DocArchive.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DocArchive.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByUsernameAsync(string username)
        => await Context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Username == username);

    public async Task<User?> GetByIdWithRoleAsync(Guid id)
        => await Context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);

    public async Task<IEnumerable<User>> GetAllWithRolesAsync(int page, int pageSize)
        => await Context.Users.Include(u => u.Role)
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<int> GetTotalCountAsync() => await Context.Users.CountAsync();
}
