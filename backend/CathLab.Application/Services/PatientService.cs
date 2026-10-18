using CathLab.Application.Common;
using CathLab.Application.DTOs.Patients;
using CathLab.Application.Interfaces;

namespace CathLab.Application.Services;

public class PatientService
{
    private readonly IPatientRepository _repo;
    private readonly AuditService _audit;

    public PatientService(IPatientRepository repo, AuditService audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task<PagedResult<PatientListItemDto>> GetListAsync(
        string? search, string? sex, string? status,
        string? sortBy, string? sortDir, int page, int pageSize)
    {
        bool? isIncomplete = status switch
        {
            "incomplete" => true,
            "complete"   => false,
            _            => null
        };

        var (items, total) = await _repo.SearchAsync(
            search, sex, isIncomplete, sortBy, sortDir, page, pageSize);

        var dtos = items.Select(p => new PatientListItemDto
        {
            Id                = p.Id,
            FullName          = p.FullName,
            ConquestPatientID = p.ConquestPatientID,
            NationalId        = p.NationalId,
            BirthDate         = p.BirthDate,
            Sex               = p.Sex,
            BloodGroup        = p.BloodGroup,
            PhoneNumber       = p.PhoneNumber,
            IsIncomplete      = p.IsIncomplete,
            StudyCount        = p.Studies.Count,
            CreatedAt         = p.CreatedAt,
            UpdatedAt         = p.UpdatedAt
        }).ToList();

        return new PagedResult<PatientListItemDto>(dtos, total, page, pageSize);
    }

    public async Task<PatientDetailDto?> GetPatientDetailAsync(Guid id)
    {
        var p = await _repo.GetByIdWithDetailsAsync(id);
        if (p == null) return null;

        return new PatientDetailDto
        {
            Id                = p.Id,
            FullName          = p.FullName,
            ConquestPatientID = p.ConquestPatientID,
            NationalId        = p.NationalId,
            BirthDate         = p.BirthDate,
            Sex               = p.Sex,
            BloodGroup        = p.BloodGroup,
            PhoneNumber       = p.PhoneNumber,
            Address           = p.Address,
            IsIncomplete      = p.IsIncomplete,
            CreatedAt         = p.CreatedAt,
            UpdatedAt         = p.UpdatedAt,
            RowVersion        = p.RowVersion,
            RiskProfile       = p.RiskProfile == null ? null : new RiskProfileDto
            {
                Id               = p.RiskProfile.Id,
                Hypertension     = p.RiskProfile.Hypertension,
                Diabetes         = p.RiskProfile.Diabetes,
                Dyslipidemia     = p.RiskProfile.Dyslipidemia,
                Smoking          = p.RiskProfile.Smoking,
                FamilyHistory    = p.RiskProfile.FamilyHistory,
                ObesityBMI       = p.RiskProfile.ObesityBMI,
                PriorMI          = p.RiskProfile.PriorMI,
                PriorPCI         = p.RiskProfile.PriorPCI,
                PriorCABG        = p.RiskProfile.PriorCABG,
                EjectionFraction = p.RiskProfile.EjectionFraction,
                Notes            = p.RiskProfile.Notes
            },
            Studies = p.Studies.Select(s => new CathStudyDto
            {
                Id              = s.Id,
                AccessionNumber = s.AccessionNumber,
                StudyInstanceUid = s.StudyInstanceUid,
                StudyDate       = s.StudyDate,
                Status          = s.Status.ToString(),
                OperatorName    = s.Operator?.FullName,
                CreatedAt       = s.CreatedAt
            }).OrderByDescending(s => s.StudyDate).ToList()
        };
    }

        private static readonly HashSet<string> ValidBloodGroups =
        new() { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };

    private static readonly HashSet<string> ValidSexValues = new() { "M", "F", "O" };

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>
    /// Manual edit by Secretaire/Admin. Returns null if the patient does not exist.
    /// Throws ArgumentException (validation) or ConcurrencyConflictException (stale RowVersion).
    /// </summary>
    public async Task<PatientDetailDto?> UpdateAsync(Guid id, UpdatePatientDto dto, Guid actorUserId)
    {
        // ---- validation ----
        if (dto.RowVersion is null || dto.RowVersion.Length == 0)
            throw new ArgumentException("Version du dossier manquante. Rechargez la page.");

        var fullName = Clean(dto.FullName);
        if (fullName is null)
            throw new ArgumentException("Le nom complet est obligatoire.");
        if (fullName.Length > 200)
            throw new ArgumentException("Le nom complet est trop long (200 caractères max).");

        var nationalId = Clean(dto.NationalId);
        if (nationalId is { Length: > 50 })
            throw new ArgumentException("L'identifiant national est trop long (50 caractères max).");

        var sex = Clean(dto.Sex)?.ToUpperInvariant();
        if (sex is not null && !ValidSexValues.Contains(sex))
            throw new ArgumentException("Sexe invalide.");

        var bloodGroup = Clean(dto.BloodGroup)?.ToUpperInvariant();
        if (bloodGroup is not null && !ValidBloodGroups.Contains(bloodGroup))
            throw new ArgumentException("Groupe sanguin invalide.");

        if (dto.BirthDate.HasValue && dto.BirthDate.Value > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("La date de naissance ne peut pas être dans le futur.");

        var phone   = Clean(dto.PhoneNumber);
        var address = Clean(dto.Address);

        // ---- load (tracked) ----
        var p = await _repo.FindByIdAsync(id);
        if (p == null) return null;

        // ---- diff, for the audit trail ----
        var changes = new List<string>();
        void Track(string field, string? before, string? after, bool showValues = false)
        {
            if (before == after) return;
            changes.Add(showValues
                ? $"{field}: {before ?? "(vide)"} → {after ?? "(vide)"}"
                : field);
        }

        Track("FullName",    p.FullName,                          fullName);
        Track("NationalId",  p.NationalId,                        nationalId);
        Track("BirthDate",   p.BirthDate?.ToString("yyyy-MM-dd"), dto.BirthDate?.ToString("yyyy-MM-dd"));
        Track("Sex",         p.Sex,                               sex,        showValues: true);
        Track("BloodGroup",  p.BloodGroup,                        bloodGroup, showValues: true);
        Track("PhoneNumber", p.PhoneNumber,                       phone);
        Track("Address",     p.Address,                           address);

        if (changes.Count == 0)
            return await GetPatientDetailAsync(id);   // nothing to save

        // ---- apply ----
        p.FullName    = fullName;
        p.NationalId  = nationalId;
        p.BirthDate   = dto.BirthDate;
        p.Sex         = sex;
        p.BloodGroup  = bloodGroup;
        p.PhoneNumber = phone;
        p.Address     = address;
        p.RecomputeCompleteness();

        await _repo.UpdateAsync(p, dto.RowVersion);

        await _audit.LogActionAsync(actorUserId, "Update", "Patient", id,
            "Champs modifiés : " + string.Join(", ", changes));

        return await GetPatientDetailAsync(id);
    }

    public async Task<List<CathStudyDto>> GetPatientStudiesAsync(Guid id)
    {
        var p = await _repo.GetByIdWithDetailsAsync(id);
        if (p == null) return new List<CathStudyDto>();

        return p.Studies.Select(s => new CathStudyDto
        {
            Id               = s.Id,
            AccessionNumber  = s.AccessionNumber,
            StudyInstanceUid = s.StudyInstanceUid,
            StudyDate        = s.StudyDate,
            Status           = s.Status.ToString(),
            OperatorName     = s.Operator?.FullName,
            CreatedAt        = s.CreatedAt
        }).OrderByDescending(s => s.StudyDate).ToList();
    }

    public async Task<RiskProfileDto?> GetPatientRiskProfileAsync(Guid id)
    {
        var p = await _repo.GetByIdWithDetailsAsync(id);
        if (p?.RiskProfile == null) return null;

        return new RiskProfileDto
        {
            Id               = p.RiskProfile.Id,
            Hypertension     = p.RiskProfile.Hypertension,
            Diabetes         = p.RiskProfile.Diabetes,
            Dyslipidemia     = p.RiskProfile.Dyslipidemia,
            Smoking          = p.RiskProfile.Smoking,
            FamilyHistory    = p.RiskProfile.FamilyHistory,
            ObesityBMI       = p.RiskProfile.ObesityBMI,
            PriorMI          = p.RiskProfile.PriorMI,
            PriorPCI         = p.RiskProfile.PriorPCI,
            PriorCABG        = p.RiskProfile.PriorCABG,
            EjectionFraction = p.RiskProfile.EjectionFraction,
            Notes            = p.RiskProfile.Notes
        };
    }
}

