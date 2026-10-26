using CathLab.Application.Interfaces;
using CathLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CathLab.Infrastructure.Persistence.Repositories;

public class ProcedureReportRepository : IProcedureReportRepository
{
    private readonly CathLabDbContext _db;

    public ProcedureReportRepository(CathLabDbContext db)
    {
        _db = db;
    }

    public async Task<ProcedureReport?> FindByIdAsync(Guid id) =>
        await _db.ProcedureReports.Include(r => r.Lesions).FirstOrDefaultAsync(r => r.Id == id);

    public async Task<ProcedureReport?> FindByCathStudyIdAsync(Guid cathStudyId) =>
        await _db.ProcedureReports.FirstOrDefaultAsync(r => r.CathStudyId == cathStudyId);

    public async Task<ProcedureReport> CreateAsync(ProcedureReport report)
    {
        _db.ProcedureReports.Add(report);
        await _db.SaveChangesAsync();
        return report;
    }

    public async Task UpdateAsync(ProcedureReport report)
    {
        _db.ProcedureReports.Update(report);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
{
    var report = await _db.ProcedureReports
        .Include(r => r.Lesions).ThenInclude(l => l.Treatments)
        .FirstOrDefaultAsync(r => r.Id == id);
    if (report is null) return;

    foreach (var lesion in report.Lesions)
        _db.RemoveRange(lesion.Treatments);
    _db.RemoveRange(report.Lesions);
    _db.ProcedureReports.Remove(report);
    await _db.SaveChangesAsync();
}

    /// <summary>
    /// A report belongs to a doctor if they were assigned as CathStudy
    /// Operator (from PACS/DICOM metadata - often empty in practice) OR if
    /// they're the one who confirmed it. Checking OperatorId alone misses
    /// every report where that DICOM field was never populated, even
    /// though the doctor genuinely confirmed it.
    /// </summary>
    public async Task<List<ProcedureReport>> GetByDoctorIdAsync(Guid doctorId)
    {
        return await _db.ProcedureReports
            .Include(r => r.CathStudy).ThenInclude(s => s.Patient)
            .Include(r => r.ConfirmedByUser)
            .Where(r => r.CathStudy.OperatorId == doctorId || r.ConfirmedByUserId == doctorId)
            .OrderByDescending(r => r.ReportGeneratedAt)
            .ToListAsync();
    }

    public async Task<PatientHeader?> GetPatientHeaderAsync(Guid cathStudyId) =>
    await _db.CathStudies.AsNoTracking()
        .Where(s => s.Id == cathStudyId && s.Patient != null)
        .Select(s => new PatientHeader(s.Patient!.FullName, s.Patient.BirthDate, s.Patient.Sex, s.StudyDate, s.AccessionNumber))
        .FirstOrDefaultAsync();
 
    public async Task<List<ProcedureReport>> GetByPatientIdAsync(Guid patientId)
    {
        return await _db.ProcedureReports
            .Include(r => r.CathStudy).ThenInclude(s => s.Patient)
            .Include(r => r.ConfirmedByUser)
            .Where(r => r.CathStudy.PatientId == patientId)
            .OrderByDescending(r => r.ReportGeneratedAt)
            .ToListAsync();
    }
 
    /// <summary>Same OperatorId-or-ConfirmedByUserId matching as GetByDoctorIdAsync - see note there.</summary>
    public async Task<bool> HasDoctorTreatedPatientAsync(Guid doctorId, Guid patientId)
    {
        return await _db.ProcedureReports
            .AnyAsync(r => (r.CathStudy.OperatorId == doctorId || r.ConfirmedByUserId == doctorId)
                        && r.CathStudy.PatientId == patientId);
    }
 
    public async Task<List<ProcedureReport>> GetAllAsync()
    {
        return await _db.ProcedureReports
            .Include(r => r.CathStudy).ThenInclude(s => s.Patient)
            .Include(r => r.ConfirmedByUser)
            .OrderByDescending(r => r.ReportGeneratedAt)
            .ToListAsync();
    }
}