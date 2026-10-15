using CathLab.Application.DTOs.Audit;
using CathLab.Application.Interfaces;
using CathLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CathLab.Infrastructure.Persistence.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly CathLabDbContext _ctx;

    public AuditRepository(CathLabDbContext context)
    {
        _ctx = context;
    }

    public async Task AddAuditLogAsync(AuditLog auditLog)
    {
        _ctx.AuditLogs.Add(auditLog);
        await _ctx.SaveChangesAsync();
    }

    public async Task AddUserActivityLogAsync(UserActivityLog activityLog)
    {
        _ctx.UserActivityLogs.Add(activityLog);
        await _ctx.SaveChangesAsync();
    }

    public async Task<PagedResult<AuditLogDto>> GetAuditsAsync(int page, int pageSize)
    {
        var query = _ctx.AuditLogs
            .Include(a => a.ActorUser)
            .OrderByDescending(a => a.OccurredAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                ActorName = a.ActorUser.Username,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Detail = a.Detail,
                OccurredAt = a.OccurredAt,
            })
            .ToListAsync();

        return new PagedResult<AuditLogDto> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<PagedResult<UserActivityLogDto>> GetActivityLogsAsync(int page, int pageSize)
    {
        var query = _ctx.UserActivityLogs
            .OrderByDescending(l => l.OccurredAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new UserActivityLogDto
            {
                Id = l.Id,
                Username = l.Username,
                Action = l.Action,
                IpAddress = l.IpAddress,
                UserAgent = l.UserAgent,
                OccurredAt = l.OccurredAt,
            })
            .ToListAsync();

        return new PagedResult<UserActivityLogDto> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }
}
