namespace SkyAuto.Core.Engine;

public class StepExecutionResult
{
    public bool Success { get; set; }
    public string? OutputData { get; set; }
    public string? Error { get; set; }
    public string? ScreenshotPath { get; set; }
}
