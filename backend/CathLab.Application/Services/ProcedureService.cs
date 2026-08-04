using CathLab.Application.DTOs.Procedures;
using CathLab.Application.Interfaces;
using CathLab.Domain.Entities;
using CathLab.Domain.Repositories;

namespace CathLab.Application.Services;

public class ProcedureService
{
    private readonly IProcedureReportRepository _reports;
    private readonly ICoronaryLesionRepository _lesions;
    private readonly ILesionTreatmentRepository _treatments;
    private readonly InventoryService _inventory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPdfGenerator _pdfGenerator;
    private readonly IUserRepository _userRepository;

    public ProcedureService(
        IProcedureReportRepository reports,
        ICoronaryLesionRepository lesions,
        ILesionTreatmentRepository treatments,
        InventoryService inventory,
        IUnitOfWork unitOfWork,
        IPdfGenerator pdfGenerator,
        IUserRepository userRepository)
    {
        _reports = reports;
        _lesions = lesions;
        _treatments = treatments;
        _inventory = inventory;
        _unitOfWork = unitOfWork;
        _pdfGenerator = pdfGenerator;
        _userRepository = userRepository;
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
            RefDiam = dto.RefDiam,
            Calcified = dto.Calcified,
            Thrombus = dto.Thrombus,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = dto.CreatedByUserId,
            ResidualStenosis = dto.ResidualStenosis,
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
    public async Task<LesionTreatmentDto> AddStentTreatmentAsync(CreateStentTreatmentDto dto)
    {
        var lesion = await _lesions.FindByIdAsync(dto.LesionId)
            ?? throw new InvalidOperationException("Lesion not found. Place a Lesion marker before adding a treatment.");
        var report = await _reports.FindByIdAsync(lesion.ProcedureReportId)
            ?? throw new InvalidOperationException("Procedure report not found for this lesion.");

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

        // Reserving here - the item stays Reserved until the
        // report is confirmed. See ProcedureService.ConfirmReportAsync.
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _inventory.ReserveItemAsync(dto.InventoryItemId, report.CathStudyId);
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
        var lesion = await _lesions.FindByIdAsync(dto.LesionId)
            ?? throw new InvalidOperationException("Lesion not found. Place a Lesion marker before adding a treatment.");
        var report = await _reports.FindByIdAsync(lesion.ProcedureReportId)
            ?? throw new InvalidOperationException("Procedure report not found for this lesion.");

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
            await _inventory.ReserveItemAsync(dto.InventoryItemId, report.CathStudyId);
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
        DiameterMm = t is StentImplant si ? si.StentDiameterMm : ((BalloonDilation)t).BalloonDiameterMm,
        LengthMm = t is StentImplant sl ? sl.StentLengthMm : ((BalloonDilation)t).BalloonLengthMm,
        InventoryItemId = t.InventoryItemId,
        Kind = kind,
        TimiFlowAfter = t.TimiFlowAfter,
        PerformedAt = t.PerformedAt,
        Notes = t.Notes
    };

    /// <summary>
    /// Confirming the generated report is the point where every device attached
    /// to this procedure transitions Reserved -> Consumed, and stock movements
    /// are recorded. Irreversible - matches the physical reality that the
    /// devices have been used on the patient.
    ///
    /// Order of operations matters here:
    ///   1. Validate required narrative fields FIRST, before anything is
    ///      mutated - a report missing a Conclusion shouldn't cause devices
    ///      to be marked Consumed.
    ///   2. Only then consume inventory (irreversible against real stock).
    ///   3. Generate the PDF and store its path.
    ///   4. Mark the report confirmed.
    /// </summary>
    public async Task ConfirmReportAsync(Guid reportId, Guid confirmedBy)
    {
        var report = await _reports.FindByIdAsync(reportId)
            ?? throw new InvalidOperationException("Procedure report not found.");

        if (report.IsConfirmed)
            throw new InvalidOperationException("Report is already confirmed.");

        var missing = GetMissingRequiredFields(report);
        if (missing.Any())
            throw new InvalidOperationException($"Cannot confirm: missing {string.Join(", ", missing)}");

        // FindByIdAsync only Includes Lesions, not their Treatments - pull the
        // fuller graph via the lesion repo instead, which Includes Treatments.
        var lesions = await _lesions.GetByProcedureReportAsync(reportId);
        report.Lesions = lesions;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                Console.WriteLine($"ConfirmReportAsync: reportId={reportId} confirmedBy={confirmedBy}");

                foreach (var lesion in lesions)
                    foreach (var treatment in lesion.Treatments)
                        await _inventory.ConsumeItemAsync(treatment.InventoryItemId, confirmedBy);

                report.PdfStoragePath = await _pdfGenerator.GenerateAsync(report);
                report.IsConfirmed = true;
                report.ConfirmedAt = DateTime.UtcNow;
                // Persist the user who confirmed the report
                report.ConfirmedByUserId = confirmedBy;

                await _reports.UpdateAsync(report);
            });
    }

    /// <summary>
    /// Fields required before a report can be confirmed. PdfStoragePath is
    /// intentionally excluded - it's produced *during* confirmation, not a
    /// precondition of it.
    /// </summary>
    public List<string> GetMissingRequiredFields(ProcedureReport report)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(report.Approach))
            missing.Add("Approach");
        if (string.IsNullOrWhiteSpace(report.Conclusion))
            missing.Add("Conclusion");
        if (string.IsNullOrWhiteSpace(report.CatheterUsed))
            missing.Add("CatheterUsed");

        return missing;
    }

    public async Task<ProcedureReport?> GetReportByIdAsync(Guid reportId) =>
        await _reports.FindByIdAsync(reportId);


    /// <summary>
    /// Reports for every study this doctor has performed, across all of
    /// their patients. Admins may pass any doctorId; a Medecin should
    /// only ever be able to request their own (enforce that in the
    /// controller by passing GetCurrentUserId(), not a client-supplied id,
    /// unless the caller is an Admin).
    /// </summary>
    public async Task<List<ProcedureReportSummaryDto>> GetReportsForDoctorAsync(Guid doctorId)
    {
        var reports = await _reports.GetByDoctorIdAsync(doctorId);
        return reports.Select(ToSummaryDto).ToList();
    }
 
    /// <summary>Admin-only: every report in the system.</summary>
    public async Task<List<ProcedureReportSummaryDto>> GetAllReportsAsync()
    {
        var reports = await _reports.GetAllAsync();
        return reports.Select(ToSummaryDto).ToList();
    }
 
    /// <summary>
    /// Reports for a specific patient, from any doctor who has treated
    /// them. A Medecin may view this only if they have themselves
    /// performed at least one study on this patient - i.e. the patient is
    /// "one of theirs" - even though the returned reports may include
    /// studies performed by other doctors. Admins bypass this check.
    /// </summary>
    public async Task<List<ProcedureReportSummaryDto>> GetReportsForPatientAsync(
        Guid patientId, Guid requestingUserId, bool requesterIsAdmin)
    {
        if (!requesterIsAdmin)
        {
            var hasTreatedPatient = await _reports.HasDoctorTreatedPatientAsync(requestingUserId, patientId);
            if (!hasTreatedPatient)
                throw new UnauthorizedAccessException("You have no procedures recorded for this patient.");
        }
 
        var reports = await _reports.GetByPatientIdAsync(patientId);
        return reports.Select(ToSummaryDto).ToList();
    }
 
    private static ProcedureReportSummaryDto ToSummaryDto(ProcedureReport r) => new()
    {
        Id = r.Id,
        CathStudyId = r.CathStudyId,
        PatientId = r.CathStudy?.PatientId ?? Guid.Empty,
        PatientName = r.CathStudy?.Patient?.FullName ?? string.Empty,
        DoctorId = r.CathStudy?.OperatorId ?? Guid.Empty,
        DoctorName = r.CathStudy?.Operator?.FullName ?? string.Empty,
        IsConfirmed = r.IsConfirmed,
        ReportGeneratedAt = r.ReportGeneratedAt,
        ConfirmedAt = r.ConfirmedAt,
        PdfStoragePath = r.PdfStoragePath
    };

    public async Task GetReportsForPatientAsync(Guid patientId, object value, bool isAdmin)
    {
        throw new NotImplementedException();
    }

    // --- Additions to ProcedureService ---

/// <summary>
/// Full hydration for the coronary mapping UI. Returns null if no report
/// exists yet for this study (virgin state - not an error, just "nothing
/// recorded yet").
/// </summary>
public async Task<ProcedureReportDetailDto?> GetReportDetailByCathStudyIdAsync(Guid cathStudyId)
{
    var report = await _reports.FindByCathStudyIdAsync(cathStudyId);
    if (report is null) return null;

    var lesions = await _lesions.GetByProcedureReportAsync(report.Id);

    return new ProcedureReportDetailDto
    {
        Id = report.Id,
        CathStudyId = report.CathStudyId,
        IsConfirmed = report.IsConfirmed,
        ReportGeneratedAt = report.ReportGeneratedAt,
        ConfirmedAt = report.ConfirmedAt,
        Approach = report.Approach,
        CatheterUsed = report.CatheterUsed,
        Conclusion = report.Conclusion,
        DominanceType = report.DominanceType,
        TimiFlowBaseline = report.TimiFlowBaseline,
        TimiFlowFinal = report.TimiFlowFinal,
        ContrastVolumeMl = report.ContrastVolumeMl,
        DoseAreaProductGycm2 = report.DoseAreaProductGycm2,
        MissingRequiredFields = report.IsConfirmed ? new() : GetMissingRequiredFields(report),
        Recommendations = report.Recommendations,
        ConfirmedByUserId = report.ConfirmedByUserId?.ToString(),

        ConfirmedByUser = (await _userRepository.GetByIdAsync(report.ConfirmedByUserId ?? Guid.Empty))?.FullName ?? string.Empty,

        Lesions = lesions.Select(l => new CoronaryLesionDto
        {
            Id = l.Id,
            Vessel = l.Vessel,
            SegmentNumber = l.SegmentNumber,
            StenosisPercent = l.StenosisPercent,
            LesionType = l.LesionType.ToString(),
            RefDiam = l.RefDiam,
            Calcified = l.Calcified,
            Thrombus = l.Thrombus,
            ResidualStenosis = l.ResidualStenosis,
            CreatedAt = l.CreatedAt,
            CreatedByUserId = l.CreatedByUserId,
            TimiFlow = l.TimiFlow,
            IsTreated = l.IsTreated,
            Treatments = l.Treatments.Select(t => new LesionTreatmentDto
            {
                Id = t.Id,
                Kind = t is StentImplant ? "Stent" : "Balloon",
                InventoryItemId = t.InventoryItemId,
                ProductName = t.InventoryItem?.Product?.ProductName ?? "?", 
                LotNumber = t.InventoryItem?.LotNumber,
                DiameterMm = t is StentImplant si ? si.StentDiameterMm : ((BalloonDilation)t).BalloonDiameterMm,
                LengthMm = t is StentImplant sl ? sl.StentLengthMm : ((BalloonDilation)t).BalloonLengthMm,
                TimiFlowAfter = t.TimiFlowAfter,
                PerformedAt = t.PerformedAt,
                Notes = t.Notes
            }).ToList()
        }).ToList()
    };
}

/// <summary>Update the narrative fields required before confirmation.</summary>
public async Task UpdateReportNarrativeAsync(Guid reportId, UpdateReportNarrativeDto dto)
{
    var report = await _reports.FindByIdAsync(reportId)
        ?? throw new InvalidOperationException("Procedure report not found.");

    if (report.IsConfirmed)
        throw new InvalidOperationException("Cannot edit a confirmed report.");

    if (dto.Approach is not null) report.Approach = dto.Approach;
    if (dto.CatheterUsed is not null) report.CatheterUsed = dto.CatheterUsed;
    if (dto.Conclusion is not null) report.Conclusion = dto.Conclusion;
    if (dto.DominanceType is not null) report.DominanceType = dto.DominanceType;
    if (dto.TimiFlowBaseline.HasValue) report.TimiFlowBaseline = dto.TimiFlowBaseline;
    if (dto.TimiFlowFinal.HasValue) report.TimiFlowFinal = dto.TimiFlowFinal;
    if (dto.ContrastVolumeMl.HasValue) report.ContrastVolumeMl = dto.ContrastVolumeMl;
    if (dto.DoseAreaProductGycm2.HasValue) report.DoseAreaProductGycm2 = dto.DoseAreaProductGycm2;
    if (dto.Recommendations is not null) report.Recommendations = dto.Recommendations;

    await _reports.UpdateAsync(report);
}

/// <summary>Deletes a single treatment and releases its reserved device back to stock.</summary>
public async Task DeleteTreatmentAsync(Guid treatmentId)
{
    var treatment = await _treatments.FindByIdAsync(treatmentId)
        ?? throw new InvalidOperationException("Treatment not found.");
    var lesion = await _lesions.FindByIdAsync(treatment.LesionId)
        ?? throw new InvalidOperationException("Lesion not found for this treatment.");
    var report = await _reports.FindByIdAsync(lesion.ProcedureReportId)
        ?? throw new InvalidOperationException("Report not found.");

    if (report.IsConfirmed)
        throw new InvalidOperationException("Cannot delete a treatment from a confirmed report.");

    await _unitOfWork.ExecuteInTransactionAsync(async () =>
    {
        await _inventory.ReleaseItemAsync(treatment.InventoryItemId);
        await _treatments.DeleteAsync(treatmentId);

        var remaining = await _treatments.GetByLesionIdAsync(lesion.Id);
        lesion.IsTreated = remaining.Any(t => t is StentImplant
            || (t is BalloonDilation b && (b.IsPostDilation || !b.IsPreDilation)));
        await _lesions.UpdateAsync(lesion);
    });
}

/// <summary>Deletes a lesion marker and every treatment/device reservation attached to it.</summary>
public async Task DeleteLesionAsync(Guid lesionId)
{
    var lesion = await _lesions.FindByIdAsync(lesionId)
        ?? throw new InvalidOperationException("Lesion not found.");
    var report = await _reports.FindByIdAsync(lesion.ProcedureReportId)
        ?? throw new InvalidOperationException("Report not found.");

    if (report.IsConfirmed)
        throw new InvalidOperationException("Cannot delete a lesion from a confirmed report.");

    var treatments = await _treatments.GetByLesionIdAsync(lesionId);

    await _unitOfWork.ExecuteInTransactionAsync(async () =>
    {
        foreach (var t in treatments)
            await _inventory.ReleaseItemAsync(t.InventoryItemId);

        await _lesions.DeleteAsync(lesionId);
    });
}}