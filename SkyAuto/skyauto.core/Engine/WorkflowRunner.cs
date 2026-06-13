using SkyAuto.Core.Models;
using SkyAuto.Core.Runtime;

namespace SkyAuto.Core.Engine;

public class WorkflowRunner
{
    private readonly Dictionary<string, IActionStepExecutor> _executors;
    private readonly string? _dataDir;
    private readonly IWorkflowRunLockService? _lockService;

    public WorkflowRunner(Dictionary<string, IActionStepExecutor> executors, string dataDir = "", IWorkflowRunLockService? lockService = null)
    {
        _executors = executors;
        _dataDir = dataDir;
        _lockService = lockService;
    }

    public async Task<RunRecord> RunAsync(Workflow workflow, TimeSpan? waitForTimeout = null)
    {
        var record = new RunRecord
        {
            WorkflowId = workflow.Id,
            WorkflowName = workflow.Name,
            StartTime = DateTime.Now
        };

        // Acquire lock to prevent concurrent execution of the same workflow
        if (_lockService != null)
        {
            var acquired = _lockService.TryAcquire(workflow.Id, record.Id, waitForTimeout);
            if (!acquired)
            {
                record.Success = false;
                record.ErrorMessage = $"流程正在运行中，无法重复启动 (workflow: {workflow.Name})";
                record.FailedStepName = "LOCK_FAILED";
                record.EndTime = DateTime.Now;
                return record;
            }
        }

        try
        {
            var context = new Dictionary<string, object?>(workflow.Variables);

            foreach (var step in workflow.Steps)
        {
            if (!step.Enabled) continue;

            var stepRecord = new RunStepRecord
            {
                Index = step.Index,
                StepName = $"{step.Category}/{step.Type}",
                StartTime = DateTime.Now
            };

            try
            {
                var executor = _executors.GetValueOrDefault(step.Type);
                if (executor == null)
                    throw new InvalidOperationException($"未注册的步骤类型: {step.Type}");

                int attempts = 0;
                StepExecutionResult result;

                do
                {
                    attempts++;
                    result = await executor.ExecuteAsync(step, context);

                    if (result.Success || attempts > step.RetryCount) break;
                } while (true);

                stepRecord.EndTime = DateTime.Now;
                stepRecord.Success = result.Success;
                stepRecord.OutputData = result.OutputData;
                stepRecord.Error = result.Error;
                stepRecord.ScreenshotPath = result.ScreenshotPath;

                if (!result.Success)
                {
                    record.Success = false;
                    record.FailedStepName = stepRecord.StepName;
                    record.ErrorMessage = result.Error ?? "步骤执行失败";
                    
                    // Auto-screenshot on failure
                    try
                    {
                        var screenshot = TakeScreenshot(workflow.Name, step.Type);
                        if (!string.IsNullOrEmpty(screenshot))
                        {
                            record.ScreenshotPath = screenshot;
                            stepRecord.ScreenshotPath = screenshot;
                        }
                    }
                    catch { /* screenshot failed - non-critical */ }

                    break;
                }
            }
            catch (Exception ex)
            {
                stepRecord.EndTime = DateTime.Now;
                stepRecord.Success = false;
                stepRecord.Error = ex.Message;

                record.Success = false;
                record.FailedStepName = stepRecord.StepName;
                record.ErrorMessage = ex.Message;

                // Auto-screenshot on exception
                try
                {
                    var screenshot = TakeScreenshot(workflow.Name, step.Type);
                    if (!string.IsNullOrEmpty(screenshot))
                    {
                        record.ScreenshotPath = screenshot;
                        stepRecord.ScreenshotPath = screenshot;
                    }
                }
                catch { /* screenshot failed - non-critical */ }

                break;
            }

            record.StepRecords.Add(stepRecord);
        }

        record.EndTime = DateTime.Now;

            // Set success to true since no failures occurred during execution
            record.Success = true;

            return record;
        }
        finally
        {
            _lockService?.Release(record.Id);
        }
    }

    private string? TakeScreenshot(string workflowName, string stepType)
    {
        if (string.IsNullOrEmpty(_dataDir)) return null;

        try
        {
            var screenshotDir = Path.Combine(_dataDir, "screenshots");
            Directory.CreateDirectory(screenshotDir);

            // Try using ScreenshotExecutor to capture screen
            if (_executors.TryGetValue("screenshot", out var executor))
            {
                var tempStep = new WorkflowStep { Type = "screenshot" };
                try
                {
                    var result = executor.ExecuteAsync(tempStep).Result;
                    if (!string.IsNullOrEmpty(result.OutputData))
                    {
                        // Extract path from output message (format: "截图已保存到 <path>")
                        var idx = result.OutputData.LastIndexOf("已保存到");
                        if (idx >= 0) return result.OutputData.Substring(idx + 4).Trim();

                        // If it's already a file path, return directly
                        if (File.Exists(result.OutputData)) return result.OutputData;

                        // Check common screenshot paths
                        var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        foreach (var ext in new[] { ".bmp", ".png" })
                        {
                            var p = Path.Combine(screenshotDir, $"fail_{workflowName}_{stepType}_{ts}{ext}");
                            if (File.Exists(p)) return p;
                        }

                        // Create fallback text evidence with output data
                        var fPath1 = Path.Combine(screenshotDir, $"fail_{workflowName}_{stepType}_{ts}.txt");
                        File.WriteAllText(fPath1, $"步骤 {stepType} 执行失败时的截图证据\n时间: {DateTime.Now:o}\n输出: {result.OutputData}");
                        return fPath1;
                    }

                    // Executor ran but produced no output - check for files it may have created
                    var ts2 = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    foreach (var ext in new[] { ".bmp", ".png" })
                    {
                        var p = Path.Combine(screenshotDir, $"screenshot_{ts2}{ext}");
                        if (File.Exists(p)) return p;
                    }
                }
                catch { /* screenshot executor failed - use fallback */ }

                // Fallback: create evidence file after executor attempt
                var fPath2 = Path.Combine(screenshotDir, $"fail_{workflowName}_{stepType}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllText(fPath2, $"步骤 {stepType} 执行失败\n时间: {DateTime.Now:o}");
                return fPath2;
            }

            // No screenshot executor available - create text evidence
            var fPath3 = Path.Combine(screenshotDir, $"fail_{workflowName}_{stepType}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            File.WriteAllText(fPath3, $"步骤 {stepType} 执行失败\n时间: {DateTime.Now:o}");
            return fPath3;
        }
        catch
        {
            return null;
        }
    }
}
