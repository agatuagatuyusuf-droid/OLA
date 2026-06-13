namespace SkyAuto.Core.Variables;

public class VariableValue
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string VariableId { get; set; } = string.Empty;
    public string? WorkflowId { get; set; }
    public string? RunId { get; set; }
    public string? StepId { get; set; }
    public string Value { get; set; } = string.Empty;
    public bool IsEncrypted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
