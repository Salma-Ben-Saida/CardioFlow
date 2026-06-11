using CathLab.Application.DTOs.Pacs;
using CathLab.Application.Interfaces;
using CathLab.Domain.Entities;

namespace CathLab.Application.Services;

public class StudyIngestionService
{
    private readonly IPacsReader _pacsReader;
    private readonly IPatientRepository _patientRepo;
    private readonly ICathStudyRepository _studyRepo;

    public StudyIngestionService(
        IPacsReader pacsReader,
        IPatientRepository patientRepo,
        ICathStudyRepository studyRepo)
    {
        _pacsReader = pacsReader;
        _patientRepo = patientRepo;
        _studyRepo = studyRepo;
    }

    public async Task<IngestResult> IngestStudyAsync(string accessionNumber)
    {
        // 1 — fetch from PACS
        var dto = await _pacsReader.GetStudyByAccessionAsync(accessionNumber);
        if (dto == null)
            return IngestResult.Fail($"Study {accessionNumber} not found in PACS");

        // 2 — avoid duplicate
        var existing = await _studyRepo.FindByAccessionNumberAsync(accessionNumber);
        if (existing != null)
            return IngestResult.AlreadyExists(existing.Id);

        // 3 — find or create patient
        var patient = await _patientRepo.FindByConquestIdAsync(dto.PatientId);
        if (patient == null)
        {
            patient = BuildPatientFromPacs(dto);
            patient = await _patientRepo.CreateAsync(patient);
        }

        // 4 — create study
        var study = new CathStudy
        {
            PatientId       = patient.Id,
            AccessionNumber = dto.AccessionNumber,
            StudyInstanceUid = dto.StudyInstanceUid,
            StudyDate       = dto.StudyDate.HasValue
                                ? dto.StudyDate.Value.ToDateTime(TimeOnly.MinValue)
                                : DateTime.UtcNow,
            Status          = StudyStatus.Scheduled
        };
        study = await _studyRepo.CreateAsync(study);

        return IngestResult.Success(study.Id, patient.Id, patient.IsIncomplete);
    }

    public async Task<List<IngestResult>> IngestAllPendingAsync()
    {
        var studies = await _pacsReader.GetPendingStudiesAsync(
            DateTime.UtcNow.AddDays(-30));

        var results = new List<IngestResult>();
        foreach (var dto in studies)
        {
            var result = await IngestStudyAsync(dto.AccessionNumber);
            results.Add(result);
        }
        return results;
    }

    private static Patient BuildPatientFromPacs(PacsStudyDto dto)
    {
        // Clean DICOM name format "LASTNAME^FIRSTNAME^" → "Firstname Lastname"
        var fullName = FormatDicomName(dto.PatientName);

        return new Patient
        {
            ConquestPatientID = dto.PatientId,
            FullName          = fullName,
            BirthDate         = dto.PatientBirthDate,
            Sex               = dto.PatientSex,
            IsIncomplete      = true   // secretary must complete phone, NationalId etc.
        };
    }

    private static string FormatDicomName(string? dicomName)
    {
        if (string.IsNullOrEmpty(dicomName)) return "Inconnu";

        // DICOM format: "ARAB^FATIMA^^^" → "Fatima Arab"
        var parts = dicomName.Split('^', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{Capitalize(parts[1])} {Capitalize(parts[0])}"
            : Capitalize(parts[0]);
    }

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s :
        char.ToUpper(s[0]) + s[1..].ToLower();
}

public class IngestResult
{
    public bool Succeeded { get; private set; }
    public bool WasAlreadyIngested { get; private set; }
    public bool PatientIsIncomplete { get; private set; }
    public Guid? StudyId { get; private set; }
    public Guid? PatientId { get; private set; }
    public string? Error { get; private set; }

    public static IngestResult Success(Guid studyId, Guid patientId, bool incomplete) =>
        new() { Succeeded = true, StudyId = studyId,
                PatientId = patientId, PatientIsIncomplete = incomplete };

    public static IngestResult AlreadyExists(Guid studyId) =>
        new() { Succeeded = true, WasAlreadyIngested = true, StudyId = studyId };

    public static IngestResult Fail(string error) =>
        new() { Succeeded = false, Error = error };
}