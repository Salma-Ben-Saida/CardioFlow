namespace CathLab.Domain.Entities;

public class CathStudy
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? OperatorId { get; set; }

    public string AccessionNumber { get; set; } = null!;
    public string? StudyInstanceUid { get; set; }
    public DateTime StudyDate { get; set; }
    public StudyStatus Status { get; set; } = StudyStatus.Scheduled;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Patient? Patient { get; set; }
    public AppUser? Operator { get; set; }

    public ProcedureReport? ProcedureReport { get; set; }
    public ICollection<InventoryItem> ReservedItems { get; set; } = new List<InventoryItem>();
}