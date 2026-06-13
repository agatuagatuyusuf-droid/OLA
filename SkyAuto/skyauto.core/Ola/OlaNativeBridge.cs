using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SkyAuto.Core.Ola;

// Native bridge for OLA plugin DLL - uses dynamic loading since path is configurable at runtime
public static class OlaNativeBridge
{
    private static IntPtr _dllHandle = IntPtr.Zero;

    // P/Invoke declarations for dynamic loading
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr NativeLoadLibrary(string dllPath);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    // Delegate for OLA function calls (typical signature: int ola_xxx(params))
    public delegate int OlaFunctionDelegate(string paramStr);

    /// <summary>
    /// Load the OLA plugin DLL from the given path. Returns true on success.
    /// </summary>
    public static bool LoadLibrary(string dllPath)
    {
        if (string.IsNullOrWhiteSpace(dllPath)) return false;
        if (!File.Exists(dllPath)) return false;

        FreeLoadedLibrary();

        try
        {
            _dllHandle = NativeLoadLibrary(dllPath);
            if (_dllHandle == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            return true;
        }
        catch (Exception ex)
        {
            _dllHandle = IntPtr.Zero;
            // Store error for later retrieval
            return false;
        }
    }

    /// <summary>
    /// Free the loaded OLA DLL.
    /// </summary>
    public static void FreeLoadedLibrary()
    {
        if (_dllHandle != IntPtr.Zero)
        {
            try { FreeLibrary(_dllHandle); } catch { /* ignore */ }
            _dllHandle = IntPtr.Zero;
        }
    }

    /// <summary>
    /// Get a delegate for the specified OLA function name.
    /// Returns null if the function is not found or DLL is not loaded.
    /// </summary>
    public static OlaFunctionDelegate? GetOlaFunction(string functionName)
    {
        if (_dllHandle == IntPtr.Zero) return null;

        var procAddr = GetProcAddress(_dllHandle, functionName);
        if (procAddr == IntPtr.Zero) return null;

        try
        {
            return Marshal.GetDelegateForFunctionPointer<OlaFunctionDelegate>(procAddr);
        }
        catch { return null; }
    }

    /// <summary>
    /// Check if the OLA DLL is currently loaded.
    /// </summary>
    public static bool IsLoaded => _dllHandle != IntPtr.Zero;

    /// <summary>
    /// Get list of exported function names from the loaded DLL (if supported).
    /// This requires parsing the PE export table - simplified version returns empty if not available.
    /// </summary>
    public static List<string> GetExportedFunctions()
    {
        var result = new List<string>();

        if (_dllHandle == IntPtr.Zero) return result;

        // Simplified: try common OLA function names
        var knownNames = new[]
        {
            "ola_init", "ola_capture_screen", "ola_move_mouse", "ola_click",
            "ola_double_click", "ola_right_click", "ola_scroll", "ola_type_text",
            "ola_press_key", "ola_hotkey", "ola_find_image", "ola_wait_image",
            "ola_click_image", "ola_image_exists", "ola_ocr_region", "ola_ocr_number",
            "ola_find_text", "ola_text_contains", "ola_get_machine_code",
            "ola_test_connection"
        };

        foreach (var name in knownNames)
        {
            if (GetOlaFunction(name) != null)
                result.Add(name);
        }

        return result;
    }
}
