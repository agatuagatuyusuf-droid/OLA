using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using SkyAuto.Core.Ola;
using SkyAuto.Infrastructure.Storage;

namespace SkyAuto.App.Views;

public partial class OlaSettingsView : UserControl
{
    private readonly SqliteStore _store;
    private IOlaClient? _olaClient;
    private string _dataDir;

    public OlaSettingsView(SqliteStore store, string dataDir)
    {
        InitializeComponent();
        _store = store;
        _dataDir = dataDir;
        LogPathBox.Text = Path.Combine(dataDir, "logs");
        ScreenshotPathBox.Text = Path.Combine(dataDir, "screenshots");

        LoadPluginPathFromSettings();
        UpdateModeDisplay(OlaConnectionMode.NotConfigured);

        var registry = OlaFunctionRegistry.CreateDefault();
        RegisteredFunctionsList.ItemsSource = registry.GetAll().OrderBy(f => f.Category).ThenBy(f => f.Name);
    }

    private void LoadPluginPathFromSettings()
    {
        var settings = _store.GetSetting("OlaPluginPath");
        if (!string.IsNullOrEmpty(settings))
        {
            PluginPathBox.Text = settings;
        }
    }

    public void SetOlaClient(IOlaClient client)
    {
        _olaClient = client;
        UpdateModeDisplay(client.Mode);
        UpdateConnectionInfo();
    }

    private void UpdateModeDisplay(OlaConnectionMode mode)
    {
        ModeDisplay.Text = mode switch
        {
            OlaConnectionMode.Real => "Real（真实 OLA）",
            OlaConnectionMode.Mock => "Mock（模拟模式）",
            _ => "NotConfigured（未配置）"
        };

        ModeDisplay.Foreground = mode switch
        {
            OlaConnectionMode.Real => new SolidColorBrush(Colors.Green),
            OlaConnectionMode.Mock => new SolidColorBrush(Colors.Orange),
            _ => new SolidColorBrush(Colors.Gray)
        };

        ModeDescription.Text = mode switch
        {
            OlaConnectionMode.Real => "当前使用真实 OLA 插件。所有动作将通过真实 DLL 执行。",
            OlaConnectionMode.Mock => "当前使用 Mock 模式。所有动作返回模拟结果，不会操作真实系统。Mock 通过不等于完成！",
            _ => "OLA 未配置或初始化失败。请设置插件路径或使用 Mock 模式进行开发测试。"
        };

        BtnSwitchReal.IsEnabled = mode != OlaConnectionMode.Real;
        BtnSwitchMock.IsEnabled = mode != OlaConnectionMode.Mock;
    }

    private void UpdateConnectionInfo()
    {
        if (_olaClient == null) return;

        var status = _olaClient.Status;
        MachineCodeBox.Text = status.MachineCode ?? "未获取";
        InitStatus.Text = $"{(status.Initialized ? "已初始化" : "未初始化")}" +
                         (string.IsNullOrEmpty(status.InitError) ? "" : $" | 错误: {status.InitError}");
    }

    private void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "DLL文件|*.dll|所有文件|*.*",
            Title = "选择 OLA 插件 DLL"
        };

        if (dialog.ShowDialog() == true)
        {
            PluginPathBox.Text = dialog.FileName;
        }
    }

    private void OnInitConnectionClick(object? sender, RoutedEventArgs e)
    {
        var path = PluginPathBox.Text?.Trim();
        _store.SaveSetting("OlaPluginPath", path ?? "");

        if (string.IsNullOrEmpty(path))
        {
            // Switch to mock mode
            _olaClient?.Dispose();
            _olaClient = new OlaMockClient();
            UpdateModeDisplay(OlaConnectionMode.Mock);
            ConnectionStatus.Text = "已切换到 Mock 模式（无真实 OLA）";
            InitStatus.Text = "已初始化 (Mock)";
            MachineCodeBox.Text = "MOCK-MACHINE-CODE-0001";
            return;
        }

        if (!File.Exists(path))
        {
            ConnectionStatus.Text = $"文件不存在: {path}";
            ConnectionStatus.Foreground = new SolidColorBrush(Colors.Red);
            return;
        }

        // Try real connection first, fall back to mock
        _olaClient?.Dispose();
        var realClient = new OlaClient();
        if (realClient.Initialize(path))
        {
            _olaClient = realClient;
            UpdateModeDisplay(OlaConnectionMode.Real);
            ConnectionStatus.Text = "真实 OLA 连接成功！";
            ConnectionStatus.Foreground = new SolidColorBrush(Colors.Green);

            // Get machine code
            var mcResult = _olaClient.GetMachineCode();
            if (mcResult.Success) MachineCodeBox.Text = mcResult.Data?.ToString() ?? "";
        }
        else
        {
            // Real connection failed, try mock mode
            ConnectionStatus.Text = $"真实 OLA 连接失败: {(realClient.Status.InitError ?? "未知错误")}";
            ConnectionStatus.Foreground = new SolidColorBrush(Colors.Red);

            _olaClient = new OlaMockClient();
            _olaClient.Initialize(path);
            UpdateModeDisplay(OlaConnectionMode.Mock);
        }

        UpdateConnectionInfo();
    }

    private void OnTestConnectionClick(object? sender, RoutedEventArgs e)
    {
        if (_olaClient == null)
        {
            ConnectionStatus.Text = "请先初始化 OLA 连接";
            return;
        }

        var result = _olaClient.TestConnection();
        ConnectionStatus.Text = $"{result.Message} ({(result.IsMock ? "Mock" : "Real")})";
        ConnectionStatus.Foreground = result.Success
            ? new SolidColorBrush(Colors.Green)
            : new SolidColorBrush(Colors.Red);
    }

    private void OnDisconnectClick(object? sender, RoutedEventArgs e)
    {
        _olaClient?.Dispose();
        _olaClient = null;
        UpdateModeDisplay(OlaConnectionMode.NotConfigured);
        ConnectionStatus.Text = "已断开连接";
        InitStatus.Text = "未初始化";
        MachineCodeBox.Text = "未获取";
    }

    private void OnSwitchToMockClick(object? sender, RoutedEventArgs e)
    {
        _olaClient?.Dispose();
        _olaClient = new OlaMockClient();
        _olaClient.Initialize("");
        UpdateModeDisplay(OlaConnectionMode.Mock);
        ConnectionStatus.Text = "已切换到 Mock 模式";
        MachineCodeBox.Text = "MOCK-MACHINE-CODE-0001";
        InitStatus.Text = "已初始化 (Mock)";
    }

    private void OnSwitchToRealClick(object? sender, RoutedEventArgs e)
    {
        var path = PluginPathBox.Text?.Trim();
        if (string.IsNullOrEmpty(path))
        {
            MessageBox.Show("请先设置 OLA 插件 DLL 路径", "提示");
            return;
        }

        if (!File.Exists(path))
        {
            MessageBox.Show($"文件不存在: {path}", "错误");
            return;
        }

        _olaClient?.Dispose();
        var realClient = new OlaClient();
        if (realClient.Initialize(path))
        {
            _olaClient = realClient;
            UpdateModeDisplay(OlaConnectionMode.Real);
            ConnectionStatus.Text = "已切换到 Real 模式";
            ConnectionStatus.Foreground = new SolidColorBrush(Colors.Green);

            var mcResult = _olaClient.GetMachineCode();
            if (mcResult.Success) MachineCodeBox.Text = mcResult.Data?.ToString() ?? "";
        }
        else
        {
            MessageBox.Show($"Real 模式初始化失败: {realClient.Status.InitError}", "错误");
            UpdateModeDisplay(OlaConnectionMode.NotConfigured);
            ConnectionStatus.Text = $"连接失败: {realClient.Status.InitError}";
            ConnectionStatus.Foreground = new SolidColorBrush(Colors.Red);
        }

        UpdateConnectionInfo();
    }

    private void OnOpenDataDirClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = _dataDir, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开失败: {ex.Message}", "错误");
        }
    }
}
