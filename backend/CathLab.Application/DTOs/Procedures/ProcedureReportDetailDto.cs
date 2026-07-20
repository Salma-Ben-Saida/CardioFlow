namespace CathLab.Application.DTOs.Procedures;

public class ProcedureReportDetailDto
{
    public Guid Id { get; set; }
    public Guid CathStudyId { get; set; }
    public bool IsConfirmed { get; set; }
    public DateTime ReportGeneratedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    public string? Approach { get; set; }
    public string? CatheterUsed { get; set; }
    public string? Conclusion { get; set; }
    public string? DominanceType { get; set; }
    public byte? TimiFlowBaseline { get; set; }
    public byte? TimiFlowFinal { get; set; }
    public decimal? ContrastVolumeMl { get; set; }
    public decimal? DoseAreaProductGycm2 { get; set; }

    public List<string> MissingRequiredFields { get; set; } = new(); 
    public List<CoronaryLesionDto> Lesions { get; set; } = new();
    public string? Recommendations { get; set; }
    public string? ConfirmedByUserId { get; set; }
    public string? ConfirmedByUser { get; set; }
}