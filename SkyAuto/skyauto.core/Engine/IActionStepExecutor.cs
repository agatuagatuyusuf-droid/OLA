using SkyAuto.Core.Models;

namespace SkyAuto.Core.Engine;

public interface IActionStepExecutor
{
    string Type { get; }

    Task<StepExecutionResult> ExecuteAsync(WorkflowStep step, Dictionary<string, object?>? context = null);
}
