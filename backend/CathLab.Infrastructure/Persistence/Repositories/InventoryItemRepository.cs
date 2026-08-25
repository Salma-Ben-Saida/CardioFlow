using CathLab.Application.Interfaces;
using CathLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CathLab.Infrastructure.Persistence.Repositories;

public class InventoryItemRepository : IInventoryItemRepository
{
    private readonly CathLabDbContext _db;

    public InventoryItemRepository(CathLabDbContext db)
    {
        _db = db;
    }

    public async Task<InventoryItem?> FindByIdAsync(Guid id) =>
        await _db.InventoryItems.Include(i => i.Product).FirstOrDefaultAsync(i => i.Id == id);

    public async Task<InventoryItem?> FindByBarcodeAsync(string barcode) =>
        await _db.InventoryItems.FirstOrDefaultAsync(i => i.Barcode == barcode);

    public async Task<List<InventoryItem>> GetAvailableByProductAsync(Guid productId) =>
        await _db.InventoryItems
            .Where(i => i.ProductId == productId && i.Status == ItemStatus.InStock)
            // FEFO - first-expiry-first-out, standard pharmacy stock rotation.
            .OrderBy(i => i.ExpiryDate)
            .ToListAsync();

    public async Task<List<InventoryItem>> GetExpiringAsync(int withinDays)
    {
        var threshold = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(withinDays));
        return await _db.InventoryItems
            .Include(i => i.Product)
            .Where(i => i.Status == ItemStatus.InStock && i.ExpiryDate <= threshold)
            .OrderBy(i => i.ExpiryDate)
            .ToListAsync();
    }
    public async Task<List<InventoryItem>> GetAllReservedAsync()
{
    return await _db.InventoryItems
        .AsNoTracking()
        .Include(i => i.Product)
        .Where(i => i.Status == ItemStatus.Reserved)
        .OrderBy(i => i.ReservedAt)
        .ToListAsync();
}

    public async Task<InventoryItem> CreateAsync(InventoryItem item)
    {
        _db.InventoryItems.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task UpdateAsync(InventoryItem item)
    {
        _db.InventoryItems.Update(item);
        await _db.SaveChangesAsync();
    }
    public async Task<List<InventoryItem>> GetReservedForStudyAsync(Guid cathStudyId) =>
    await _db.InventoryItems.Include(i => i.Product)
        .Where(i => i.ReservedForCathStudyId == cathStudyId && i.Status == ItemStatus.Reserved)
        .ToListAsync();

public async Task<List<InventoryItem>> GetAvailableAsync(DeviceType? deviceType)
{
    var query = _db.InventoryItems
        .Include(i => i.Product)
        .Where(i => i.Status == ItemStatus.InStock);

    if (deviceType.HasValue)
        query = query.Where(i => i.Product.DeviceType == deviceType.Value);

    return await query.AsNoTracking().ToListAsync();
}

public async Task<List<InventoryItem>> GetAllAsync()
{
    return await _db.InventoryItems
        .Include(i => i.Product)
        .AsNoTracking()
        .ToListAsync();
}
}