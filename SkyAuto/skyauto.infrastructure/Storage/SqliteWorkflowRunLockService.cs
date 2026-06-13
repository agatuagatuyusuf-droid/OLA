using System;
using Dapper;
using Microsoft.Data.Sqlite;
using SkyAuto.Core.Runtime;

namespace SkyAuto.Infrastructure.Storage;

/// <summary>
/// SQLite-backed implementation of IWorkflowRunLockService.
/// Uses the runtime_locks table for persistence across crashes.
/// </summary>
public class SqliteWorkflowRunLockService : IWorkflowRunLockService
{
    private readonly string _connectionString;
    private static readonly TimeSpan DefaultLockTimeout = TimeSpan.FromMinutes(30);
    private bool _disposed;

    public SqliteWorkflowRunLockService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public bool TryAcquire(string workflowId, string runId, TimeSpan? waitForTimeout = null)
    {
        var timeoutMs = (long)(waitForTimeout?.TotalMilliseconds ?? 0);
        var endTime = DateTime.UtcNow + (waitForTimeout ?? TimeSpan.Zero);

        while (true)
        {
            using var conn = CreateConnection();
            conn.Open();

            // Check if there's an existing lock for this workflow
            var existingLock = conn.QueryFirstOrDefault<RuntimeLockInfo>(
                "SELECT * FROM runtime_locks WHERE lock_key = @LockKey", new { LockKey = workflowId });

            if (existingLock == null || existingLock.IsExpired)
            {
                // No lock or expired - try to acquire it atomically
                conn.Execute(
                    "INSERT OR REPLACE INTO runtime_locks (lock_key, owner_run_id, acquired_at, expires_at) VALUES (@LockKey, @OwnerRunId, @AcquiredAt, @ExpiresAt)",
                    new
                    {
                        LockKey = workflowId,
                        OwnerRunId = runId,
                        AcquiredAt = DateTime.UtcNow.ToString("o"),
                        ExpiresAt = (DateTime.UtcNow + DefaultLockTimeout).ToString("o")
                    });

                // Double-check: someone might have acquired it between our check and insert
                var verify = conn.QueryFirstOrDefault<RuntimeLockInfo>(
                    "SELECT * FROM runtime_locks WHERE lock_key = @LockKey", new { LockKey = workflowId });

                if (verify != null && verify.OwnerRunId == runId)
                    return true;

                // Someone else got it - check timeout
                if (DateTime.UtcNow >= endTime)
                    return false;

                Thread.Sleep(100);
                continue;
            }

            // Lock exists and is not expired
            if (existingLock.OwnerRunId == runId)
                return true; // Already locked by us

            if (DateTime.UtcNow >= endTime)
                return false; // Timeout reached

            Thread.Sleep(100);
        }
    }

    public void Release(string runId)
    {
        using var conn = CreateConnection();
        conn.Open();
        conn.Execute("DELETE FROM runtime_locks WHERE owner_run_id = @OwnerRunId", new { OwnerRunId = runId });
    }

    public bool IsLocked(string workflowId)
    {
        using var conn = CreateConnection();
        conn.Open();
        var lockInfo = conn.QueryFirstOrDefault<RuntimeLockInfo>(
            "SELECT * FROM runtime_locks WHERE lock_key = @LockKey", new { LockKey = workflowId });

        return lockInfo != null && !lockInfo.IsExpired;
    }

    public RuntimeLockInfo? GetLockInfo(string workflowId)
    {
        using var conn = CreateConnection();
        conn.Open();
        return conn.QueryFirstOrDefault<RuntimeLockInfo>(
            "SELECT * FROM runtime_locks WHERE lock_key = @LockKey", new { LockKey = workflowId });
    }

    public int CleanupExpiredLocks()
    {
        using var conn = CreateConnection();
        conn.Open();
        return conn.Execute("DELETE FROM runtime_locks WHERE expires_at < @Now", new { Now = DateTime.UtcNow.ToString("o") });
    }

    private SqliteConnection CreateConnection() => new(_connectionString);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
