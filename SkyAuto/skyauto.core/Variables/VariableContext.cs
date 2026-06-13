namespace SkyAuto.Core.Variables;

public class VariableContext
{
    public Dictionary<string, object?> GlobalVariables { get; set; } = new();
    public Dictionary<string, object?> WorkflowVariables { get; set; } = new();
    public Dictionary<string, object?> RunVariables { get; set; } = new();
    public Dictionary<string, object?> StepOutputs { get; set; } = new();

    public object? GetValue(string key)
    {
        if (StepOutputs.TryGetValue(key, out var stepValue)) return stepValue;
        if (RunVariables.TryGetValue(key, out var runValue)) return runValue;
        if (WorkflowVariables.TryGetValue(key, out var workflowValue)) return workflowValue;
        if (GlobalVariables.TryGetValue(key, out var globalValue)) return globalValue;
        return null;
    }

    public void SetValue(string key, object? value, VariableScope scope = VariableScope.Run)
    {
        switch (scope)
        {
            case VariableScope.Global:
                GlobalVariables[key] = value;
                break;
            case VariableScope.Workflow:
                WorkflowVariables[key] = value;
                break;
            case VariableScope.Run:
                RunVariables[key] = value;
                break;
            case VariableScope.Step:
                StepOutputs[key] = value;
                break;
        }
    }

    public void SetStepOutput(string stepKey, object? value)
    {
        StepOutputs[stepKey] = value;
    }

    public object? GetStepOutput(string stepKey)
    {
        return StepOutputs.TryGetValue(stepKey, out var value) ? value : null;
    }
}
