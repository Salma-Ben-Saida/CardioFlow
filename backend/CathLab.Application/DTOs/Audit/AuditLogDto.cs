namespace CathLab.Application.DTOs.Audit;

public class AuditLogDto
{
    public Guid Id { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? Detail { get; set; }
    public DateTime OccurredAt { get; set; }
}

public class UserActivityLogDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime OccurredAt { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}