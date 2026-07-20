namespace CathLab.Application.DTOs.Procedures;

public class CoronaryLesionDto
{
    public Guid Id { get; set; }
    public Guid ProcedureReportId { get; set; }
    public string Vessel { get; set; } = string.Empty;
    public byte SegmentNumber { get; set; }
    public byte StenosisPercent { get; set; }
    public string LesionType { get; set; } = string.Empty;
    public byte? TimiFlow { get; set; }
    public bool IsTreated { get; set; }
    public List<LesionTreatmentDto> Treatments { get; set; } = new();
    public string? RefDiam { get; set; }
    public bool Calcified { get; set; }
    public bool Thrombus { get; set; }
    public string? ResidualStenosis { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByUserId { get; set; }

}