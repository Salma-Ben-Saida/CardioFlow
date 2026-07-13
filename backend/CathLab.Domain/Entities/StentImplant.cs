namespace CathLab.Domain.Entities;

public class StentImplant : LesionTreatment
{
    public string? LandingZoneProximal { get; set; }
    public string? LandingZoneDistal { get; set; }
    public decimal StentDiameterMm { get; set; }
    public decimal StentLengthMm { get; set; }
    public bool PostDilationPerformed { get; set; }
    public byte? PostDilationPressure { get; set; }
}