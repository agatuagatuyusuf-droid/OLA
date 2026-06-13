using System.Text.Json;

namespace SkyAuto.Core.Ola;

/// <summary>
/// Verifies OLA call results with structured evidence.
/// Does NOT trust Success=true alone — validates actual side effects.
/// </summary>
public static class OlaCallVerifier
{
    /// <summary>
    /// Verify a CaptureScreen result.
    /// Must have: file exists, file size > 0, IsMock=false.
    /// </summary>
    public static OlaCallResult VerifyCaptureScreen(OlaCallResult result, string screenshotPath)
    {
        result.FunctionKey = "capture_screen";
        result.ScreenshotPath = screenshotPath;

        if (result.IsMock)
        {
            result.Verified = false;
            result.NotVerified = true;
            result.VerifyMessage = "Mock模式，非真实OLA截图";
            result.Evidence["reason"] = "mock_mode";
            return result;
        }

        if (!result.Success)
        {
            result.Verified = false;
            result.VerifyMessage = result.Message;
            return result;
        }

        var fileExists = File.Exists(screenshotPath);
        long fileSize = 0;
        if (fileExists)
        {
            var fi = new FileInfo(screenshotPath);
            fileSize = fi.Length;
        }

        result.Evidence["screenshotPath"] = screenshotPath;
        result.Evidence["fileExists"] = fileExists;
        result.Evidence["fileSize"] = fileSize;

        if (!fileExists)
        {
            result.Verified = false;
            result.VerifyMessage = "截图文件不存在";
            return result;
        }

        if (fileSize == 0)
        {
            result.Verified = false;
            result.VerifyMessage = "截图文件大小为0";
            return result;
        }

        result.Verified = true;
        result.VerifyMessage = $"截图验证通过: {screenshotPath} ({fileSize} bytes)";
        return result;
    }

    /// <summary>
    /// Verify a TestConnection result.
    /// Must have: Success=true, IsMock=false.
    /// </summary>
    public static OlaCallResult VerifyConnection(OlaCallResult result)
    {
        result.FunctionKey = "test_connection";

        if (result.IsMock)
        {
            result.Verified = false;
            result.NotVerified = true;
            result.VerifyMessage = "Mock模式，非真实OLA连接测试";
            result.Evidence["reason"] = "mock_mode";
            return result;
        }

        if (!result.Success)
        {
            result.Verified = false;
            result.VerifyMessage = result.Message;
            return result;
        }

        result.Verified = true;
        result.VerifyMessage = "OLA连接测试通过";
        return result;
    }

    /// <summary>
    /// Verify a GetMachineCode result (alias for VerifyGetMachineCode).
    /// </summary>
    public static OlaCallResult VerifyMachineCode(OlaCallResult result) => VerifyGetMachineCode(result);

    /// <summary>
    /// Verify a GetMachineCode result.
    /// Must have: non-empty machine code, IsMock=false.
    /// </summary>
    public static OlaCallResult VerifyGetMachineCode(OlaCallResult result)
    {
        result.FunctionKey = "get_machine_code";

        if (result.IsMock)
        {
            result.Verified = false;
            result.NotVerified = true;
            result.VerifyMessage = "Mock模式，非真实机器码";
            result.Evidence["reason"] = "mock_mode";
            return result;
        }

        if (!result.Success)
        {
            result.Verified = false;
            result.VerifyMessage = result.Message;
            return result;
        }

        var machineCode = result.Data?.ToString() ?? "";
        result.Evidence["machineCode"] = machineCode;

        if (string.IsNullOrWhiteSpace(machineCode))
        {
            result.Verified = false;
            result.VerifyMessage = "机器码为空";
            return result;
        }

        // Machine code must not be a simple integer return code
        if (int.TryParse(machineCode, out _))
        {
            result.Verified = false;
            result.NotVerified = true;
            result.VerifyMessage = "机器码为整数返回码，非真实机器码";
            result.Evidence["reason"] = "int_return_code_not_machine_code";
            return result;
        }

        result.Verified = true;
        result.VerifyMessage = $"机器码验证通过: {machineCode}";
        return result;
    }

    /// <summary>
    /// Verify a FindImage result.
    /// Must have: x/y coordinates, IsMock=false.
    /// </summary>
    public static OlaCallResult VerifyFindImage(OlaCallResult result, string imagePath, double similarity)
    {
        result.FunctionKey = "find_image";

        if (result.IsMock)
        {
            result.Verified = false;
            result.NotVerified = true;
            result.VerifyMessage = "Mock模式，非真实找图";
            result.Evidence["reason"] = "mock_mode";
            return result;
        }

        if (!result.Success)
        {
            result.Verified = false;
            result.VerifyMessage = result.Message;
            return result;
        }

        result.Evidence["imagePath"] = imagePath;
        result.Evidence["similarity"] = similarity;

        var data = result.Data;
        if (data == null)
        {
            result.Verified = false;
            result.VerifyMessage = "找图未返回坐标数据";
            return result;
        }

        // Try to extract x,y from data
        int x = -1, y = -1;
        if (data is JsonElement je)
        {
            if (je.TryGetProperty("x", out var xProp)) x = xProp.GetInt32();
            if (je.TryGetProperty("y", out var yProp)) y = yProp.GetInt32();
        }
        else if (data is Dictionary<string, object> dict)
        {
            if (dict.TryGetValue("x", out var xv)) x = Convert.ToInt32(xv);
            if (dict.TryGetValue("y", out var yv)) y = Convert.ToInt32(yv);
        }

        result.Evidence["x"] = x;
        result.Evidence["y"] = y;

        if (x < 0 || y < 0)
        {
            result.Verified = false;
            result.VerifyMessage = $"找图未返回有效坐标 (x={x}, y={y})";
            return result;
        }

        result.Verified = true;
        result.VerifyMessage = $"找图验证通过: ({x}, {y})";
        return result;
    }

    /// <summary>
    /// Verify an OCR result.
    /// Must have: non-empty text, IsMock=false.
    /// </summary>
    public static OlaCallResult VerifyOcrRegion(OlaCallResult result, int x, int y, int w, int h)
    {
        result.FunctionKey = "ocr_region";

        if (result.IsMock)
        {
            result.Verified = false;
            result.NotVerified = true;
            result.VerifyMessage = "Mock模式，非真实OCR";
            result.Evidence["reason"] = "mock_mode";
            return result;
        }

        if (!result.Success)
        {
            result.Verified = false;
            result.VerifyMessage = result.Message;
            return result;
        }

        result.Evidence["region"] = $"{x},{y},{w}x{h}";
        var text = result.Data?.ToString() ?? "";
        result.Evidence["text"] = text;

        if (string.IsNullOrWhiteSpace(text))
        {
            result.Verified = false;
            result.VerifyMessage = "OCR未返回识别文本";
            return result;
        }

        result.Verified = true;
        result.VerifyMessage = $"OCR验证通过: \"{text.Truncate(50)}\"";
        return result;
    }

    /// <summary>
    /// Verify a MoveMouse result.
    /// Cannot verify actual mouse position change without a real OLA response, so marks NotVerified.
    /// </summary>
    public static OlaCallResult VerifyMoveMouse(OlaCallResult result, int targetX, int targetY)
    {
        result.FunctionKey = "move_mouse";

        if (result.IsMock)
        {
            result.Verified = false;
            result.NotVerified = true;
            result.VerifyMessage = "Mock模式，非真实鼠标移动";
            result.Evidence["reason"] = "mock_mode";
            return result;
        }

        if (!result.Success)
        {
            result.Verified = false;
            result.VerifyMessage = result.Message;
            return result;
        }

        result.Evidence["targetX"] = targetX;
        result.Evidence["targetY"] = targetY;

        // Cannot verify actual cursor position without OLA's cursor position API
        result.Verified = false;
        result.NotVerified = true;
        result.VerifyMessage = $"鼠标移动已执行到 ({targetX}, {targetY})，但无法验证实际坐标变化";
        result.Evidence["reason"] = "cannot_verify_cursor_position_without_ola_api";
        return result;
    }

    /// <summary>
    /// Verify a Click result.
    /// Cannot verify actual click behavior without before/after screenshots, so marks NotVerified.
    /// </summary>
    public static OlaCallResult VerifyClick(OlaCallResult result, int x, int y, string? beforeScreenshot = null, string? afterScreenshot = null)
    {
        result.FunctionKey = "click";

        if (result.IsMock)
        {
            result.Verified = false;
            result.NotVerified = true;
            result.VerifyMessage = "Mock模式，非真实点击";
            result.Evidence["reason"] = "mock_mode";
            return result;
        }

        if (!result.Success)
        {
            result.Verified = false;
            result.VerifyMessage = result.Message;
            return result;
        }

        result.Evidence["x"] = x;
        result.Evidence["y"] = y;
        if (beforeScreenshot != null) result.Evidence["beforeScreenshot"] = beforeScreenshot;
        if (afterScreenshot != null) result.Evidence["afterScreenshot"] = afterScreenshot;

        // Cannot verify actual click effect without visual comparison
        result.Verified = false;
        result.NotVerified = true;
        result.VerifyMessage = $"点击已执行到 ({x}, {y})，但无法验证真实点击效果";
        result.Evidence["reason"] = "cannot_verify_click_effect_without_ola_api";
        return result;
    }
}

public static class StringExtensions
{
    public static string Truncate(this string value, int maxLength) =>
        string.IsNullOrEmpty(value) ? value : (value.Length <= maxLength ? value : value[..maxLength] + "...");
}
