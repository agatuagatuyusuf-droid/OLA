using System;

namespace SkyAuto.Core.Runtime;

/// <summary>
/// Prevents multiple workflows from running simultaneously, especially for mouse/keyboard operations.
/// </summary>
public interface IWorkflowRunLockService : IDisposable
{
    /// <summary>
    /// Try to acquire a lock for the given workflow. Returns true if successful.
    /// If waitForTimeout is null or zero, returns immediately (non-blocking).
    /// If waitForTimeout > 0, waits up to that many milliseconds.
    /// </summary>
    bool TryAcquire(string workflowId, string runId, TimeSpan? waitForTimeout = null);

    /// <summary>
    /// Release the lock for the given run ID.
    /// </summary>
    void Release(string runId);

    /// <summary>
    /// Check if a specific workflow is currently locked (running).
    /// </summary>
    bool IsLocked(string workflowId);

    /// <summary>
    /// Get information about who holds the lock for this workflow.
    /// Returns null if not locked.
    /// </summary>
    RuntimeLockInfo? GetLockInfo(string workflowId);

    /// <summary>
    /// Clean up expired locks (from previous crashes).
    /// Called on startup.
    /// </summary>
    int CleanupExpiredLocks();
}

public class RuntimeLockInfo
{
    public string LockKey { get; set; } = string.Empty;
    public string OwnerRunId { get; set; } = string.Empty;
    public DateTime AcquiredAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}
