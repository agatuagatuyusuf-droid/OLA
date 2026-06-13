namespace SkyAuto.Core.Variables;

public interface IVariableStore
{
    List<VariableDefinition> GetVariables(string? workflowId = null);
    VariableDefinition? GetVariableByKey(string key, string? workflowId = null);
    void SaveVariable(VariableDefinition variable);
    void DeleteVariable(string id);

    List<VariableValue> GetVariableValues(string? workflowId = null, string? runId = null);
    void SaveVariableValue(VariableValue value);
    VariableValue? GetLatestValue(string variableId, string? workflowId = null);
}
