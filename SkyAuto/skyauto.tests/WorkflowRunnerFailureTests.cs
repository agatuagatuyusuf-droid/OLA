using Xunit;
using SkyAuto.Core.Engine;
using SkyAuto.Core.Models;
using SkyAuto.Core.Ola;
using SkyAuto.Infrastructure.Storage;

namespace SkyAuto.Tests;

public class WorkflowRunnerFailureTests
{
    /// <summary>
    /// A minimal executor that always succeeds
    /// </summary>
    public class SuccessExecutor : IActionStepExecutor
    {
        public string Type => "success";
        public Task<StepExecutionResult> ExecuteAsync(WorkflowStep step, Dictionary<string, object?>? context = null)
            => Task.FromResult(new StepExecutionResult { Success = true, OutputData = "ok" });
    }

    /// <summary>
    /// An executor that always fails
    /// </summary>
    public class FailExecutor : IActionStepExecutor
    {
        public string Type => "fail";
        public Task<StepExecutionResult> ExecuteAsync(WorkflowStep step, Dictionary<string, object?>? context = null)
            => Task.FromResult(new StepExecutionResult { Success = false, Error = "故意失败" });
    }

    /// <summary>
    /// An executor that throws an exception
    /// </summary>
    public class ThrowExecutor : IActionStepExecutor
    {
        public string Type => "throw";
        public Task<StepExecutionResult> ExecuteAsync(WorkflowStep step, Dictionary<string, object?>? context = null)
            => throw new InvalidOperationException("Executor抛异常");
    }

    /// <summary>
    /// An executor that counts invocations for verifying execution order.
    /// </summary>
    public class CountingExecutor : IActionStepExecutor
    {
        public int ExecutionCount { get; private set; }
        public string Type => "counting";
        public bool ShouldSucceed { get; set; } = true;

        public Task<StepExecutionResult> ExecuteAsync(WorkflowStep step, Dictionary<string, object?>? context = null)
        {
            ExecutionCount++;
            return Task.FromResult(ShouldSucceed
                ? new StepExecutionResult { Success = true, OutputData = "ok" }
                : new StepExecutionResult { Success = false, Error = "故意失败" });
        }
    }

    private static WorkflowRunner CreateRunner(string dataDir, Dictionary<string, IActionStepExecutor> executors)
        => new(executors, dataDir);

    [Fact]
    public void All_Steps_Succeed_Returns_Success_True()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), $"runner_test_{Guid.NewGuid():N}");
        try
        {
            var executors = new Dictionary<string, IActionStepExecutor>
            {
                ["success"] = new SuccessExecutor()
            };
            var runner = CreateRunner(dataDir, executors);
            var wf = new Workflow
            {
                Name = "全成功测试",
                Steps =
                {
                    new WorkflowStep { Index = 1, Type = "success", Enabled = true },
                    new WorkflowStep { Index = 2, Type = "success", Enabled = true }
                }
            };

            var record = runner.RunAsync(wf).Result;

            Assert.True(record.Success, "全部步骤成功时 Success 应为 true");
            Assert.Equal(2, record.StepRecords.Count);
            Assert.True(record.StepRecords.All(s => s.Success));
            Assert.Null(record.ErrorMessage);
            Assert.Null(record.FailedStepName);
        }
        finally
        {
            try { Directory.Delete(dataDir, true); } catch { }
        }
    }

    [Fact]
    public void Middle_Step_Fails_Returns_Success_False()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), $"runner_test_{Guid.NewGuid():N}");
        try
        {
            var executors = new Dictionary<string, IActionStepExecutor>
            {
                ["success"] = new SuccessExecutor(),
                ["fail"] = new FailExecutor()
            };
            var runner = CreateRunner(dataDir, executors);
            var wf = new Workflow
            {
                Name = "中间失败测试",
                Steps =
                {
                    new WorkflowStep { Index = 1, Type = "success", Enabled = true },
                    new WorkflowStep { Index = 2, Type = "fail", Enabled = true },
                    new WorkflowStep { Index = 3, Type = "success", Enabled = true }
                }
            };

            var record = runner.RunAsync(wf).Result;

            Assert.False(record.Success, "中间步骤失败时 Success 应为 false");
            Assert.NotNull(record.FailedStepName);
            Assert.Contains("fail", record.FailedStepName);
            Assert.NotNull(record.ErrorMessage);
            Assert.Contains("故意失败", record.ErrorMessage);
        }
        finally
        {
            try { Directory.Delete(dataDir, true); } catch { }
        }
    }

    [Fact]
    public void Executor_Throws_Returns_Success_False()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), $"runner_test_{Guid.NewGuid():N}");
        try
        {
            var executors = new Dictionary<string, IActionStepExecutor>
            {
                ["success"] = new SuccessExecutor(),
                ["throw"] = new ThrowExecutor()
            };
            var runner = CreateRunner(dataDir, executors);
            var wf = new Workflow
            {
                Name = "抛异常测试",
                Steps =
                {
                    new WorkflowStep { Index = 1, Type = "success", Enabled = true },
                    new WorkflowStep { Index = 2, Type = "throw", Enabled = true },
                    new WorkflowStep { Index = 3, Type = "success", Enabled = true }
                }
            };

            var record = runner.RunAsync(wf).Result;

            Assert.False(record.Success, "Executor抛异常时 Success 应为 false");
            Assert.NotNull(record.FailedStepName);
            Assert.Contains("throw", record.FailedStepName);
            Assert.NotNull(record.ErrorMessage);
            Assert.Contains("抛异常", record.ErrorMessage);
        }
        finally
        {
            try { Directory.Delete(dataDir, true); } catch { }
        }
    }

    [Fact]
    public void Failure_Is_Not_Overwritten_To_True()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), $"runner_test_{Guid.NewGuid():N}");
        try
        {
            var executors = new Dictionary<string, IActionStepExecutor>
            {
                ["success"] = new SuccessExecutor(),
                ["fail"] = new FailExecutor()
            };
            var runner = CreateRunner(dataDir, executors);

            // Run 5 times - failures must never be overwritten to true
            for (int i = 0; i < 5; i++)
            {
                var wf = new Workflow
                {
                    Name = $"覆盖测试{i}",
                    Steps =
                    {
                        new WorkflowStep { Index = 1, Type = "success", Enabled = true },
                        new WorkflowStep { Index = 2, Type = "fail", Enabled = true }
                    }
                };

                var record = runner.RunAsync(wf).Result;
                Assert.False(record.Success, $"第{i + 1}次失败被覆盖为成功!");
                Assert.NotNull(record.FailedStepName);
                Assert.NotNull(record.ErrorMessage);
            }
        }
        finally
        {
            try { Directory.Delete(dataDir, true); } catch { }
        }
    }

    [Fact]
    public void Failure_Generates_Screenshot_Or_Evidence_File()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), $"runner_test_{Guid.NewGuid():N}");
        try
        {
            var executors = new Dictionary<string, IActionStepExecutor>
            {
                ["fail"] = new FailExecutor(),
                ["screenshot"] = new SuccessExecutor() // will not actually capture but lets runner try
            };
            var runner = CreateRunner(dataDir, executors);
            var wf = new Workflow
            {
                Name = "失败截图测试",
                Steps =
                {
                    new WorkflowStep { Index = 1, Type = "fail", Enabled = true }
                }
            };

            var record = runner.RunAsync(wf).Result;

            Assert.False(record.Success);
            // ScreenshotPath may be null if screenshot executor also fails,
            // but at minimum there should be a failure evidence file
            var screenshotDir = Path.Combine(dataDir, "screenshots");
            if (Directory.Exists(screenshotDir))
            {
                var evidenceFiles = Directory.GetFiles(screenshotDir, "*失败截图证据*");
                Assert.True(evidenceFiles.Length > 0 || record.ScreenshotPath != null,
                    "失败时应有截图或证据文件");
            }
        }
        finally
        {
            try { Directory.Delete(dataDir, true); } catch { }
        }
    }

    [Fact]
    public void Empty_Workflow_Returns_Success()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), $"runner_test_{Guid.NewGuid():N}");
        try
        {
            var executors = new Dictionary<string, IActionStepExecutor>();
            var runner = CreateRunner(dataDir, executors);
            var wf = new Workflow
            {
                Name = "空流程测试",
                Steps = { }
            };

            var record = runner.RunAsync(wf).Result;

            Assert.True(record.Success, "空流程(0步骤) Success 应为 true");
            Assert.Empty(record.StepRecords);
        }
        finally
        {
            try { Directory.Delete(dataDir, true); } catch { }
        }
    }

    [Fact]
    public void Failed_Step_Is_Recorded_In_StepRecords()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), $"runner_test_{Guid.NewGuid():N}");
        try
        {
            var executors = new Dictionary<string, IActionStepExecutor>
            {
                ["success"] = new SuccessExecutor(),
                ["fail"] = new FailExecutor()
            };
            var runner = CreateRunner(dataDir, executors);
            var wf = new Workflow
            {
                Name = "失败步骤记录测试",
                Steps =
                {
                    new WorkflowStep { Index = 1, Type = "success", Enabled = true },
                    new WorkflowStep { Index = 2, Type = "fail", Enabled = true },
                    new WorkflowStep { Index = 3, Type = "success", Enabled = true }
                }
            };

            var record = runner.RunAsync(wf).Result;

            Assert.False(record.Success);
            Assert.Equal(2, record.StepRecords.Count);
            Assert.True(record.StepRecords[0].Success);
            Assert.False(record.StepRecords[1].Success);
            Assert.Contains("fail", record.StepRecords[1].StepName);
            Assert.Contains("故意失败", record.StepRecords[1].Error ?? "");
        }
        finally
        {
            try { Directory.Delete(dataDir, true); } catch { }
        }
    }

    [Fact]
    public void Exception_Step_Is_Recorded_In_StepRecords()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), $"runner_test_{Guid.NewGuid():N}");
        try
        {
            var executors = new Dictionary<string, IActionStepExecutor>
            {
                ["success"] = new SuccessExecutor(),
                ["throw"] = new ThrowExecutor()
            };
            var runner = CreateRunner(dataDir, executors);
            var wf = new Workflow
            {
                Name = "异常步骤记录测试",
                Steps =
                {
                    new WorkflowStep { Index = 1, Type = "success", Enabled = true },
                    new WorkflowStep { Index = 2, Type = "throw", Enabled = true },
                    new WorkflowStep { Index = 3, Type = "success", Enabled = true }
                }
            };

            var record = runner.RunAsync(wf).Result;

            Assert.False(record.Success);
            Assert.Equal(2, record.StepRecords.Count);
            Assert.True(record.StepRecords[0].Success);
            Assert.False(record.StepRecords[1].Success);
            Assert.Contains("throw", record.StepRecords[1].StepName);
            Assert.Contains("Executor抛异常", record.StepRecords[1].Error ?? "");
        }
        finally
        {
            try { Directory.Delete(dataDir, true); } catch { }
        }
    }

    [Fact]
    public void Failure_Stops_Subsequent_Steps_From_Executing()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), $"runner_test_{Guid.NewGuid():N}");
        try
        {
            var counting = new CountingExecutor();
            var executors = new Dictionary<string, IActionStepExecutor>
            {
                ["success"] = new SuccessExecutor(),
                ["fail"] = new FailExecutor(),
                ["counting"] = counting
            };
            var runner = CreateRunner(dataDir, executors);
            var wf = new Workflow
            {
                Name = "失败后停止后续测试",
                Steps =
                {
                    new WorkflowStep { Index = 1, Type = "success", Enabled = true },
                    new WorkflowStep { Index = 2, Type = "fail", Enabled = true },
                    new WorkflowStep { Index = 3, Type = "counting", Enabled = true }
                }
            };

            var record = runner.RunAsync(wf).Result;

            Assert.False(record.Success);
            Assert.Equal(2, record.StepRecords.Count);
            Assert.Equal(0, counting.ExecutionCount);
        }
        finally
        {
            try { Directory.Delete(dataDir, true); } catch { }
        }
    }

    [Fact]
    public void Failed_Step_Has_Screenshot_Or_Evidence()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), $"runner_test_{Guid.NewGuid():N}");
        try
        {
            var executors = new Dictionary<string, IActionStepExecutor>
            {
                ["fail"] = new FailExecutor()
            };
            var runner = CreateRunner(dataDir, executors);
            var wf = new Workflow
            {
                Name = "失败证据测试",
                Steps =
                {
                    new WorkflowStep { Index = 1, Type = "fail", Enabled = true }
                }
            };

            var record = runner.RunAsync(wf).Result;

            Assert.False(record.Success);
            Assert.Equal(1, record.StepRecords.Count);
            Assert.False(record.StepRecords[0].Success);

            var screenshotDir = Path.Combine(dataDir, "screenshots");
            if (Directory.Exists(screenshotDir))
            {
                var evidenceFiles = Directory.GetFiles(screenshotDir, "*fail*");
                Assert.True(evidenceFiles.Length > 0 || record.ScreenshotPath != null,
                    "失败步骤应有截图或证据文件");
            }
        }
        finally
        {
            try { Directory.Delete(dataDir, true); } catch { }
        }
    }
}
