using CathLab.Domain.Entities;

namespace CathLab.Application.Interfaces;

public interface IProcedureReportRepository
{
    Task<ProcedureReport?> FindByIdAsync(Guid id);
    Task<ProcedureReport?> FindByCathStudyIdAsync(Guid cathStudyId);
    Task<ProcedureReport> CreateAsync(ProcedureReport report);
    Task UpdateAsync(ProcedureReport report);
    
    /// <summary>Deletes the report together with its lesions and treatments.</summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// All reports for studies performed BY this doctor, across all of
    /// their patients. Ordered newest-first. Expected to Include the
    /// CathStudy -> Patient and CathStudy -> Operator navigations so the
    /// summary DTO can be built without extra round-trips.
    /// </summary>
    Task<List<ProcedureReport>> GetByDoctorIdAsync(Guid doctorId);
 
    /// <summary>
    /// All reports for a given patient, regardless of which doctor
    /// performed each study. Same Include expectations as above.
    /// </summary>
    Task<List<ProcedureReport>> GetByPatientIdAsync(Guid patientId);
 
    /// <summary>
    /// True if this doctor has performed at least one CathStudy for this
    /// patient (past or present) - i.e. whether the patient is "one of
    /// their patients" for authorization purposes.
    /// </summary>
    Task<bool> HasDoctorTreatedPatientAsync(Guid doctorId, Guid patientId);
 
    /// <summary>Admin-only: every report in the system, newest-first.</summary>
    Task<List<ProcedureReport>> GetAllAsync();
}