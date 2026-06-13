using Xunit;
using Dapper;
using SkyAuto.Core.Engine;
using SkyAuto.Core.Models;
using SkyAuto.Core.StepTesting;

namespace SkyAuto.Tests;

public class StepTestServiceTests
{
    private static StepTestRequest MakeRequest(WorkflowStep step, Dictionary<string, IActionStepExecutor> executors)
    {
        return new StepTestRequest
        {
            Workflow = new Workflow { Id = "test_wf", Name = "测试流程" },
            Step = step,
            DataDir = Path.Combine(Path.GetTempPath(), $"step_test_{Guid.NewGuid():N}")
        };
    }

    private static void Cleanup(string dataDir)
    {
        try { Directory.Delete(dataDir, true); } catch { }
    }

    [Fact]
    public async Task Success_Returns_Success_True()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), $"step_test_{Guid.NewGuid():N}");
        try
        {
            var executors = new Dictionary<string, IActionStepExecutor>
            {
                ["test"] = new FakeStepExecutor("test", true, "output-ok")
            };
            var service = new StepTestService(executors);
            var request = MakeRequest(new WorkflowStep { Index = 1, Type = "test", Enabled = true }, executors);

            var result = await service.TestStepAsync(request);

            Assert.True(result.Success);
            Assert.Equal("output-ok", result.OutputData);
            Assert.NotEmpty(result.EvidenceJson);
            Assert.True(result.DurationMs >= 0);
            Assert.True(result.EndedAt >= result.StartedAt);
        }
        finally { Cleanup(dataDir); }
    }

    [Fact]
    public async Task Failure_Returns_Success_False()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), $"step_test_{Guid.NewGuid():N}");
        try
        {
            var executors = new Dictionary<string, IActionStepExecutor>
            {
                ["fail_step"] = new FakeStepExecutor("fail_step", false, error: "故意失败")
            };
            var service = new StepTestService(executors);
            var request = MakeRequest(new WorkflowStep { Index = 1, Type = "fail_step", Enabled = true }, executors);

            var result = await service.TestStepAsync(request);

            Assert.False(result.Success);
            Assert.Contains("故意失败", result.Error ?? "");
            Assert.NotNull(result.ScreenshotPath);
            Assert.True(File.Exists(result.ScreenshotPath));
            Assert.NotEmpty(result.EvidenceJson);
            Assert.True(result.DurationMs >= 0);
            Assert.True(result.EndedAt >= result.StartedAt);
        }
        finally { Cleanup(dataDir); }
    }

    [Fact]
    public async Task Executor_Not_Found_Returns_Error()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), $"step_test_{Guid.NewGuid():N}");
        try
        {
            var executors = new Dictionary<string, IActionStepExecutor>();
            var service = new StepTestService(executors);
            var request = MakeRequest(new WorkflowStep { Index = 1, Type = "nonexistent", Enabled = true }, executors);

            var result = await service.TestStepAsync(request);

            Assert.False(result.Success);
            Assert.Contains("未注册", result.Error ?? "");
            Assert.NotEmpty(result.EvidenceJson);
            Assert.True(result.DurationMs >= 0);
            Assert.True(result.EndedAt >= result.StartedAt);
        }
        finally { Cleanup(dataDir); }
    }

    [Fact]
    public async Task Step_Disabled_Returns_Error()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), $"step_test_{Guid.NewGuid():N}");
        try
        {
            var executors = new Dictionary<string, IActionStepExecutor>
            {
                ["some_step"] = new FakeStepExecutor("some_step", true)
            };
            var service = new StepTestService(executors);
            var request = MakeRequest(new WorkflowStep { Index = 1, Type = "some_step", Enabled = false }, executors);

            var result = await service.TestStepAsync(request);

            Assert.False(result.Success);
            Assert.Contains("已禁用", result.Error ?? "");
            Assert.NotEmpty(result.EvidenceJson);
            Assert.True(result.DurationMs >= 0);
            Assert.True(result.EndedAt >= result.StartedAt);
        }
        finally { Cleanup(dataDir); }
    }

    [Fact]
    public async Task Executor_Throws_Returns_Error()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), $"step_test_{Guid.NewGuid():N}");
        try
        {
            var executors = new Dictionary<string, IActionStepExecutor>
            {
                ["throw_step"] = new ThrowStepExecutor()
            };
            var service = new StepTestService(executors);
            var request = MakeRequest(new WorkflowStep { Index = 1, Type = "throw_step", Enabled = true }, executors);

            var result = await service.TestStepAsync(request);

            Assert.False(result.Success);
            Assert.Contains("测试抛异常", result.Error ?? "");
            Assert.NotEmpty(result.EvidenceJson);
            Assert.True(result.DurationMs >= 0);
            Assert.True(result.EndedAt >= result.StartedAt);
        }
        finally { Cleanup(dataDir); }
    }

    [Fact]
    public async Task MouseClick_Requires_Global_Lock()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), $"step_test_{Guid.NewGuid():N}");
        try
        {
            var executors = new Dictionary<string, IActionStepExecutor>
            {
                ["mouse_click"] = new FakeStepExecutor("mouse_click", true)
            };
            var service = new StepTestService(executors);
            var request = MakeRequest(new WorkflowStep { Index = 1, Type = "mouse_click", Enabled = true }, executors);

            var result = await service.TestStepAsync(request);

            Assert.True(result.Success);
            Assert.True(result.UsedGlobalAutomationLock);
        }
        finally { Cleanup(dataDir); }
    }

    [Fact]
    public async Task ReadFile_Does_Not_Require_Global_Lock()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), $"step_test_{Guid.NewGuid():N}");
        try
        {
            var executors = new Dictionary<string, IActionStepExecutor>
            {
                ["read_file"] = new FakeStepExecutor("read_file", true)
            };
            var service = new StepTestService(executors);
            var request = MakeRequest(new WorkflowStep { Index = 1, Type = "read_file", Enabled = true }, executors);

            var result = await service.TestStepAsync(request);

            Assert.True(result.Success);
            Assert.False(result.UsedGlobalAutomationLock);
        }
        finally { Cleanup(dataDir); }
    }

    [Fact]
    public async Task GlobalLock_Held_Step_Test_Fails()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"lock_test_{Guid.NewGuid():N}.db");
        var dataDir = Path.Combine(Path.GetTempPath(), $"step_test_{Guid.NewGuid():N}");
        try
        {
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
            // Pre-acquire global:automation lock
            var lockService = new SkyAuto.Infrastructure.Storage.SqliteWorkflowRunLockService(connStr);
            lockService.TryAcquire("global:automation", "pre_acquired_owner", TimeSpan.FromSeconds(10));

            var executors = new Dictionary<string, IActionStepExecutor>
            {
                ["mouse_click"] = new FakeStepExecutor("mouse_click", true)
            };
            var service = new StepTestService(executors, lockService);
            var request = MakeRequest(new WorkflowStep { Index = 1, Type = "mouse_click", Enabled = true }, executors);
            request.LockWaitTimeout = TimeSpan.FromSeconds(3);

            var result = await service.TestStepAsync(request);

            Assert.False(result.Success);
            Assert.True(result.UsedGlobalAutomationLock);
            Assert.Contains("global:automation", result.Error ?? "");
            Assert.True(result.NotVerified);
            Assert.NotEmpty(result.EvidenceJson);
            Assert.True(result.DurationMs >= 0);
            Assert.True(result.EndedAt >= result.StartedAt);
        }
        finally
        {
            Cleanup(dataDir);
            try { File.Delete(dbPath); } catch { }
            try { File.Delete(dbPath + "-shm"); } catch { }
            try { File.Delete(dbPath + "-wal"); } catch { }
        }
    }

    public class FakeStepExecutor : IActionStepExecutor
    {
        public string Type { get; }
        private readonly bool _success;
        private readonly string? _outputData;
        private readonly string? _error;

        public FakeStepExecutor(string type, bool success, string? outputData = null, string? error = null)
        {
            Type = type;
            _success = success;
            _outputData = outputData;
            _error = error;
        }

        public Task<StepExecutionResult> ExecuteAsync(WorkflowStep step, Dictionary<string, object?>? context = null)
            => Task.FromResult(new StepExecutionResult
            {
                Success = _success,
                OutputData = _outputData,
                Error = _error
            });
    }

    public class ThrowStepExecutor : IActionStepExecutor
    {
        public string Type => "throw_step";

        public Task<StepExecutionResult> ExecuteAsync(WorkflowStep step, Dictionary<string, object?>? context = null)
            => throw new InvalidOperationException("测试抛异常");
    }
}
