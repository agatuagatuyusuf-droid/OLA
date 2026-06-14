# OLA 插件接入资料清单

本目录记录用户提供的 OLA beta68 接入资料。原始文件已在本地上传给项目负责人；由于普通 GitHub 仓库不适合直接提交 37MB DLL / 74MB EXE 这类二进制安装包，二进制文件建议通过 GitHub Release、Git LFS 或开发机本地 `third_party/ola/local/` 目录管理。

## 版本

- OLA 版本：`1.0.0-beta.68_31`
- 主要目标：接入 OLA Real，替换当前 Mock / NotVerified 壳子，完成最小真实闭环。

## 已提供文件

| 文件 | 大小 | SHA256 | 用途 |
|---|---:|---|---|
| `ola_export_com.json` | 1,090,260 bytes | `eb4d020d7577dc369b52a0b2ac9e1b3f6f3b67b4d1df952475b4af2a8c2b584a` | COM/DLL 接口导出定义，914 个函数 |
| `ola_export_def.json` | 1,289,781 bytes | `73da0e13157f2033de86cdeabe602ecb4005d7bb8fee8a707f6ccac20941c61a` | 更适合代码生成/PInvoke 的接口定义，914 个函数 |
| `OLAPlug_x86_1.0.0-beta.68_31.dll` | 38,671,872 bytes | `145d6b4321adbb4748edbaef623c1a07e62da3576dc311ecb6584fe3ea7c2811` | OLA x86 插件 DLL，优先以此版本为准 |
| `OLAPlug_x86_1.0.0-beta.68_30.dll` | 38,625,792 bytes | `31971a4680a03289cc382c83ad5e5a350a7096274469f5157fc7f2c37fec2588` | OLA x86 插件 DLL 旧一版，备用对比 |
| `欧拉插件文档_v1.0.0-beta.68_29.chm` | 2,195,440 bytes | `68fc78b51953a6e89f1c10398034723a17f2418b0c58319c99785a84baa463df` | CHM 帮助文档 |
| `欧拉开发者工具_beta68_20.exe` | 77,573,993 bytes | `a283ae0e28bf99f784083d252a1e1264ccb34e4e51ea6fc6c1413a02a860f65b` | 官方开发者工具，用于本地验证函数 |

## 关键接口方向

从导出 JSON 看，OLA DLL 模式核心入口包括：

- `CreateCOLAPlugInterFace`：创建 OLA 对象，返回对象指针。
- `DestroyCOLAPlugInterFace`：释放 OLA 对象。
- `Ver` / `GetPlugInfo`：读取版本和插件信息。
- `FreeStringPtr` / `GetStringSize` / `GetStringFromPtr`：处理 DLL 返回的字符串指针。
- `BindWindow` / `BindWindowEx` / `UnBindWindow` / `GetBindWindow`：窗口绑定。
- `Capture` / `GetScreenData` / `GetScreenDataBmp` / `GetScreenDataPtr`：截图。
- `FindImageFromPath` / `FindImageFromPathAll`：找图。
- `FindColor` / `FindMultiColor`：找色。
- `Ocr` / `OcrDetails` / `OcrLoadModel` / `OcrListModels`：OCR。
- `MoveTo` / `MoveR` / `LeftClick` / `RightClick`：鼠标。
- `KeyPress` / `KeyPressStr` / `SendString`：键盘输入。
- `GetLastError` / `GetLastErrorString`：错误诊断。
- `IsElevated`：管理员权限检测。

## 二进制文件处理规则

不要把 DLL / EXE / CHM 直接提交进普通 Git 历史，除非项目负责人明确要求并启用 Git LFS。推荐路径：

```text
third_party/ola/local/OLAPlug_x86_1.0.0-beta.68_31.dll
third_party/ola/local/OLAPlug_x86_1.0.0-beta.68_30.dll
third_party/ola/local/欧拉开发者工具_beta68_20.exe
third_party/ola/local/欧拉插件文档_v1.0.0-beta.68_29.chm
```

`third_party/ola/local/` 应作为本地依赖目录，不提交到 Git。

## 接入原则

1. 先做最小真实闭环，不要一口气包装 914 个函数。
2. 优先验证 `Ver`、`GetPlugInfo`、`IsElevated`、`Capture`、`FindImageFromPath`、`Ocr`、`MoveTo`、`LeftClick`。
3. 任何没有真实调用成功的功能，必须保持 `NotVerified=true`。
4. 不能让 Mock 结果冒充 Real 结果。
5. DLL 返回字符串指针时，必须读取后调用 `FreeStringPtr` 释放。
6. x86 DLL 要注意进程架构，SkyAuto 需要提供 x86 运行/测试方案，不能用 AnyCPU/x64 直接误调。
