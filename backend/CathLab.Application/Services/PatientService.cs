using CathLab.Application.Common;
using CathLab.Application.DTOs.Patients;
using CathLab.Application.Interfaces;

namespace CathLab.Application.Services;

public class PatientService
{
    private readonly IPatientRepository _repo;

    public PatientService(IPatientRepository repo) => _repo = repo;

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

