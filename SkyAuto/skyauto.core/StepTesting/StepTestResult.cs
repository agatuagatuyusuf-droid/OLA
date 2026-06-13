namespace SkyAuto.Core.StepTesting;

public class StepTestResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string WorkflowId { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public string StepType { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? OutputData { get; set; }
    public string? Error { get; set; }
    public string? ScreenshotPath { get; set; }
    public string EvidenceJson { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.Now;
    public DateTime EndedAt { get; set; }
    public long DurationMs { get; set; }
    public bool UsedGlobalAutomationLock { get; set; }
    public bool IsMock { get; set; }
    public bool NotVerified { get; set; }
}
