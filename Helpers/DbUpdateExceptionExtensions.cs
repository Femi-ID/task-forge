using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace api.Helpers
{
    public static class DbUpdateExceptionExtensions
    {
        // SQL Server: 2601 = duplicate key in a unique index, 2627 = unique/primary key constraint violation.
        // If i move to PostgreSQL, change this one method to check PostgresException { SqlState: "23505" }.
        public static bool IsUniqueConstraintViolation(this DbUpdateException ex) =>
            ex.InnerException is SqlException { Number: 2601 or 2627 };
    }
}
