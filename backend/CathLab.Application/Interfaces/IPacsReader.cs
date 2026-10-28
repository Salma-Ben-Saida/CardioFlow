using CathLab.Application.DTOs.Pacs;

namespace CathLab.Application.Interfaces;

public interface IPacsReader
{
    Task<PacsStudyDto?> GetStudyByUidAsync(
    string studyInstanceUid);
    Task<List<PacsStudyDto>> GetPendingStudiesAsync();
    Task<List<PacsStudyDto>> GetPatientStudiesAsync(string patientId);
}