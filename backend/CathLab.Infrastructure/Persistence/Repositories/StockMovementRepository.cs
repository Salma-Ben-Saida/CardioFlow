using CathLab.Application.Interfaces;
using CathLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CathLab.Infrastructure.Persistence.Repositories;

public class StockMovementRepository : IStockMovementRepository
{
    private readonly CathLabDbContext _db;

    public StockMovementRepository(CathLabDbContext db)
    {
        _db = db;
    }

    public async Task<StockMovement> CreateAsync(StockMovement movement)
    {
        _db.StockMovements.Add(movement);
        await _db.SaveChangesAsync();
        return movement;
    }

    public async Task<(List<StockMovement> Items, int TotalCount)> GetByProductAsync(Guid productId, int page, int pageSize)
    {
        var query = _db.StockMovements.Where(m => m.ProductId == productId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(m => m.MovedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<(IEnumerable<StockMovement> items, int total)> GetListAsync(int page, int pageSize)
    {
        var query = _db.StockMovements.AsQueryable();

        var total = query.Count();

        var items = query
            .OrderByDescending(m => m.MovedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult((items.AsEnumerable(), total));
    }
}