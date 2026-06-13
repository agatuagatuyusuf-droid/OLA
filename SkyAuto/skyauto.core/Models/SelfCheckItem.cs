namespace SkyAuto.Core.Models;

public class SelfCheckItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Level { get; set; } // 1=结构, 2=功能, 3=真实OLA
    public string Status { get; set; } = "pending"; // pending, running, pass, fail, skip
    public string? Evidence { get; set; }
    public bool IsMock { get; set; }

    public bool Passed => Status == "pass" || Status == "skip";
    public bool Failed => Status == "fail";
}
