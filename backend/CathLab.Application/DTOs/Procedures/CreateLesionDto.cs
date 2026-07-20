namespace CathLab.Application.DTOs.Procedures;

/// <summary>
/// Placing a Lesion marker in the segmentation UI. If no ProcedureReport
/// exists yet for this CathStudy, ProcedureService creates one on the fly.
/// </summary>
public class CreateLesionDto
{
    public Guid CathStudyId { get; set; }
    public string Vessel { get; set; } = string.Empty;
    public byte SegmentNumber { get; set; }
    public byte StenosisPercent { get; set; }
    public string LesionType { get; set; } = string.Empty;
    public byte? TimiFlow { get; set; }
    public string? RefDiam { get; set; }
    public bool Calcified { get; set; }
    public bool Thrombus { get; set; }
    public string? ResidualStenosis { get; set; }
    public string? CreatedByUserId { get; set; }

}