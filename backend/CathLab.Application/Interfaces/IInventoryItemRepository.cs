using CathLab.Domain.Entities;

namespace CathLab.Application.Interfaces;

public interface IInventoryItemRepository
{
    Task<InventoryItem?> FindByIdAsync(Guid id);
    Task<InventoryItem?> FindByBarcodeAsync(string barcode);
    Task<List<InventoryItem>> GetAvailableByProductAsync(Guid productId);
    Task<List<InventoryItem>> GetExpiringAsync(int withinDays);
    Task<InventoryItem> CreateAsync(InventoryItem item);
    Task UpdateAsync(InventoryItem item);
    Task<List<InventoryItem>> GetReservedForStudyAsync(Guid cathStudyId);
    Task<List<InventoryItem>> GetAvailableAsync(DeviceType? deviceType);
    Task<List<InventoryItem>> GetAllReservedAsync();
    Task<List<InventoryItem>> GetAllAsync();
}