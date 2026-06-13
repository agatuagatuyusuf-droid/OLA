namespace SkyAuto.Infrastructure.Logging;

public class AppLogger
{
    private readonly string _logPath;
    private static readonly object Lock = new();

    public AppLogger(string logDir)
    {
        Directory.CreateDirectory(logDir);
        _logPath = Path.Combine(logDir, "app.log");
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message) => Write("ERROR", message);
    public void Debug(string message) => Write("DEBUG", message);

    private void Write(string level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}\n";
        lock (Lock)
        {
            File.AppendAllText(_logPath, line);
        }
    }
}
