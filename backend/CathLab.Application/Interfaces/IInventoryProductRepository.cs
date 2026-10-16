using CathLab.Domain.Entities;

namespace CathLab.Application.Interfaces;

public interface IInventoryProductRepository
{
    Task<InventoryProduct?> FindByIdAsync(Guid id);
    Task<InventoryProduct?> FindByReferenceAsync(string reference);
    Task<(List<InventoryProduct> Items, int TotalCount)> GetListAsync(string? search, int page, int pageSize);
    Task<List<InventoryProduct>> GetLowStockAsync();
    Task<InventoryProduct> CreateAsync(InventoryProduct product);
    Task UpdateAsync(InventoryProduct product);
    Task DeleteAsync(InventoryProduct product);
}