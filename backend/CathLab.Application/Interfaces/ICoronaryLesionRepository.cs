using CathLab.Domain.Entities;

namespace CathLab.Application.Interfaces;

public interface ICoronaryLesionRepository
{
    Task<CoronaryLesion?> FindByIdAsync(Guid id);
    Task<List<CoronaryLesion>> GetByProcedureReportAsync(Guid procedureReportId);
    Task<CoronaryLesion> CreateAsync(CoronaryLesion lesion);
    Task UpdateAsync(CoronaryLesion lesion);
    Task DeleteAsync(Guid id);
}