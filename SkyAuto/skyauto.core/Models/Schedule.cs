namespace SkyAuto.Core.Models;

public class Schedule
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string WorkflowId { get; set; } = string.Empty;
    public string RuleType { get; set; } = "daily"; // daily, interval, weekly, startup
    public string CronExpression { get; set; } = string.Empty;
    public int IntervalMinutes { get; set; } = 60;
    public bool Enabled { get; set; } = true;
    public DateTime? NextRunTime { get; set; }
    public string? LastResult { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public static readonly Dictionary<string, string> RuleLabels = new()
    {
        ["daily"] = "每天固定时间",
        ["interval"] = "间隔 N 分钟",
        ["weekly"] = "每周指定",
        ["startup"] = "开机自动启动"
    };
}
