namespace CathLab.Application.DTOs.Patients;

public class StudyListItemDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = "";
    public string AccessionNumber { get; set; } = "";
    public DateTime IngestedAt { get; set; }
    public DateTime StudyDate { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public string Status { get; set; } = "";
    public bool NeedsScheduling { get; set; }
}

public class ScheduleStudyDto
{
    public DateTime ScheduledAt { get; set; }
}