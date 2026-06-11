using CathLab.Domain.Entities;

namespace CathLab.Application.Interfaces;

public interface IPatientRepository
{
    Task<Patient?> FindByConquestIdAsync(string conquestPatientId);
    Task<Patient> CreateAsync(Patient patient);
    Task<Patient> UpdateAsync(Patient patient);
}