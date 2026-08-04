namespace CathLab.Application.DTOs.Pharmacy;

public class StockMovementDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? ReferenceNote { get; set; }
    public DateTime MovedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public string? actorName { get; set; }
}