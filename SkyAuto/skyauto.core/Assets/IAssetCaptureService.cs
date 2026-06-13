using SkyAuto.Core.Engine;
using SkyAuto.Core.Models;

namespace SkyAuto.Core.Assets;

public interface IAssetCaptureService
{
    Task<Asset> CaptureScreenAsImageAssetAsync(string name, string dataDir, IActionStepExecutor screenshotExecutor);
    Asset CreateOcrRegionAsset(string name, int x, int y, int width, int height, string dataDir);
}
