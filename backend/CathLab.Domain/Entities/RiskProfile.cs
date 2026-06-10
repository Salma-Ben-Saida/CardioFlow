namespace CathLab.Domain.Entities;

public class RiskProfile
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }

    public bool Hypertension { get; set; }
    public bool Diabetes { get; set; }
    public bool Dyslipidemia { get; set; }
    public bool Smoking { get; set; }
    public bool FamilyHistory { get; set; }
    public decimal? ObesityBMI { get; set; }
    public bool PriorMI { get; set; }
    public bool PriorPCI { get; set; }
    public bool PriorCABG { get; set; }
    public decimal? EjectionFraction { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Patient? Patient { get; set; }
}