using CathLab.Application.Common;
using CathLab.Application.Interfaces;
using CathLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CathLab.Infrastructure.Persistence.Repositories;

public class CathStudyRepository : ICathStudyRepository
{
    private readonly CathLabDbContext _ctx;
    public CathStudyRepository(CathLabDbContext ctx) => _ctx = ctx;

    public async Task<CathStudy?> FindByAccessionNumberAsync(string accessionNumber) =>
        await _ctx.CathStudies
            .FirstOrDefaultAsync(s => s.AccessionNumber == accessionNumber);

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
            throw new DuplicateEntityException($"Study {study.AccessionNumber} already exists.", ex);
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
}