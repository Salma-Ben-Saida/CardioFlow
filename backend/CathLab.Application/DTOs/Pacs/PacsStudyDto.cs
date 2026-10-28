namespace CathLab.Application.DTOs.Pacs;

public class PacsStudyDto
{
    public string StudyInstanceUid { get; set; } = null!;
    public string AccessionNumber { get; set; } = "";
    public string PatientId { get; set; } = null!;
    public string? PatientName { get; set; }
    public DateOnly? PatientBirthDate { get; set; }
    public string? PatientSex { get; set; }
    public DateOnly? StudyDate { get; set; }
    public string? Modality { get; set; }
}