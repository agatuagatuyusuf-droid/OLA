using System.Net.Http.Headers;
using System.Text;
using SkyAuto.Core.Engine;
using SkyAuto.Core.Models;

namespace SkyAuto.Core.Steps.NetworkSteps;

public class HttpGetExecutor : ActionStepExecutorBase
{
    public override string Type => "http_get";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var url = step.Params.GetValueOrDefault("url")?.ToString();
        if (string.IsNullOrEmpty(url)) return "URL未指定";
        using var client = new HttpClient();
        var resp = await client.GetAsync(url);
        var content = await resp.Content.ReadAsStringAsync();
        return $"状态码={resp.StatusCode}\n{content}";
    }
}

public class HttpPostExecutor : ActionStepExecutorBase
{
    public override string Type => "http_post";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var url = step.Params.GetValueOrDefault("url")?.ToString();
        var body = step.Params.GetValueOrDefault("body")?.ToString() ?? "";
        if (string.IsNullOrEmpty(url)) return "URL未指定";
        using var client = new HttpClient();
        var content = new StringContent(body, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));
        var resp = await client.PostAsync(url, content);
        var result = await resp.Content.ReadAsStringAsync();
        return $"状态码={resp.StatusCode}\n{result}";
    }
}

public class DownloadFileExecutor : ActionStepExecutorBase
{
    public override string Type => "download_file";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var url = step.Params.GetValueOrDefault("url")?.ToString();
        var savePath = step.Params.GetValueOrDefault("savePath")?.ToString() ?? Path.GetFileName(url);
        if (string.IsNullOrEmpty(url)) return "URL未指定";
        using var client = new HttpClient();
        var data = await client.GetByteArrayAsync(url);
        await File.WriteAllBytesAsync(savePath, data);
        return $"已下载: {url} -> {savePath} ({data.Length} 字节)";
    }
}
