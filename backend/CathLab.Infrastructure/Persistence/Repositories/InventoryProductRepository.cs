using CathLab.Application.Interfaces;
using CathLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CathLab.Infrastructure.Persistence.Repositories;

public class InventoryProductRepository : IInventoryProductRepository
{
    private readonly CathLabDbContext _db;

    public InventoryProductRepository(CathLabDbContext db)
    {
        _db = db;
    }

    public async Task<InventoryProduct?> FindByIdAsync(Guid id) =>
        await _db.InventoryProducts.FirstOrDefaultAsync(p => p.Id == id);

    public async Task<InventoryProduct?> FindByReferenceAsync(string reference) =>
        await _db.InventoryProducts.FirstOrDefaultAsync(p => p.Reference == reference);

    public async Task<(List<InventoryProduct> Items, int TotalCount)> GetListAsync(string? search, int page, int pageSize)
    {
        var query = _db.InventoryProducts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                EF.Functions.Like(p.ProductName, $"%{term}%") ||
                EF.Functions.Like(p.Reference, $"%{term}%"));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.ProductName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<List<InventoryProduct>> GetLowStockAsync() =>
        await _db.InventoryProducts
            .Where(p => p.CurrentStock <= p.MinimumStockLevel)
            .OrderBy(p => p.ProductName)
            .ToListAsync();

    public async Task<InventoryProduct> CreateAsync(InventoryProduct product)
    {
        _db.InventoryProducts.Add(product);
        await _db.SaveChangesAsync();
        return product;
    }

    public async Task UpdateAsync(InventoryProduct product)
    {
        _db.InventoryProducts.Update(product);
        await _db.SaveChangesAsync();
    }
}