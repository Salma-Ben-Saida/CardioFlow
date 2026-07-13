using CathLab.Domain.Entities;

namespace CathLab.Application.Interfaces;

public interface IProcedureReportRepository
{
    Task<ProcedureReport?> FindByIdAsync(Guid id);
    Task<ProcedureReport?> FindByCathStudyIdAsync(Guid cathStudyId);
    Task<ProcedureReport> CreateAsync(ProcedureReport report);
    Task UpdateAsync(ProcedureReport report);
}