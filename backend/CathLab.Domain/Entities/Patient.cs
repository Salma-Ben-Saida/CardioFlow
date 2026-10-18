namespace CathLab.Domain.Entities;

public class Patient
{
    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }

    public string? ConquestPatientID { get; set; }
    public string? NationalId { get; set; }
    public string FullName { get; set; } = null!;
    public DateOnly? BirthDate { get; set; }
    public string? Sex { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? BloodGroup { get; set; }
    public bool IsIncomplete { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Optimistic concurrency token (SQL Server rowversion)
    public byte[] RowVersion { get; set; } = null!;

    // ---- Soft delete (archive) ----
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public string? DeleteReason { get; set; }

    public AppUser? CreatedByUser { get; set; }
    public RiskProfile? RiskProfile { get; set; }
    public ICollection<CathStudy> Studies { get; set; } = new List<CathStudy>();
    
    /// <summary>Single source of truth for the "dossier incomplet" rule.</summary>
    public void RecomputeCompleteness() =>
        IsIncomplete = string.IsNullOrWhiteSpace(NationalId)
                    || string.IsNullOrWhiteSpace(PhoneNumber)
                    || string.IsNullOrWhiteSpace(BloodGroup)
                    || string.IsNullOrWhiteSpace(Address)
                    || BirthDate is null
                    || string.IsNullOrWhiteSpace(Sex);

        public void Archive(Guid byUserId, string reason, DateTime nowUtc)
    {
        IsDeleted = true;
        DeletedAt = nowUtc;
        DeletedByUserId = byUserId;
        DeleteReason = reason;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedByUserId = null;
        DeleteReason = null;
    }
}