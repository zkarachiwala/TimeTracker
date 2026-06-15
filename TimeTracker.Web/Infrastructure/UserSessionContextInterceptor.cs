using System.Data.Common;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TimeTracker.Web.Infrastructure;

/// <summary>
/// Sets SESSION_CONTEXT(N'UserId') before every EF Core command so that SQL Server
/// Row-Level Security filter predicates can read it via CAST(SESSION_CONTEXT(N'UserId') AS nvarchar(450)).
///
/// Must run before every command (not just on connection open) because connection pooling
/// reuses physical connections across requests without resetting SESSION_CONTEXT.
///
/// db_owner / sysadmin users (e.g. 'sa' in local dev) are exempt from RLS by SQL Server design,
/// so this interceptor has no practical effect locally. RLS is enforced in production where the
/// Managed Identity holds only db_datareader + db_datawriter.
/// </summary>
public sealed class UserSessionContextInterceptor(IHttpContextAccessor httpContextAccessor) : DbCommandInterceptor
{
    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        await SetSessionContextAsync(command, cancellationToken);
        return result;
    }

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await SetSessionContextAsync(command, cancellationToken);
        return result;
    }

    public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        await SetSessionContextAsync(command, cancellationToken);
        return result;
    }

    private async Task SetSessionContextAsync(DbCommand command, CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null || command.Connection is null)
            return;

        using var ctx = command.Connection.CreateCommand();
        ctx.CommandText = "EXEC sp_set_session_context N'UserId', @userId";
        ctx.Parameters.Add(new SqlParameter("@userId", userId));
        ctx.Transaction = command.Transaction;
        await ctx.ExecuteNonQueryAsync(cancellationToken);
    }
}
