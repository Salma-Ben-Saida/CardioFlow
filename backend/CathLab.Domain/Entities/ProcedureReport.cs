namespace CathLab.Domain.Entities;

public class ProcedureReport
{
    public Guid Id { get; set; }
    public Guid CathStudyId { get; set; }

    public string? Approach { get; set; }
    public string? CatheterUsed { get; set; }
    public decimal? ContrastVolumeMl { get; set; }
    public decimal? DoseAreaProductGycm2 { get; set; }
    public string? DominanceType { get; set; }
    public byte? TimiFlowBaseline { get; set; }
    public byte? TimiFlowFinal { get; set; }
    public string? Conclusion { get; set; }
    public DateTime ReportGeneratedAt { get; set; }
    public string? PdfStoragePath { get; set; }
    public bool IsConfirmed { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    // Navigation
    public CathStudy CathStudy { get; set; } = null!;
    public ICollection<CoronaryLesion> Lesions { get; set; } = new List<CoronaryLesion>();
}