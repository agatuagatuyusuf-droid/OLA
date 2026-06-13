using SkyAuto.Core.Engine;
using SkyAuto.Core.Models;

namespace SkyAuto.Core.Steps.OcrSteps;

public class OcrRegionExecutor : ActionStepExecutorBase
{
    public override string Type => "ocr_region";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var x = int.Parse(step.Params.GetValueOrDefault("x")?.ToString() ?? "0");
        var y = int.Parse(step.Params.GetValueOrDefault("y")?.ToString() ?? "0");
        var w = int.Parse(step.Params.GetValueOrDefault("w")?.ToString() ?? "0");
        var h = int.Parse(step.Params.GetValueOrDefault("h")?.ToString() ?? "0");

        await Task.Yield();
        return $"OCR区域识别需要 OLA 插件支持，返回空结果 (区域: {x},{y},{w}x{h})";
    }
}

public class RecognizeNumberExecutor : ActionStepExecutorBase
{
    public override string Type => "recognize_number";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var x = int.Parse(step.Params.GetValueOrDefault("x")?.ToString() ?? "0");
        var y = int.Parse(step.Params.GetValueOrDefault("y")?.ToString() ?? "0");
        var w = int.Parse(step.Params.GetValueOrDefault("w")?.ToString() ?? "0");
        var h = int.Parse(step.Params.GetValueOrDefault("h")?.ToString() ?? "0");

        await Task.Yield();
        return $"识别数字功能需要 OLA 插件支持 (区域: {x},{y},{w}x{h})";
    }
}

public class OcrImageExecutor : ActionStepExecutorBase
{
    public override string Type => "ocr_image";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var imagePath = step.Params.GetValueOrDefault("image_asset_id")?.ToString();
        if (string.IsNullOrEmpty(imagePath)) return "图片素材ID未指定";

        await Task.Yield();
        return "OCR图片识别需要 OLA 插件支持";
    }
}

public class FindTextExecutor : ActionStepExecutorBase
{
    public override string Type => "find_text";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var text = step.Params.GetValueOrDefault("text")?.ToString();
        if (string.IsNullOrEmpty(text)) return "文字内容未指定";

        await Task.Yield();
        return "查找文字功能需要 OLA 插件支持";
    }
}

public class JudgeTextExecutor : ActionStepExecutorBase
{
    public override string Type => "judge_text";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var text = step.Params.GetValueOrDefault("text")?.ToString();
        if (string.IsNullOrEmpty(text)) return "文字内容未指定";

        await Task.Yield();
        return "判断文字功能需要 OLA 插件支持，返回默认不匹配";
    }
}
