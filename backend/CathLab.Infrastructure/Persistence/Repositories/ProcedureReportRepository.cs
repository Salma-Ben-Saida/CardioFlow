using CathLab.Application.Interfaces;
using CathLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CathLab.Infrastructure.Persistence.Repositories;

public class ProcedureReportRepository : IProcedureReportRepository
{
    private readonly CathLabDbContext _db;

    public ProcedureReportRepository(CathLabDbContext db)
    {
        _db = db;
    }

    public async Task<ProcedureReport?> FindByIdAsync(Guid id) =>
        await _db.ProcedureReports.Include(r => r.Lesions).FirstOrDefaultAsync(r => r.Id == id);

    public async Task<ProcedureReport?> FindByCathStudyIdAsync(Guid cathStudyId) =>
        await _db.ProcedureReports.FirstOrDefaultAsync(r => r.CathStudyId == cathStudyId);

    public async Task<ProcedureReport> CreateAsync(ProcedureReport report)
    {
        _db.ProcedureReports.Add(report);
        await _db.SaveChangesAsync();
        return report;
    }

    public async Task UpdateAsync(ProcedureReport report)
    {
        _db.ProcedureReports.Update(report);
        await _db.SaveChangesAsync();
    }
}