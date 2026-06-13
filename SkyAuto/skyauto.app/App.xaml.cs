using System.IO;
using System.Windows;
using System.Windows.Data;

namespace SkyAuto.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var appDir = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? AppDomain.CurrentDomain.BaseDirectory;
        if (!appDir.EndsWith("bin"))
            appDir = AppDomain.CurrentDomain.BaseDirectory;

        var dataDir = Path.Combine(appDir, "..", "..", "data");
        if (!Directory.Exists(dataDir))
            dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");

        Directory.CreateDirectory(dataDir);
    }
}

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool b)
            return b ? "#38A169" : "#E53E3E";
        return "Gray";
    }

    public object ConvertBack(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture) => throw new System.NotSupportedException();
}
