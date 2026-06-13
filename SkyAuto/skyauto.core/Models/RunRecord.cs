namespace SkyAuto.Core.Models;

public class RunRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string WorkflowId { get; set; } = string.Empty;
    public string? WorkflowName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public long DurationMs => (EndTime - StartTime).Ticks / 10_000;
    public bool Success { get; set; }
    public string? FailedStepName { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ScreenshotPath { get; set; }
    public List<RunStepRecord> StepRecords { get; set; } = new();
}
