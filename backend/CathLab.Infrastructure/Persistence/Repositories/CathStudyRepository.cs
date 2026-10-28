using CathLab.Application.Common;
using CathLab.Application.Interfaces;
using CathLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CathLab.Infrastructure.Persistence.Repositories;

public class CathStudyRepository : ICathStudyRepository
{
    private readonly CathLabDbContext _ctx;
    public CathStudyRepository(CathLabDbContext ctx) => _ctx = ctx;

    public async Task<CathStudy?> FindByStudyInstanceUidAsync(string studyInstanceUid) =>
        await _ctx.CathStudies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.StudyInstanceUid == studyInstanceUid);

    public async Task<CathStudy> CreateAsync(CathStudy study)
    {
        study.Id = Guid.NewGuid();
        study.CreatedAt = DateTime.UtcNow;
        _ctx.CathStudies.Add(study);
        try
        {
            await _ctx.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            _ctx.Entry(study).State = EntityState.Detached;   // keep the scoped context clean
            throw new DuplicateEntityException($"Study {study.StudyInstanceUid} already exists.", ex);
        }
        catch
        {
            _ctx.Entry(study).State = EntityState.Detached;
            throw;
        }
        return study;
    }
    public async Task<CathStudy?> FindByIdAsync(Guid id) =>
    await _ctx.CathStudies
        .Include(s => s.Operator) 
        .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<List<CathStudy>> GetAllWithPatientAsync() =>
    await _ctx.CathStudies
        .AsNoTracking()
        .Include(s => s.Patient)
        .OrderByDescending(s => s.CreatedAt)
        .ToListAsync();

    public async Task UpdateAsync(CathStudy study) => await _ctx.SaveChangesAsync();

    public async Task<int> StartDueStudiesAsync(DateTime nowUtc)
    {
        var due = await _ctx.CathStudies
            .Where(s => s.Status == StudyStatus.Scheduled && s.ScheduledAt != null && s.ScheduledAt <= nowUtc)
            .ToListAsync();
        foreach (var s in due) s.StartIfDue(nowUtc);
        if (due.Count > 0) await _ctx.SaveChangesAsync();
        return due.Count;
    }
}