using CathLab.Application.Common;
using CathLab.Application.Interfaces;
using CathLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CathLab.Infrastructure.Persistence.Repositories;

public class PatientRepository : IPatientRepository
{
    private readonly CathLabDbContext _ctx;
    public PatientRepository(CathLabDbContext ctx) => _ctx = ctx;

    public async Task<Patient?> FindByConquestIdAsync(string conquestPatientId) =>
        await _ctx.Patients
            .FirstOrDefaultAsync(p => p.ConquestPatientID == conquestPatientId);

        public async Task<Patient> CreateAsync(Patient patient)
    {
        patient.Id = Guid.NewGuid();
        patient.CreatedAt = DateTime.UtcNow;
        patient.UpdatedAt = DateTime.UtcNow;
        _ctx.Patients.Add(patient);
        try
        {
            await _ctx.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            _ctx.Entry(patient).State = EntityState.Detached;
            throw new DuplicateEntityException($"Patient {patient.ConquestPatientID} already exists.", ex);
        }
        catch
        {
            _ctx.Entry(patient).State = EntityState.Detached;
            throw;
        }
        return patient;
    }

    public async Task<Patient> UpdateAsync(Patient patient)
    {
        patient.UpdatedAt = DateTime.UtcNow;
        _ctx.Patients.Update(patient);
        await _ctx.SaveChangesAsync();
        return patient;
    }
    // Tracked on purpose: this entity is modified and saved.
    public async Task<Patient?> FindByIdAsync(Guid id) =>
        await _ctx.Patients.FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Patient> UpdateAsync(Patient patient, byte[] rowVersion)
    {
        // Tell EF which version the client saw; the UPDATE affects 0 rows if it changed since.
        _ctx.Entry(patient).Property(p => p.RowVersion).OriginalValue = rowVersion;
        patient.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _ctx.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException(
                "Ce dossier a été modifié par un autre utilisateur. Rechargez la page puis réessayez.");
        }
        return patient;
    }

    public async Task<(List<Patient> Items, int TotalCount)> SearchAsync(
    string? search, 
    string? sex, 
    bool? isIncomplete, 
    string? sortBy,     // Added parameter
    string? sortDir,    // Added parameter
    int page, 
    int pageSize)
{
    var query = _ctx.Patients
        .Include(p => p.Studies)
        .AsNoTracking()
        .AsQueryable();

    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(p =>
            p.FullName.Contains(search) ||
            p.ConquestPatientID!.Contains(search) ||
            p.NationalId!.Contains(search));

    if (!string.IsNullOrWhiteSpace(sex))
        query = query.Where(p => p.Sex == sex);

    if (isIncomplete.HasValue)
        query = query.Where(p => p.IsIncomplete == isIncomplete.Value);

    // 1. Calculate the total count BEFORE pagination modifiers are applied
    var total = await query.CountAsync();

    // 2. Check if the sorting direction requested by the UI is descending
    bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

    // 3. Dynamically switch sorting column based on what header the user clicked
    query = (sortBy?.ToLowerInvariant()) switch
    {
        "fullname"  => desc ? query.OrderByDescending(p => p.FullName)  : query.OrderBy(p => p.FullName),
        "birthdate" => desc ? query.OrderByDescending(p => p.BirthDate) : query.OrderBy(p => p.BirthDate),
        "updatedat" => desc ? query.OrderByDescending(p => p.UpdatedAt) : query.OrderBy(p => p.UpdatedAt),
        _           => query.OrderByDescending(p => p.UpdatedAt), // Default sorting fallback
    };

    // 4. Fetch the paginated page subset from the database
    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return (items, total);
}
public async Task<Patient?> GetByIdWithDetailsAsync(Guid id) =>
    await _ctx.Patients
        .Include(p => p.RiskProfile)
        .Include(p => p.Studies)
            .ThenInclude(s => s.Operator)
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == id);

}