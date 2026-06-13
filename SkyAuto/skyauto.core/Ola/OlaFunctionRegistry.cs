namespace SkyAuto.Core.Ola;

public class OlaFunctionRegistry
{
    private readonly Dictionary<string, OlaFunctionInfo> _functions = new();

    public IReadOnlyDictionary<string, OlaFunctionInfo> Functions => _functions;

    public void Register(OlaFunctionInfo info)
    {
        _functions[info.Type] = info;
    }

    public bool Contains(string type) => _functions.ContainsKey(type);

    public OlaFunctionInfo? Get(string type) => _functions.GetValueOrDefault(type);

    public List<OlaFunctionInfo> GetAll() => _functions.Values.ToList();

    public List<OlaFunctionInfo> GetByCategory(string category)
        => _functions.Values.Where(f => f.Category == category).ToList();

    public List<string> GetCategories()
        => _functions.Values.Select(f => f.Category).Distinct().OrderBy(x => x).ToList();

    public static OlaFunctionRegistry CreateDefault()
    {
        var registry = new OlaFunctionRegistry();

        // === 系统类 ===
        registry.Register(new OlaFunctionInfo
        { Type = "open_program", Category = "系统", Name = "打开程序", Description = "启动一个可执行程序或文件",
          Parameters = [new OlaParamInfo { Key = "path", Label = "路径", Type = "file" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "open_url", Category = "系统", Name = "打开网址", Description = "在默认浏览器中打开一个URL",
          Parameters = [new OlaParamInfo { Key = "url", Label = "网址", Type = "string" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "run_cmd", Category = "系统", Name = "执行CMD", Description = "运行命令行指令",
          Parameters = [new OlaParamInfo { Key = "command", Label = "命令", Type = "string" },
                        new OlaParamInfo { Key = "workingDir", Label = "工作目录", Type = "string" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "sleep", Category = "系统", Name = "等待", Description = "暂停执行指定秒数",
          Parameters = [new OlaParamInfo { Key = "seconds", Label = "秒数", Type = "int", DefaultValue = "3" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "kill_process", Category = "系统", Name = "关闭进程", Description = "按名称关闭一个进程",
          Parameters = [new OlaParamInfo { Key = "processName", Label = "进程名", Type = "string" }] });

        // === 窗口类 ===
        registry.Register(new OlaFunctionInfo
        { Type = "find_window", Category = "窗口", Name = "查找窗口", Description = "按标题或类名查找窗口",
          Parameters = [new OlaParamInfo { Key = "title", Label = "窗口标题", Type = "string" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "activate_window", Category = "窗口", Name = "激活窗口", Description = "将指定窗口置于前台",
          Parameters = [new OlaParamInfo { Key = "title", Label = "窗口标题", Type = "string" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "move_window", Category = "窗口", Name = "移动窗口", Description = "将窗口移动到指定位置",
          Parameters = [new OlaParamInfo { Key = "title", Label = "窗口标题", Type = "string" },
                        new OlaParamInfo { Key = "x", Label = "X坐标", Type = "int" },
                        new OlaParamInfo { Key = "y", Label = "Y坐标", Type = "int" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "screenshot_window", Category = "窗口", Name = "窗口截图", Description = "截取指定窗口的屏幕图像",
          Parameters = [new OlaParamInfo { Key = "title", Label = "窗口标题", Type = "string" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "bind_window", Category = "窗口", Name = "绑定窗口", Description = "绑定指定窗口以便后续操作",
          Parameters = [new OlaParamInfo { Key = "title", Label = "窗口标题", Type = "string" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "unbind_window", Category = "窗口", Name = "解绑窗口", Description = "解除之前绑定的窗口关联",
          Parameters = [] });

        // === 鼠标键盘类 ===
        registry.Register(new OlaFunctionInfo
        { Type = "mouse_move", Category = "鼠标键盘", Name = "移动鼠标", Description = "将鼠标移动到指定坐标",
          Parameters = [new OlaParamInfo { Key = "x", Label = "X坐标", Type = "int" },
                        new OlaParamInfo { Key = "y", Label = "Y坐标", Type = "int" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "mouse_click", Category = "鼠标键盘", Name = "单击", Description = "在指定位置点击左键",
          Parameters = [new OlaParamInfo { Key = "x", Label = "X坐标", Type = "int" },
                        new OlaParamInfo { Key = "y", Label = "Y坐标", Type = "int" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "mouse_double_click", Category = "鼠标键盘", Name = "双击", Description = "在指定位置双击左键",
          Parameters = [new OlaParamInfo { Key = "x", Label = "X坐标", Type = "int" },
                        new OlaParamInfo { Key = "y", Label = "Y坐标", Type = "int" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "mouse_right_click", Category = "鼠标键盘", Name = "右键单击", Description = "在指定位置点击右键",
          Parameters = [new OlaParamInfo { Key = "x", Label = "X坐标", Type = "int" },
                        new OlaParamInfo { Key = "y", Label = "Y坐标", Type = "int" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "mouse_scroll", Category = "鼠标键盘", Name = "滚轮滚动", Description = "在当前位置滚动鼠标滚轮",
          Parameters = [new OlaParamInfo { Key = "delta", Label = "滚动量(正向上)", Type = "int" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "keyboard_type", Category = "鼠标键盘", Name = "输入文字", Description = "模拟键盘输入文本内容",
          Parameters = [new OlaParamInfo { Key = "text", Label = "文本", Type = "string" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "keyboard_press", Category = "鼠标键盘", Name = "按键", Description = "按下指定键(如 Enter, Tab)",
          Parameters = [new OlaParamInfo { Key = "key", Label = "键名", Type = "string" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "keyboard_hotkey", Category = "鼠标键盘", Name = "快捷键", Description = "按下组合键(如 Ctrl+C)",
          Parameters = [new OlaParamInfo { Key = "keys", Label = "按键组合(Ctrl+Alt+A)", Type = "string" }] });

        // === 图像类 ===
        registry.Register(new OlaFunctionInfo
        { Type = "screenshot", Category = "图像", Name = "全屏截图", Description = "截取整个屏幕并保存",
          Parameters = [new OlaParamInfo { Key = "savePath", Label = "保存路径", Type = "string" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "find_image", Category = "图像", Name = "找图", Description = "在屏幕上查找指定图片",
          Parameters = [new OlaParamInfo { Key = "image_asset_id", Label = "图片素材ID", Type = "image_asset" },
                        new OlaParamInfo { Key = "similarity", Label = "相似度(0-1)", Type = "string", DefaultValue = "0.85" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "wait_image", Category = "图像", Name = "等待图片出现", Description = "在超时前等待指定图片出现在屏幕上",
          Parameters = [new OlaParamInfo { Key = "image_asset_id", Label = "图片素材ID", Type = "image_asset" },
                        new OlaParamInfo { Key = "timeout", Label = "超时秒数", Type = "int", DefaultValue = "15" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "click_image", Category = "图像", Name = "点击图片", Description = "找到图片后点击其中心位置",
          Parameters = [new OlaParamInfo { Key = "image_asset_id", Label = "图片素材ID", Type = "image_asset" },
                        new OlaParamInfo { Key = "similarity", Label = "相似度(0-1)", Type = "string", DefaultValue = "0.85" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "judge_image", Category = "图像", Name = "判断图片是否存在", Description = "检查指定图片是否出现在屏幕上",
          Parameters = [new OlaParamInfo { Key = "image_asset_id", Label = "图片素材ID", Type = "image_asset" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "find_color", Category = "图像", Name = "找色", Description = "在屏幕上查找指定颜色点",
          Parameters = [new OlaParamInfo { Key = "color", Label = "RGB颜色(#RRGGBB)", Type = "string" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "find_multi_color", Category = "图像", Name = "多点找色", Description = "按相对坐标查找多个颜色点组合",
          Parameters = [new OlaParamInfo { Key = "baseColor", Label = "基准色(#RRGGBB)", Type = "string" },
                        new OlaParamInfo { Key = "offsetColors", Label = "偏移颜色(x,y,#RRGGBB;...)", Type = "string" }] });

        // === OCR类 ===
        registry.Register(new OlaFunctionInfo
        { Type = "ocr_region", Category = "OCR", Name = "识别区域文字", Description = "对屏幕指定区域进行OCR识别",
          Parameters = [new OlaParamInfo { Key = "x", Label = "左上X", Type = "int" },
                        new OlaParamInfo { Key = "y", Label = "左上Y", Type = "int" },
                        new OlaParamInfo { Key = "w", Label = "宽度", Type = "int" },
                        new OlaParamInfo { Key = "h", Label = "高度", Type = "int" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "ocr_image", Category = "OCR", Name = "识别图片文字", Description = "对指定图片素材进行OCR识别",
          Parameters = [new OlaParamInfo { Key = "image_path", Label = "图片路径", Type = "string" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "recognize_number", Category = "OCR", Name = "识别数字", Description = "从屏幕指定区域识别纯数字内容",
          Parameters = [new OlaParamInfo { Key = "x", Label = "左上X", Type = "int" },
                        new OlaParamInfo { Key = "y", Label = "左上Y", Type = "int" },
                        new OlaParamInfo { Key = "w", Label = "宽度", Type = "int" },
                        new OlaParamInfo { Key = "h", Label = "高度", Type = "int" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "find_text", Category = "OCR", Name = "查找文字", Description = "在屏幕上查找指定文字",
          Parameters = [new OlaParamInfo { Key = "text", Label = "目标文字", Type = "string" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "judge_text", Category = "OCR", Name = "判断文字是否存在", Description = "检查屏幕上是否有指定文字",
          Parameters = [new OlaParamInfo { Key = "text", Label = "目标文字", Type = "string" }] });

        // === 文件类 ===
        registry.Register(new OlaFunctionInfo
        { Type = "read_file", Category = "文件", Name = "读取文件", Description = "读取文本文件内容",
          Parameters = [new OlaParamInfo { Key = "path", Label = "文件路径", Type = "string" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "write_file", Category = "文件", Name = "写入文件", Description = "将文本内容写入文件",
          Parameters = [new OlaParamInfo { Key = "path", Label = "文件路径", Type = "string" },
                        new OlaParamInfo { Key = "content", Label = "内容", Type = "string" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "copy_file", Category = "文件", Name = "复制文件", Description = "将文件复制到目标路径",
          Parameters = [new OlaParamInfo { Key = "sourcePath", Label = "源路径", Type = "string" },
                        new OlaParamInfo { Key = "destPath", Label = "目标路径", Type = "string" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "move_file", Category = "文件", Name = "移动文件", Description = "将文件移动到目标路径",
          Parameters = [new OlaParamInfo { Key = "sourcePath", Label = "源路径", Type = "string" },
                        new OlaParamInfo { Key = "destPath", Label = "目标路径", Type = "string" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "delete_file", Category = "文件", Name = "删除文件", Description = "删除指定文件",
          Parameters = [new OlaParamInfo { Key = "path", Label = "文件路径", Type = "string" }] });

        // === 网络类 ===
        registry.Register(new OlaFunctionInfo
        { Type = "http_get", Category = "网络", Name = "HTTP GET请求", Description = "发送GET请求获取数据",
          Parameters = [new OlaParamInfo { Key = "url", Label = "URL地址", Type = "string" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "http_post", Category = "网络", Name = "HTTP POST请求", Description = "发送POST请求提交数据",
          Parameters = [new OlaParamInfo { Key = "url", Label = "URL地址", Type = "string" },
                        new OlaParamInfo { Key = "body", Label = "请求体(JSON)", Type = "string" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "download_file", Category = "网络", Name = "下载文件", Description = "从URL下载文件到本地",
          Parameters = [new OlaParamInfo { Key = "url", Label = "下载地址", Type = "string" },
                        new OlaParamInfo { Key = "savePath", Label = "保存路径", Type = "file" }] });

        // === 日志类 ===
        registry.Register(new OlaFunctionInfo
        { Type = "write_log", Category = "日志", Name = "写入日志", Description = "向日志文件写入一条消息",
          Parameters = [new OlaParamInfo { Key = "message", Label = "日志内容", Type = "string" }] });

        registry.Register(new OlaFunctionInfo
        { Type = "save_screenshot", Category = "日志", Name = "保存截图", Description = "截取当前屏幕并保存到指定位置",
          Parameters = [new OlaParamInfo { Key = "path", Label = "保存路径", Type = "string" }] });

        return registry;
    }
}
