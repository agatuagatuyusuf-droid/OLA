using SkyAuto.Core.Engine;
using SkyAuto.Core.Models;

namespace SkyAuto.Core.Steps.SystemSteps;

public class OpenProgramExecutor : ActionStepExecutorBase
{
    public override string Type => "open_program";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var path = step.Params.GetValueOrDefault("path")?.ToString();
        if (string.IsNullOrEmpty(path)) return "路径未指定";

        var proc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        { FileName = path, UseShellExecute = true });
        await Task.Yield();
        return $"已启动: {path} (PID={proc?.Id})";
    }
}

public class OpenUrlExecutor : ActionStepExecutorBase
{
    public override string Type => "open_url";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var url = step.Params.GetValueOrDefault("url")?.ToString();
        if (string.IsNullOrEmpty(url)) return "网址未指定";

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        { FileName = url, UseShellExecute = true });
        await Task.Yield();
        return $"已打开: {url}";
    }
}

public class RunCmdExecutor : ActionStepExecutorBase
{
    public override string Type => "run_cmd";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var cmd = step.Params.GetValueOrDefault("command")?.ToString() ?? "";
        var workingDir = step.Params.GetValueOrDefault("workingDir")?.ToString();

        using var proc = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {cmd}",
                WorkingDirectory = string.IsNullOrEmpty(workingDir) ? Environment.CurrentDirectory : workingDir,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        proc.Start();
        var output = await proc.StandardOutput.ReadToEndAsync();
        await proc.WaitForExitAsync();
        return $"退出码={proc.ExitCode}\n{output}";
    }
}

public class SleepExecutor : ActionStepExecutorBase
{
    public override string Type => "sleep";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var seconds = int.TryParse(step.Params.GetValueOrDefault("seconds")?.ToString(), out var s) ? s : 3;
        await Task.Delay(seconds * 1000);
        return $"已等待 {seconds} 秒";
    }
}

public class KillProcessExecutor : ActionStepExecutorBase
{
    public override string Type => "kill_process";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var name = step.Params.GetValueOrDefault("processName")?.ToString();
        if (string.IsNullOrEmpty(name)) return "进程名未指定";

        var processes = System.Diagnostics.Process.GetProcessesByName(name);
        foreach (var p in processes)
        {
            try { p.Kill(); } catch { }
        }
        await Task.Yield();
        return $"已关闭 {processes.Length} 个进程: {name}";
    }
}
