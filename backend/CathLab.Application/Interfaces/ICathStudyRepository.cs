using CathLab.Domain.Entities;

namespace CathLab.Application.Interfaces;

public interface ICathStudyRepository
{
    Task<CathStudy?> FindByStudyInstanceUidAsync(
    string studyInstanceUid);
    Task<CathStudy> CreateAsync(CathStudy study);
    Task<CathStudy?> FindByIdAsync(Guid id);
    Task<List<CathStudy>> GetAllWithPatientAsync();
    Task UpdateAsync(CathStudy study);                 // study must be tracked (loaded via FindByIdAsync)
    Task<int> StartDueStudiesAsync(DateTime nowUtc);

}