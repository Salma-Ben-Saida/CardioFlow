namespace CathLab.Application.DTOs.Pacs;

public class PatientOverrideDto
{
    public string? FullName { get; set; }
    public string? NationalId { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Sex { get; set; }
    public string? PhoneNumber { get; set; }
    public string? BloodGroup { get; set; }
    public string? Address { get; set; }
}