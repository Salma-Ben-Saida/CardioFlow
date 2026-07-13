namespace CathLab.Application.DTOs.Procedures;

/// <summary>
/// Adding a Stent treatment to an existing Lesion. Requires the physical
/// InventoryItem the cardiologist selected - ProcedureService consumes it
/// via InventoryService as part of recording this treatment.
/// </summary>
public class CreateStentTreatmentDto
{
    public Guid LesionId { get; set; }
    public Guid InventoryItemId { get; set; }
    public string? LandingZoneProximal { get; set; }
    public string? LandingZoneDistal { get; set; }
    public decimal StentDiameterMm { get; set; }
    public decimal StentLengthMm { get; set; }
    public bool PostDilationPerformed { get; set; }
    public byte? PostDilationPressure { get; set; }
    public byte? TimiFlowAfter { get; set; }
    public string? Notes { get; set; }
}