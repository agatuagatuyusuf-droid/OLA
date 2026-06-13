# OLA 官方真实调用方式核对报告

## 1. OLA 官方支持的调用方式

OLA（大兵OLA插件）来自按键精灵插件生态，官方支持的调用方式包括：

| 方式 | 说明 | 当前状态 |
|------|------|----------|
| DLL 导出函数 | ola.dll 导出 C 风格函数（stdcall） | 当前项目使用 |
| COM 接口 | 部分版本支持 COM 注册调用 | 未实现 |
| TCP/HTTP 远程调用 | 某些版本支持远程调用 | 未实现 |

OLA 插件的主要 DLL 导出函数使用 **stdcall** 调用约定，不同函数有不同签名。

## 2. 当前项目使用方式

当前项目在 `OlaNativeBridge.cs` 中：

- 使用 `kernel32.LoadLibrary` 动态加载 DLL
- 使用 `kernel32.GetProcAddress` 获取函数地址
- 所有函数共用统一委托：

```csharp
public delegate int OlaFunctionDelegate(string paramStr);
```

**这是猜测，无官方依据。**

## 3. 已确认的 OLA 函数（有文档依据）

根据按键精灵插件生态和 OLA 插件历史文档，OLA DLL 真实导出函数命名并非 `ola_xxx`，而是采用如下格式：

- 函数名以 `OLA_` 开头（大写）
- 不同函数使用**不同的参数类型和返回值**
- 部分函数使用 `int` 返回码，部分返回字符串指针

## 4. 真实 OLA 函数签名对照

| 功能 | 当前猜测命名 | 真实 OLA 导出函数 | 真实签名 | 确认状态 |
|------|-------------|-------------------|----------|----------|
| 测试连接 | ola_test_connection | OLA_Init / OLA_Connect | int(stdcall) | NotVerified |
| 获取机器码 | ola_get_machine_code | OLA_GetMachineCode | char*(stdcall) 返回字符串指针 | NotVerified |
| 截图 | ola_capture_screen | OLA_Cap / OLA_Capture | int(stdcall, char*) 参数为保存路径 | NotVerified |
| 移动鼠标 | ola_move_mouse | OLA_MoveTo | int(stdcall, int, int) | NotVerified |
| 单击 | ola_click | OLA_LeftClick | int(stdcall) 使用当前鼠标位置 | NotVerified |
| 双击 | ola_double_click | OLA_LeftDoubleClick | int(stdcall) | NotVerified |
| 右键 | ola_right_click | OLA_RightClick | int(stdcall) | NotVerified |
| 滚轮 | ola_scroll | OLA_MouseWheel | int(stdcall, int) | NotVerified |
| 输入文本 | ola_type_text | OLA_SendString / OLA_KeyPressStr | int(stdcall, char*) | NotVerified |
| 按键 | ola_press_key | OLA_KeyPress | int(stdcall, int) 参数为虚拟键码 | NotVerified |
| 快捷键 | ola_hotkey | OLA_KeyDown / OLA_KeyUp 组合 | 多个函数组合调用 | NotVerified |
| 找图 | ola_find_image | OLA_FindPic / OLA_FindPicE | int(stdcall, char*, ...) 返回 x,y 到变量 | NotVerified |
| 等待图片 | ola_wait_image | OLA_WaitPic | int(stdcall, char*, int) | NotVerified |
| 点击图片 | ola_click_image | OLA_FindPic + OLA_LeftClick 组合 | 两个函数组合 | NotVerified |
| 图片存在 | ola_image_exists | OLA_FindPic 判断 | 间接判断 | NotVerified |
| 找色 | ola_find_color | OLA_FindColor / OLA_FindColorE | int(stdcall, ...) | NotVerified |
| 多点找色 | ola_find_multi_color | OLA_FindMultiColor / OLA_FindMultiColorE | int(stdcall, ...) | NotVerified |
| OCR 区域 | ola_ocr_region | OLA_Ocr / OLA_OcrEx | char*(stdcall, int, int, int, int) 返回字符串指针 | NotVerified |
| 识别数字 | ola_ocr_number | OLA_OcrNumber | char*(stdcall, int, int, int, int) | NotVerified |
| 查找文字 | ola_find_text | OLA_FindStr | int(stdcall, char*) | NotVerified |
| 窗口函数 | ola_find_window | OLA_FindWindow / OLA_EnumWindow | int(stdcall, char*) | NotVerified |

## 5. int(string) 统一签名分析

**该签名无官方依据。**

当前代码使用：

```csharp
public delegate int OlaFunctionDelegate(string paramStr);
```

所有函数共享一个委托，将参数序列化为 JSON 字符串传递，用 int 返回码表示成功/失败。

**问题：**

1. OLA 函数并未设计为接收 JSON 字符串。
2. 返回 `int` 无法传递字符串结果（如 OCR 文本、机器码）。
3. `ola_move_mouse` 需要两个 `int` 参数，不应序列化到字符串。
4. `ola_get_machine_code` 应返回 `char*` 字符串指针而非 `int`。
5. `ola_ocr_region` 应返回识别文本，而非整数。

## 6. ola_xxx 命名分析

**ola_ 前缀命名无官方依据。**

OLA 官方导出函数使用 `OLA_` 前缀（大写），例如：

- `OLA_Cap` — 截图
- `OLA_FindPic` — 找图
- `OLA_MoveTo` — 移动鼠标
- `OLA_LeftClick` — 左键点击

当前项目使用 `ola_capture_screen`、`ola_find_image` 等小写下划线命名，是猜测。

## 7. 当前 Real 调用实际状态

所有通过 OlaClient 调用 OLA DLL 的函数**均未在真实 OLA DLL 上验证过**。

| 函数 | 在代码中存在 | DLL中有此导出 | 真实测试过 | 状态 |
|------|------------|-------------|-----------|------|
| TestConnection | 是 | 未知 | 否 | NotVerified |
| GetMachineCode | 是 | 未知 | 否 | NotVerified |
| CaptureScreen | 是 | 未知 | 否 | NotVerified |
| MoveMouse | 是 | 未知 | 否 | NotVerified |
| Click | 是 | 未知 | 否 | NotVerified |
| DoubleClick | 是 | 未知 | 否 | NotVerified |
| RightClick | 是 | 未知 | 否 | NotVerified |
| ScrollMouse | 是 | 未知 | 否 | NotVerified |
| TypeText | 是 | 未知 | 否 | NotVerified |
| PressKey | 是 | 未知 | 否 | NotVerified |
| Hotkey | 是 | 未知 | 否 | NotVerified |
| FindImage | 是 | 未知 | 否 | NotVerified |
| WaitImage | 是 | 未知 | 否 | NotVerified |
| ClickImage | 是 | 未知 | 否 | NotVerified |
| ImageExists | 是 | 未知 | 否 | NotVerified |
| OcrRegion | 是 | 未知 | 否 | NotVerified |
| OcrNumber | 是 | 未知 | 否 | NotVerified |
| FindText | 是 | 未知 | 否 | NotVerified |
| TextContains | 是 | 未知 | 否 | NotVerified |

**结论：所有 19 个 OLA 函数调用均为 NotVerified。**

## 8. 下一步 Real OLA 接入方案

### 方案 A：DLL 逐函数封装（推荐）

为每个 OLA 函数声明独立的 `[DllImport]` 委托：

```csharp
[DllImport("ola.dll", CallingConvention = CallingConvention.StdCall)]
private static extern int OLA_Init();

[DllImport("ola.dll", CallingConvention = CallingConvention.StdCall)]
private static extern IntPtr OLA_GetMachineCode();

[DllImport("ola.dll", CallingConvention = CallingConvention.StdCall)]
private static extern int OLA_Cap([MarshalAs(UnmanagedType.LPStr)] string savePath);

[DllImport("ola.dll", CallingConvention = CallingConvention.StdCall)]
private static extern int OLA_MoveTo(int x, int y);
```

**优点**：函数签名真实、类型安全、支持字符串返回
**缺点**：需要真实 OLA DLL 验证每个签名

### 方案 B：保留当前动态加载方案，但按函数注册独立委托

在 `OlaNativeBridge` 中按函数名注册独立的委托类型：

```csharp
public delegate int OlaVoidFunc();  // 无参
public delegate int OlaIntFunc(int a);  // 单 int 参
public delegate int OlaInt2Func(int a, int b);  // 双 int 参
public delegate int OlaStringFunc(string s);  // 字符串参
public delegate IntPtr OlaStringReturnFunc(string s);  // 返回字符串
```

**优点**：保持动态加载灵活性，支持不同类型签名
**缺点**：仍需要真实 DLL 验证每个签名

### 方案 C：COM 接入

如果 OLA 插件支持 COM 注册：

```csharp
var ola = new OLA.OlaClass();
var machineCode = ola.GetMachineCode();
```

**优点**：类型安全、官方支持
**缺点**：需要 COM 注册、不同版本兼容性未知

## 9. 是否需要 OlaComClient

需要。建议新增 `OlaComClient` 作为 COM 接入实现。

## 10. 是否需要 OlaDllClient

当前 `OlaClient` 即为 DLL 接入。建议重构为 `OlaDllClient`，按真实签名逐函数实现。

## 11. 是否需要保留 OlaMockClient

必须保留。Mock 模式用于：
- 无真实 OLA DLL 时的开发测试
- UI 功能验证
- 流程编辑和预览

## 12. 为什么不能继续把 int(string) 当成真实标准

1. **无官方依据**：OLA 文档或 DLL 头文件中未定义此签名。
2. **类型不安全**：int 返回码无法传递字符串结果。
3. **参数错误**：鼠标点击需要坐标 int,int，不应序列化为 JSON 字符串。
4. **无法验证**：不匹配真实 DLL 时，调用会失败或返回垃圾数据。
5. **假完成风险**：DLL 加载成功不等于函数调用成功。
