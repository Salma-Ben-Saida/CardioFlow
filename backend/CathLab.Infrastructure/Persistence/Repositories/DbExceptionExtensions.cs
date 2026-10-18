using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CathLab.Infrastructure.Persistence.Repositories;

internal static class DbExceptionExtensions
{
    // SQL Server: 2601 = duplicate key in unique index, 2627 = unique constraint violation
    public static bool IsUniqueViolation(this DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };
}