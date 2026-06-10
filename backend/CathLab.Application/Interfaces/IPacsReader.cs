using CathLab.Application.DTOs.Pacs;

namespace CathLab.Application.Interfaces;

public interface IPacsReader
{
    Task<PacsStudyDto?> GetStudyByAccessionAsync(string accessionNumber);
    Task<List<PacsStudyDto>> GetPendingStudiesAsync(DateTime since);
    Task<List<PacsStudyDto>> GetPatientStudiesAsync(string patientId);
}