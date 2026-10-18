using CathLab.Application.DTOs.Archive;
using CathLab.Application.Interfaces;
using CathLab.Domain.Entities;
using CathLab.Domain.Repositories;

namespace CathLab.Application.Services;

public class PatientLifecycleService
{
    private readonly IArchiveRepository _repo;
    private readonly IInventoryItemRepository _items;
    private readonly InventoryService _inventory;
    private readonly IUnitOfWork _uow;
    private readonly AuditService _audit;
    private readonly IUserRepository _users;

    public PatientLifecycleService(
        IArchiveRepository repo,
        IInventoryItemRepository items,
        InventoryService inventory,
        IUnitOfWork uow,
        AuditService audit,
        IUserRepository users)
    {
        _repo = repo;
        _items = items;
        _inventory = inventory;
        _uow = uow;
        _audit = audit;
        _users = users;
    }

    // ───────────────────────── helpers ─────────────────────────

    private static string CleanReason(string? reason)
    {
        var r = reason?.Trim();
        if (string.IsNullOrEmpty(r) || r.Length < 3)
            throw new ArgumentException("Un motif d'au moins 3 caractères est obligatoire.");
        if (r.Length > 500)
            throw new ArgumentException("Le motif est trop long (500 caractères max).");
        return r;
    }

    private static bool HasConfirmedReport(CathStudy s) => s.ProcedureReport?.IsConfirmed == true;

    // Reserved devices go back to stock before their study is physically deleted.
    private async Task ReleaseReservedItemsAsync(Guid studyId)
    {
        var reserved = await _items.GetReservedForStudyAsync(studyId);
        foreach (var item in reserved)
            await _inventory.ReleaseItemAsync(item.Id);
    }

    // ───────────────────────── studies ─────────────────────────

    public async Task<bool> ArchiveStudyAsync(Guid studyId, string? reason, Guid actorUserId)
    {
        var why = CleanReason(reason);
        var study = await _repo.FindStudyAsync(studyId);
        if (study == null) return false;

        if (study.IsDeleted)
            throw new InvalidOperationException("Cette étude est déjà archivée.");
        if (HasConfirmedReport(study))
            throw new InvalidOperationException("Impossible d'archiver une étude dont le rapport est confirmé.");

        study.Archive(actorUserId, why, DateTime.UtcNow);
        await _repo.SaveChangesAsync();

        await _audit.LogActionAsync(actorUserId, "Archive", "CathStudy", study.Id,
            $"Étude {study.AccessionNumber} archivée. Motif : {why}");
        return true;
    }

    public async Task<bool> RestoreStudyAsync(Guid studyId, Guid actorUserId)
    {
        var study = await _repo.FindStudyAsync(studyId);
        if (study == null) return false;

        if (!study.IsDeleted)
            throw new InvalidOperationException("Cette étude n'est pas archivée.");
        if (study.Patient is { IsDeleted: true })
            throw new InvalidOperationException(
                "Le patient de cette étude est archivé : restaurez d'abord le patient.");

        study.Restore();
        await _repo.SaveChangesAsync();

        await _audit.LogActionAsync(actorUserId, "Restore", "CathStudy", study.Id,
            $"Étude {study.AccessionNumber} restaurée.");
        return true;
    }

    public async Task<bool> PurgeStudyAsync(Guid studyId, Guid actorUserId)
    {
        var study = await _repo.FindStudyAsync(studyId);
        if (study == null) return false;

        if (!study.IsDeleted)
            throw new InvalidOperationException(
                "Archivez d'abord l'étude avant de la supprimer définitivement.");
        if (HasConfirmedReport(study))
            throw new InvalidOperationException(
                "Suppression définitive impossible : le rapport de cette étude est confirmé.");

        var accession = study.AccessionNumber;

        await _uow.ExecuteInTransactionAsync(async () =>
        {
            await ReleaseReservedItemsAsync(study.Id);
            _repo.RemoveStudy(study);                 // DB cascades report → lesions → treatments
            await _repo.SaveChangesAsync();
        });

        await _audit.LogActionAsync(actorUserId, "Purge", "CathStudy", studyId,
            $"Étude {accession} supprimée définitivement.");
        return true;
    }

    // ───────────────────────── patients ─────────────────────────

    public async Task<bool> ArchivePatientAsync(Guid patientId, string? reason, Guid actorUserId)
    {
        var why = CleanReason(reason);
        var patient = await _repo.FindPatientAsync(patientId);
        if (patient == null) return false;

        if (patient.IsDeleted)
            throw new InvalidOperationException("Ce patient est déjà archivé.");
        if (patient.Studies.Any(HasConfirmedReport))
            throw new InvalidOperationException(
                "Impossible d'archiver ce patient : au moins une de ses études a un rapport confirmé.");

        var now = DateTime.UtcNow;
        var liveStudies = patient.Studies.Where(s => !s.IsDeleted).ToList();

        patient.Archive(actorUserId, why, now);
        foreach (var s in liveStudies)
            s.Archive(actorUserId, why, now, withPatient: true);

        await _repo.SaveChangesAsync();

        await _audit.LogActionAsync(actorUserId, "Archive", "Patient", patient.Id,
            $"Patient {patient.ConquestPatientID ?? patient.Id.ToString()} archivé " +
            $"({liveStudies.Count} étude(s)). Motif : {why}");
        return true;
    }

    public async Task<bool> RestorePatientAsync(Guid patientId, Guid actorUserId)
    {
        var patient = await _repo.FindPatientAsync(patientId);
        if (patient == null) return false;

        if (!patient.IsDeleted)
            throw new InvalidOperationException("Ce patient n'est pas archivé.");

        var withPatient = patient.Studies.Where(s => s.IsDeleted && s.DeletedWithPatient).ToList();

        patient.Restore();
        foreach (var s in withPatient)
            s.Restore();

        await _repo.SaveChangesAsync();

        await _audit.LogActionAsync(actorUserId, "Restore", "Patient", patient.Id,
            $"Patient {patient.ConquestPatientID ?? patient.Id.ToString()} restauré " +
            $"({withPatient.Count} étude(s) restaurée(s)).");
        return true;
    }

    public async Task<bool> PurgePatientAsync(Guid patientId, Guid actorUserId)
    {
        var patient = await _repo.FindPatientAsync(patientId);
        if (patient == null) return false;

        if (!patient.IsDeleted)
            throw new InvalidOperationException(
                "Archivez d'abord le patient avant de le supprimer définitivement.");
        if (patient.Studies.Any(HasConfirmedReport))
            throw new InvalidOperationException(
                "Suppression définitive impossible : au moins une étude a un rapport confirmé.");

        var label = patient.ConquestPatientID ?? patient.Id.ToString();
        var studyCount = patient.Studies.Count;

        await _uow.ExecuteInTransactionAsync(async () =>
        {
            foreach (var s in patient.Studies)
                await ReleaseReservedItemsAsync(s.Id);

            _repo.RemovePatient(patient);             // cascades studies, reports, risk profile…
            await _repo.SaveChangesAsync();
        });

        await _audit.LogActionAsync(actorUserId, "Purge", "Patient", patientId,
            $"Patient {label} supprimé définitivement ({studyCount} étude(s)).");
        return true;
    }

    // ───────────────────────── overview ─────────────────────────

    public async Task<ArchiveOverviewDto> GetArchiveAsync()
    {
        var patients = await _repo.GetArchivedPatientsAsync();
        var studies  = await _repo.GetArchivedStudiesAsync();

        var nameCache = new Dictionary<Guid, string>();
        async Task<string?> NameOf(Guid? userId)
        {
            if (userId is null) return null;
            if (nameCache.TryGetValue(userId.Value, out var cached)) return cached;

            var u = await _users.GetByIdAsync(userId.Value);
            var name = u is null ? "(utilisateur supprimé)"
                     : string.IsNullOrWhiteSpace(u.FullName) ? u.Username : u.FullName;
            nameCache[userId.Value] = name;
            return name;
        }

        var dto = new ArchiveOverviewDto();

        foreach (var p in patients)
            dto.Patients.Add(new ArchivedPatientDto
            {
                Id = p.Id,
                FullName = p.FullName,
                ConquestPatientID = p.ConquestPatientID,
                StudyCount = p.Studies.Count,
                DeletedAt = p.DeletedAt,
                DeletedByName = await NameOf(p.DeletedByUserId),
                DeleteReason = p.DeleteReason
            });

        foreach (var s in studies)
            dto.Studies.Add(new ArchivedStudyDto
            {
                Id = s.Id,
                AccessionNumber = s.AccessionNumber,
                StudyDate = s.StudyDate,
                PatientId = s.PatientId,
                PatientName = s.Patient?.FullName,
                PatientArchived = s.Patient?.IsDeleted ?? false,
                DeletedWithPatient = s.DeletedWithPatient,
                HasConfirmedReport = HasConfirmedReport(s),
                DeletedAt = s.DeletedAt,
                DeletedByName = await NameOf(s.DeletedByUserId),
                DeleteReason = s.DeleteReason
            });

        return dto;
    }
}