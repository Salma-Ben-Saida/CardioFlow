namespace CathLab.Application.DTOs.Pharmacy;

public class InventoryItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string? LotNumber { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public string? SerialNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? ReservedForCathStudyId { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? ReservedAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
}