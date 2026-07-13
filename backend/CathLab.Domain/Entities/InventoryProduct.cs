namespace CathLab.Domain.Entities;

/// <summary>
/// Catalog-level product (e.g. "Xience 3.0x18mm DES"). Physical, trackable
/// units of this product are represented individually by InventoryItem.
/// </summary>
public class InventoryProduct
{
    public Guid Id { get; set; }

    public string Reference { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public DeviceType DeviceType { get; set; }
    public string? Manufacturer { get; set; }
    public string? UnitOfMeasure { get; set; }
    public int MinimumStockLevel { get; set; }

    // Computed cache - source of truth is the sum of StockMovement.Quantity
    // for this product. Recalculated on every Reception/Consumption/Waste/etc.
    public int CurrentStock { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public ICollection<InventoryItem> Items { get; set; } = new List<InventoryItem>();
    public ICollection<StockMovement> Movements { get; set; } = new List<StockMovement>();
}