using SkyAuto.Core.Models;

namespace SkyAuto.Core.Runtime;

public static class AutomationActionClassifier
{
    private static readonly HashSet<string> GlobalLockRequiredTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "find_window",
        "activate_window",
        "move_window",
        "bind_window",
        "unbind_window",
        "screenshot_window",

        "mouse_move",
        "mouse_click",
        "mouse_double_click",
        "mouse_right_click",
        "mouse_scroll",

        "keyboard_type",
        "keyboard_press",
        "keyboard_hotkey",

        "screenshot",
        "find_image",
        "wait_image",
        "click_image",
        "judge_image",
        "find_color",
        "find_multi_color",

        "ocr_region",
        "recognize_number",
        "ocr_image",
        "find_text",
        "judge_text",

        "save_screenshot"
    };

    public static bool RequiresGlobalAutomationLock(Workflow workflow)
    {
        return workflow.Steps
            .Where(step => step.Enabled)
            .Any(step => GlobalLockRequiredTypes.Contains(step.Type));
    }

    public static bool RequiresGlobalAutomationLock(WorkflowStep step)
    {
        return step.Enabled && GlobalLockRequiredTypes.Contains(step.Type);
    }
}
