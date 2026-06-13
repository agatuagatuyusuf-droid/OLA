using System.Runtime.InteropServices;
using SkyAuto.Core.Engine;
using SkyAuto.Core.Models;

namespace SkyAuto.Core.Steps.WindowSteps;

public class FindWindowExecutor : ActionStepExecutorBase
{
    public override string Type => "find_window";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var title = step.Params.GetValueOrDefault("title")?.ToString();
        if (string.IsNullOrEmpty(title)) return "窗口标题未指定";

        var hwnd = FindWindowA(IntPtr.Zero, title);
        await Task.Yield();

        if (hwnd == IntPtr.Zero)
            return "未找到窗口";

        return $"找到窗口: {title}, HWND={hwnd.ToInt64()}";
    }

    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    private static extern IntPtr FindWindowA(IntPtr lpClassName, string? lpWindowName);
}

public class ActivateWindowExecutor : ActionStepExecutorBase
{
    public override string Type => "activate_window";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var hwndValue = step.Params.GetValueOrDefault("hwnd");
        if (hwndValue == null) return "窗口句柄未指定";

        int hwnd;
        if (hwndValue is int h)
            hwnd = h;
        else if (long.TryParse(hwndValue.ToString(), out long lh))
            hwnd = (int)lh;
        else
            return $"无效的窗口句柄: {hwndValue}";

        SetForegroundWindow((IntPtr)hwnd);
        await Task.Yield();
        return $"已激活窗口, HWND={hwnd}";
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}

public class MoveWindowExecutor : ActionStepExecutorBase
{
    public override string Type => "move_window";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var hwndValue = step.Params.GetValueOrDefault("hwnd");
        if (hwndValue == null) return "窗口句柄未指定";

        int hwnd;
        if (hwndValue is int h)
            hwnd = h;
        else if (long.TryParse(hwndValue.ToString(), out long lh))
            hwnd = (int)lh;
        else
            return $"无效的窗口句柄: {hwndValue}";

        var x = int.Parse(step.Params.GetValueOrDefault("x")?.ToString() ?? "0");
        var y = int.Parse(step.Params.GetValueOrDefault("y")?.ToString() ?? "0");

        SetWindowPos((IntPtr)hwnd, IntPtr.Zero, x, y, 0, 0, 0x0001 | 0x0002);
        await Task.Yield();
        return $"窗口已移动到 ({x}, {y}), HWND={hwnd}";
    }

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
}

public class BindWindowExecutor : ActionStepExecutorBase
{
    public override string Type => "bind_window";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var title = step.Params.GetValueOrDefault("title")?.ToString();
        if (string.IsNullOrEmpty(title)) return "窗口标题未指定";

        var hwnd = FindWindowA(null, title);
        if (hwnd == IntPtr.Zero) return $"未找到窗口: {title}";

        ctx?.TryAdd("bound_hwnd", (long)hwnd);
        await Task.Yield();
        return $"已绑定窗口 \"{title}\", HWND={hwnd}";
    }

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindowA(string? lpClassName, string? lpWindowName);
}

public class UnbindWindowExecutor : ActionStepExecutorBase
{
    public override string Type => "unbind_window";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        if (ctx != null && ctx.Remove("bound_hwnd", out var hwnd))
        {
            await Task.Yield();
            return $"已解绑窗口, HWND={hwnd}";
        }

        await Task.Yield();
        return "当前没有绑定任何窗口";
    }
}

public class ScreenshotWindowExecutor : ActionStepExecutorBase
{
    public override string Type => "screenshot_window";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var path = $"data/screenshots/window_{timestamp}.bmp";
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
            return $"窗口截图失败: {ex.Message}";
        }

        await Task.Yield();
        return $"已保存窗口截图: {path}";
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

        var bihSize = Marshal.SizeOf<BmpHeader>();
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
        var bih = new BmpHeader { biSize = Marshal.SizeOf<BmpHeader>(), biWidth = width, biHeight = height, biPlanes = 1, biBitCount = 32 };
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
    private static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint uStartScan, uint cScanLines, IntPtr lpvBits, ref BmpHeader bmi, int iUsage);

    [StructLayout(LayoutKind.Sequential)]
    private struct BmpHeader
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
