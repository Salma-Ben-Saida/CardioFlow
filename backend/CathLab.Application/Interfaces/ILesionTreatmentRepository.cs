using CathLab.Domain.Entities;

namespace CathLab.Application.Interfaces;

public interface ILesionTreatmentRepository
{
    Task<LesionTreatment?> FindByIdAsync(Guid id);
    Task<StentImplant> CreateStentAsync(StentImplant stent);
    Task<BalloonDilation> CreateBalloonAsync(BalloonDilation balloon);

    Task<List<LesionTreatment>> GetByLesionIdAsync(Guid lesionId); // NEW
    Task DeleteAsync(Guid id);  
}