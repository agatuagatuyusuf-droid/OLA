namespace SkyAuto.Core.Ola;

// Mock OLA client - used when no real OLA plugin is available
public class OlaMockClient : IOlaClient
{
    private readonly OlaRuntimeStatus _status = new();
    private readonly List<OlaCallResult> _callLog = new();

    public OlaConnectionMode Mode => OlaConnectionMode.Mock;
    public OlaRuntimeStatus Status => _status;

    public bool Initialize(string pluginPath)
    {
        _status.PluginPath = pluginPath;
        _status.Mode = OlaConnectionMode.Mock;
        _status.Initialized = true;
        _status.LastConnectedAt = DateTime.Now;
        return true;
    }

    public OlaCallResult TestConnection()
    {
        var result = new OlaCallResult
        {
            Success = true,
            FunctionKey = "test_connection",
            Message = "Mock连接成功（非真实OLA）",
            IsMock = true
        };
        _callLog.Add(result);
        return result;
    }

    public OlaCallResult Call(string functionKey, Dictionary<string, object> parameters)
    {
        var result = new OlaCallResult
        {
            Success = true,
            FunctionKey = functionKey,
            Message = $"Mock调用: {functionKey}（非真实OLA）",
            Data = parameters?.ToString() ?? "",
            IsMock = true
        };
        _callLog.Add(result);
        return result;
    }

    public OlaCallResult GetMachineCode()
    {
        var result = new OlaCallResult
        {
            Success = true,
            FunctionKey = "get_machine_code",
            Message = "Mock机器码（非真实OLA）",
            Data = "MOCK-MACHINE-CODE-0001",
            IsMock = true
        };
        _callLog.Add(result);
        return result;
    }

    public OlaCallResult CaptureScreen(string savePath)
    {
        var result = new OlaCallResult
        {
            Success = true,
            FunctionKey = "capture_screen",
            Message = $"Mock截图已保存到: {savePath}（非真实OLA）",
            IsMock = true
        };
        _callLog.Add(result);
        return result;
    }

    public OlaCallResult MoveMouse(int x, int y)
    {
        var result = new OlaCallResult
        {
            Success = true,
            FunctionKey = "move_mouse",
            Message = $"Mock移动鼠标到 ({x}, {y})（非真实OLA）",
            IsMock = true
        };
        _callLog.Add(result);
        return result;
    }

    public OlaCallResult Click(int x, int y)
    {
        var result = new OlaCallResult
        {
            Success = true,
            FunctionKey = "click",
            Message = $"Mock点击 ({x}, {y})（非真实OLA）",
            IsMock = true
        };
        _callLog.Add(result);
        return result;
    }

    public OlaCallResult DoubleClick(int x, int y)
    {
        var result = new OlaCallResult
        {
            Success = true,
            FunctionKey = "double_click",
            Message = $"Mock双击 ({x}, {y})（非真实OLA）",
            IsMock = true
        };
        _callLog.Add(result);
        return result;
    }

    public OlaCallResult RightClick(int x, int y)
    {
        var result = new OlaCallResult
        {
            Success = true,
            FunctionKey = "right_click",
            Message = $"Mock右键点击 ({x}, {y})（非真实OLA）",
            IsMock = true
        };
        _callLog.Add(result);
        return result;
    }

    public OlaCallResult ScrollMouse(int delta)
    {
        var result = new OlaCallResult
        {
            Success = true,
            FunctionKey = "scroll_mouse",
            Message = $"Mock滚轮滚动 {delta}（非真实OLA）",
            IsMock = true
        };
        _callLog.Add(result);
        return result;
    }

    public OlaCallResult TypeText(string text)
    {
        var result = new OlaCallResult
        {
            Success = true,
            FunctionKey = "type_text",
            Message = $"Mock输入文本: {text}（非真实OLA）",
            IsMock = true
        };
        _callLog.Add(result);
        return result;
    }

    public OlaCallResult PressKey(string key)
    {
        var result = new OlaCallResult
        {
            Success = true,
            FunctionKey = "press_key",
            Message = $"Mock按键: {key}（非真实OLA）",
            IsMock = true
        };
        _callLog.Add(result);
        return result;
    }

    public OlaCallResult Hotkey(string keys)
    {
        var result = new OlaCallResult
        {
            Success = true,
            FunctionKey = "hotkey",
            Message = $"Mock快捷键: {keys}（非真实OLA）",
            IsMock = true
        };
        _callLog.Add(result);
        return result;
    }

    public OlaCallResult FindImage(string imagePath, double similarity)
    {
        var result = new OlaCallResult
        {
            Success = true,
            FunctionKey = "find_image",
            Message = $"Mock找图: {imagePath} 相似度={similarity:F2}（非真实OLA）",
            Data = new { x = 100, y = 200, similarity = similarity },
            IsMock = true
        };
        _callLog.Add(result);
        return result;
    }

    public OlaCallResult WaitImage(string imagePath, int timeoutSeconds)
    {
        var result = new OlaCallResult
        {
            Success = true,
            FunctionKey = "wait_image",
            Message = $"Mock等待图片: {imagePath} 超时={timeoutSeconds}s（非真实OLA）",
            Data = new { x = 100, y = 200 },
            IsMock = true
        };
        _callLog.Add(result);
        return result;
    }

    public OlaCallResult ClickImage(string imagePath, double similarity)
    {
        var result = new OlaCallResult
        {
            Success = true,
            FunctionKey = "click_image",
            Message = $"Mock点击图片: {imagePath} 相似度={similarity:F2}（非真实OLA）",
            IsMock = true
        };
        _callLog.Add(result);
        return result;
    }

    public OlaCallResult ImageExists(string imagePath)
    {
        var result = new OlaCallResult
        {
            Success = true,
            FunctionKey = "image_exists",
            Message = $"Mock判断图片: {imagePath} 存在（非真实OLA）",
            Data = true,
            IsMock = true
        };
        _callLog.Add(result);
        return result;
    }

    public OlaCallResult OcrRegion(int x, int y, int width, int height)
    {
        var result = new OlaCallResult
        {
            Success = true,
            FunctionKey = "ocr_region",
            Message = $"Mock OCR区域: ({x},{y}) {width}x{height}（非真实OLA）",
            Data = "Mock识别结果文本",
            IsMock = true
        };
        _callLog.Add(result);
        return result;
    }

    public OlaCallResult OcrNumber(int x, int y, int width, int height)
    {
        var result = new OlaCallResult
        {
            Success = true,
            FunctionKey = "ocr_number",
            Message = $"Mock数字识别: ({x},{y}) {width}x{height}（非真实OLA）",
            Data = "123456",
            IsMock = true
        };
        _callLog.Add(result);
        return result;
    }

    public OlaCallResult FindText(string text)
    {
        var result = new OlaCallResult
        {
            Success = true,
            FunctionKey = "find_text",
            Message = $"Mock查找文字: {text}（非真实OLA）",
            Data = new { x = 150, y = 300 },
            IsMock = true
        };
        _callLog.Add(result);
        return result;
    }

    public OlaCallResult TextContains(string text)
    {
        var result = new OlaCallResult
        {
            Success = true,
            FunctionKey = "text_contains",
            Message = $"Mock判断文字包含: {text}（非真实OLA）",
            Data = true,
            IsMock = true
        };
        _callLog.Add(result);
        return result;
    }

    public string GetLastError() => "无错误";

    public void Dispose()
    {
        _status.Initialized = false;
        _status.LastDisconnecedAt = DateTime.Now;
    }
}
