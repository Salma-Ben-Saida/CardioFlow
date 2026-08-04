namespace CathLab.Domain.Entities;

/// <summary>
/// Generic audit-trail row for a meaningful action taken anywhere in the
/// system (create/update/delete/confirm, etc.) — not tied to any one
/// module. Distinct from StockMovement, which is pharmacy-specific and
/// quantity-driven. Distinct from UserActivityLog, which only tracks
/// authentication events.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public Guid ActorUserId { get; set; }

    /// <summary>e.g. "Create", "Update", "Delete", "Confirm"</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>e.g. "Patient", "User", "InventoryProduct", "ProcedureReport"</summary>
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }

    /// <summary>Short free-text detail, e.g. "Role changed Medecin -> Admin"</summary>
    public string? Detail { get; set; }

    public DateTime OccurredAt { get; set; }

    // Navigation
    public AppUser ActorUser { get; set; } = null!;
}