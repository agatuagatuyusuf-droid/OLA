# SkyAuto OLA 项目体检与下一轮执行计划

更新时间：2026-06-13

## 规则

后续每一轮都按以下规则执行：

1. 先检查 GitHub main 分支真实代码。
2. ChatGPT 能安全修改并推送的，先直接提交到 GitHub。
3. ChatGPT 不能安全修改、需要本地编译验证或涉及大范围重构的，交给本地开发 AI 执行。
4. 本地开发 AI 完成后必须执行 `dotnet build` 和 `dotnet test`。
5. build/test 通过后必须 commit + push 到 `main`。
6. 没有 push 到 GitHub，不算完成。

## 当前已确认进展

### 已完成

- WPF EXE 项目结构已建立。
- Core / Infrastructure / Tests 分层已建立。
- WorkflowRunner 假成功问题已经初步修复。
- SqliteStore.GetSetting SQL 已修正为 `WHERE key = @Key`。
- OlaCallResult 已增加证据链字段。
- OlaCallVerifier 已增加基础验证方法。
- OLA 真实调用方式已明确标记为 NotVerified，避免 Mock/猜测冒充 Real。
- ActionStepExecutors 已从单体文件拆分为多个分类目录。
- StepExecutorFactory 已集中注册动作执行器。

## 当前主要风险

### 1. WorkflowRunner 失败步骤可能未写入 StepRecords

当前失败分支里会 `break`，但 `record.StepRecords.Add(stepRecord)` 在 break 后面。因此失败步骤和异常步骤可能没有进入 StepRecords。

影响：

- 日志页面可能看不到失败步骤。
- run_step_records 可能缺少关键失败记录。
- 后续单步回放和问题定位会不完整。

必须修复：

- 成功、失败、异常步骤都必须写入 StepRecords。
- 失败后可以停止流程，但不能丢掉当前失败步骤记录。

### 2. 运行锁仍然是 workflowId 级别，缺少 GlobalAutomationLock

当前锁使用 workflowId 作为 lock_key，只能防止同一个流程重复运行。

OLA 会控制鼠标、键盘、窗口、截图、找图、OCR，不同流程同时执行会抢控制权。

必须新增：

- `global:automation` 全局锁。
- 只要流程包含鼠标、键盘、窗口、截图、找图、OCR 类动作，就必须先获取全局锁。
- 定时任务遇到全局锁要记录 skipped_by_lock。
- 手动运行遇到全局锁要提示用户等待或取消。

### 3. OLA Real 仍未验证

当前文档已经承认 `ola_xxx + int(string)` 是猜测，方向正确。

后续必须基于真实 OLA DLL 或 COM 文档做二次实现，不能继续扩展猜测式 RealClient。

### 4. 变量系统尚未落地

当前 Workflow 只有 `Dictionary<string, object?> Variables`，缺少：

- 变量定义表
- 密码变量脱敏
- OCR 输出变量
- 上一步输出变量
- 参数模板解析

### 5. 单步测试和素材截图取样尚未落地

没有单步测试，用户搭自动化时会很难调试。

没有截图取样，找图/OCR 类动作很难真正配置。

## 下一轮只做 3 件事

下一轮不要做 UI 美化，不要做 README，不要做变量系统。

优先完成：

1. 修 WorkflowRunner：失败/异常步骤也必须写入 StepRecords。
2. 补测试：失败步骤和异常步骤都必须出现在 StepRecords。
3. 实现 GlobalAutomationLock，防止多个 OLA 自动化流程抢鼠标键盘窗口。

## 下一轮验收标准

### WorkflowRunner

- 成功步骤写入 StepRecords。
- 失败步骤写入 StepRecords。
- 异常步骤写入 StepRecords。
- 失败后 record.Success=false。
- 失败后 FailedStepName 不为空。
- 失败后 ErrorMessage 不为空。
- 失败后 ScreenshotPath 或失败证据存在。

### GlobalAutomationLock

- 普通文件/日志类流程可以按 workflow lock 执行。
- 鼠标、键盘、窗口、截图、找图、OCR 类流程必须获取 `global:automation`。
- 两个 OLA 自动化流程不能并发执行。
- 定时任务遇锁必须记录 skipped_by_lock。
- 手动运行遇锁必须返回明确错误信息。

## 给本地开发 AI 的执行摘要

请只做以下 3 项：

1. 修复 WorkflowRunner 失败/异常步骤不进入 StepRecords 的问题。
2. 为上述问题补充单元测试。
3. 实现 GlobalAutomationLock。

完成后必须：

```bash
dotnet build
dotnet test
git status
git add .
git commit -m "fix: persist failed step records and add global automation lock"
git push origin main
git status
```

汇报必须包含：

- commit hash
- build 结果
- test 结果
- 修改文件列表
- 新增文件列表
- 是否 push 到 main
- git status 是否 clean
