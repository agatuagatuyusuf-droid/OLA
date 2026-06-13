using SkyAuto.Core.Engine;
using SkyAuto.Core.Models;

namespace SkyAuto.Core.Steps.FileSteps;

public class ReadFileExecutor : ActionStepExecutorBase
{
    public override string Type => "read_file";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var path = step.Params.GetValueOrDefault("path")?.ToString();
        if (string.IsNullOrEmpty(path)) return "路径未指定";
        var content = await File.ReadAllTextAsync(path);
        return content;
    }
}

public class WriteFileExecutor : ActionStepExecutorBase
{
    public override string Type => "write_file";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var path = step.Params.GetValueOrDefault("path")?.ToString();
        var content = step.Params.GetValueOrDefault("content")?.ToString() ?? "";
        if (string.IsNullOrEmpty(path)) return "路径未指定";
        await File.WriteAllTextAsync(path, content);
        return $"已写入: {path} ({content.Length} 字符)";
    }
}

public class CopyFileExecutor : ActionStepExecutorBase
{
    public override string Type => "copy_file";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var src = step.Params.GetValueOrDefault("sourcePath")?.ToString();
        var dst = step.Params.GetValueOrDefault("destPath")?.ToString();
        if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(dst)) return "路径不完整";
        File.Copy(src, dst, true);
        await Task.Yield();
        return $"已复制: {src} -> {dst}";
    }
}

public class MoveFileExecutor : ActionStepExecutorBase
{
    public override string Type => "move_file";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var src = step.Params.GetValueOrDefault("sourcePath")?.ToString();
        var dst = step.Params.GetValueOrDefault("destPath")?.ToString();
        if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(dst)) return "路径不完整";
        File.Move(src, dst);
        await Task.Yield();
        return $"已移动: {src} -> {dst}";
    }
}

public class DeleteFileExecutor : ActionStepExecutorBase
{
    public override string Type => "delete_file";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var path = step.Params.GetValueOrDefault("path")?.ToString();
        if (string.IsNullOrEmpty(path)) return "路径未指定";
        File.Delete(path);
        await Task.Yield();
        return $"已删除: {path}";
    }
}
