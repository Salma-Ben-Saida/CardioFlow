using CathLab.Application.DTOs.Audit;
using CathLab.Application.Interfaces;
using CathLab.Domain.Entities;

namespace CathLab.Application.Services;

public class AuditService
{
    private readonly IAuditRepository _auditRepository;

    public AuditService(IAuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    /* ---------- Writing ---------- */

    public async Task LogActionAsync(Guid actorUserId, string action, string entityType, Guid entityId, string? detail = null)
    {
        await _auditRepository.AddAuditLogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Detail = detail,
            OccurredAt = DateTime.UtcNow,
        });
    }

    public async Task LogActivityAsync(Guid? userId, string username, string action, string? ipAddress, string? userAgent)
    {
        await _auditRepository.AddUserActivityLogAsync(new UserActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Username = username,
            Action = action,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            OccurredAt = DateTime.UtcNow,
        });
    }

    /* ---------- Reading ---------- */

    public Task<PagedResult<AuditLogDto>> GetAuditsAsync(int page, int pageSize)
    {
        return _auditRepository.GetAuditsAsync(page, pageSize);
    }

    public Task<PagedResult<UserActivityLogDto>> GetActivityLogsAsync(int page, int pageSize)
    {
        return _auditRepository.GetActivityLogsAsync(page, pageSize);
    }
}