namespace CathLab.Application.DTOs.Procedures;

/// <summary>
/// Common response shape for either a StentImplant or BalloonDilation.
/// Kind distinguishes which one it actually is ("Stent" / "Balloon");
/// type-specific fields (diameter, landing zone, etc.) aren't included
/// here - fetch the specific entity if the UI needs those details.
/// </summary>
public class LesionTreatmentDto
{
    public Guid Id { get; set; }
    public Guid LesionId { get; set; }
    public Guid InventoryItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? LotNumber { get; set; }
    public decimal DiameterMm { get; set; }
    public decimal LengthMm { get; set; }
    public string Kind { get; set; } = string.Empty;
    public byte? TimiFlowAfter { get; set; }
    public DateTime PerformedAt { get; set; }
    public string? Notes { get; set; }
}