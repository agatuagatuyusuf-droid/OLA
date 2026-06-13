using System.IO;
using System.Windows;
using System.Windows.Controls;
using SkyAuto.Core.Engine;
using SkyAuto.Core.Models;
using SkyAuto.Infrastructure.Storage;

namespace SkyAuto.App.Views;

public partial class WorkflowListView : UserControl
{
    private readonly SqliteStore _store;
    private readonly WorkflowRunner? _runner;
    private List<Workflow> _workflows = new();

    public WorkflowListView(SqliteStore store, WorkflowRunner? runner)
    {
        InitializeComponent();
        _store = store;
        _runner = runner;
        RefreshWorkflows();
        WorkflowGrid.SelectionChanged += OnSelectionChanged;
    }

    private void RefreshWorkflows()
    {
        var workflows = _store.GetAllWorkflows().OrderByDescending(w => w.UpdatedAt).ToList();

        // Enrich with schedule info
        var schedules = _store.GetAllSchedules();
        foreach (var wf in workflows)
        {
            wf.HasSchedule = schedules.Any(s => s.WorkflowId == wf.Id);
        }

        _workflows = workflows;
        WorkflowGrid.ItemsSource = null;
        WorkflowGrid.ItemsSource = workflows;

        StatusText.Text = $"共{_workflows.Count} 个流程";
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ActionButtons.Visibility = WorkflowGrid.SelectedItem != null ? Visibility.Visible : Visibility.Collapsed;
    }

    private string? GetSelectedId()
    {
        if (WorkflowGrid.SelectedItem is Workflow wf) return wf.Id;
        return null;
    }

    private void OnNewWorkflowClick(object? sender, RoutedEventArgs e)
    {
        var name = PromptDialog.Show("新建流程", "请输入流程名称");
        if (string.IsNullOrWhiteSpace(name)) return;

        var wf = new Workflow { Name = name.Trim() };
        _store.SaveWorkflow(wf);
        RefreshWorkflows();
        StatusText.Text = $"已创建流程: {name}";
    }

    private async void OnRunWorkflowClick(object? sender, RoutedEventArgs e)
    {
        var id = GetSelectedId();
        if (id == null) return;

        var wf = _store.GetWorkflow(id);
        if (wf == null || _runner == null) return;

        StatusText.Text = $"正在执行: {wf.Name}...";

        try
        {
            wf.LastRunTime = DateTime.Now;
            var record = await _runner.RunAsync(wf);
            wf.LastResult = record.Success ? "成功" : $"失败: {record.ErrorMessage?.Split('\n')[0]}";

            _store.SaveWorkflow(wf);
            _store.SaveRunRecord(record);

            RefreshWorkflows();
            StatusText.Text = record.Success ? $"执行成功: {wf.Name}" : $"执行失败: {record.ErrorMessage}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"执行异常: {ex.Message}";
        }
    }

    private void OnEditWorkflowClick(object? sender, RoutedEventArgs e) => MessageBox.Show("请切换到步骤编辑器进行编辑", "提示");

    private void OnCopyWorkflowClick(object? sender, RoutedEventArgs e)
    {
        var id = GetSelectedId();
        if (id == null) return;

        var wf = _store.GetWorkflow(id);
        if (wf == null) return;

        var json = _store.ExportWorkflowToJson(wf);
        var newWf = _store.ImportWorkflowFromJson(json);
        if (newWf != null)
        {
            newWf.Id = Guid.NewGuid().ToString("N");
            newWf.Name += " (副本)";
            newWf.CreatedAt = DateTime.Now;
            _store.SaveWorkflow(newWf);
            RefreshWorkflows();
            StatusText.Text = $"已复制流程: {newWf.Name}";
        }
    }

    private void OnExportJsonClick(object? sender, RoutedEventArgs e)
    {
        var id = GetSelectedId();
        if (id == null) return;

        var wf = _store.GetWorkflow(id);
        if (wf == null) return;

        var json = _store.ExportWorkflowToJson(wf);
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "data", "workflows", $"{wf.Name}_{DateTime.Now:yyyyMMddHHmmss}.json");
        File.WriteAllText(path, json);
        MessageBox.Show($"已导出到:\n{path}", "导出成功");
    }

    private void OnImportJsonClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "data", "workflows");
            if (!Directory.Exists(dataDir)) return;

            var files = Directory.GetFiles(dataDir, "*.json").Take(5).ToList();
            if (files.Count == 0)
            {
                MessageBox.Show("workflows 目录下没有 JSON 文件可导入", "提示");
                return;
            }

            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var wf = _store.ImportWorkflowFromJson(json);
                    if (wf != null && !string.IsNullOrEmpty(wf.Name))
                    {
                        wf.Id = Guid.NewGuid().ToString("N"); // New ID to avoid conflicts
                        wf.CreatedAt = DateTime.Now;
                        _store.SaveWorkflow(wf);
                    }
                }
                catch { /* skip invalid files */ }
            }

            RefreshWorkflows();
            StatusText.Text = $"已从本地导入 {_workflows.Count} 个流程";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导入失败: {ex.Message}", "错误");
        }
    }

    private void OnDeleteWorkflowClick(object? sender, RoutedEventArgs e)
    {
        var id = GetSelectedId();
        if (id == null) return;

        var wf = _store.GetWorkflow(id);
        if (wf == null) return;

        MessageBoxResult result = MessageBox.Show($"确定要删除流程 \"{wf.Name}\" 吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        _store.DeleteWorkflow(id);
        RefreshWorkflows();
        StatusText.Text = $"已删除流程: {wf.Name}";
    }

    private void OnViewLogsClick(object? sender, RoutedEventArgs e) => MessageBox.Show("请从左侧导航点击执行日志", "提示");

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        var search = SearchBox.Text?.Trim().ToLower() ?? "";
        if (string.IsNullOrEmpty(search))
        {
            WorkflowGrid.ItemsSource = _workflows;
        }
        else
        {
            WorkflowGrid.ItemsSource = _workflows.Where(w => w.Name.ToLower().Contains(search)).ToList();
        }
    }

    // Simple prompt dialog
    private class PromptDialog
    {
        public static string? Show(string title, string message)
        {
            var win = new Window
            {
                Title = title, Width = 350, Height = 120, ResizeMode = ResizeMode.NoResize,
                Content = new StackPanel { Margin = new Thickness(15) }
            };

            ((StackPanel)win.Content!).Children.Add(new TextBlock { Text = message });
            var tb = new TextBox();
            ((StackPanel)win.Content!).Children.Add(tb);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 15, 0, 0) };
            var okBtn = new Button { Content = "确定", Width = 70, Margin = new Thickness(0, 0, 8, 0) };
            var cancelBtn = new Button { Content = "取消", Width = 70 };

            string? result = null;

            okBtn.Click += (s, e2) => { result = tb.Text; win.Close(); };
            cancelBtn.Click += (s, e2) => { win.Close(); };

            btnPanel.Children.Add(okBtn);
            btnPanel.Children.Add(cancelBtn);
            ((StackPanel)win.Content!).Children.Add(btnPanel);

            win.ShowDialog();
            return result?.Trim();
        }
    }
}
