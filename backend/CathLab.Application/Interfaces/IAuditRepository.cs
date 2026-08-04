using CathLab.Application.DTOs.Audit;
using CathLab.Domain.Entities;

namespace CathLab.Application.Interfaces;

public interface IAuditRepository
{
    Task AddAuditLogAsync(AuditLog auditLog);
    Task AddUserActivityLogAsync(UserActivityLog activityLog);
    Task<PagedResult<AuditLogDto>> GetAuditsAsync(int page, int pageSize);
    Task<PagedResult<UserActivityLogDto>> GetActivityLogsAsync(int page, int pageSize);
}
