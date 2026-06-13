using System.Text.Json.Serialization;

namespace SkyAuto.Core.Models;

public class WorkflowStep
{
    public int Index { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object?> Params { get; set; } = new();
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 0;
    public bool Enabled { get; set; } = true;

    [JsonIgnore]
    public string ParamSummary => System.Text.Json.JsonSerializer.Serialize(Params);
}
