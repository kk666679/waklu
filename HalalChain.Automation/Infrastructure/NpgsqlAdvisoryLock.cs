namespace HalalChain.Automation.Infrastructure;

using HalalChain.Automation.Scheduling;
using Npgsql;

/// <summary>
/// Advisory lock backed by Postgres. Two replicas running the same job
/// contend for the lock; the loser skips the tick.
///
/// Uses pg_try_advisory_lock with a hash of the key, so the lock is
/// session-scoped and released automatically on connection close.
/// </summary>
public sealed class NpgsqlAdvisoryLock : IDistributedLock
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlAdvisoryLock(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<IAsyncDisposable?> TryAcquireAsync(
        string key,
        TimeSpan timeout,
        CancellationToken ct)
    {
        var connection = await _dataSource.OpenConnectionAsync(ct);
        var lockId = StableHash(key);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT pg_try_advisory_lock(@id)";
        cmd.Parameters.AddWithValue("id", lockId);

        var acquired = (bool)(await cmd.ExecuteScalarAsync(ct) ?? false);
        if (!acquired)
        {
            await connection.DisposeAsync();
            return null;
        }

        return new Release(connection, lockId);
    }

    private static long StableHash(string key)
    {
        // FNV-1a 64-bit; stable across processes and languages.
        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        var hash = offset;
        foreach (var c in key)
        {
            hash ^= c;
            hash *= prime;
        }
        return unchecked((long)hash);
    }

    private sealed class Release(NpgsqlConnection connection, long lockId) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT pg_advisory_unlock(@id)";
            cmd.Parameters.AddWithValue("id", lockId);
            await cmd.ExecuteScalarAsync();
            await connection.DisposeAsync();
        }
    }
}
