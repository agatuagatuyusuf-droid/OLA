using System.IO;
using System.Windows;
using SkyAuto.App.Views;
using SkyAuto.Core.Engine;
using SkyAuto.Core.Models;
using SkyAuto.Core.Ola;
using SkyAuto.Core.Runtime;
using SkyAuto.Infrastructure.Logging;
using SkyAuto.Infrastructure.Scheduling;
using SkyAuto.Infrastructure.SelfCheck;
using SkyAuto.Infrastructure.Storage;

namespace SkyAuto.App;

public partial class MainWindow : Window
{
    private SqliteStore? _store;
    private OlaFunctionRegistry? _olaRegistry;
    private Dictionary<string, IActionStepExecutor>? _executors;
    private WorkflowRunner? _runner;
    private QuartzSchedulerService? _scheduler;
    private AppLogger? _logger;
    private IOlaClient? _olaClient;
    private IWorkflowRunLockService? _lockService;
    private string _dataDir = "data";

    public MainWindow()
    {
        InitializeComponent();
        InitializeServices();
        OnNavigateDashboard(null, null);
    }

    private void InitializeServices()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var dataDir = Path.Combine(appDir, "..", "..", "data");
        if (!Directory.Exists(dataDir))
            dataDir = Path.Combine(appDir, "data");
        Directory.CreateDirectory(dataDir);
        _dataDir = dataDir;

        _store = new SqliteStore(dataDir);
        _logger = new AppLogger(Path.Combine(dataDir, "logs"));
        _olaRegistry = OlaFunctionRegistry.CreateDefault();

        // Register executors via StepExecutorFactory
        _executors = StepExecutorFactory.CreateAll();

        _runner = new WorkflowRunner(_executors, dataDir);

        // Initialize run lock service and clean up expired locks
        _lockService = new SqliteWorkflowRunLockService(_store!.ConnectionString!);
        var cleaned = _lockService.CleanupExpiredLocks();
        if (cleaned > 0)
            _logger?.Info($"已清理 {cleaned} 个过期运行锁");

        // Re-initialize runner with lock service
        _runner = new WorkflowRunner(_executors, dataDir, _lockService);

        // Initialize OlaClient with Mock mode by default
        _olaClient = new OlaMockClient();
        _olaClient.Initialize("");

        // Initialize Quartz scheduler
        _scheduler = new QuartzSchedulerService(async schedule =>
        {
            try
            {
                var wf = _store?.GetWorkflow(schedule.WorkflowId);
                if (wf != null && _runner != null)
                {
                    wf.LastRunTime = DateTime.Now;
                    var record = await _runner.RunAsync(wf);
                    schedule.LastResult = record.Success ? "成功" : $"失败: {record.ErrorMessage}";
                    _store?.SaveWorkflow(wf);
                    _store?.SaveSchedule(schedule);
                    _store?.SaveRunRecord(record);
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"定时任务执行异常 [{schedule.WorkflowId}]: {ex.Message}");
            }
        });

        // Start Quartz scheduler
        if (_scheduler != null)
            _scheduler.InitializeAsync().Wait();
    }

    private static IActionStepExecutor? CreateExecutor(string type) => StepExecutorFactory.Create(type);
    private void OnNavigateDashboard(object? sender, RoutedEventArgs? e) => ShowContent(new DashboardView(_store!, _olaRegistry!));
    private void OnNavigateWorkflows(object? sender, RoutedEventArgs? e) => ShowContent(new WorkflowListView(_store!, _runner));
    private void OnNavigateEditor(object? sender, RoutedEventArgs? e) => ShowContent(new WorkflowEditorView(_store!, _olaRegistry!, _runner));
    private void OnNavigateAssets(object? sender, RoutedEventArgs? e) => ShowContent(new AssetLibraryView(_store!));
    private void OnNavigateSchedules(object? sender, RoutedEventArgs? e) => ShowContent(new ScheduleView(_store!, _scheduler!));
    private void OnNavigateLogs(object? sender, RoutedEventArgs? e) => ShowContent(new RunLogView(_store!));
    private void OnNavigateOlaSettings(object? sender, RoutedEventArgs? e)
    {
        var view = new OlaSettingsView(_store!, _dataDir);
        if (_olaClient != null) view.SetOlaClient(_olaClient);
        ShowContent(view);
    }
    private void OnNavigateOlaFunctionManager(object? sender, RoutedEventArgs? e) => ShowContent(new OlaFunctionManagerView(_store!, _olaClient));
    private void OnNavigateSelfCheck(object? sender, RoutedEventArgs? e) => ShowContent(new SelfCheckView(_store!, _olaRegistry!));

    private void OnSelfCheckClick(object? sender, RoutedEventArgs? e)
    {
        ShowContent(new SelfCheckView(_store!, _olaRegistry!));
    }

    private void ShowContent(UIElement content)
    {
        MainContent.Content = content;
    }

    protected override void OnClosed(EventArgs e)
    {
        try { _scheduler?.StopAllAsync().Wait(); } catch { /* ignore shutdown errors */ }
        _olaClient?.Dispose();
        base.OnClosed(e);
    }
}
