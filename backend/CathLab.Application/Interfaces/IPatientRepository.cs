using CathLab.Domain.Entities;

namespace CathLab.Application.Interfaces;

public interface IPatientRepository
{
    Task<Patient?> FindByConquestIdAsync(string conquestPatientId);
    Task<Patient> CreateAsync(Patient patient);
    Task<Patient> UpdateAsync(Patient patient);

    Task<(List<Patient> Items, int TotalCount)> SearchAsync(
    string? search,
    string? sex,
    bool? isIncomplete,
    string? sortBy,     
    string? sortDir,    
    int page,
    int pageSize);
    Task<Patient?> GetByIdWithDetailsAsync(Guid id);
}