using System.Runtime.InteropServices;

namespace SkyAuto.Core.Ola;

// Real OLA client - attempts to call actual OLA plugin DLL
public class OlaClient : IOlaClient
{
    private readonly OlaRuntimeStatus _status = new();
    private string? _lastError;

    public OlaConnectionMode Mode => _status.Mode;
    public OlaRuntimeStatus Status => _status;

    /// <summary>
    /// Initialize by loading the OLA plugin DLL from the given path.
    /// Returns true if DLL loaded successfully and connection test passed.
    /// </summary>
    public bool Initialize(string pluginPath)
    {
        try
        {
            _status.PluginPath = pluginPath;

            // Attempt to load the DLL
            var dllLoaded = OlaNativeBridge.LoadLibrary(pluginPath);
            if (!dllLoaded)
            {
                _lastError = $"无法加载 OLA 插件: {pluginPath}";
                _status.InitError = _lastError;
                _status.Mode = OlaConnectionMode.NotConfigured;
                return false;
            }

            // Test connection by calling a known function if available
            var connResult = TestConnection();
            if (!connResult.Success)
            {
                _lastError = $"OLA 连接测试失败: {connResult.Message}";
                _status.InitError = _lastError;
                _status.Mode = OlaConnectionMode.NotConfigured;
                OlaNativeBridge.FreeLoadedLibrary();
                return false;
            }

            _status.Mode = OlaConnectionMode.Real;
            _status.Initialized = true;
            _status.LastConnectedAt = DateTime.Now;
            _lastError = null;
            return true;
        }
        catch (Exception ex)
        {
            _lastError = $"OLA 初始化异常: {ex.Message}";
            _status.InitError = _lastError;
            _status.Mode = OlaConnectionMode.NotConfigured;
            OlaNativeBridge.FreeLoadedLibrary();
            return false;
        }
    }

    public OlaCallResult TestConnection()
    {
        if (!OlaNativeBridge.IsLoaded)
            return FailResult("test_connection", "OLA DLL未加载");

        // Try calling ola_test_connection or ola_init
        var fn = OlaNativeBridge.GetOlaFunction("ola_test_connection")
                ?? OlaNativeBridge.GetOlaFunction("ola_init");

        if (fn == null)
            return FailResult("test_connection", "OLA DLL中未找到测试连接函数");

        try
        {
            var retCode = fn("");
            if (retCode != 0 && retCode != 1)
                return FailResult("test_connection", $"OLA返回错误码: {retCode}");

            return new OlaCallResult
            {
                Success = true,
                FunctionKey = "test_connection",
                Message = "OLA连接成功",
                IsMock = false
            };
        }
        catch (Exception ex)
        {
            return FailResult("test_connection", $"调用异常: {ex.Message}");
        }
    }

    public OlaCallResult Call(string functionKey, Dictionary<string, object> parameters)
    {
        if (!OlaNativeBridge.IsLoaded)
            return FailResult(functionKey, "OLA DLL未加载");

        // Map the function key to OLA's exported function name (e.g., ola_xxx)
        var functionName = $"ola_{functionKey.Replace("_", "_")}";

        var fn = OlaNativeBridge.GetOlaFunction(functionName);
        if (fn == null)
            return FailResult(functionKey, $"OLA DLL中未找到函数: {functionName}");

        // Serialize parameters to a string the OLA function expects
        var paramStr = System.Text.Json.JsonSerializer.Serialize(parameters ?? new());

        try
        {
            var retCode = fn(paramStr);
            if (retCode < 0)
                return FailResult(functionKey, $"OLA返回错误码: {retCode}");

            // The OLA function might set a global result buffer; simplified for now
            return new OlaCallResult
            {
                Success = true,
                FunctionKey = functionKey,
                Message = "执行成功",
                RawResult = retCode.ToString(),
                IsMock = false
            };
        }
        catch (Exception ex)
        {
            return FailResult(functionKey, $"调用异常: {ex.Message}");
        }
    }

    public OlaCallResult GetMachineCode()
    {
        if (!OlaNativeBridge.IsLoaded)
            return FailResult("get_machine_code", "OLA DLL未加载");

        var fn = OlaNativeBridge.GetOlaFunction("ola_get_machine_code");
        if (fn == null)
            return FailResult("get_machine_code", "OLA DLL中未找到 get_machine_code 函数");

        try
        {
            var retCode = fn("");
            _status.MachineCode = retCode.ToString();
            return new OlaCallResult
            {
                Success = true,
                FunctionKey = "get_machine_code",
                Message = $"机器码: {_status.MachineCode}",
                Data = retCode,
                IsMock = false
            };
        }
        catch (Exception ex)
        {
            return FailResult("get_machine_code", $"调用异常: {ex.Message}");
        }
    }

    public OlaCallResult CaptureScreen(string savePath)
    {
        if (!OlaNativeBridge.IsLoaded)
            return FailResult("capture_screen", "OLA DLL未加载");

        var fn = OlaNativeBridge.GetOlaFunction("ola_capture_screen");
        if (fn == null)
            return FailResult("capture_screen", "OLA DLL中未找到 capture_screen 函数");

        try
        {
            var retCode = fn(savePath);
            if (retCode < 0)
                return FailResult("capture_screen", $"OLA返回错误码: {retCode}");

            return new OlaCallResult
            {
                Success = true,
                FunctionKey = "capture_screen",
                Message = $"截图已保存到: {savePath}",
                IsMock = false
            };
        }
        catch (Exception ex)
        {
            return FailResult("capture_screen", $"调用异常: {ex.Message}");
        }
    }

    public OlaCallResult MoveMouse(int x, int y) =>
        CallOlaFunction("ola_move_mouse", "move_mouse", $"{x},{y}");

    public OlaCallResult Click(int x, int y) =>
        CallOlaFunction("ola_click", "click", $"{x},{y}");

    public OlaCallResult DoubleClick(int x, int y) =>
        CallOlaFunction("ola_double_click", "double_click", $"{x},{y}");

    public OlaCallResult RightClick(int x, int y) =>
        CallOlaFunction("ola_right_click", "right_click", $"{x},{y}");

    public OlaCallResult ScrollMouse(int delta) =>
        CallOlaFunction("ola_scroll", "scroll_mouse", delta.ToString());

    public OlaCallResult TypeText(string text) =>
        CallOlaFunction("ola_type_text", "type_text", text);

    public OlaCallResult PressKey(string key) =>
        CallOlaFunction("ola_press_key", "press_key", key);

    public OlaCallResult Hotkey(string keys) =>
        CallOlaFunction("ola_hotkey", "hotkey", keys);

    public OlaCallResult FindImage(string imagePath, double similarity)
    {
        if (!OlaNativeBridge.IsLoaded)
            return FailResult("find_image", "OLA DLL未加载");

        var fn = OlaNativeBridge.GetOlaFunction("ola_find_image");
        if (fn == null)
            return FailResult("find_image", "OLA DLL中未找到 find_image 函数");

        try
        {
            var retCode = fn($"{imagePath},{similarity:F2}");
            if (retCode < 0)
                return FailResult("find_image", $"OLA返回错误码: {retCode}");

            return new OlaCallResult
            {
                Success = true,
                FunctionKey = "find_image",
                Message = $"找到图片: {imagePath}",
                RawResult = retCode.ToString(),
                IsMock = false
            };
        }
        catch (Exception ex)
        {
            return FailResult("find_image", $"调用异常: {ex.Message}");
        }
    }

    public OlaCallResult WaitImage(string imagePath, int timeoutSeconds)
    {
        if (!OlaNativeBridge.IsLoaded)
            return FailResult("wait_image", "OLA DLL未加载");

        var fn = OlaNativeBridge.GetOlaFunction("ola_wait_image");
        if (fn == null)
            return FailResult("wait_image", "OLA DLL中未找到 wait_image 函数");

        try
        {
            var retCode = fn($"{imagePath},{timeoutSeconds}");
            if (retCode < 0)
                return FailResult("wait_image", $"OLA返回错误码: {retCode}");

            return new OlaCallResult
            {
                Success = true,
                FunctionKey = "wait_image",
                Message = $"图片已出现: {imagePath}",
                IsMock = false
            };
        }
        catch (Exception ex)
        {
            return FailResult("wait_image", $"调用异常: {ex.Message}");
        }
    }

    public OlaCallResult ClickImage(string imagePath, double similarity) =>
        CallOlaFunction($"ola_click_image", "click_image", $"{imagePath},{similarity:F2}");

    public OlaCallResult ImageExists(string imagePath)
    {
        if (!OlaNativeBridge.IsLoaded)
            return FailResult("image_exists", "OLA DLL未加载");

        var fn = OlaNativeBridge.GetOlaFunction("ola_image_exists");
        if (fn == null)
            return FailResult("image_exists", "OLA DLL中未找到 image_exists 函数");

        try
        {
            var retCode = fn(imagePath);
            bool exists = retCode != 0;
            return new OlaCallResult
            {
                Success = true,
                FunctionKey = "image_exists",
                Message = $"图片{imagePath}: {(exists ? "存在" : "不存在")}",
                Data = exists,
                IsMock = false
            };
        }
        catch (Exception ex)
        {
            return FailResult("image_exists", $"调用异常: {ex.Message}");
        }
    }

    public OlaCallResult OcrRegion(int x, int y, int width, int height) =>
        CallOlaFunction($"ola_ocr_region", "ocr_region", $"{x},{y},{width},{height}");

    public OlaCallResult OcrNumber(int x, int y, int width, int height) =>
        CallOlaFunction($"ola_ocr_number", "ocr_number", $"{x},{y},{width},{height}");

    public OlaCallResult FindText(string text) =>
        CallOlaFunction("ola_find_text", "find_text", text);

    public OlaCallResult TextContains(string text)
    {
        if (!OlaNativeBridge.IsLoaded)
            return FailResult("text_contains", "OLA DLL未加载");

        var fn = OlaNativeBridge.GetOlaFunction("ola_text_contains");
        if (fn == null)
            return FailResult("text_contains", "OLA DLL中未找到 text_contains 函数");

        try
        {
            var retCode = fn(text);
            bool contains = retCode != 0;
            return new OlaCallResult
            {
                Success = true,
                FunctionKey = "text_contains",
                Message = $"文字{text}: {(contains ? "存在" : "不存在")}",
                Data = contains,
                IsMock = false
            };
        }
        catch (Exception ex)
        {
            return FailResult("text_contains", $"调用异常: {ex.Message}");
        }
    }

    public string GetLastError() => _lastError ?? "无错误";

    private OlaCallResult CallOlaFunction(string functionName, string functionKey, string paramStr)
    {
        if (!OlaNativeBridge.IsLoaded)
            return FailResult(functionKey, "OLA DLL未加载");

        var fn = OlaNativeBridge.GetOlaFunction(functionName);
        if (fn == null)
            return FailResult(functionKey, $"OLA DLL中未找到函数: {functionName}");

        try
        {
            var retCode = fn(paramStr);
            if (retCode < 0)
                return FailResult(functionKey, $"OLA返回错误码: {retCode}");

            return new OlaCallResult
            {
                Success = true,
                FunctionKey = functionKey,
                Message = "执行成功",
                RawResult = retCode.ToString(),
                IsMock = false
            };
        }
        catch (Exception ex)
        {
            return FailResult(functionKey, $"调用异常: {ex.Message}");
        }
    }

    private OlaCallResult FailResult(string functionKey, string message)
    {
        _lastError = message;
        return new OlaCallResult
        {
            Success = false,
            FunctionKey = functionKey,
            Message = message,
            ErrorCode = "OLA_ERROR",
            IsMock = false
        };
    }

    public void Dispose()
    {
        if (_status.Initialized)
        {
            OlaNativeBridge.FreeLoadedLibrary();
            _status.Initialized = false;
            _status.LastDisconnecedAt = DateTime.Now;
        }
    }
}
