using CathLab.Application.DTOs.Patients;
using CathLab.Application.Interfaces;
using CathLab.Domain.Entities;

namespace CathLab.Application.Services;

public class StudySchedulingService
{
    private readonly ICathStudyRepository _studies;
    public StudySchedulingService(ICathStudyRepository studies) => _studies = studies;

    public async Task<List<StudyListItemDto>> GetAllAsync()
    {
        await StartDueStudiesAsync();
        var list = await _studies.GetAllWithPatientAsync();
        return list.Select(ToDto).ToList();
    }

    public Task<int> StartDueStudiesAsync() => _studies.StartDueStudiesAsync(DateTime.UtcNow);

    public async Task ScheduleAsync(Guid id, DateTime scheduledAt)
    {
        var study = await _studies.FindByIdAsync(id)
            ?? throw new KeyNotFoundException("Étude introuvable.");
        var utc = scheduledAt.ToUniversalTime();
        if (utc < DateTime.UtcNow.AddMinutes(-1))
            throw new InvalidOperationException("La date d'intervention doit être dans le futur.");
        study.Schedule(utc);
        await _studies.UpdateAsync(study);
    }

    public async Task CancelAsync(Guid id)
    {
        var study = await _studies.FindByIdAsync(id)
            ?? throw new KeyNotFoundException("Étude introuvable.");
        study.Cancel();
        await _studies.UpdateAsync(study);
    }

    private static StudyListItemDto ToDto(CathStudy s) => new()
    {
        Id = s.Id,
        PatientId = s.PatientId,
        PatientName = s.Patient?.FullName ?? "—",
        AccessionNumber = s.AccessionNumber,
        IngestedAt = DateTime.SpecifyKind(s.CreatedAt, DateTimeKind.Utc),
        StudyDate = s.StudyDate,
        ScheduledAt = s.ScheduledAt.HasValue ? DateTime.SpecifyKind(s.ScheduledAt.Value, DateTimeKind.Utc) : null,
        Status = s.Status.ToString(),
        NeedsScheduling = s.Status == StudyStatus.Pending
    };
}