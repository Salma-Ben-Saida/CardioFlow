using CathLab.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CathLab.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly CathLabDbContext _db;

    public UnitOfWork(CathLabDbContext db)
    {
        _db = db;
    }

    public async Task ExecuteInTransactionAsync(Func<Task> operation)
    {
        // CreateExecutionStrategy handles provider-specific transient retry
        // (important on SQL Server; a no-op wrapper on Postgres/SQLite).
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                await operation();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }
}