using System.Text.Json;
using SkyAuto.Core.Engine;
using SkyAuto.Core.Models;

namespace SkyAuto.Core.Assets;

public class AssetCaptureService : IAssetCaptureService
{
    public async Task<Asset> CaptureScreenAsImageAssetAsync(string name, string dataDir, IActionStepExecutor screenshotExecutor)
    {
        if (string.IsNullOrEmpty(dataDir))
            throw new InvalidOperationException("dataDir 不能为空");

        var imagesDir = Path.Combine(dataDir, "assets", "images");
        Directory.CreateDirectory(imagesDir);

        var tempStep = new WorkflowStep { Type = "screenshot" };
        var result = await screenshotExecutor.ExecuteAsync(tempStep);

        if (!result.Success)
            throw new InvalidOperationException($"截图执行失败: {result.Error ?? "未知错误"}");

        var sourcePath = ExtractScreenshotPath(result);
        if (sourcePath == null || !File.Exists(sourcePath))
            throw new InvalidOperationException("截图执行成功但未返回真实图片路径");

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var ext = Path.GetExtension(sourcePath);
        if (string.IsNullOrEmpty(ext)) ext = ".png";
        var destPath = Path.Combine(imagesDir, $"{name}_{timestamp}{ext}");
        File.Copy(sourcePath, destPath, overwrite: true);

        return new Asset
        {
            Name = name,
            Type = "image",
            FilePath = destPath,
            Description = "截图取样生成",
            CreatedAt = DateTime.Now
        };
    }

    public Asset CreateOcrRegionAsset(string name, int x, int y, int width, int height, string dataDir)
    {
        if (string.IsNullOrEmpty(dataDir))
            throw new InvalidOperationException("dataDir 不能为空");

        var ocrDir = Path.Combine(dataDir, "assets", "ocr_regions");
        Directory.CreateDirectory(ocrDir);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var path = Path.Combine(ocrDir, $"{name}_{timestamp}.json");

        var json = JsonSerializer.Serialize(new { x, y, width, height }, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);

        return new Asset
        {
            Name = name,
            Type = "ocr_region",
            FilePath = path,
            Description = "OCR区域配置",
            CreatedAt = DateTime.Now
        };
    }

    private static string? ExtractScreenshotPath(StepExecutionResult result)
    {
        if (!string.IsNullOrEmpty(result.ScreenshotPath) && File.Exists(result.ScreenshotPath))
            return result.ScreenshotPath;

        if (!string.IsNullOrEmpty(result.OutputData))
        {
            if (File.Exists(result.OutputData))
                return result.OutputData;

            var idx = result.OutputData.LastIndexOf("已保存到");
            if (idx >= 0)
            {
                var candidate = result.OutputData.Substring(idx + 4).Trim();
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return null;
    }
}
