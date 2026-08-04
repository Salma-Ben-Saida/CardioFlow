using CathLab.Domain.Entities;

namespace CathLab.Application.Interfaces;

public interface IStockMovementRepository
{
    Task<StockMovement> CreateAsync(StockMovement movement);
    Task<(List<StockMovement> Items, int TotalCount)> GetByProductAsync(Guid productId, int page, int pageSize);
    Task<(IEnumerable<StockMovement> items, int total)> GetListAsync(int page, int pageSize);
}