using System.Net.Sockets;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace RecruitmentPlatform.Infrastructure.Data;

/// <summary>
/// Retrying execution strategy that treats transient socket/DNS failures as retryable.
/// <para>
/// EF Core's built-in Npgsql retry treats a name-resolution error ("No such host is known") as
/// permanent — reasonable, since it usually means a misconfigured hostname. But against a managed
/// cloud database (Neon) whose hostname is correct, that lookup fails intermittently, and the stock
/// strategy surfaces it as a 500 instead of retrying. This strategy retries socket errors and
/// Npgsql's own transient errors (connection drops, timeouts) while deliberately NOT retrying
/// query errors such as constraint violations (their <see cref="NpgsqlException.IsTransient"/> is
/// false), so correctness is preserved.
/// </para>
/// </summary>
public sealed class ResilientNpgsqlExecutionStrategy : ExecutionStrategy
{
    public ResilientNpgsqlExecutionStrategy(ExecutionStrategyDependencies dependencies, int maxRetryCount, TimeSpan maxRetryDelay)
        : base(dependencies, maxRetryCount, maxRetryDelay)
    {
    }

    protected override bool ShouldRetryOn(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            // DNS / connection socket failures — the intermittent Neon lookup blips.
            if (current is SocketException or TimeoutException)
            {
                return true;
            }

            // Connection-level Npgsql failures (drops, cold-start resets) are flagged transient;
            // query errors (unique violations, etc.) are not, so they still fail fast.
            if (current is NpgsqlException { IsTransient: true })
            {
                return true;
            }
        }

        return false;
    }
}
