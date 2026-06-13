using SkyAuto.Core.Models;

namespace SkyAuto.Core.Engine;

public abstract class ActionStepExecutorBase : IActionStepExecutor
{
    public abstract string Type { get; }

    public async Task<StepExecutionResult> ExecuteAsync(WorkflowStep step, Dictionary<string, object?>? context = null)
    {
        try
        {
            var result = await DoExecuteAsync(step, context);
            return new StepExecutionResult { Success = true, OutputData = result };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new StepExecutionResult { Success = false, Error = ex.Message };
        }
        catch (OperationCanceledException ex)
        {
            return new StepExecutionResult { Success = false, Error = $"操作被取消: {ex.Message}" };
        }
    }

    protected abstract Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? context);
}
