using System.Runtime.InteropServices;
using SkyAuto.Core.Engine;
using SkyAuto.Core.Models;

namespace SkyAuto.Core.Steps.MouseKeyboardSteps;

public class MouseMoveExecutor : ActionStepExecutorBase
{
    public override string Type => "mouse_move";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var x = int.Parse(step.Params.GetValueOrDefault("x")?.ToString() ?? "0");
        var y = int.Parse(step.Params.GetValueOrDefault("y")?.ToString() ?? "0");

        SetCursorPos(x, y);
        await Task.Yield();
        return $"鼠标已移动到 ({x}, {y})";
    }

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);
}

public class MouseClickExecutor : ActionStepExecutorBase
{
    public override string Type => "mouse_click";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var x = int.Parse(step.Params.GetValueOrDefault("x")?.ToString() ?? "0");
        var y = int.Parse(step.Params.GetValueOrDefault("y")?.ToString() ?? "0");

        SetCursorPos(x, y);
        await Task.Delay(50);
        mouse_event(0x0002, 0, 0, 0, IntPtr.Zero);
        await Task.Delay(30);
        mouse_event(0x0004, 0, 0, 0, IntPtr.Zero);

        return $"已单击 ({x}, {y})";
    }

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, IntPtr lParam);
}

public class MouseDoubleClickExecutor : ActionStepExecutorBase
{
    public override string Type => "mouse_double_click";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var x = int.Parse(step.Params.GetValueOrDefault("x")?.ToString() ?? "0");
        var y = int.Parse(step.Params.GetValueOrDefault("y")?.ToString() ?? "0");

        SetCursorPos(x, y);
        await Task.Delay(50);
        mouse_event(0x0002 | 0x0010, 0, 0, 0, IntPtr.Zero);

        return $"已双击 ({x}, {y})";
    }

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, IntPtr lParam);
}

public class MouseRightClickExecutor : ActionStepExecutorBase
{
    public override string Type => "mouse_right_click";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var x = int.Parse(step.Params.GetValueOrDefault("x")?.ToString() ?? "0");
        var y = int.Parse(step.Params.GetValueOrDefault("y")?.ToString() ?? "0");

        SetCursorPos(x, y);
        await Task.Delay(50);
        mouse_event(0x0008, 0, 0, 0, IntPtr.Zero);
        await Task.Delay(30);
        mouse_event(0x0010, 0, 0, 0, IntPtr.Zero);

        return $"已右键单击 ({x}, {y})";
    }

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, IntPtr lParam);
}

public class MouseScrollExecutor : ActionStepExecutorBase
{
    public override string Type => "mouse_scroll";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var delta = int.Parse(step.Params.GetValueOrDefault("delta")?.ToString() ?? "120");

        mouse_event(0x0800, 0, 0, (uint)delta, IntPtr.Zero);
        await Task.Yield();
        return $"滚轮滚动: {delta}";
    }

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, IntPtr lParam);
}

public class KeyboardTypeExecutor : ActionStepExecutorBase
{
    public override string Type => "keyboard_type";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var text = step.Params.GetValueOrDefault("text")?.ToString() ?? "";
        foreach (var c in text)
        {
            SendUnicodeChar(c);
            await Task.Delay(10);
        }

        return $"已输入: {text}";
    }

    private static void SendUnicodeChar(char c)
    {
        const uint KEYEVENTF_KEYUP = 0x0002;
        const uint KEYEVENTF_UNICODE = 0x0004;

        var downInput = new INPUT
        {
            Type = 1,
            Ki = new InputUnion { Ki = new KeyboardInput { WVk = 0, WScan = (ushort)c, DwFlags = KEYEVENTF_UNICODE } }
        };

        var upInput = new INPUT
        {
            Type = 1,
            Ki = new InputUnion { Ki = new KeyboardInput { WVk = 0, WScan = (ushort)c, DwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP } }
        };

        var inputs = new[] { downInput, upInput };
        int cbSize = Marshal.SizeOf(typeof(INPUT));
        SendInput((uint)inputs.Length, inputs, cbSize);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int Type;
        public InputUnion Ki;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KeyboardInput Ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort WVk;
        public ushort WScan;
        public uint DwFlags;
        public int Time;
        public IntPtr DwExtraInfo;
    }
}

public class KeyboardPressExecutor : ActionStepExecutorBase
{
    public override string Type => "keyboard_press";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var key = step.Params.GetValueOrDefault("key")?.ToString() ?? "";

        byte vk = GetVirtualKey(key);
        if (vk == 0) return $"未知的键名: {key}";

        SendKey(vk);
        await Task.Yield();

        return $"已按键: {key}";
    }

    private static void SendKey(byte vk)
    {
        const uint KEYEVENTF_KEYUP = 0x0002;
        var downInput = new INPUT
        {
            Type = 1,
            Ki = new InputUnion { Ki = new KeyboardInput { WVk = vk, WScan = 0, DwFlags = 0 } }
        };

        var upInput = new INPUT
        {
            Type = 1,
            Ki = new InputUnion { Ki = new KeyboardInput { WVk = vk, WScan = 0, DwFlags = KEYEVENTF_KEYUP } }
        };

        var inputs = new[] { downInput, upInput };
        int cbSize = Marshal.SizeOf(typeof(INPUT));
        SendInput((uint)inputs.Length, inputs, cbSize);
    }

    private static byte GetVirtualKey(string keyName)
    {
        switch (keyName.ToLowerInvariant())
        {
            case "enter": return 13;
            case "tab": return 9;
            case "escape" or "esc": return 27;
            case "delete" or "del": return 46;
            case "backspace" or "bs": return 8;
            case "space": return 32;
            case "capslock" or "caps": return 20;
            case "numlock": return 144;
            case "scrolllock" or "scroll": return 145;
            case "insert" or "ins": return 45;
            case "home": return 36;
            case "end": return 35;
            case "pageup" or "pgup": return 33;
            case "pagedown" or "pgdn" or "pagedn": return 34;
            case "up" or "arrowup": return 38;
            case "down" or "arrowdown": return 40;
            case "left" or "arrowleft": return 37;
            case "right" or "arrowright": return 39;
            case "f1": return 112;
            case "f2": return 113;
            case "f3": return 114;
            case "f4": return 115;
            case "f5": return 116;
            case "f6": return 117;
            case "f7": return 118;
            case "f8": return 119;
            case "f9": return 120;
            case "f10": return 121;
            case "f11": return 122;
            case "f12": return 123;
            case "ctrl" or "control": return 17;
            case "alt": return 18;
            case "shift": return 16;
            default: return 0;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int Type;
        public InputUnion Ki;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KeyboardInput Ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort WVk;
        public ushort WScan;
        public uint DwFlags;
        public int Time;
        public IntPtr DwExtraInfo;
    }
}

public class KeyboardHotkeyExecutor : ActionStepExecutorBase
{
    public override string Type => "keyboard_hotkey";

    protected override async Task<string?> DoExecuteAsync(WorkflowStep step, Dictionary<string, object?>? ctx)
    {
        var keys = step.Params.GetValueOrDefault("keys")?.ToString() ?? "";
        if (string.IsNullOrEmpty(keys)) return "快捷键为空";

        var parts = keys.Split('+', '-');
        if (parts.Length < 2) return $"无效的组合键格式: {keys}，使用 '+' 或 '-' 分隔（如 Ctrl+C）";

        var vkCodes = new List<byte>();
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            byte vk = GetVirtualKey(trimmed);
            if (vk == 0) return $"未知的键名: {trimmed}";
            vkCodes.Add(vk);
        }

        const uint KEYEVENTF_KEYUP = 0x0002;
        var inputs = new List<INPUT>();

        foreach (var vk in vkCodes)
        {
            inputs.Add(new INPUT
            {
                Type = 1,
                Ki = new InputUnion { Ki = new KeyboardInput { WVk = vk, WScan = 0, DwFlags = 0 } }
            });
        }

        for (var i = vkCodes.Count - 1; i >= 0; i--)
        {
            inputs.Add(new INPUT
            {
                Type = 1,
                Ki = new InputUnion { Ki = new KeyboardInput { WVk = vkCodes[i], WScan = 0, DwFlags = KEYEVENTF_KEYUP } }
            });
        }

        int cbSize = Marshal.SizeOf(typeof(INPUT));
        SendInput((uint)inputs.Count, inputs.ToArray(), cbSize);
        await Task.Yield();

        return $"已发送快捷键: {keys}";
    }

    private static byte GetVirtualKey(string keyName)
    {
        switch (keyName.ToLowerInvariant())
        {
            case "enter": return 13;
            case "tab": return 9;
            case "escape" or "esc": return 27;
            case "delete" or "del": return 46;
            case "backspace" or "bs": return 8;
            case "space": return 32;
            case "capslock" or "caps": return 20;
            case "insert" or "ins": return 45;
            case "home": return 36;
            case "end": return 35;
            case "pageup" or "pgup": return 33;
            case "pagedown" or "pgdn" or "pagedn": return 34;
            case "up" or "arrowup": return 38;
            case "down" or "arrowdown": return 40;
            case "left" or "arrowleft": return 37;
            case "right" or "arrowright": return 39;
            case "f1": return 112;
            case "f2": return 113;
            case "f3": return 114;
            case "f4": return 115;
            case "f5": return 116;
            case "f6": return 117;
            case "f7": return 118;
            case "f8": return 119;
            case "f9": return 120;
            case "f10": return 121;
            case "f11": return 122;
            case "f12": return 123;
            case "ctrl" or "control": return 17;
            case "alt": return 18;
            case "shift": return 16;
            default: return 0;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int Type;
        public InputUnion Ki;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KeyboardInput Ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort WVk;
        public ushort WScan;
        public uint DwFlags;
        public int Time;
        public IntPtr DwExtraInfo;
    }
}
