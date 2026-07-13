using CathLab.Application.DTOs.Procedures;
using CathLab.Application.Interfaces;
using CathLab.Domain.Entities;

namespace CathLab.Application.Services;

public class ProcedureService
{
    private readonly IProcedureReportRepository _reports;
    private readonly ICoronaryLesionRepository _lesions;
    private readonly ILesionTreatmentRepository _treatments;
    private readonly InventoryService _inventory;
    private readonly IUnitOfWork _unitOfWork;

    public ProcedureService(
        IProcedureReportRepository reports,
        ICoronaryLesionRepository lesions,
        ILesionTreatmentRepository treatments,
        InventoryService inventory,
        IUnitOfWork unitOfWork)
    {
        _reports = reports;
        _lesions = lesions;
        _treatments = treatments;
        _inventory = inventory;
        _unitOfWork = unitOfWork;
    }

    /// <summary>Placing a Lesion marker. Creates the ProcedureReport on first use for this CathStudy.</summary>
    public async Task<CoronaryLesionDto> AddLesionAsync(CreateLesionDto dto)
    {
        if (!Enum.TryParse<LesionType>(dto.LesionType, ignoreCase: true, out var lesionType))
            throw new ArgumentException($"Unknown lesion type '{dto.LesionType}'.");

        var report = await _reports.FindByCathStudyIdAsync(dto.CathStudyId);
        if (report is null)
        {
            report = new ProcedureReport
            {
                Id = Guid.NewGuid(),
                CathStudyId = dto.CathStudyId,
                ReportGeneratedAt = DateTime.UtcNow
            };
            await _reports.CreateAsync(report);
        }

        var lesion = new CoronaryLesion
        {
            Id = Guid.NewGuid(),
            ProcedureReportId = report.Id,
            Vessel = dto.Vessel,
            SegmentNumber = dto.SegmentNumber,
            StenosisPercent = dto.StenosisPercent,
            LesionType = lesionType,
            TimiFlow = dto.TimiFlow,
            IsTreated = false
        };
        await _lesions.CreateAsync(lesion);

        return ToDto(lesion);
    }

    /// <summary>
    /// Adding a Stent treatment - must reference an existing Lesion.
    /// Consumes the selected InventoryItem before persisting the treatment,
    /// so a treatment is never recorded against stock that wasn't actually used.
    /// </summary>
    public async Task<LesionTreatmentDto> AddStentTreatmentAsync(CreateStentTreatmentDto dto, Guid performedBy)
    {
        _ = await _lesions.FindByIdAsync(dto.LesionId)
            ?? throw new InvalidOperationException("Lesion not found. Place a Lesion marker before adding a treatment.");

        var stent = new StentImplant
        {
            Id = Guid.NewGuid(),
            LesionId = dto.LesionId,
            InventoryItemId = dto.InventoryItemId,
            LandingZoneProximal = dto.LandingZoneProximal,
            LandingZoneDistal = dto.LandingZoneDistal,
            StentDiameterMm = dto.StentDiameterMm,
            StentLengthMm = dto.StentLengthMm,
            PostDilationPerformed = dto.PostDilationPerformed,
            PostDilationPressure = dto.PostDilationPressure,
            TimiFlowAfter = dto.TimiFlowAfter,
            Notes = dto.Notes,
            PerformedAt = DateTime.UtcNow
        };

        // Consuming the item, inserting the treatment, and flagging the
        // lesion as treated all succeed together or not at all.
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _inventory.ConsumeItemAsync(dto.InventoryItemId, performedBy);
            await _treatments.CreateStentAsync(stent);
            await MarkLesionTreatedAsync(dto.LesionId);
        });

        return ToDto(stent, "Stent");
    }

    /// <summary>
    /// Adding a Balloon treatment - must reference an existing Lesion.
    /// A pre-dilation-only balloon doesn't yet mark the lesion as treated
    /// (the stent that follows does); a stand-alone or post-dilation
    /// balloon does.
    /// </summary>
    public async Task<LesionTreatmentDto> AddBalloonTreatmentAsync(CreateBalloonTreatmentDto dto, Guid performedBy)
    {
        _ = await _lesions.FindByIdAsync(dto.LesionId)
            ?? throw new InvalidOperationException("Lesion not found. Place a Lesion marker before adding a treatment.");

        var balloon = new BalloonDilation
        {
            Id = Guid.NewGuid(),
            LesionId = dto.LesionId,
            InventoryItemId = dto.InventoryItemId,
            BalloonDiameterMm = dto.BalloonDiameterMm,
            BalloonLengthMm = dto.BalloonLengthMm,
            MaxPressureAtm = dto.MaxPressureAtm,
            InflationDurationSec = dto.InflationDurationSec,
            IsPreDilation = dto.IsPreDilation,
            IsPostDilation = dto.IsPostDilation,
            TimiFlowAfter = dto.TimiFlowAfter,
            Notes = dto.Notes,
            PerformedAt = DateTime.UtcNow
        };

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _inventory.ConsumeItemAsync(dto.InventoryItemId, performedBy);
            await _treatments.CreateBalloonAsync(balloon);

            if (!dto.IsPreDilation || dto.IsPostDilation)
                await MarkLesionTreatedAsync(dto.LesionId);
        });

        return ToDto(balloon, "Balloon");
    }

    private async Task MarkLesionTreatedAsync(Guid lesionId)
    {
        var lesion = await _lesions.FindByIdAsync(lesionId);
        if (lesion is null) return;

        lesion.IsTreated = true;
        await _lesions.UpdateAsync(lesion);
    }

    private static CoronaryLesionDto ToDto(CoronaryLesion l) => new()
    {
        Id = l.Id,
        ProcedureReportId = l.ProcedureReportId,
        Vessel = l.Vessel,
        SegmentNumber = l.SegmentNumber,
        StenosisPercent = l.StenosisPercent,
        LesionType = l.LesionType.ToString(),
        TimiFlow = l.TimiFlow,
        IsTreated = l.IsTreated
    };

    private static LesionTreatmentDto ToDto(LesionTreatment t, string kind) => new()
    {
        Id = t.Id,
        LesionId = t.LesionId,
        InventoryItemId = t.InventoryItemId,
        Kind = kind,
        TimiFlowAfter = t.TimiFlowAfter,
        PerformedAt = t.PerformedAt,
        Notes = t.Notes
    };
}