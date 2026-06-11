using CathLab.Domain.Entities;

namespace CathLab.Application.Interfaces;

public interface ICathStudyRepository
{
    Task<CathStudy?> FindByAccessionNumberAsync(string accessionNumber);
    Task<CathStudy> CreateAsync(CathStudy study);
}