# OLA 本地依赖目录

该目录用于记录 OLA 插件本地依赖的放置方式。

## 重要说明

OLA 官方 DLL / EXE / CHM 文件体积较大，不建议直接提交到普通 Git 历史。请在开发机本地创建：

```text
third_party/ola/local/
```

然后放入：

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

## 校验信息

参考：

```text
docs/ola/OLA_ASSETS_MANIFEST.md
```

## 接入任务

参考：

```text
docs/ola/OLA_REAL_INTEGRATION_TASK.md
```

## x86 注意事项

当前 DLL 文件名包含 `x86`，A 线接入时必须处理进程架构：

1. WPF App 如果是 x64/AnyCPU，直接加载 x86 DLL 可能抛 `BadImageFormatException`。
2. 优先做 win-x86 publish 验证。
3. 如果主程序必须 x64，则需要单独 x86 bridge/helper 进程。
4. 不能忽略架构错误，必须在 SelfCheck 中明确显示。
