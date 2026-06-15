namespace CathLab.Application.DTOs.Patients;

public class CathStudyDto
{
    public Guid Id { get; set; }
    public string AccessionNumber { get; set; } = null!;
    public string? StudyInstanceUid { get; set; }
    public DateTime StudyDate { get; set; }
    public string Status { get; set; } = null!;
    public string? OperatorName { get; set; }
    public DateTime CreatedAt { get; set; }
}