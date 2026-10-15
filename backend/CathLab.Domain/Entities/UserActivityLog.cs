namespace CathLab.Domain.Entities;

/// <summary>
/// Authentication activity row — successful/failed login attempts and
/// logouts. Kept separate from AuditLog so login noise doesn't drown out
/// meaningful business-action audits, and so it can be queried/purged on
/// its own retention policy if needed.
/// </summary>
public class UserActivityLog
{
    public Guid Id { get; set; }

    /// <summary>Null on a failed login attempt against an unknown username.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Username as typed, kept even on failure for investigation.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>"LoginSuccess", "LoginFailed", "Logout"</summary>
    public string Action { get; set; } = string.Empty;

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime OccurredAt { get; set; }

}