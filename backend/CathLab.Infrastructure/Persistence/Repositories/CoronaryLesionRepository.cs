using CathLab.Application.Interfaces;
using CathLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CathLab.Infrastructure.Persistence.Repositories;

public class CoronaryLesionRepository : ICoronaryLesionRepository
{
    private readonly CathLabDbContext _db;

    public CoronaryLesionRepository(CathLabDbContext db)
    {
        _db = db;
    }

    public async Task<CoronaryLesion?> FindByIdAsync(Guid id) =>
        await _db.CoronaryLesions.FirstOrDefaultAsync(l => l.Id == id);

    public async Task<List<CoronaryLesion>> GetByProcedureReportAsync(Guid procedureReportId) =>
        await _db.CoronaryLesions
            .Where(l => l.ProcedureReportId == procedureReportId)
            .Include(l => l.Treatments)
            .ToListAsync();

    public async Task<CoronaryLesion> CreateAsync(CoronaryLesion lesion)
    {
        _db.CoronaryLesions.Add(lesion);
        await _db.SaveChangesAsync();
        return lesion;
    }

    public async Task UpdateAsync(CoronaryLesion lesion)
    {
        _db.CoronaryLesions.Update(lesion);
        await _db.SaveChangesAsync();
    }
}