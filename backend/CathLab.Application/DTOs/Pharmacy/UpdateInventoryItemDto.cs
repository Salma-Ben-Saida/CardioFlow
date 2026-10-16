namespace CathLab.Application.DTOs.Pharmacy;

public class UpdateInventoryItemDto
{
    public string? Barcode { get; set; }
    public string? LotNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? SerialNumber { get; set; }
    public string? Status { get; set; }
}
