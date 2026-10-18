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
    
    // ---- Soft delete (archive) ----
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public string? DeleteReason { get; set; }

    /// <summary>True when archived as part of archiving its patient (so restoring the patient restores it).</summary>
    public bool DeletedWithPatient { get; set; }

    public Patient? Patient { get; set; }
    public AppUser? Operator { get; set; }

    public ProcedureReport? ProcedureReport { get; set; }
    public ICollection<InventoryItem> ReservedItems { get; set; } = new List<InventoryItem>();

    public void Archive(Guid byUserId, string reason, DateTime nowUtc, bool withPatient = false)
    {
        IsDeleted = true;
        DeletedAt = nowUtc;
        DeletedByUserId = byUserId;
        DeleteReason = reason;
        DeletedWithPatient = withPatient;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedByUserId = null;
        DeleteReason = null;
        DeletedWithPatient = false;
    }
}