namespace SkyAuto.Core.Models;

public class Workflow
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public Dictionary<string, object?> Variables { get; set; } = new();
    public List<WorkflowStep> Steps { get; set; } = new();
    public DateTime? LastRunTime { get; set; }
    public string? LastResult { get; set; }
    public bool HasSchedule { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
