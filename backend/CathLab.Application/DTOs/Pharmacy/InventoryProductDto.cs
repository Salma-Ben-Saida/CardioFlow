namespace CathLab.Application.DTOs.Pharmacy;

public class InventoryProductDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? UnitOfMeasure { get; set; }
    public int MinimumStockLevel { get; set; }
    public int CurrentStock { get; set; }
    public bool IsLowStock => CurrentStock <= MinimumStockLevel;
    public DateTime CreatedAt { get; set; }
}