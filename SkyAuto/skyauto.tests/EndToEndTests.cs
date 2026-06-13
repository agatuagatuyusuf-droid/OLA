using System.Diagnostics;
using Xunit;
using SkyAuto.Core.Engine;
using SkyAuto.Core.Models;
using SkyAuto.Core.Ola;
using SkyAuto.Infrastructure.Logging;
using SkyAuto.Infrastructure.Scheduling;
using SkyAuto.Infrastructure.SelfCheck;
using SkyAuto.Infrastructure.Storage;

namespace SkyAuto.Tests;

public class EndToEndTests
{
    private string _testDataDir = "";
    private SqliteStore? _store;
    private AppLogger? _logger;

    [Fact]
    public void Can_Create_And_Delete_Workflow()
    {
        SetupTest();
        
        var wf = new Workflow { Name = "测试流程" };
        _store!.SaveWorkflow(wf);
        
        Assert.NotNull(wf.Id);
        Assert.Equal("测试流程", wf.Name);
        
        var loaded = _store.GetWorkflow(wf.Id);
        Assert.NotNull(loaded);
        Assert.Equal("测试流程", loaded.Name);
        
        _store.DeleteWorkflow(wf.Id);
        var afterDelete = _store.GetWorkflow(wf.Id);
        Assert.Null(afterDelete);
    }

    [Fact]
    public void Can_Export_And_Import_Workflow()
    {
        SetupTest();
        
        var wf = new Workflow 
        { 
            Name = "导出测试", 
            Steps = 
            { 
                new WorkflowStep { Index = 1, Category = "系统", Type = "sleep", Params = { ["seconds"] = 1 } } 
            } 
        };
        _store!.SaveWorkflow(wf);
        
        var json = _store.ExportWorkflowToJson(wf);
        Assert.Contains("导出测试", json);
        
        var imported = _store.ImportWorkflowFromJson(json);
        Assert.NotNull(imported);
        Assert.Equal("导出测试", imported.Name);
        Assert.Single(imported.Steps);
    }

    [Fact]
    public void Can_Create_And_Run_Simple_Workflow()
    {
        SetupTest();
        
        var wf = new Workflow 
        { 
            Name = "执行测试", 
            Steps = 
            { 
                new WorkflowStep { Index = 1, Category = "系统", Type = "sleep", Params = { ["seconds"] = 1 }, Enabled = true } 
            } 
        };
        
        var executors = CreateTestExecutors();
        var runner = new WorkflowRunner(executors);
        
        var stopwatch = Stopwatch.StartNew();
        var record = runner.RunAsync(wf).Result;
        stopwatch.Stop();
        
        Assert.True(record.Success, $"Workflow failed: {record.ErrorMessage}");
        Assert.InRange(stopwatch.ElapsedMilliseconds, 900, 3000); // sleep for ~1 second
        
        _store!.SaveRunRecord(record);
        var saved = _store.GetAllRunRecords();
        Assert.Single(saved);
    }

    [Fact]
    public void Can_Execute_Multi_Step_Workflow()
    {
        SetupTest();
        
        var wf = new Workflow 
        { 
            Name = "多步测试", 
            Steps = 
            { 
                new WorkflowStep { Index = 1, Category = "系统", Type = "write_log", Params = { ["message"] = "第一步" }, Enabled = true },
                new WorkflowStep { Index = 2, Category = "系统", Type = "sleep", Params = { ["seconds"] = 1 }, Enabled = true },
                new WorkflowStep { Index = 3, Category = "文件", Type = "write_file", Params = { ["path"] = Path.Combine(_testDataDir, "test_output.txt"), ["content"] = "测试内容" }, Enabled = true }
            } 
        };
        
        var executors = CreateTestExecutors();
        var runner = new WorkflowRunner(executors);
        
        var record = runner.RunAsync(wf).Result;
        
        Assert.True(record.Success, $"Workflow failed: {record.ErrorMessage}");
        Assert.Equal(3, record.StepRecords.Count);
        
        // Verify file was created
        Assert.True(File.Exists(Path.Combine(_testDataDir, "test_output.txt")));
    }

    [Fact]
    public void Workflow_Runs_Three_Times_Successfully()
    {
        SetupTest();
        
        for (int i = 0; i < 3; i++)
        {
            var wf = new Workflow 
            { 
                Name = $"轮次{i + 1}", 
                Steps = 
                { 
                    new WorkflowStep { Index = 1, Category = "系统", Type = "sleep", Params = { ["seconds"] = 1 }, Enabled = true } 
                } 
            };
            
            var executors = CreateTestExecutors();
            var runner = new WorkflowRunner(executors);
            
            var record = runner.RunAsync(wf).Result;
            Assert.True(record.Success, $"第{i + 1}次执行失败: {record.ErrorMessage}");
        }
    }

    [Fact]
    public void Ola_Registry_Has_All_Functions()
    {
        var registry = OlaFunctionRegistry.CreateDefault();
        
        Assert.True(registry.GetAll().Count >= 25, $"Only {registry.GetAll().Count} functions registered");
        Assert.True(registry.GetCategories().Count >= 8, $"Only {registry.GetCategories().Count} categories registered");
        
        // Check specific functions exist
        Assert.True(registry.Contains("open_program"));
        Assert.True(registry.Contains("sleep"));
        Assert.True(registry.Contains("write_file"));
        Assert.True(registry.Contains("find_window"));
        Assert.True(registry.Contains("ocr_region"));
    }

    [Fact]
    public void Self_Check_Passes_All_Items()
    {
        SetupTest();
        
        var registry = OlaFunctionRegistry.CreateDefault();
        var checker = new SelfChecker(_store!, _testDataDir, _logger, registry);
        
        var results = checker.RunAllChecksAsync().Result;
        
        Assert.Equal(30, results.Count);
        
        var failed = results.Where(r => !r.Passed).ToList();
        foreach (var f in failed)
            Console.WriteLine($"FAILED: {f.Id} - {f.Name}: {f.Evidence}");
        
        if (failed.Count > 0)
            throw new Exception($"Failed checks: {string.Join(", ", failed.Select(f => $"{f.Id}: {f.Name} - {f.Evidence}"))}");
    }

    [Fact]
    public void Can_Create_And_Manage_Schedule()
    {
        SetupTest();
        
        var schedule = new Schedule 
        { 
            WorkflowId = "test_wf", 
            RuleType = "interval", 
            IntervalMinutes = 5, 
            Enabled = true 
        };
        
        _store!.SaveSchedule(schedule);
        
        var loaded = _store.GetSchedule(schedule.Id);
        Assert.NotNull(loaded);
        Assert.Equal("interval", loaded.RuleType);
        
        _store.DeleteSchedule(schedule.Id);
        Assert.Null(_store.GetSchedule(schedule.Id));
    }

    [Fact]
    public void Can_Manage_Assets()
    {
        SetupTest();
        
        var asset = new Asset 
        { 
            Name = "测试素材", 
            Type = "image", 
            Description = "OCR模板" 
        };
        
        _store!.SaveAsset(asset);
        
        var loaded = _store.GetAsset(asset.Id);
        Assert.NotNull(loaded);
        Assert.Equal("测试素材", loaded.Name);
        
        _store.DeleteAsset(asset.Id);
        Assert.Null(_store.GetAsset(asset.Id));
    }

    [Fact]
    public void Sqlite_Store_Can_Create_And_Read_Workflow()
    {
        var sqliteDataDir = Path.Combine(Path.GetTempPath(), $"skyauto_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(sqliteDataDir);

        try
        {
            var store = new SqliteStore(sqliteDataDir);

            var wf = new Workflow 
            { 
                Name = "SQLite测试", 
                Steps = 
                { 
                    new WorkflowStep { Index = 1, Type = "sleep", Params = { ["seconds"] = 2 } } 
                },
                Variables = { ["key1"] = "value1" }
            };

            store.SaveWorkflow(wf);

            var loaded = store.GetWorkflow(wf.Id);
            Assert.NotNull(loaded);
            Assert.Equal("SQLite测试", loaded.Name);
            Assert.Single(loaded.Steps);

            var all = store.GetAllWorkflows();
            Assert.Contains(all, w => w.Id == wf.Id);

            // Update
            loaded.Name = "SQLite更新";
            store.SaveWorkflow(loaded!);

            var updated = store.GetWorkflow(wf.Id);
            Assert.Equal("SQLite更新", updated!.Name);
        }
        finally
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            // Best-effort cleanup for SQLite file locks
            try
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        Directory.Delete(sqliteDataDir, true);
                        break;
                    }
                    catch when (i < 9)
                    {
                        Thread.Sleep(200);
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }
            }
            catch
            {
                // Ignore cleanup failures - file may still be locked by SQLite native handles
            }
        }
    }

    [Fact]
    public void Simple_Scheduler_Can_Create_And_Stop_Timer()
    {
        var scheduler = new SimpleTaskScheduler(async _ => await Task.CompletedTask);

        var schedule = new Schedule 
        { 
            RuleType = "interval", 
            IntervalMinutes = 1, 
            Enabled = true 
        };
        
        scheduler.Start(schedule);
        
        // Wait a bit to let timer fire
        Thread.Sleep(500);
        
        scheduler.StopAll();
    }

    private void SetupTest()
    {
        _testDataDir = Path.Combine(Path.GetTempPath(), $"skyauto_e2e_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDataDir);
        
        _store = new SqliteStore(_testDataDir);
        _logger = new AppLogger(Path.Combine(_testDataDir, "logs"));
    }

    private Dictionary<string, IActionStepExecutor> CreateTestExecutors() => StepExecutorFactory.CreateAll();
}
