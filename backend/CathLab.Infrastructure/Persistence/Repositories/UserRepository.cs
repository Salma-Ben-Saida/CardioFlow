namespace CathLab.Infrastructure.Repositories;

using CathLab.Domain.Entities;
using CathLab.Domain.Repositories;
using CathLab.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class UserRepository : IUserRepository
{
    private readonly CathLabDbContext _context;

    public UserRepository(CathLabDbContext context)
    {
        _context = context;
    }

    public async Task<AppUser?> GetByIdAsync(Guid id)
    {
        return await _context.AppUsers
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task AddAsync(AppUser user)
    {
        await _context.AppUsers.AddAsync(user);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
    public async Task<List<AppUser>> GetAllAsync(int page, int pageSize)
{
    return await _context.AppUsers
        .AsNoTracking() // Optimise les performances (lecture seule)
        .OrderByDescending(u => u.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
}

    public async Task<AppUser?> GetByUsernameAsync(string username)
{
    return await _context.AppUsers
        .FirstOrDefaultAsync(u => u.Username == username);
}
}
