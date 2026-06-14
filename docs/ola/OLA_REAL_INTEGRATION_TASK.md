# A 线任务：OLA Real 最小真实闭环接入

> 目标：基于 OLA beta68 接口资料，让 SkyAuto 从 Mock / NotVerified 壳子进入真实 OLA 调用阶段。

## 任务优先级

本轮不要继续做 UI 大扩展，也不要包装全部 914 个函数。先完成最小真实闭环。

优先顺序：

1. 能加载 OLA DLL。
2. 能创建/销毁 OLA 对象。
3. 能读取版本号 `Ver`。
4. 能读取插件信息 `GetPlugInfo`。
5. 能检查管理员权限 `IsElevated`。
6. 能调用至少一个真实截图函数。
7. 能调用至少一个真实鼠标/点击函数。
8. 能调用至少一个真实 OCR 或找图函数。
9. SelfCheck 能显示哪些 Real 已验证，哪些仍 NotVerified。

## 输入资料

查看：

- `docs/ola/OLA_ASSETS_MANIFEST.md`

开发机需要把原始二进制文件放到：

```text
third_party/ola/local/OLAPlug_x86_1.0.0-beta.68_31.dll
third_party/ola/local/OLAPlug_x86_1.0.0-beta.68_30.dll
third_party/ola/local/欧拉开发者工具_beta68_20.exe
third_party/ola/local/欧拉插件文档_v1.0.0-beta.68_29.chm
```

优先使用：

```text
OLAPlug_x86_1.0.0-beta.68_31.dll
```

## 关键风险

### 1. x86 DLL

当前 DLL 文件名是 `OLAPlug_x86`，所以必须确认调用进程架构。

要求：

1. 新增 x86 运行说明。
2. 如果当前 WPF 项目默认 x64/AnyCPU 无法加载 x86 DLL，必须新增 x86 publish 配置或单独 x86 bridge。
3. 不能忽略 BadImageFormatException。

### 2. DLL 返回字符串指针

接口导出说明里，`Ver`、`GetPlugInfo`、`GetPath`、`GetLastErrorString` 等会返回字符串指针。

要求：

1. 先用 `GetStringSize(ptr)` 获取长度。
2. 再用 `GetStringFromPtr(ptr, buffer, size + 1)` 读取字符串。
3. 最后必须调用 `FreeStringPtr(ptr)` 释放。
4. 如果读取失败，记录 `GetLastError` 和 `GetLastErrorString`。

### 3. 对象生命周期

DLL 模式需要：

1. `CreateCOLAPlugInterFace()` 创建 OLA 对象。
2. 后续函数传入 `instance`。
3. 程序退出或 Dispose 时调用 `DestroyCOLAPlugInterFace(instance)`。

## 建议新增项目/目录

优先新增：

```text
SkyAuto/skyauto.ola/
```

如果不想新增项目，也可以先放：

```text
SkyAuto/skyauto.infrastructure/OlaReal/
```

推荐文件：

1. `OlaNativeMethods.cs`
2. `OlaNativeStringReader.cs`
3. `OlaRealClient.cs`
4. `OlaRealOptions.cs`
5. `OlaRealSelfCheck.cs`
6. `OlaRealCallResult.cs`
7. `OlaFunctionVerificationRecord.cs`

## OlaNativeMethods 要求

用 P/Invoke 声明最小接口。

至少声明：

```csharp
CreateCOLAPlugInterFace
DestroyCOLAPlugInterFace
Ver
GetPlugInfo
GetStringSize
GetStringFromPtr
FreeStringPtr
GetLastError
GetLastErrorString
IsElevated
Capture
GetScreenData
FindImageFromPath
Ocr
MoveTo
LeftClick
```

注意：

1. DllImport 路径不要写死。
2. 支持从设置读取 DLL 路径。
3. 如果 DllImport 不能动态路径加载，使用 NativeLibrary.Load + Marshal.GetDelegateForFunctionPointer。
4. 所有异常都要转成明确错误结果，不要直接崩 UI。

## OlaRealClient 要求

实现真实客户端，和现有 IOlaClient / OlaCallResult 对齐。

必须有：

1. `Initialize(OlaRealOptions options)`
2. `SelfCheck()`
3. `GetVersion()`
4. `GetPlugInfo()`
5. `Capture(...)`
6. `Ocr(...)`
7. `FindImageFromPath(...)`
8. `MoveTo(...)`
9. `LeftClick(...)`
10. `Dispose()`

返回结果必须包含：

```text
Success
IsMock = false
Verified
NotVerified
FunctionName
RawResponse
ErrorMessage
EvidenceJson
ElapsedMs
```

## SelfCheck 要求

新增/增强 SelfCheck 页面，显示：

1. OLA DLL 路径
2. DLL 是否存在
3. 进程架构 x86/x64
4. 是否管理员权限
5. 是否能 CreateCOLAPlugInterFace
6. 是否能 Ver
7. 是否能 GetPlugInfo
8. 是否能 Capture
9. 是否能 Ocr
10. 是否能 FindImageFromPath
11. 是否能 MoveTo / LeftClick

每一项状态：

```text
Verified
NotVerified
Failed
Skipped
```

## 最小验收闭环

本轮至少要实现一个真实可验证闭环：

### 闭环 A：版本检测

1. 加载 DLL
2. 创建 instance
3. 调用 Ver
4. 正确读取字符串
5. 释放字符串
6. 销毁 instance
7. UI / 日志显示真实版本号

### 闭环 B：截图

1. 加载 DLL
2. 调用 Capture 或 GetScreenData
3. 保存截图文件
4. 路径存在
5. 文件大小 > 0
6. 返回 Verified=true

### 闭环 C：OCR 或找图

如果 OCR 模型缺失，可以先 NotVerified，不要假成功。

如果找图缺素材，可以先用测试图片做真实验证。

## 测试要求

由于 CI 不一定有 OLA DLL，测试分两类：

### Unit Tests

不依赖真实 DLL：

1. OlaNativeStringReader 的错误处理
2. OlaRealCallResult 映射
3. NotVerified 规则
4. BadImageFormatException 转错误结果
5. DLL 不存在时 SelfCheck 失败但不崩溃

### Manual Real Tests

需要 Windows + OLA DLL：

新增文档：

```text
docs/ola/OLA_REAL_MANUAL_TEST.md
```

里面写清：

1. DLL 放哪里
2. 如何切 x86
3. 如何运行 EXE
4. 如何点 SelfCheck
5. Ver 成功标准
6. Capture 成功标准
7. Ocr 成功标准
8. 失败如何看日志

## 禁止行为

1. 不要把 Mock 标记成 Verified。
2. 不要 DLL 不存在还返回成功。
3. 不要吞异常。
4. 不要把 x86/x64 架构问题忽略。
5. 不要一口气包装 914 个函数。
6. 不要提交本地 DLL/EXE/CHM 到普通 Git 历史，除非负责人明确要求用 Git LFS 或 Release。
7. 不要破坏现有 WorkflowRunner / StepTestService / VariableSystem 测试。

## 完成后执行

```powershell
dotnet build
dotnet test
git status
```

如果需要 x86 发布测试：

```powershell
dotnet publish SkyAuto/skyauto.app/SkyAuto.App.csproj -c Release -r win-x86 --self-contained false
```

## 提交要求

```powershell
git add .
git commit -m "feat: add OLA real native integration diagnostics"
git push origin main
git status
```

## 最终汇报格式

```text
【A线任务】
OLA Real 最小真实闭环接入

【GitHub】
- commit hash：
- 是否 push main：
- git status clean：

【构建测试】
- dotnet build：
- dotnet test：
- x86 publish：

【真实 OLA 验证】
- DLL 路径：
- DLL 是否存在：
- 进程架构：
- 是否管理员：
- CreateCOLAPlugInterFace：
- Ver：
- GetPlugInfo：
- Capture：
- Ocr：
- FindImageFromPath：
- MoveTo/LeftClick：

【哪些是 Verified】
1.
2.
3.

【哪些仍 NotVerified】
1.
2.
3.

【没有 Mock 冒充 Real 的证明】

【新增文件】

【修改文件】

【已知风险】
```
