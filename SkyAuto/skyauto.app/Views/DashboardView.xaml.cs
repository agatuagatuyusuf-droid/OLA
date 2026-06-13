using System.IO;
using System.Windows;
using System.Windows.Controls;
using SkyAuto.Core.Ola;
using SkyAuto.Infrastructure.SelfCheck;
using SkyAuto.Infrastructure.Storage;

namespace SkyAuto.App.Views;

public partial class DashboardView : UserControl
{
    private readonly SqliteStore? _store;
    private readonly OlaFunctionRegistry? _olaRegistry;

    public DashboardView(SqliteStore store, OlaFunctionRegistry olaRegistry)
    {
        InitializeComponent();
        _store = store;
        _olaRegistry = olaRegistry;
        RefreshData();
    }

    private void RefreshData()
    {
        var records = _store?.GetAllRunRecords() ?? new List<Core.Models.RunRecord>();
        var today = DateTime.Today;
        var todayRuns = records.Where(r => r.StartTime.Date == today).ToList();

        TodayRunsCount.Text = todayRuns.Count.ToString();
        SuccessCount.Text = todayRuns.Count(r => r.Success).ToString();
        FailCount.Text = todayRuns.Count(r => !r.Success).ToString();
        WorkflowCount.Text = (_store?.GetAllWorkflows()?.Count ?? 0).ToString();

        OlaStatus.Text = _olaRegistry != null ? $"已注册 {_olaRegistry.GetAll().Count} 个动作" : "未初始化";

        var recentFails = records.Where(r => !r.Success).OrderByDescending(r => r.EndTime).Take(5).ToList();
        RecentFailuresGrid.ItemsSource = recentFails;
    }

    private void OnNewWorkflowClick(object? sender, RoutedEventArgs e)
    {
        MessageBox.Show("请在步骤编辑器中新建流程", "提示");
    }

    private async void OnSelfCheckClick(object? sender, RoutedEventArgs e)
    {
        SelfCheckStatus.Text = "检查中...";
        var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "data");
        if (!Directory.Exists(dataDir))
            dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");

        var checker = new SelfChecker(_store!, dataDir, olaRegistry: _olaRegistry);
        var results = await checker.RunAllChecksAsync();

        var passCount = results.Count(r => r.Passed);
        SelfCheckStatus.Text = $"通过 {passCount}/{results.Count}";

        if (passCount < results.Count)
        {
            var failed = results.Where(r => r.Failed).Select(r => $"[{r.Id}] {r.Name}: {r.Evidence}");
            MessageBox.Show($"自检完成: 通过 {passCount}/{results.Count}\n\n{string.Join("\n", failed)}", "自检结果");
        }
        else
        {
            MessageBox.Show($"自检完成: 全部通过 {passCount}/{results.Count}", "自检结果");
        }
    }

    private void OnViewLogsClick(object? sender, RoutedEventArgs e) => MessageBox.Show("请从左侧导航点击执行日志", "提示");

    private void OnOlaSettingsClick(object? sender, RoutedEventArgs e) => MessageBox.Show("请从左侧导航点击OLA 设置", "提示");
}
