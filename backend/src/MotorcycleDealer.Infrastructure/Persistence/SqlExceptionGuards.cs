using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MotorcycleDealer.Infrastructure.Persistence;

internal static class SqlExceptionGuards
{
    public static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    public static bool TryGetUniqueViolationMessage(DbUpdateException exception, out string sqlMessage)
    {
        if (exception.InnerException is SqlException { Number: 2601 or 2627 } sqlEx)
        {
            sqlMessage = sqlEx.Message;
            return true;
        }

        sqlMessage = string.Empty;
        return false;
    }
}
