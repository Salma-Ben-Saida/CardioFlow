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
        await _ctx.SaveChangesAsync();
        return patient;
    }

    public async Task<Patient> UpdateAsync(Patient patient)
    {
        patient.UpdatedAt = DateTime.UtcNow;
        _ctx.Patients.Update(patient);
        await _ctx.SaveChangesAsync();
        return patient;
    }
}