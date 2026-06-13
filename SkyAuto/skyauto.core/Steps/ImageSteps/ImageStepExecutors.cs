using System.Runtime.InteropServices;
using SkyAuto.Core.Engine;
using SkyAuto.Core.Models;

namespace SkyAuto.Core.Steps.ImageSteps;

public class ScreenshotExecutor : ActionStepExecutorBase
{
    public override string Type => "screenshot";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var path = $"data/screenshots/screenshot_{timestamp}.bmp";
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "data/screenshots");

        try
        {
            var hScreen = GetDC(IntPtr.Zero);
            var width = GetSystemMetrics(0);
            var height = GetSystemMetrics(1);

            var hBmp = CreateCompatibleBitmap(hScreen, width, height);
            var hMemDc = CreateCompatibleDC(hScreen);
            SelectObject(hMemDc, hBmp);
            BitBlt(hMemDc, 0, 0, width, height, hScreen, 0, 0, 0x40000000 | 0xCC0020);

            SaveBitmapToFile(hBmp, path);
            DeleteObject(hBmp);
            DeleteDC(hMemDc);
            ReleaseDC(IntPtr.Zero, hScreen);
        }
        catch (Exception ex)
        {
            return $"截图失败: {ex.Message}";
        }

        await Task.Yield();
        return $"已保存截图: {path}";
    }

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);
    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);
    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);
    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private void SaveBitmapToFile(IntPtr hBmp, string path) => File.WriteAllBytes(path, GenerateBmpData(hBmp));

    private byte[] GenerateBmpData(IntPtr hBmp)
    {
        int width = GetSystemMetrics(0);
        int height = GetSystemMetrics(1);

        var bihSize = Marshal.SizeOf<BitmapInfoHeader>();
        var sizeImage = (width * 32 + 31) / 32 * 4 * height;
        int fileSize = 54 + sizeImage;

        using var ms = new MemoryStream(fileSize);
        using var bw = new BinaryWriter(ms);

        bw.Write((short)0x4D42);
        bw.Write(fileSize);
        bw.Write(0);
        bw.Write(54);

        bw.Write(bihSize);
        bw.Write(width);
        bw.Write(height);
        bw.Write((short)1);
        bw.Write((short)32);
        bw.Write(0);
        bw.Write(sizeImage);
        bw.Write(2835);
        bw.Write(2835);
        bw.Write(0);
        bw.Write(0);

        var pixels = new byte[sizeImage];
        var bih = new BitmapInfoHeader { biSize = Marshal.SizeOf<BitmapInfoHeader>(), biWidth = width, biHeight = height, biPlanes = 1, biBitCount = 32 };
        GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            GetDIBits(IntPtr.Zero, hBmp, 0, (uint)height, handle.AddrOfPinnedObject(), ref bih, 0);
        }
        finally
        {
            if (handle.IsAllocated) handle.Free();
        }

        bw.Write(pixels);
        return ms.ToArray();
    }

    [DllImport("gdi32.dll")]
    private static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint uStartScan, uint cScanLines, IntPtr lpvBits, ref BitmapInfoHeader bmi, int iUsage);

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public int biSize;
        public int biWidth;
        public int biHeight;
        public short biPlanes;
        public short biBitCount;
        public int biCompression;
        public int biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public int biClrUsed;
        public int biClrImportant;
    }
}

public class FindImageExecutor : ActionStepExecutorBase
{
    public override string Type => "find_image";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var imagePath = step.Params.GetValueOrDefault("image_asset_id")?.ToString();
        if (string.IsNullOrEmpty(imagePath)) return "图片素材ID未指定";

        await Task.Yield();
        return "图像查找功能需要 OLA 插件支持，当前返回占位结果";
    }
}

public class WaitImageExecutor : ActionStepExecutorBase
{
    public override string Type => "wait_image";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var imagePath = step.Params.GetValueOrDefault("image_asset_id")?.ToString();
        if (string.IsNullOrEmpty(imagePath)) return "图片素材ID未指定";

        var timeout = int.Parse(step.Params.GetValueOrDefault("timeout")?.ToString() ?? "30");

        await Task.Delay(timeout * 1000);
        return "等待图片超时，OLA 插件未连接";
    }
}

public class ClickImageExecutor : ActionStepExecutorBase
{
    public override string Type => "click_image";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var imagePath = step.Params.GetValueOrDefault("image_asset_id")?.ToString();
        if (string.IsNullOrEmpty(imagePath)) return "图片素材ID未指定";

        await Task.Yield();
        return "图像点击功能需要 OLA 插件支持";
    }
}

public class JudgeImageExecutor : ActionStepExecutorBase
{
    public override string Type => "judge_image";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var imagePath = step.Params.GetValueOrDefault("image_asset_id")?.ToString();
        if (string.IsNullOrEmpty(imagePath)) return "图片路径未指定";

        await Task.Yield();
        return "图像判断功能需要 OLA 插件支持，返回默认不匹配";
    }
}

public class FindColorExecutor : ActionStepExecutorBase
{
    public override string Type => "find_color";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var color = step.Params.GetValueOrDefault("color")?.ToString();
        if (string.IsNullOrEmpty(color)) return "颜色值未指定";

        await Task.Yield();
        return "找色功能需要 OLA 插件支持";
    }
}

public class FindMultiColorExecutor : ActionStepExecutorBase
{
    public override string Type => "find_multi_color";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var colors = step.Params.GetValueOrDefault("offsetColors")?.ToString();
        if (string.IsNullOrEmpty(colors)) return "颜色值未指定";

        await Task.Yield();
        return "多点找色功能需要 OLA 插件支持";
    }
}
