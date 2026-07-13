namespace CathLab.Domain.Repositories;

using CathLab.Domain.Entities;

public interface IUserRepository
{
    Task<AppUser?> GetByUsernameAsync(string username);
    Task AddAsync(AppUser user);
    Task<bool> SaveChangesAsync();
}
