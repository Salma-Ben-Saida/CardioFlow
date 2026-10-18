namespace CathLab.Application.DTOs.Archive;

public class ArchiveRequestDto
{
    public string? Reason { get; set; }
}

public class ArchivedPatientDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string? ConquestPatientID { get; set; }
    public int StudyCount { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedByName { get; set; }
    public string? DeleteReason { get; set; }
}

public class ArchivedStudyDto
{
    public Guid Id { get; set; }
    public string AccessionNumber { get; set; } = null!;
    public DateTime StudyDate { get; set; }
    public Guid PatientId { get; set; }
    public string? PatientName { get; set; }
    public bool PatientArchived { get; set; }
    public bool DeletedWithPatient { get; set; }
    public bool HasConfirmedReport { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedByName { get; set; }
    public string? DeleteReason { get; set; }
}

public class ArchiveOverviewDto
{
    public List<ArchivedPatientDto> Patients { get; set; } = new();
    public List<ArchivedStudyDto> Studies { get; set; } = new();
}