using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using SkyAuto.Core.Models;
using SkyAuto.Infrastructure.Storage;

namespace SkyAuto.App.Views;

public partial class RunLogView : UserControl
{
    private readonly SqliteStore _store;
    private List<RunRecord> _allRecords = new();

    public RunLogView(SqliteStore store)
    {
        InitializeComponent();
        _store = store;
        RefreshLogs();
        LogSummaryGrid.SelectionChanged += OnSelectionChanged;
    }

    private void RefreshLogs()
    {
        _allRecords = _store.GetAllRunRecords().OrderByDescending(r => r.StartTime).ToList();
        LogSummaryGrid.ItemsSource = null;
        LogSummaryGrid.ItemsSource = _allRecords;
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (LogSummaryGrid.SelectedItem is RunRecord record)
        {
            DetailGroup.Visibility = Visibility.Visible;
            StepDetailGrid.ItemsSource = record.StepRecords;
        }
        else
        {
            DetailGroup.Visibility = Visibility.Collapsed;
        }
    }

    private void OnRefreshClick(object? sender, RoutedEventArgs e) => RefreshLogs();

    private void OnFilterKeyDown(object? sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.Enter) return;

        var filter = FilterWorkflowId.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(filter))
        {
            LogSummaryGrid.ItemsSource = _allRecords;
        }
        else
        {
            LogSummaryGrid.ItemsSource = _allRecords.Where(r => r.WorkflowName?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true
                || r.WorkflowId.Contains(filter)).ToList();
        }
    }

    // Simple bool-to-color converter inline (would normally be a proper IValueConverter)

    private void OnViewScreenshotClick(object? sender, RoutedEventArgs e)
    {
        if (LogSummaryGrid.SelectedItem is not RunRecord record) return;
        var path = record.ScreenshotPath;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            MessageBox.Show("该记录没有失败截图", "提示");
            return;
        }

        try
        {
            // Open the screenshot in default image viewer
            System.Diagnostics.Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开截图失败: {ex.Message}", "错误");
        }
    }
}
