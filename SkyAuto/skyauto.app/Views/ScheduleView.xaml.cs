using System.Windows;
using System.Windows.Controls;
using SkyAuto.Core.Models;
using SkyAuto.Infrastructure.Scheduling;
using SkyAuto.Infrastructure.Storage;

namespace SkyAuto.App.Views;

public partial class ScheduleView : UserControl
{
    private readonly SqliteStore _store;
    private readonly QuartzSchedulerService? _scheduler;

    public ScheduleView(SqliteStore store, QuartzSchedulerService scheduler)
    {
        InitializeComponent();
        _store = store;
        _scheduler = scheduler;
        RefreshSchedules();
        ScheduleGrid.SelectionChanged += OnSelectionChanged;
    }

    private void RefreshSchedules()
    {
        var schedules = _store.GetAllSchedules().ToList();
        ScheduleGrid.ItemsSource = null;
        ScheduleGrid.ItemsSource = schedules;
        StatusText.Text = $"共 {schedules.Count} 个定时规则";
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ScheduleActions.Visibility = ScheduleGrid.SelectedItem != null ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void OnAddScheduleClick(object? sender, RoutedEventArgs e)
    {
        var win = new Window { Title = "新建定时规则", Width = 400, Height = 320 };
        var stack = new StackPanel { Margin = new Thickness(15) };

        AddField(stack, "流程ID:", out TextBox wfIdBox);
        AddComboBoxField(stack, "规则类型:", out ComboBox ruleTypeBox);
        ruleTypeBox.ItemsSource = Schedule.RuleLabels.Values.ToList();
        ruleTypeBox.SelectedIndex = 0;

        AddField(stack, "间隔(分):", out TextBox intervalBox);
        intervalBox.Text = "60";

        var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 15, 0, 0) };
        bool saved = false;

        var saveBtn = new Button { Content = "保存", Width = 70 };
        btns.Children.Add(saveBtn);
        saveBtn.Click += async (s2, e2) =>
        {
            string? selectedRule = ruleTypeBox.SelectedItem as string;
            string ruleKey = Schedule.RuleLabels.FirstOrDefault(kv => kv.Value == selectedRule).Key ?? "interval";

            var schedule = new Schedule
            {
                WorkflowId = wfIdBox.Text?.Trim() ?? "",
                RuleType = ruleKey,
                IntervalMinutes = int.TryParse(intervalBox.Text, out var min) ? min : 60,
                NextRunTime = DateTime.Now.AddMinutes(int.TryParse(intervalBox.Text, out var m2) ? m2 : 60),
                Enabled = true
            };

            if (string.IsNullOrEmpty(schedule.WorkflowId))
            {
                MessageBox.Show("请输入流程ID", "提示");
                return;
            }

            _store.SaveSchedule(schedule);

            // Register with Quartz scheduler
            try
            {
                await _scheduler!.AddScheduleAsync(schedule);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"注册定时任务失败: {ex.Message}", "错误");
            }

            saved = true;
            win.Close();
        };

        var cancelBtn = new Button { Content = "取消", Width = 70, Margin = new Thickness(8, 0, 0, 0) };
        cancelBtn.Click += (s2, e2) => win.Close();
        btns.Children.Add(cancelBtn);
        stack.Children.Add(btns);

        win.Content = stack;
        win.ShowDialog();

        if (saved) RefreshSchedules();
    }

    private void OnStartScheduleClick(object? sender, RoutedEventArgs e)
    {
        if (ScheduleGrid.SelectedItem is not Schedule schedule) return;

        if (!schedule.Enabled)
        {
            schedule.Enabled = true;
            _store.SaveSchedule(schedule);
        }

        // Re-register with Quartz scheduler
        Task.Run(async () =>
        {
            try { await _scheduler!.AddScheduleAsync(schedule); } catch { /* ignore */ }
        });

        RefreshSchedules();
        StatusText.Text = $"已启动定时任务: {schedule.WorkflowId}";
    }

    private void OnStopScheduleClick(object? sender, RoutedEventArgs e)
    {
        if (ScheduleGrid.SelectedItem is not Schedule schedule) return;

        Task.Run(async () =>
        {
            try { await _scheduler!.RemoveScheduleAsync(schedule.Id); } catch { /* ignore */ }
        });

        RefreshSchedules();
        StatusText.Text = $"已停止定时任务: {schedule.WorkflowId}";
    }

    private void onDeleteScheduleClick(object? sender, RoutedEventArgs e)
    {
        if (ScheduleGrid.SelectedItem is not Schedule schedule) return;

        Task.Run(async () =>
        {
            try { await _scheduler!.RemoveScheduleAsync(schedule.Id); } catch { /* ignore */ }
        });

        _store.DeleteSchedule(schedule.Id);
        RefreshSchedules();
    }

    private static void AddField(StackPanel parent, string label, out TextBox box)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(new TextBlock { Text = label, Width = 60 });
        box = new TextBox { Width = 250, Height = 24 };
        row.Children.Add(box);
        parent.Children.Add(row);
    }

    private static void AddComboBoxField(StackPanel parent, string label, out ComboBox box)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(new TextBlock { Text = label, Width = 60 });
        box = new ComboBox { Width = 250, Height = 24 };
        row.Children.Add(box);
        parent.Children.Add(row);
    }
}
