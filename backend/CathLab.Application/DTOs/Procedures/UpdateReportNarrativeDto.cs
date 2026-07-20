// UpdateReportNarrativeDto.cs
namespace CathLab.Application.DTOs.Procedures;

/// <summary>
/// PUT body to fill in the narrative fields that GetMissingRequiredFields
/// checks before allowing confirmation. Sent from the "Rapport" panel
/// before the doctor hits "Confirmer".
/// </summary>
public class UpdateReportNarrativeDto
{
    public string? Approach { get; set; }
    public string? CatheterUsed { get; set; }
    public string? Conclusion { get; set; }
    public string? DominanceType { get; set; }
    public byte? TimiFlowBaseline { get; set; }
    public byte? TimiFlowFinal { get; set; }
    public decimal? ContrastVolumeMl { get; set; }
    public decimal? DoseAreaProductGycm2 { get; set; }
    public string? Recommendations { get; set; }
}