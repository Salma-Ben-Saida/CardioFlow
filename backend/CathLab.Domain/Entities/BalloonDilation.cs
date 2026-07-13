namespace CathLab.Domain.Entities;

public class BalloonDilation : LesionTreatment
{
    public decimal BalloonDiameterMm { get; set; }
    public decimal BalloonLengthMm { get; set; }
    public byte MaxPressureAtm { get; set; }
    public int InflationDurationSec { get; set; }
    public bool IsPreDilation { get; set; }
    public bool IsPostDilation { get; set; }
}