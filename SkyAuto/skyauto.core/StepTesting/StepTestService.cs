using System.Text.Json;
using SkyAuto.Core.Engine;
using SkyAuto.Core.Models;
using SkyAuto.Core.Runtime;

namespace SkyAuto.Core.StepTesting;

public class StepTestService : IStepTestService
{
    private readonly Dictionary<string, IActionStepExecutor> _executors;
    private readonly IWorkflowRunLockService? _lockService;

    public StepTestService(
        Dictionary<string, IActionStepExecutor> executors,
        IWorkflowRunLockService? lockService = null)
    {
        _executors = executors;
        _lockService = lockService;
    }

    public async Task<StepTestResult> TestStepAsync(StepTestRequest request)
    {
        var result = new StepTestResult
        {
            WorkflowId = request.Workflow.Id,
            StepName = $"{request.Step.Category}/{request.Step.Type}",
            StepType = request.Step.Type,
            Success = true,
            StartedAt = DateTime.Now
        };

        bool lockAcquired = false;

        try
        {
            if (!request.Step.Enabled)
            {
                result.Success = false;
                result.Error = "步骤已禁用";
            }
            else
            {
                var executor = _executors.GetValueOrDefault(request.Step.Type);
                if (executor == null)
                {
                    result.Success = false;
                    result.Error = $"未注册的步骤类型: {request.Step.Type}";
                }
                else
                {
                    var requiresLock = AutomationActionClassifier.RequiresGlobalAutomationLock(request.Step);
                    result.UsedGlobalAutomationLock = requiresLock;

                    if (requiresLock && _lockService != null)
                    {
                        lockAcquired = _lockService.TryAcquire("global:automation", result.Id, request.LockWaitTimeout);
                        if (!lockAcquired)
                        {
                            result.Success = false;
                            result.Error = "已有自动化流程正在控制鼠标/键盘/窗口，当前单步测试已跳过 (global:automation)";
                            result.NotVerified = true;
                        }
                    }

                    if (result.Success)
                    {
                        var context = request.Context ?? new Dictionary<string, object?>();
                        var executionResult = await executor.ExecuteAsync(request.Step, context);
                        result.Success = executionResult.Success;
                        result.OutputData = executionResult.OutputData;
                        result.Error = executionResult.Error;
                        result.ScreenshotPath = executionResult.ScreenshotPath;

                        if (!result.Success)
                        {
                            var evidencePath = CreateFailureEvidence(request.DataDir, request.Workflow, request.Step, result.Error);
                            if (evidencePath != null && result.ScreenshotPath == null)
                                result.ScreenshotPath = evidencePath;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }
        finally
        {
            if (lockAcquired)
                _lockService?.Release(result.Id);
        }

        result.EndedAt = DateTime.Now;
        result.DurationMs = (long)(result.EndedAt - result.StartedAt).TotalMilliseconds;
        result.EvidenceJson = BuildEvidenceJson(result, request);
        return result;
    }

    private static string? CreateFailureEvidence(string dataDir, Workflow workflow, WorkflowStep step, string? error)
    {
        if (string.IsNullOrEmpty(dataDir)) return null;

        try
        {
            var screenshotDir = Path.Combine(dataDir, "screenshots");
            Directory.CreateDirectory(screenshotDir);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var path = Path.Combine(screenshotDir, $"step_test_fail_{workflow.Name}_{step.Type}_{timestamp}.txt");
            File.WriteAllText(path, $"Workflow: {workflow.Name}\nStep: {step.Type}\nIndex: {step.Index}\nError: {error}\nTime: {DateTime.Now:o}");
            return path;
        }
        catch
        {
            return null;
        }
    }

    private static string BuildEvidenceJson(StepTestResult result, StepTestRequest request)
    {
        var evidence = new Dictionary<string, object?>
        {
            ["workflowId"] = result.WorkflowId,
            ["stepIndex"] = request.Step.Index,
            ["stepType"] = result.StepType,
            ["startedAt"] = result.StartedAt.ToString("o"),
            ["endedAt"] = result.EndedAt.ToString("o"),
            ["durationMs"] = result.DurationMs,
            ["usedGlobalAutomationLock"] = result.UsedGlobalAutomationLock,
            ["success"] = result.Success,
            ["outputData"] = result.OutputData,
            ["error"] = result.Error,
            ["screenshotPath"] = result.ScreenshotPath
        };
        return JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true });
    }
}
