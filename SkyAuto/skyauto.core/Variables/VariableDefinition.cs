namespace SkyAuto.Core.Variables;

public class VariableDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public VariableType Type { get; set; } = VariableType.Text;
    public VariableScope Scope { get; set; } = VariableScope.Workflow;
    public string? WorkflowId { get; set; }
    public string? DefaultValue { get; set; }
    public bool Required { get; set; }
    public bool IsSecret { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public void Normalize()
    {
        Key = Key.Trim();

        if (Type == VariableType.Password)
            IsSecret = true;

        UpdatedAt = DateTime.Now;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Key))
            throw new InvalidOperationException("变量 Key 不能为空");

        if (Key.Contains(' '))
            throw new InvalidOperationException("变量 Key 不能包含空格");
    }
}
