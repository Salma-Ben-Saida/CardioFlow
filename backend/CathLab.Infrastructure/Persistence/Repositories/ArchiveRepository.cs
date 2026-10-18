using CathLab.Application.Interfaces;
using CathLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CathLab.Infrastructure.Persistence.Repositories;

public class ArchiveRepository : IArchiveRepository
{
    private readonly CathLabDbContext _ctx;
    public ArchiveRepository(CathLabDbContext ctx) => _ctx = ctx;

    public async Task<CathStudy?> FindStudyAsync(Guid id) =>
        await _ctx.CathStudies
            .IgnoreQueryFilters()
            .Include(s => s.Patient)
            .Include(s => s.ProcedureReport)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<Patient?> FindPatientAsync(Guid id) =>
        await _ctx.Patients
            .IgnoreQueryFilters()
            .Include(p => p.Studies).ThenInclude(s => s.ProcedureReport)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<List<CathStudy>> GetArchivedStudiesAsync() =>
        await _ctx.CathStudies
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(s => s.Patient)
            .Include(s => s.ProcedureReport)
            .Where(s => s.IsDeleted)
            .OrderByDescending(s => s.DeletedAt)
            .ToListAsync();

    public async Task<List<Patient>> GetArchivedPatientsAsync() =>
        await _ctx.Patients
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(p => p.Studies)
            .Where(p => p.IsDeleted)
            .OrderByDescending(p => p.DeletedAt)
            .ToListAsync();

    public void RemoveStudy(CathStudy study) => _ctx.CathStudies.Remove(study);
    public void RemovePatient(Patient patient) => _ctx.Patients.Remove(patient);
    public Task SaveChangesAsync() => _ctx.SaveChangesAsync();
}