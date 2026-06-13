using SkyAuto.Core.Steps.FileSteps;
using SkyAuto.Core.Steps.ImageSteps;
using SkyAuto.Core.Steps.LogSteps;
using SkyAuto.Core.Steps.MouseKeyboardSteps;
using SkyAuto.Core.Steps.NetworkSteps;
using SkyAuto.Core.Steps.OcrSteps;
using SkyAuto.Core.Steps.SystemSteps;
using SkyAuto.Core.Steps.WindowSteps;

namespace SkyAuto.Core.Engine;

public static class StepExecutorFactory
{
    private static readonly Dictionary<string, Func<IActionStepExecutor>> _creators = new()
    {
        ["open_program"] = () => new OpenProgramExecutor(),
        ["open_url"] = () => new OpenUrlExecutor(),
        ["run_cmd"] = () => new RunCmdExecutor(),
        ["sleep"] = () => new SleepExecutor(),
        ["kill_process"] = () => new KillProcessExecutor(),
        ["find_window"] = () => new FindWindowExecutor(),
        ["activate_window"] = () => new ActivateWindowExecutor(),
        ["move_window"] = () => new MoveWindowExecutor(),
        ["bind_window"] = () => new BindWindowExecutor(),
        ["unbind_window"] = () => new UnbindWindowExecutor(),
        ["screenshot_window"] = () => new ScreenshotWindowExecutor(),
        ["mouse_move"] = () => new MouseMoveExecutor(),
        ["mouse_click"] = () => new MouseClickExecutor(),
        ["mouse_double_click"] = () => new MouseDoubleClickExecutor(),
        ["mouse_right_click"] = () => new MouseRightClickExecutor(),
        ["mouse_scroll"] = () => new MouseScrollExecutor(),
        ["keyboard_type"] = () => new KeyboardTypeExecutor(),
        ["keyboard_press"] = () => new KeyboardPressExecutor(),
        ["keyboard_hotkey"] = () => new KeyboardHotkeyExecutor(),
        ["screenshot"] = () => new ScreenshotExecutor(),
        ["find_image"] = () => new FindImageExecutor(),
        ["wait_image"] = () => new WaitImageExecutor(),
        ["click_image"] = () => new ClickImageExecutor(),
        ["judge_image"] = () => new JudgeImageExecutor(),
        ["find_color"] = () => new FindColorExecutor(),
        ["find_multi_color"] = () => new FindMultiColorExecutor(),
        ["ocr_region"] = () => new OcrRegionExecutor(),
        ["recognize_number"] = () => new RecognizeNumberExecutor(),
        ["ocr_image"] = () => new OcrImageExecutor(),
        ["find_text"] = () => new FindTextExecutor(),
        ["judge_text"] = () => new JudgeTextExecutor(),
        ["read_file"] = () => new ReadFileExecutor(),
        ["write_file"] = () => new WriteFileExecutor(),
        ["copy_file"] = () => new CopyFileExecutor(),
        ["move_file"] = () => new MoveFileExecutor(),
        ["delete_file"] = () => new DeleteFileExecutor(),
        ["http_get"] = () => new HttpGetExecutor(),
        ["http_post"] = () => new HttpPostExecutor(),
        ["download_file"] = () => new DownloadFileExecutor(),
        ["write_log"] = () => new WriteLogExecutor(),
        ["save_screenshot"] = () => new SaveScreenshotExecutor(),
    };

    public static IActionStepExecutor? Create(string type) =>
        _creators.TryGetValue(type, out var factory) ? factory() : null;

    public static Dictionary<string, IActionStepExecutor> CreateAll()
    {
        var result = new Dictionary<string, IActionStepExecutor>();
        foreach (var kvp in _creators)
            result[kvp.Key] = kvp.Value();
        return result;
    }

    public static bool IsRegistered(string type) => _creators.ContainsKey(type);

    public static IReadOnlyCollection<string> RegisteredTypes => _creators.Keys;
}
