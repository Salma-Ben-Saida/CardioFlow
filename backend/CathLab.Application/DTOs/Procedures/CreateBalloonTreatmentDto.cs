namespace CathLab.Application.DTOs.Procedures;

/// <summary>
/// Adding a Balloon treatment to an existing Lesion (pre-dilation,
/// stand-alone DCB, or post-dilation after a stent).
/// </summary>
public class CreateBalloonTreatmentDto
{
    public Guid LesionId { get; set; }
    public Guid InventoryItemId { get; set; }
    public decimal BalloonDiameterMm { get; set; }
    public decimal BalloonLengthMm { get; set; }
    public byte MaxPressureAtm { get; set; }
    public int InflationDurationSec { get; set; }
    public bool IsPreDilation { get; set; }
    public bool IsPostDilation { get; set; }
    public byte? TimiFlowAfter { get; set; }
    public string? Notes { get; set; }
}