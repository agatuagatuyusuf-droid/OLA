using Quartz;
using Quartz.Impl;
using SkyAuto.Core.Models;

namespace SkyAuto.Infrastructure.Scheduling;

public class QuartzSchedulerService : IDisposable
{
    private readonly IScheduler _scheduler;
    private static Func<Schedule, Task>? Callback { get; set; }

    public QuartzSchedulerService(Func<Schedule, Task> onTrigger)
    {
        Callback = onTrigger;
        _scheduler = StdSchedulerFactory.GetDefaultScheduler().GetAwaiter().GetResult();
    }

    public async Task InitializeAsync() => await _scheduler.Start();

    public async Task AddScheduleAsync(Schedule schedule)
    {
        if (!schedule.Enabled) return;

        var jobKey = new JobKey(schedule.WorkflowId);
        try { await _scheduler.DeleteJob(jobKey); } catch { /* may not exist */ }

        var job = JobBuilder.Create<WorkflowTriggerJob>()
            .WithIdentity(jobKey)
            .Build();

        ITrigger trigger = CreateTrigger(schedule, schedule.Id);

        await _scheduler.ScheduleJob(job, trigger);
    }

    private static ITrigger CreateTrigger(Schedule schedule, string id)
    {
        switch (schedule.RuleType)
        {
            case "interval":
                return TriggerBuilder.Create()
                    .WithIdentity(id + "_trigger")
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithIntervalInMinutes(schedule.IntervalMinutes).RepeatForever())
                    .Build();

            case "daily":
            {
                var hour = schedule.IntervalMinutes > 24 ? 8 : schedule.IntervalMinutes % 24;
                return TriggerBuilder.Create()
                    .WithIdentity(id + "_trigger")
                    .StartNow()
                    .WithCronSchedule($"0 0 {hour} * * ?", x => x.WithMisfireHandlingInstructionFireAndProceed())
                    .Build();
            }

            case "weekly":
            {
                var hour = schedule.IntervalMinutes > 24 ? 8 : schedule.IntervalMinutes % 24;
                return TriggerBuilder.Create()
                    .WithIdentity(id + "_trigger")
                    .StartNow()
                    .WithCronSchedule($"0 0 {hour} * * MON", x => x.WithMisfireHandlingInstructionFireAndProceed())
                    .Build();
            }

            case "startup":
                // Fire once now and never repeat - this is the app-startup trigger
                return TriggerBuilder.Create()
                    .WithIdentity(id + "_trigger")
                    .StartNow()
                    .Build();

            default:
                return TriggerBuilder.Create()
                    .WithIdentity(id + "_trigger")
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithIntervalInMinutes(60).RepeatForever())
                    .Build();
        }
    }

    public async Task RemoveScheduleAsync(string scheduleId)
    {
        var jobKey = new JobKey(scheduleId);
        await _scheduler.DeleteJob(jobKey);
    }

    public async Task StopAllAsync()
    {
        if (_scheduler.IsStarted)
            await _scheduler.Shutdown(true);
    }

    public void Dispose()
    {
        try { _scheduler?.Shutdown(false).Wait(); } catch { /* already disposed */ }
    }

    [DisallowConcurrentExecution]
    internal class WorkflowTriggerJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var scheduleId = context.JobDetail.Key.Name;
            if (Callback != null)
                await Callback(new Schedule { Id = scheduleId, WorkflowId = scheduleId });
        }
    }
}
