using CathLab.Domain.Entities;

namespace CathLab.Application.Interfaces;

/// <summary>Persistence for archive / restore / purge. All finders include archived rows.</summary>
public interface IArchiveRepository
{
    Task<CathStudy?> FindStudyAsync(Guid id);      // tracked, + Patient + ProcedureReport
    Task<Patient?> FindPatientAsync(Guid id);      // tracked, + Studies (+ ProcedureReport)

    Task<List<CathStudy>> GetArchivedStudiesAsync();
    Task<List<Patient>> GetArchivedPatientsAsync();
    Task<List<CathStudy>> GetStudiesByAccessionAsync(IEnumerable<string> accessionNumbers); // includes archived

    void RemoveStudy(CathStudy study);
    void RemovePatient(Patient patient);
    Task SaveChangesAsync();
}