namespace SkyAuto.Core.Ola;

public interface IOlaClient : IDisposable
{
    OlaConnectionMode Mode { get; }
    OlaRuntimeStatus Status { get; }

    bool Initialize(string pluginPath);
    OlaCallResult TestConnection();
    OlaCallResult Call(string functionKey, Dictionary<string, object> parameters);
    OlaCallResult GetMachineCode();
    OlaCallResult CaptureScreen(string savePath);
    OlaCallResult MoveMouse(int x, int y);
    OlaCallResult Click(int x, int y);
    OlaCallResult DoubleClick(int x, int y);
    OlaCallResult RightClick(int x, int y);
    OlaCallResult ScrollMouse(int delta);
    OlaCallResult TypeText(string text);
    OlaCallResult PressKey(string key);
    OlaCallResult Hotkey(string keys);
    OlaCallResult FindImage(string imagePath, double similarity);
    OlaCallResult WaitImage(string imagePath, int timeoutSeconds);
    OlaCallResult ClickImage(string imagePath, double similarity);
    OlaCallResult ImageExists(string imagePath);
    OlaCallResult OcrRegion(int x, int y, int width, int height);
    OlaCallResult OcrNumber(int x, int y, int width, int height);
    OlaCallResult FindText(string text);
    OlaCallResult TextContains(string text);
    string GetLastError();
}
