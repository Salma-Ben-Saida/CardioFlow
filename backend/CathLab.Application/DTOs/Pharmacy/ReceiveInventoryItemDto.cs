namespace CathLab.Application.DTOs.Pharmacy;

/// <summary>
/// Request to register one new physical unit of a product into stock
/// (e.g. scanning in a box of stents delivered by the supplier).
/// </summary>
public class ReceiveInventoryItemDto
{
    public Guid ProductId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string? LotNumber { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public string? SerialNumber { get; set; }
}