namespace CathLab.Domain.Entities;

public class CathStudy
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? OperatorId { get; set; }

    public string AccessionNumber { get; set; } = null!;
    public string? StudyInstanceUid { get; set; }
    public DateTime StudyDate { get; set; }
    public StudyStatus Status { get; set; } = StudyStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ScheduledAt { get; set; }
    
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

        public void Schedule(DateTime scheduledAtUtc)
    {
        if (Status is StudyStatus.InProgress or StudyStatus.Completed)
            throw new InvalidOperationException("Une étude en cours ou terminée ne peut plus être planifiée.");
        ScheduledAt = scheduledAtUtc;
        Status = StudyStatus.Scheduled;
    }

    public void Cancel()
    {
        if (Status is not (StudyStatus.Pending or StudyStatus.Scheduled))
            throw new InvalidOperationException("Seule une étude à planifier ou planifiée peut être annulée.");
        Status = StudyStatus.Cancelled;
        ScheduledAt = null;
    }

    public void StartIfDue(DateTime nowUtc)
    {
        if (Status == StudyStatus.Scheduled && ScheduledAt <= nowUtc)
            Status = StudyStatus.InProgress;
    }

    public void Complete()
    {
        if (Status != StudyStatus.Cancelled)
            Status = StudyStatus.Completed;
    }

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