namespace CathLab.Domain.Entities;

public class CoronaryLesion
{
    public Guid Id { get; set; }
    public Guid ProcedureReportId { get; set; }

    public string Vessel { get; set; } = string.Empty;
    public byte SegmentNumber { get; set; }
    public byte StenosisPercent { get; set; }
    public LesionType LesionType { get; set; }
    public byte? TimiFlow { get; set; }
    public bool IsTreated { get; set; }

    // Navigation
    public ProcedureReport ProcedureReport { get; set; } = null!;

    // A lesion can go through several treatments in sequence
    // (e.g. pre-dilation balloon -> stent -> post-dilation balloon).
    public ICollection<LesionTreatment> Treatments { get; set; } = new List<LesionTreatment>();
}