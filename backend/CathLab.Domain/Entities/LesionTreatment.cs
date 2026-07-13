namespace CathLab.Domain.Entities;

/// <summary>
/// Base class for a treatment action applied to a specific CoronaryLesion.
/// A LesionTreatment can never exist without a lesion behind it - in the UI,
/// Stent/Balloon should only be addable from an already-placed Lesion marker.
/// Mapped with EF Core's Table-Per-Type (TPT) strategy: StentImplant and
/// BalloonDilation get their own tables sharing this table's primary key.
/// </summary>
public abstract class LesionTreatment
{
    public Guid Id { get; set; }
    public Guid LesionId { get; set; }

    // Exactly one physical, single-use device is consumed per treatment.
    public Guid InventoryItemId { get; set; }

    public byte? TimiFlowAfter { get; set; }
    public DateTime PerformedAt { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public CoronaryLesion Lesion { get; set; } = null!;
    public InventoryItem InventoryItem { get; set; } = null!;
}