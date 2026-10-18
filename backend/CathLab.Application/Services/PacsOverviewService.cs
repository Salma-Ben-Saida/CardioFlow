using CathLab.Application.DTOs.Pacs;
using CathLab.Application.Interfaces;

namespace CathLab.Application.Services;

/// <summary>Lists Orthanc studies and tells, for each, where it stands in CathLab.</summary>
public class PacsOverviewService
{
    private readonly IPacsReader _pacs;
    private readonly IArchiveRepository _repo;

    public PacsOverviewService(IPacsReader pacs, IArchiveRepository repo)
    {
        _pacs = pacs;
        _repo = repo;
    }

    public async Task<List<PacsStudyStateDto>> GetAsync()
    {
        var pacsStudies = await _pacs.GetPendingStudiesAsync();

        var accessions = pacsStudies.Select(s => s.AccessionNumber).Distinct().ToList();
        var dbStudies = (await _repo.GetStudiesByAccessionAsync(accessions))
            .ToDictionary(s => s.AccessionNumber);

        var archivedPatients = await _repo.GetArchivedPatientsAsync();
        var archivedIds = archivedPatients.Select(p => p.Id).ToHashSet();
        var archivedByDicomId = archivedPatients
            .Where(p => !string.IsNullOrEmpty(p.ConquestPatientID))
            .ToDictionary(p => p.ConquestPatientID!);

        var result = new List<PacsStudyStateDto>();
        foreach (var s in pacsStudies)
        {
            var dto = new PacsStudyStateDto
            {
                AccessionNumber  = s.AccessionNumber,
                StudyInstanceUid = s.StudyInstanceUid,
                PatientDicomId   = s.PatientId,
                PatientName      = s.PatientName,
                StudyDate        = s.StudyDate,
                Modality         = s.Modality,
                State            = "New"
            };

            if (dbStudies.TryGetValue(s.AccessionNumber, out var db))
            {
                dto.CathStudyId = db.Id;
                dto.PatientId   = db.PatientId;
                dto.State = archivedIds.Contains(db.PatientId) ? "PatientArchived"
                          : db.IsDeleted ? "Archived"
                          : "Ingested";
            }
            else if (archivedByDicomId.TryGetValue(s.PatientId, out var archived))
            {
                // not in the DB, but its patient is archived: ingestion skips it
                dto.PatientId = archived.Id;
                dto.State = "PatientArchived";
            }

            result.Add(dto);
        }

        return result
            .OrderBy(d => d.State == "New" ? 0 : 1)
            .ThenByDescending(d => d.StudyDate)
            .ToList();
    }
}