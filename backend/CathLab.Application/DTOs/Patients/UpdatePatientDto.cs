namespace CathLab.Application.DTOs.Patients;

public class UpdatePatientDto
{
    public string FullName { get; set; } = null!;
    public string? NationalId { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Sex { get; set; }
    public string? BloodGroup { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }

    /// <summary>The RowVersion the client last saw (optimistic concurrency).</summary>
    public byte[] RowVersion { get; set; } = null!;
}