namespace CathLab.Application.DTOs.Procedures;

/// <summary>
/// Lightweight projection for report list views (doctor's dashboard,
/// patient history) - deliberately excludes lesion/treatment detail,
/// which belongs to the single-report detail endpoint.
/// </summary>
public class ProcedureReportSummaryDto
{
    public Guid Id { get; set; }
    public Guid CathStudyId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public bool IsConfirmed { get; set; }
    public DateTime ReportGeneratedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string? PdfStoragePath { get; set; }
}