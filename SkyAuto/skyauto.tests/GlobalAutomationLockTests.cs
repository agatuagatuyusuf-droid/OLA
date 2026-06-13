using Xunit;
using Dapper;
using SkyAuto.Core.Engine;
using SkyAuto.Core.Models;
using SkyAuto.Core.Runtime;
using SkyAuto.Infrastructure.Storage;

namespace SkyAuto.Tests;

public class GlobalAutomationLockTests
{
    private static Workflow CreateWorkflow(params string[] stepTypes)
    {
        var wf = new Workflow
        {
            Name = "GlobalLockTest",
            Steps = stepTypes.Select((t, i) => new WorkflowStep
            {
                Index = i + 1,
                Type = t,
                Enabled = true
            }).ToList()
        };
        return wf;
    }

    /// <summary>
    /// Creates a fresh SQLite database with runtime_locks table.
    /// </summary>
    private static (string dbPath, SqliteWorkflowRunLockService service) CreateLockService()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"lock_test_{Guid.NewGuid():N}.db");
        var connStr = $"Data Source={dbPath}";
        using (var conn = new Microsoft.Data.Sqlite.SqliteConnection(connStr))
        {
            conn.Open();
            conn.Execute(@"
                CREATE TABLE IF NOT EXISTS runtime_locks (
                    lock_key TEXT PRIMARY KEY,
                    owner_run_id TEXT NOT NULL,
                    acquired_at TEXT NOT NULL,
                    expires_at TEXT NOT NULL
                )");
        }
        return (dbPath, new SqliteWorkflowRunLockService(connStr));
    }

    private static void CleanupDb(string dbPath)
    {
        try { File.Delete(dbPath); } catch { }
        try { File.Delete(dbPath + "-shm"); } catch { }
        try { File.Delete(dbPath + "-wal"); } catch { }
    }

    [Fact]
    public void FileAndLogWorkflow_DoesNotRequireGlobalLock()
    {
        var wf = CreateWorkflow("read_file", "write_log");
        Assert.False(AutomationActionClassifier.RequiresGlobalAutomationLock(wf));
    }

    [Fact]
    public void MouseClick_RequiresGlobalLock()
    {
        var wf = CreateWorkflow("mouse_click");
        Assert.True(AutomationActionClassifier.RequiresGlobalAutomationLock(wf));
    }

    [Fact]
    public void OcrRegion_RequiresGlobalLock()
    {
        var wf = CreateWorkflow("ocr_region");
        Assert.True(AutomationActionClassifier.RequiresGlobalAutomationLock(wf));
    }

    [Fact]
    public async Task TwoAutomationWorkflows_CannotRunConcurrently()
    {
        var (dbPath, lockService) = CreateLockService();
        try
        {
            // Use a blocking executor so the first run holds locks until we release it
            var blocking = new BlockingExecutor();
            var executors = new Dictionary<string, IActionStepExecutor>
            {
                ["mouse_click"] = blocking
            };

            var runner = new WorkflowRunner(executors, "", lockService);

            var wf1 = CreateWorkflow("mouse_click");
            var wf2 = CreateWorkflow("mouse_click");

            // Start first workflow in background so it acquires locks and blocks
            var task1 = runner.RunAsync(wf1, TimeSpan.FromSeconds(10));

            // Wait for it to acquire the locks and start executing
            await blocking.WaitUntilStarted(TimeSpan.FromSeconds(5));

            // Second workflow should fail because global:automation is still held
            var record2 = await runner.RunAsync(wf2, TimeSpan.FromSeconds(3));
            Assert.False(record2.Success, "第二个流程应因global lock而失败");
            Assert.Equal("GLOBAL_AUTOMATION_LOCK_FAILED", record2.FailedStepName);
            Assert.Contains("global:automation", record2.ErrorMessage ?? "");

            // Release the first workflow
            blocking.Unblock();
            var record1 = await task1;
            Assert.True(record1.Success, $"第一个流程应成功, Error: {record1.ErrorMessage ?? "无"}");
        }
        finally
        {
            CleanupDb(dbPath);
        }
    }

    /// <summary>
    /// An executor that blocks until Unblock() is called, allowing tests to hold locks.
    /// </summary>
    public class BlockingExecutor : IActionStepExecutor
    {
        private readonly TaskCompletionSource _started = new();
        private readonly TaskCompletionSource _block = new();

        public string Type => "blocking";

        /// <summary>
        /// Waits until the executor has been entered (locks are acquired).
        /// </summary>
        public async Task WaitUntilStarted(TimeSpan timeout)
        {
            await _started.Task.WaitAsync(timeout);
        }

        /// <summary>
        /// Unblocks the executor, allowing ExecuteAsync to complete.
        /// </summary>
        public void Unblock() => _block.TrySetResult();

        public async Task<StepExecutionResult> ExecuteAsync(WorkflowStep step, Dictionary<string, object?>? context = null)
        {
            _started.TrySetResult();
            await _block.Task;
            return new StepExecutionResult { Success = true, OutputData = "ok" };
        }
    }

    [Fact]
    public async Task GlobalLock_Released_CanBeReAcquired()
    {
        var (dbPath, lockService) = CreateLockService();
        try
        {
            var executors = new Dictionary<string, IActionStepExecutor>
            {
                ["mouse_click"] = new FakeLockTestExecutor("mouse_click", true)
            };

            var runner = new WorkflowRunner(executors, "", lockService);
            var wf = CreateWorkflow("mouse_click");

            // First run succeeds
            var record1 = await runner.RunAsync(wf, TimeSpan.FromSeconds(10));
            Assert.True(record1.Success, $"第一次运行应成功, Error: {record1.ErrorMessage ?? "无"}");

            await Task.Delay(50);

            // Second run should also succeed because first run released the lock
            var record2 = await runner.RunAsync(wf, TimeSpan.FromSeconds(10));
            Assert.True(record2.Success, $"释放后第二个流程应成功获取global lock, Error: {record2.ErrorMessage ?? "无"}");
        }
        finally
        {
            CleanupDb(dbPath);
        }
    }

    /// <summary>
    /// Minimal executor that always succeeds/fails for lock tests.
    /// </summary>
    public class FakeLockTestExecutor : IActionStepExecutor
    {
        public string Type { get; }
        private readonly bool _success;

        public FakeLockTestExecutor(string type, bool success)
        {
            Type = type;
            _success = success;
        }

        public Task<StepExecutionResult> ExecuteAsync(WorkflowStep step, Dictionary<string, object?>? context = null)
            => Task.FromResult(new StepExecutionResult
            {
                Success = _success,
                OutputData = _success ? "ok" : null,
                Error = _success ? null : "fail"
            });
    }
}
