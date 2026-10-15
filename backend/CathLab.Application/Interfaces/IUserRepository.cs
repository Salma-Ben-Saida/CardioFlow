namespace CathLab.Domain.Repositories;

using System;
using CathLab.Domain.Entities;

public interface IUserRepository
{
    Task<AppUser?> GetByUsernameAsync(string username);
    Task AddAsync(AppUser user);
    Task UpdateAsync(AppUser user);
    Task DeleteAsync(AppUser user);
    Task<bool> SaveChangesAsync();
    Task<List<AppUser>> GetAllAsync(int page, int pageSize);
    Task<AppUser?> GetByIdAsync(Guid id);
}
