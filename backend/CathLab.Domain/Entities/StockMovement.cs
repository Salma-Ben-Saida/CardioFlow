namespace CathLab.Domain.Entities;

/// <summary>
/// Audit-trail row for a change in stock at the product level. Written only
/// at terminal transitions (Consumed/Wasted/Returned/Expired/Received),
/// never at a plain Reservation.
/// </summary>
public class StockMovement
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid CreatedBy { get; set; }

    public MovementType MovementType { get; set; }
    public int Quantity { get; set; }
    public string? ReferenceNote { get; set; }
    public DateTime MovedAt { get; set; }

    // Navigation
    public InventoryProduct Product { get; set; } = null!;
    public AppUser CreatedByUser { get; set; } = null!;
}