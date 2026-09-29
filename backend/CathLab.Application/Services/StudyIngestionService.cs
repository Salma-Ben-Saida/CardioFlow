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

    public async Task<IngestResult> IngestStudyAsync(string accessionNumber, PatientOverrideDto? overrideData = null)
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
        patient = BuildPatientFromPacs(dto, overrideData);
        patient = await _patientRepo.CreateAsync(patient);
    }
    else if (overrideData != null)
    {
        ApplyOverride(patient, overrideData);
        patient = await _patientRepo.UpdateAsync(patient);
    }

    // 4 — create study
    var study = new CathStudy
    {
        PatientId        = patient.Id,
        AccessionNumber  = dto.AccessionNumber,
        StudyInstanceUid = dto.StudyInstanceUid,
        StudyDate        = dto.StudyDate.HasValue
                            ? dto.StudyDate.Value.ToDateTime(TimeOnly.MinValue)
                            : DateTime.UtcNow,
        Status           = StudyStatus.Scheduled
    };
    study = await _studyRepo.CreateAsync(study);

    return IngestResult.Success(study.Id, patient.Id, patient.IsIncomplete);
}

public async Task<List<IngestResult>> IngestAllPendingAsync()
{
    // unchanged — background worker still ingests with no overrides
    var studies = await _pacsReader.GetPendingStudiesAsync(DateTime.UtcNow.AddDays(-30));
    var results = new List<IngestResult>();
    foreach (var dto in studies)
    {
        var result = await IngestStudyAsync(dto.AccessionNumber);
        results.Add(result);
    }
    return results;
}

private static Patient BuildPatientFromPacs(PacsStudyDto dto, PatientOverrideDto? overrideData)
{
    var patient = new Patient
    {
        ConquestPatientID = dto.PatientId,
        FullName          = FormatDicomName(dto.PatientName),
        BirthDate         = dto.PatientBirthDate,
        Sex               = dto.PatientSex,
        IsIncomplete      = true
    };

    if (overrideData != null)
        ApplyOverride(patient, overrideData);

    // Recompute completeness after any overrides were applied
    patient.IsIncomplete = patient.NationalId is null || patient.PhoneNumber is null
                         || patient.BloodGroup is null || patient.Address is null
                         || patient.BirthDate is null || string.IsNullOrWhiteSpace(patient.Sex);

    return patient;
}

// Applies non-empty override fields onto an existing (or freshly-built) patient.
// Blank/whitespace fields are left untouched so a partial edit never blanks
// out a value that was correct from PACS.
private static void ApplyOverride(Patient patient, PatientOverrideDto overrideData)
{
    if (!string.IsNullOrWhiteSpace(overrideData.FullName))
        patient.FullName = overrideData.FullName.Trim();

    if (overrideData.BirthDate.HasValue)
        patient.BirthDate = overrideData.BirthDate;

    if (!string.IsNullOrWhiteSpace(overrideData.Sex))
        patient.Sex = overrideData.Sex;

    if (!string.IsNullOrWhiteSpace(overrideData.NationalId))
        patient.NationalId = overrideData.NationalId.Trim();

    if (!string.IsNullOrWhiteSpace(overrideData.PhoneNumber))
        patient.PhoneNumber = overrideData.PhoneNumber.Trim();

    if (!string.IsNullOrWhiteSpace(overrideData.BloodGroup))
        patient.BloodGroup = overrideData.BloodGroup;

    if (!string.IsNullOrWhiteSpace(overrideData.Address))
        patient.Address = overrideData.Address.Trim();

    patient.UpdatedAt = DateTime.UtcNow;
}

private static string FormatDicomName(string? dicomName)
{
    if (string.IsNullOrEmpty(dicomName)) return "Inconnu";
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