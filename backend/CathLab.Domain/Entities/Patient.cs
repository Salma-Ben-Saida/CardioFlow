namespace CathLab.Domain.Entities;

public class Patient
{
    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }

    public string? ConquestPatientID { get; set; }
    public string? NationalId { get; set; }
    public string FullName { get; set; } = null!;
    public DateOnly? BirthDate { get; set; }
    public string? Sex { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? BloodGroup { get; set; }
    public bool IsIncomplete { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public AppUser? CreatedByUser { get; set; }
    public RiskProfile? RiskProfile { get; set; }
    public ICollection<CathStudy> Studies { get; set; } = new List<CathStudy>();
}