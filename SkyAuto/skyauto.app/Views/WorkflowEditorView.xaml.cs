using System.IO;
using System.Windows;
using System.Windows.Controls;
using SkyAuto.Core.Engine;
using SkyAuto.Core.Models;
using SkyAuto.Core.Ola;
using SkyAuto.Infrastructure.Storage;

namespace SkyAuto.App.Views;

public partial class WorkflowEditorView : UserControl
{
    private readonly SqliteStore _store;
    private readonly OlaFunctionRegistry _olaRegistry;
    private readonly WorkflowRunner? _runner;
    private Workflow? _currentWorkflow;

    public WorkflowEditorView(SqliteStore store, OlaFunctionRegistry olaRegistry, WorkflowRunner? runner)
    {
        InitializeComponent();
        _store = store;
        _olaRegistry = olaRegistry;
        _runner = runner;
        LoadWorkflows();
        LoadActions();
        StepsGrid.SelectionChanged += OnStepSelected;
    }

    private void LoadWorkflows()
    {
        var workflows = _store.GetAllWorkflows().OrderByDescending(w => w.UpdatedAt).ToList();
        WorkflowSelector.ItemsSource = workflows;
        WorkflowSelector.DisplayMemberPath = "Name";
        WorkflowSelector.SelectedValuePath = "Id";
        if (workflows.Count > 0)
            WorkflowSelector.SelectedItem = workflows[0];
    }

    private void LoadActions()
    {
        var actions = _olaRegistry.GetAll().OrderBy(a => a.Category).ThenBy(a => a.Name);
        ActionSelector.ItemsSource = actions;
        ActionSelector.DisplayMemberPath = "Name";
        ActionSelector.SelectedValuePath = "Type";

        // Populate help text
        HelpText.Text = string.Join("\n", actions.Select(a => $"[{a.Category}] {a.Type} - {a.Description}"));
    }

    private void OnWorkflowSelected(object? sender, SelectionChangedEventArgs e)
    {
        var wf = WorkflowSelector.SelectedItem as Workflow;
        _currentWorkflow = wf;

        if (wf != null)
            StepsGrid.ItemsSource = wf.Steps;

        ParamEditor.Visibility = Visibility.Collapsed;
        StepActions.Visibility = Visibility.Collapsed;
    }

    private void OnNewWorkflowClick(object? sender, RoutedEventArgs e)
    {
        var name = PromptDialog.Show("新建流程", "请输入流程名称");
        if (string.IsNullOrWhiteSpace(name)) return;

        var wf = new Workflow { Name = name.Trim() };
        _store.SaveWorkflow(wf);
        LoadWorkflows();
        WorkflowSelector.SelectedItem = wf;
    }

    private void OnAddStepClick(object? sender, RoutedEventArgs e)
    {
        if (_currentWorkflow == null) return;
        var type = ActionSelector.SelectedValue?.ToString();
        if (string.IsNullOrEmpty(type)) return;

        var funcInfo = _olaRegistry.Get(type);
        if (funcInfo == null) return;

        var stepIndex = _currentWorkflow.Steps.Count + 1;
        var paramsDict = new Dictionary<string, object?>();
        foreach (var p in funcInfo.Parameters)
            paramsDict[p.Key] = p.DefaultValue;

        var step = new WorkflowStep
        {
            Index = stepIndex,
            Category = funcInfo.Category,
            Type = type,
            Params = paramsDict,
            TimeoutSeconds = 30,
            RetryCount = 0,
            Enabled = true
        };

        _currentWorkflow.Steps.Add(step);
        StepsGrid.ItemsSource = null;
        StepsGrid.ItemsSource = _currentWorkflow.Steps;
        StatusText.Text = $"已添加步骤: {funcInfo.Name}";
    }

    private void OnStepSelected(object? sender, SelectionChangedEventArgs e)
    {
        StepActions.Visibility = StepsGrid.SelectedItem != null ? Visibility.Visible : Visibility.Collapsed;
    }

    private int GetSelectedIndex()
    {
        if (StepsGrid.SelectedItem is WorkflowStep step && _currentWorkflow != null)
            return _currentWorkflow.Steps.IndexOf(step);
        return -1;
    }

    private void OnMoveUpClick(object? sender, RoutedEventArgs e)
    {
        var idx = GetSelectedIndex();
        if (idx <= 0 || _currentWorkflow == null) return;

        var temp = _currentWorkflow.Steps[idx];
        _currentWorkflow.Steps[idx] = _currentWorkflow.Steps[idx - 1];
        _currentWorkflow.Steps[idx - 1] = temp;

        ReindexSteps();
    }

    private void OnMoveDownClick(object? sender, RoutedEventArgs e)
    {
        if (_currentWorkflow == null) return;
        var idx = GetSelectedIndex();
        if (idx < 0 || idx >= _currentWorkflow.Steps.Count - 1) return;

        var temp = _currentWorkflow.Steps[idx];
        _currentWorkflow.Steps[idx] = _currentWorkflow.Steps[idx + 1];
        _currentWorkflow.Steps[idx + 1] = temp;

        ReindexSteps();
    }

    private void ReindexSteps()
    {
        if (_currentWorkflow == null) return;

        for (int i = 0; i < _currentWorkflow.Steps.Count; i++)
            _currentWorkflow.Steps[i].Index = i + 1;

        StepsGrid.ItemsSource = null;
        StepsGrid.ItemsSource = _currentWorkflow.Steps;
    }

    private void OnEditStepParamsClick(object? sender, RoutedEventArgs e)
    {
        if (StepsGrid.SelectedItem is not WorkflowStep step || _currentWorkflow == null) return;

        var funcInfo = _olaRegistry.Get(step.Type);
        ParamEditor.Visibility = Visibility.Visible;
        ParamFields.Children.Clear();

        foreach (var p in funcInfo?.Parameters ?? Enumerable.Empty<OlaParamInfo>())
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 0) };
            var label = new TextBlock { Text = $"{p.Label}({p.Key}): ", Width = 150, VerticalAlignment = VerticalAlignment.Center };
            var input = new TextBox
            {
                Width = 200, Height = 24, Padding = new Thickness(3),
                Text = step.Params.GetValueOrDefault(p.Key)?.ToString() ?? ""
            };
            input.Tag = p.Key;

            row.Children.Add(label);
            row.Children.Add(input);
            ParamFields.Children.Add(row);
        }

        // Save button inside param editor
        var saveRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };
        var saveBtn = new Button { Content = "保存参数", Style = (Style)FindResource("PrimaryButtonStyle") };
        saveBtn.Click += (s2, e2) =>
        {
            foreach (var child in ParamFields.Children.OfType<StackPanel>())
            {
                if (child.Children.Count == 2 && child.Children[1] is TextBox tb && tb.Tag?.ToString() is var key)
                    step.Params[key] = string.IsNullOrEmpty(tb.Text) ? null : tb.Text;
            }
            ReindexSteps();
            ParamEditor.Visibility = Visibility.Collapsed;
            StatusText.Text = "参数已保存";
        };
        saveRow.Children.Add(saveBtn);
        ParamFields.Children.Add(saveRow);
    }

    private void OnDeleteStepClick(object? sender, RoutedEventArgs e)
    {
        if (_currentWorkflow == null || StepsGrid.SelectedItem is not WorkflowStep step) return;
        _currentWorkflow.Steps.Remove(step);
        ReindexSteps();
    }

    private async void OnRunWorkflowClick(object? sender, RoutedEventArgs e)
    {
        if (_currentWorkflow == null || _runner == null) return;

        try
        {
            StatusText.Text = $"正在执行: {_currentWorkflow.Name}...";
            var record = await _runner.RunAsync(_currentWorkflow);
            _store.SaveRunRecord(record);
            StatusText.Text = record.Success ? "执行成功!" : $"执行失败: {record.ErrorMessage?.Split('\n')[0]}";

            _currentWorkflow.LastRunTime = DateTime.Now;
            _currentWorkflow.LastResult = record.Success ? "成功" : "失败";
            _store.SaveWorkflow(_currentWorkflow);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"执行异常: {ex.Message}";
        }
    }

    private void OnSaveWorkflowClick(object? sender, RoutedEventArgs e)
    {
        if (_currentWorkflow == null) return;
        _store.SaveWorkflow(_currentWorkflow);
        StatusText.Text = $"已保存流程: {_currentWorkflow.Name}";
    }

    private void OnExportJsonClick(object? sender, RoutedEventArgs e)
    {
        if (_currentWorkflow == null) return;
        var json = _store.ExportWorkflowToJson(_currentWorkflow);
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "data", "workflows", $"{_currentWorkflow.Name}_{DateTime.Now:yyyyMMddHHmmss}.json");
        File.WriteAllText(path, json);
        MessageBox.Show($"已导出到:\n{path}", "导出成功");
    }

    private class PromptDialog
    {
        public static string? Show(string title, string message)
        {
            var win = new Window { Title = title, Width = 350, Height = 120 };
            var tb = new TextBox();
            var stack = new StackPanel { Margin = new Thickness(15), Children = { new TextBlock { Text = message }, tb } };

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 15, 0, 0) };
            string? result = null;

            var okBtn = new Button { Content = "确定", Width = 70, Margin = new Thickness(5, 0, 0, 0) };
            var cancelBtn = new Button { Content = "取消", Width = 70 };

            okBtn.Click += (s, e2) => { result = tb.Text?.Trim(); win.Close(); };
            cancelBtn.Click += (s, e2) => { win.Close(); };
            btnPanel.Children.Add(okBtn);
            btnPanel.Children.Add(cancelBtn);
            stack.Children.Add(btnPanel);

            win.Content = stack;
            win.ShowDialog();
            return result;
        }
    }
}
