using Xunit;
using SkyAuto.Core.Assets;
using SkyAuto.Core.Engine;
using SkyAuto.Core.Models;

namespace SkyAuto.Tests;

public class AssetCaptureServiceTests
{
    private static string CreateTestImage(string dir)
    {
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "test_screenshot.png");
        File.WriteAllBytes(path, new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 });
        return path;
    }

    [Fact]
    public async Task CaptureScreen_Creates_Image_Asset()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), $"asset_test_{Guid.NewGuid():N}");
        try
        {
            var executor = new FakeScreenshotExecutor(true, CreateTestImage(dataDir));
            var service = new AssetCaptureService();

            var asset = await service.CaptureScreenAsImageAssetAsync("测试截图", dataDir, executor);

            Assert.Equal("image", asset.Type);
            Assert.Equal("测试截图", asset.Name);
            Assert.True(File.Exists(asset.FilePath), "Asset 文件应存在");
            Assert.True(new FileInfo(asset.FilePath).Length > 0, "文件大小应大于 0");
        }
        finally { try { Directory.Delete(dataDir, true); } catch { } }
    }

    [Fact]
    public async Task CaptureScreen_NoRealImage_Fails()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), $"asset_test_{Guid.NewGuid():N}");
        try
        {
            var executor = new FakeScreenshotExecutor(true, outputData: "无真实路径，只有文本");
            var service = new AssetCaptureService();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CaptureScreenAsImageAssetAsync("失败测试", dataDir, executor));

            Assert.Contains("未返回真实图片路径", ex.Message);
        }
        finally { try { Directory.Delete(dataDir, true); } catch { } }
    }

    [Fact]
    public void CreateOcrRegionAsset_Generates_Json()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), $"asset_test_{Guid.NewGuid():N}");
        try
        {
            var service = new AssetCaptureService();

            var asset = service.CreateOcrRegionAsset("测试OCR区域", 100, 200, 300, 400, dataDir);

            Assert.Equal("ocr_region", asset.Type);
            Assert.Equal("测试OCR区域", asset.Name);
            Assert.True(File.Exists(asset.FilePath), "json 文件应存在");

            var json = File.ReadAllText(asset.FilePath);
            Assert.Contains("\"x\": 100", json);
            Assert.Contains("\"y\": 200", json);
            Assert.Contains("\"width\": 300", json);
            Assert.Contains("\"height\": 400", json);
        }
        finally { try { Directory.Delete(dataDir, true); } catch { } }
    }

    [Fact]
    public async Task DataDir_NotExist_Creates_Automatically()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), $"asset_test_{Guid.NewGuid():N}", "subdir", "nested");
        try
        {
            Assert.False(Directory.Exists(dataDir), "测试前目录不应存在");

            var executor = new FakeScreenshotExecutor(true, CreateTestImage(dataDir));
            var service = new AssetCaptureService();

            var asset = await service.CaptureScreenAsImageAssetAsync("自动创建目录", dataDir, executor);

            Assert.True(Directory.Exists(dataDir), "目录应被自动创建");
            Assert.True(File.Exists(asset.FilePath));
        }
        finally { try { Directory.Delete(dataDir, true); } catch { } }
    }

    [Fact]
    public void Asset_Name_Is_Preserved()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), $"asset_test_{Guid.NewGuid():N}");
        try
        {
            var service = new AssetCaptureService();
            var asset = service.CreateOcrRegionAsset("自定义名称", 0, 0, 100, 100, dataDir);

            Assert.Equal("自定义名称", asset.Name);
            Assert.Equal("ocr_region", asset.Type);
        }
        finally { try { Directory.Delete(dataDir, true); } catch { } }
    }

    public class FakeScreenshotExecutor : IActionStepExecutor
    {
        public string Type => "screenshot";
        private readonly bool _success;
        private readonly string? _screenshotPath;
        private readonly string? _outputData;

        public FakeScreenshotExecutor(bool success, string? screenshotPath = null, string? outputData = null)
        {
            _success = success;
            _screenshotPath = screenshotPath;
            _outputData = outputData;
        }

        public Task<StepExecutionResult> ExecuteAsync(WorkflowStep step, Dictionary<string, object?>? context = null)
            => Task.FromResult(new StepExecutionResult
            {
                Success = _success,
                OutputData = _outputData ?? (_screenshotPath != null ? $"截图已保存到 {_screenshotPath}" : null),
                ScreenshotPath = _screenshotPath,
                Error = _success ? null : "截图失败"
            });
    }
}
