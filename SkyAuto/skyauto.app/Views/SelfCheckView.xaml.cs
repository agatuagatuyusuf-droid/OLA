using System.IO;
using System.Windows;
using System.Windows.Controls;
using SkyAuto.Core.Models;
using SkyAuto.Core.Ola;
using SkyAuto.Infrastructure.SelfCheck;
using SkyAuto.Infrastructure.Storage;

namespace SkyAuto.App.Views;

public partial class SelfCheckView : UserControl
{
    private readonly SqliteStore _store;
    private readonly OlaFunctionRegistry? _olaRegistry;
    private List<SelfCheckItem> _results = new();

    public SelfCheckView(SqliteStore store, OlaFunctionRegistry? olaRegistry = null)
    {
        InitializeComponent();
        _store = store;
        _olaRegistry = olaRegistry;
    }

    private async void OnStartCheckClick(object? sender, RoutedEventArgs e)
    {
        CheckProgress.Visibility = Visibility.Visible;
        CheckProgress.Value = 0;
        StatusText.Text = "自检进行中...";

        var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "data");
        if (!Directory.Exists(dataDir))
            dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");

        var checker = new SelfChecker(_store, dataDir, olaRegistry: _olaRegistry);
        _results = await checker.RunAllChecksAsync();

        UpdateDisplay();
        CheckProgress.Visibility = Visibility.Collapsed;
    }

    private async void OnRerunFailedClick(object? sender, RoutedEventArgs e)
    {
        var failedIds = _results.Where(r => r.Failed).Select(r => r.Id).ToHashSet();
        if (failedIds.Count == 0)
        {
            MessageBox.Show("没有失败的检查项", "提示");
            return;
        }

        StatusText.Text = $"重新检查 {failedIds.Count} 个失败项...";

        var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "data");
        if (!Directory.Exists(dataDir))
            dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");

        var checker = new SelfChecker(_store, dataDir, olaRegistry: _olaRegistry);
        var allResults = await checker.RunAllChecksAsync();

        foreach (var id in failedIds)
        {
            if (_results.FirstOrDefault(r => r.Id == id)?.Failed == true)
            {
                var newResult = allResults.FirstOrDefault(r => r.Id == id);
                if (newResult != null && _results.Contains(_results.First(r => r.Id == id)))
                {
                    int idx = _results.FindIndex(r => r.Id == id);
                    if (idx >= 0) _results[idx] = newResult;
                }
            }
        }

        UpdateDisplay();
    }

    private void OnExportReportClick(object? sender, RoutedEventArgs e)
    {
        if (_results.Count == 0)
        {
            MessageBox.Show("请先运行自检", "提示");
            return;
        }

        var passCount = _results.Count(r => r.Passed);
        var failCount = _results.Count(r => r.Failed);
        var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "data");
        if (!Directory.Exists(dataDir))
            dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");

        var reportPath = Path.Combine(dataDir, $"self_check_report_{DateTime.Now:yyyyMMddHHmmss}.txt");

        var lines = new List<string>
        {
            $"SkyAuto 自检报告 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"总计: {_results.Count}项, 通过: {passCount}, 失败: {failCount}",
            "",
            "=== 详细结果 ==="
        };

        foreach (var item in _results)
        {
            lines.Add($"[{item.Status.ToUpper()}] [{item.Id}] {item.Name} ({item.Category}) - {item.Evidence}");
        }

        File.WriteAllText(reportPath, string.Join("\n", lines));
        MessageBox.Show($"报告已导出到:\n{reportPath}", "导出成功");
    }

    private void UpdateDisplay()
    {
        var passCount = _results.Count(r => r.Passed);
        var failCount = _results.Count(r => r.Failed);

        CheckResultsGrid.ItemsSource = null;
        CheckResultsGrid.ItemsSource = _results;

        if (SummaryPanel.Children.Cast<Border>().FirstOrDefault() is Border totalBorder &&
            totalBorder.Child is TextBlock totalText)
            totalText.Text = $"总计: {passCount}/{_results.Count}";

        if (SummaryPanel.Children.Cast<Border>().ElementAtOrDefault(1) is Border passBorder &&
            passBorder.Child is TextBlock passText)
            passText.Text = $"通过: {passCount}";

        if (SummaryPanel.Children.Cast<Border>().ElementAtOrDefault(2) is Border failBorder &&
            failBorder.Child is TextBlock failText)
            failText.Text = $"失败: {failCount}";

        StatusText.Text = failCount == 0
            ? "所有检查项通过!"
            : $"{failCount} 项未通过，请查看详情并修复后重新检查";
    }
}
