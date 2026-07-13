namespace CathLab.Domain.Entities;

/// <summary>
/// One physical, single-use, individually tracked device (stent, balloon,
/// guide catheter, etc.), identified by lot/serial/barcode.
/// </summary>
public class InventoryItem
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }

    // Set the moment a cardiologist selects this item in the segmentation UI,
    // before the procedure report (and therefore the exact LesionTreatment)
    // is finalized. Prevents two concurrent procedures from claiming the
    // same physical item.
    public Guid? ReservedForCathStudyId { get; set; }

    public string Barcode { get; set; } = string.Empty;
    public string? LotNumber { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public string? SerialNumber { get; set; }
    public ItemStatus Status { get; set; } = ItemStatus.InStock;
    public DateTime ReceivedAt { get; set; }
    public DateTime? ReservedAt { get; set; }
    public DateTime? ConsumedAt { get; set; }

    // Navigation
    public InventoryProduct Product { get; set; } = null!;
    public CathStudy? ReservedForCathStudy { get; set; }

    // Set once this item is actually implanted/used (Status -> Consumed).
    public LesionTreatment? Treatment { get; set; }
}