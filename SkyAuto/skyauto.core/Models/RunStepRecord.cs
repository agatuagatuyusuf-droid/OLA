namespace SkyAuto.Core.Models;

public class RunStepRecord
{
    public int Index { get; set; }
    public string StepName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public long DurationMs => (EndTime - StartTime).Ticks / 10_000;
    public bool Success { get; set; }
    public string? InputData { get; set; }
    public string? OutputData { get; set; }
    public string? Error { get; set; }
    public string? ScreenshotPath { get; set; }
}
