using SkyAuto.Core.Models;

namespace SkyAuto.Core.StepTesting;

public class StepTestRequest
{
    public Workflow Workflow { get; set; } = new();
    public WorkflowStep Step { get; set; } = new();
    public Dictionary<string, object?>? Context { get; set; }
    public string DataDir { get; set; } = string.Empty;
    public TimeSpan? LockWaitTimeout { get; set; }
}
