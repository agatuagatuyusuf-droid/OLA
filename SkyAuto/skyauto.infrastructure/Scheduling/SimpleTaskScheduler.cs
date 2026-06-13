using SkyAuto.Core.Models;

namespace SkyAuto.Infrastructure.Scheduling;

public class SimpleTaskScheduler
{
    private readonly Dictionary<string, System.Threading.Timer> _timers = new();
    private readonly Func<Schedule, Task> _onTrigger;
    private readonly ILogger? _logger;

    public SimpleTaskScheduler(Func<Schedule, Task> onTrigger, ILogger? logger = null)
    {
        _onTrigger = onTrigger;
        _logger = logger;
    }

    public void Start(Schedule schedule)
    {
        if (!schedule.Enabled) return;

        _timers[schedule.Id] = CreateTimer(schedule);
    }

    public void Stop(string id)
    {
        if (_timers.ContainsKey(id))
        {
            var timer = _timers[id];
            _timers.Remove(id);
            timer.Dispose();
        }
    }

    public void StopAll()
    {
        foreach (var kv in _timers)
        {
            kv.Value.Dispose();
        }
        _timers.Clear();
    }

    private System.Threading.Timer CreateTimer(Schedule schedule)
    {
        TimeSpan interval;
        switch (schedule.RuleType)
        {
            case "interval":
                interval = TimeSpan.FromMinutes(schedule.IntervalMinutes);
                break;
            case "daily":
            default:
                // Default to 60 minutes for daily (simplified - full cron support can be added later)
                interval = TimeSpan.FromHours(24);
                break;
        }

        var timer = new System.Threading.Timer(async _ =>
        {
            try
            {
                await _onTrigger(schedule);
                schedule.NextRunTime = DateTime.Now + interval;
            }
            catch (Exception ex)
            {
                _logger?.Error($"定时任务执行失败 [{schedule.Id}]: {ex.Message}");
            }
        }, null, 0, (long)interval.TotalMilliseconds);

        return timer;
    }

    public interface ILogger
    {
        void Error(string message);
    }
}
