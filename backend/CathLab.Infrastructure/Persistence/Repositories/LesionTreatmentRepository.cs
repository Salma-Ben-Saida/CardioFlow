using CathLab.Application.Interfaces;
using CathLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CathLab.Infrastructure.Persistence.Repositories;

public class LesionTreatmentRepository : ILesionTreatmentRepository
{
    private readonly CathLabDbContext _db;

    public LesionTreatmentRepository(CathLabDbContext db)
    {
        _db = db;
    }

    // LesionTreatment is abstract (TPT base) - no DbSet<LesionTreatment>
    // property on the context, but it's still queryable via Set<T>() since
    // it's mapped with its own table for shared columns.
    public async Task<LesionTreatment?> FindByIdAsync(Guid id) =>
        await _db.Set<LesionTreatment>().FirstOrDefaultAsync(t => t.Id == id);

    public async Task<StentImplant> CreateStentAsync(StentImplant stent)
    {
        _db.StentImplants.Add(stent);
        await _db.SaveChangesAsync();
        return stent;
    }

    public async Task<BalloonDilation> CreateBalloonAsync(BalloonDilation balloon)
    {
        _db.BalloonDilations.Add(balloon);
        await _db.SaveChangesAsync();
        return balloon;
    }
}