namespace CathLab.Application.DTOs.Pacs;

public class PacsStudyStateDto
{
    public string AccessionNumber { get; set; } = ""; 
    public string StudyInstanceUid { get; set; } = null!;
    public string PatientDicomId { get; set; } = string.Empty;
    public string? PatientName { get; set; }
    public DateOnly? StudyDate { get; set; }
    public string? Modality { get; set; }

    /// <summary>New | Ingested | Archived | PatientArchived</summary>
    public string State { get; set; } = "New";

    public Guid? CathStudyId { get; set; }
    public Guid? PatientId { get; set; }
}