namespace CathLab.Application.DTOs.Patients;

public class PatientDetailDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string? ConquestPatientID { get; set; }
    public string? NationalId { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Sex { get; set; }
    public string? BloodGroup { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public bool IsIncomplete { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = null!;
    public RiskProfileDto? RiskProfile { get; set; }
    public List<CathStudyDto> Studies { get; set; } = new();
}