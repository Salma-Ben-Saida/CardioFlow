namespace CathLab.Application.DTOs.Pharmacy;

public class UpdateInventoryProductDto
{
    public string? ProductName { get; set; }
    public string? Reference { get; set; }
    public string? DeviceType { get; set; }
    public string? Manufacturer { get; set; }
    public string? UnitOfMeasure { get; set; }
    public int? MinimumStockLevel { get; set; }
}
