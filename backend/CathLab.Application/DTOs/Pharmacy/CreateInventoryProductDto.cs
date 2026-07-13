namespace CathLab.Application.DTOs.Pharmacy;

public class CreateInventoryProductDto
{
    public string Reference { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;

    // Parsed against the DeviceType enum in InventoryService - kept as a
    // string here so the API returns a clean 400 instead of a model-binding
    // error if the frontend sends an unexpected value.
    public string DeviceType { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? UnitOfMeasure { get; set; }
    public int MinimumStockLevel { get; set; }
}