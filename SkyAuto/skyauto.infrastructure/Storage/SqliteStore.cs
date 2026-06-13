using System.Text.Encodings.Web;
using System.Text.Json;
using Dapper;
using Microsoft.Data.Sqlite;
using SkyAuto.Core.Models;
using SkyAuto.Core.Ola;

namespace SkyAuto.Infrastructure.Storage;

public class SqliteStore : IWorkflowStore, IAssetStore, IScheduleStore, IRunRecordStore
{
    private readonly string _connectionString;

    public string ConnectionString => _connectionString;

    public SqliteStore(string dataDir)
    {
        var dbPath = Path.Combine(dataDir, "data", "skyauto.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        // Ensure all required directories exist
        Directory.CreateDirectory(Path.Combine(dataDir, "assets"));
        Directory.CreateDirectory(Path.Combine(dataDir, "screenshots"));
        Directory.CreateDirectory(Path.Combine(dataDir, "logs"));
        Directory.CreateDirectory(Path.Combine(dataDir, "workflows"));

        _connectionString = $"Data Source={dbPath}";
        SqliteDbInitializer.RunMigrations(_connectionString);
    }

    private SqliteConnection CreateConnection() => new(_connectionString);

    #region Workflows

    public List<Workflow> GetAllWorkflows()
    {
        using var conn = CreateConnection();
        return conn.Query("SELECT * FROM workflows ORDER BY created_at DESC")
            .Select(MapRowToWorkflow).ToList();
    }

    public Workflow? GetWorkflow(string id)
    {
        using var conn = CreateConnection();
        var row = conn.QueryFirstOrDefault<dynamic>("SELECT * FROM workflows WHERE id = @Id", new { Id = id });
        return row != null ? MapRowToWorkflow(row) : null;
    }

    public void SaveWorkflow(Workflow wf)
    {
        wf.UpdatedAt = DateTime.Now;
        using var conn = CreateConnection();
        bool exists = conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM workflows WHERE id = @Id", new { Id = wf.Id }) > 0;

        if (exists)
        {
            conn.Execute(@"UPDATE workflows SET name = @Name, enabled = @Enabled, variables = @Variables,
                steps = @Steps, last_run_time = @LastRunTime, last_result = @LastResult, updated_at = @UpdatedAt
                WHERE id = @Id", new
            {
                wf.Id,
                wf.Name,
                Enabled = wf.Enabled ? 1 : 0,
                Variables = Serialize(wf.Variables),
                Steps = Serialize(wf.Steps),
                LastRunTime = wf.LastRunTime?.ToString("o"),
                wf.LastResult,
                UpdatedAt = wf.UpdatedAt.ToString("o")
            });
        }
        else
        {
            conn.Execute(@"INSERT INTO workflows (id, name, enabled, variables, steps, last_run_time,
                last_result, created_at, updated_at) VALUES (@Id, @Name, @Enabled, @Variables,
                @Steps, @LastRunTime, @LastResult, @CreatedAt, @UpdatedAt)", new
            {
                wf.Id,
                wf.Name,
                Enabled = wf.Enabled ? 1 : 0,
                Variables = Serialize(wf.Variables),
                Steps = Serialize(wf.Steps),
                LastRunTime = wf.LastRunTime?.ToString("o"),
                wf.LastResult,
                CreatedAt = wf.CreatedAt.ToString("o"),
                UpdatedAt = wf.UpdatedAt.ToString("o")
            });
        }
    }

    public void DeleteWorkflow(string id)
    {
        using var conn = CreateConnection();
        conn.Execute("DELETE FROM workflows WHERE id = @Id", new { Id = id });
    }

    public string ExportWorkflowToJson(Workflow wf) => Serialize(wf)!;

    public Workflow? ImportWorkflowFromJson(string json)
        => Deserialize<Workflow>(json);

    #endregion

    #region Assets

    public List<Asset> GetAllAssets()
    {
        using var conn = CreateConnection();
        return conn.Query("SELECT * FROM assets ORDER BY created_at DESC")
            .Select(MapRowToAsset).ToList();
    }

    public Asset? GetAsset(string id)
    {
        using var conn = CreateConnection();
        var row = conn.QueryFirstOrDefault<dynamic>("SELECT * FROM assets WHERE id = @Id", new { Id = id });
        return row != null ? MapRowToAsset(row) : null;
    }

    public void SaveAsset(Asset asset)
    {
        using var conn = CreateConnection();
        bool exists = conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM assets WHERE id = @Id", new { Id = asset.Id }) > 0;

        if (exists)
        {
            conn.Execute(@"UPDATE assets SET name = @Name, type = @Type, file_path = @FilePath,
                description = @Description WHERE id = @Id", new
            {
                asset.Id,
                asset.Name,
                asset.Type,
                FilePath = asset.FilePath,
                Description = asset.Description ?? (string?)null
            });
        }
        else
        {
            conn.Execute(@"INSERT INTO assets (id, name, type, file_path, description, created_at)
                VALUES (@Id, @Name, @Type, @FilePath, @Description, @CreatedAt)", new
            {
                asset.Id,
                asset.Name,
                asset.Type,
                FilePath = asset.FilePath,
                Description = asset.Description ?? (string?)null,
                CreatedAt = asset.CreatedAt.ToString("o")
            });
        }
    }

    public void DeleteAsset(string id)
    {
        using var conn = CreateConnection();
        conn.Execute("DELETE FROM assets WHERE id = @Id", new { Id = id });
    }

    #endregion

    #region Schedules

    public List<Schedule> GetAllSchedules()
    {
        using var conn = CreateConnection();
        return conn.Query("SELECT * FROM schedules ORDER BY created_at DESC")
            .Select(MapRowToSchedule).ToList();
    }

    public Schedule? GetSchedule(string id)
    {
        using var conn = CreateConnection();
        var row = conn.QueryFirstOrDefault<dynamic>("SELECT * FROM schedules WHERE id = @Id", new { Id = id });
        return row != null ? MapRowToSchedule(row) : null;
    }

    public void SaveSchedule(Schedule schedule)
    {
        using var conn = CreateConnection();
        bool exists = conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM schedules WHERE id = @Id", new { Id = schedule.Id }) > 0;

        if (exists)
        {
            conn.Execute(@"UPDATE schedules SET workflow_id = @WorkflowId, rule_type = @RuleType,
                cron_expression = @CronExpression, interval_minutes = @IntervalMinutes, enabled = @Enabled,
                next_run_time = @NextRunTime, last_result = @LastResult WHERE id = @Id", new
            {
                schedule.Id,
                WorkflowId = schedule.WorkflowId,
                RuleType = schedule.RuleType,
                CronExpression = schedule.CronExpression,
                IntervalMinutes = schedule.IntervalMinutes,
                Enabled = schedule.Enabled ? 1 : 0,
                NextRunTime = schedule.NextRunTime?.ToString("o"),
                LastResult = schedule.LastResult ?? (string?)null
            });
        }
        else
        {
            conn.Execute(@"INSERT INTO schedules (id, workflow_id, rule_type, cron_expression,
                interval_minutes, enabled, next_run_time, last_result, created_at) VALUES (@Id,
                @WorkflowId, @RuleType, @CronExpression, @IntervalMinutes, @Enabled, @NextRunTime,
                @LastResult, @CreatedAt)", new
            {
                schedule.Id,
                WorkflowId = schedule.WorkflowId,
                RuleType = schedule.RuleType,
                CronExpression = schedule.CronExpression,
                IntervalMinutes = schedule.IntervalMinutes,
                Enabled = schedule.Enabled ? 1 : 0,
                NextRunTime = schedule.NextRunTime?.ToString("o"),
                LastResult = schedule.LastResult ?? (string?)null,
                CreatedAt = schedule.CreatedAt.ToString("o")
            });
        }
    }

    public void DeleteSchedule(string id)
    {
        using var conn = CreateConnection();
        conn.Execute("DELETE FROM schedules WHERE id = @Id", new { Id = id });
    }

    #endregion

    #region RunRecords

    public List<RunRecord> GetAllRunRecords()
    {
        using var conn = CreateConnection();
        return conn.Query("SELECT * FROM run_records ORDER BY start_time DESC")
            .Select(MapRowToRunRecord).ToList();
    }

    public void SaveRunRecord(RunRecord record)
    {
        using var conn = CreateConnection();

        // Upsert into run_records table
        conn.Execute(@"INSERT OR REPLACE INTO run_records (id, workflow_id, workflow_name, start_time,
            end_time, success, failed_step_name, error_message, screenshot_path, step_records_json)
            VALUES (@Id, @WorkflowId, @WorkflowName, @StartTime, @EndTime, @Success,
            @FailedStepName, @ErrorMessage, @ScreenshotPath, @StepRecordsJson)", new
        {
            record.Id,
            WorkflowId = record.WorkflowId,
            WorkflowName = record.WorkflowName ?? (string?)null,
            StartTime = record.StartTime.ToString("o"),
            EndTime = record.EndTime.ToString("o"),
            Success = record.Success ? 1 : 0,
            FailedStepName = record.FailedStepName ?? (string?)null,
            ErrorMessage = record.ErrorMessage ?? (string?)null,
            ScreenshotPath = record.ScreenshotPath ?? (string?)null,
            StepRecordsJson = Serialize(record.StepRecords)
        });

        // Also insert individual step records into run_step_records table
        foreach (var sr in record.StepRecords)
        {
            var stepId = $"{record.Id}_s{sr.Index:D3}";
            conn.Execute(@"INSERT OR REPLACE INTO run_step_records (id, run_record_id, `index`, action_type, success, start_time, end_time, duration_ms, output_data, error_message, screenshot_path)
                VALUES (@Id, @RunRecordId, @Index, @ActionType, @Success, @StartTime, @EndTime, @DurationMs, @OutputData, @ErrorMessage, @ScreenshotPath)", new
            {
                Id = stepId,
                RunRecordId = record.Id,
                Index = sr.Index,
                ActionType = sr.StepName ?? (string?)null,
                Success = sr.Success ? 1 : 0,
                StartTime = sr.StartTime.ToString("o"),
                EndTime = sr.EndTime.ToString("o"),
                DurationMs = sr.DurationMs,
                OutputData = sr.OutputData ?? (string?)null,
                ErrorMessage = sr.Error ?? (string?)null,
                ScreenshotPath = sr.ScreenshotPath ?? (string?)null
            });
        }
    }

    public List<RunRecord> GetRunRecordsForWorkflow(string workflowId)
    {
        using var conn = CreateConnection();
        return conn.Query("SELECT * FROM run_records WHERE workflow_id = @WorkflowId ORDER BY start_time DESC",
            new { WorkflowId = workflowId })
            .Select(MapRowToRunRecord).ToList();
    }

    public string GetSetting(string key)
    {
        using var conn = CreateConnection();
        return conn.QuerySingleOrDefault<string>("SELECT value FROM settings WHERE key = @Key", new { Key = key }) ?? "";
    }

    public void SaveSetting(string key, string value)
    {
        using var conn = CreateConnection();
        conn.Execute("INSERT OR REPLACE INTO settings (key, value) VALUES (@Key, @Value)", new { Key = key, Value = value });
    }

    // OlaFunctionStatusRecord CRUD
    public void SaveOlaFunctionStatus(OlaFunctionStatusRecord record)
    {
        using var conn = CreateConnection();
        conn.Execute(@"INSERT OR REPLACE INTO ola_function_status 
            (function_key, category, chinese_name, raw_function_name, parameters_json, 
             implemented, real_ola_connected, tested, test_status, test_message, last_tested_at)
            VALUES (@FunctionKey, @Category, @ChineseName, @RawFunctionName, @ParametersJson,
                    @Implemented, @RealOlaConnected, @Tested, @TestStatus, @TestMessage, @LastTestedAt)", record);
    }

    public OlaFunctionStatusRecord? GetOlaFunctionStatus(string functionKey)
    {
        using var conn = CreateConnection();
        return conn.QueryFirstOrDefault<OlaFunctionStatusRecord>("SELECT * FROM ola_function_status WHERE function_key = @FunctionKey", new { FunctionKey = functionKey });
    }

    public List<OlaFunctionStatusRecord> GetAllOlaFunctionStatuses()
    {
        using var conn = CreateConnection();
        return conn.Query<OlaFunctionStatusRecord>("SELECT * FROM ola_function_status ORDER BY category, chinese_name").ToList();
    }

    #endregion

    #region Mapping Helpers

    private Workflow MapRowToWorkflow(dynamic row) => new()
    {
        Id = (string)row.id,
        Name = (string)row.name,
        Enabled = Convert.ToInt32(row.enabled) != 0,
        Variables = Deserialize<Dictionary<string, object?>>((string?)row.variables) ?? new(),
        Steps = Deserialize<List<WorkflowStep>>((string?)row.steps) ?? new(),
        LastRunTime = ParseDateTime((string?)row.last_run_time),
        LastResult = (string?)row.last_result,
        CreatedAt = ParseDateTimeNotNull((string)row.created_at),
        UpdatedAt = ParseDateTimeNotNull((string)row.updated_at)
    };

    private Asset MapRowToAsset(dynamic row) => new()
    {
        Id = (string)row.id,
        Name = (string)row.name,
        Type = (string)(row.type ?? "image"),
        FilePath = (string)(row.file_path ?? string.Empty),
        Description = (string?)row.description ?? string.Empty,
        CreatedAt = ParseDateTimeNotNull((string)row.created_at)
    };

    private Schedule MapRowToSchedule(dynamic row) => new()
    {
        Id = (string)row.id,
        WorkflowId = (string)row.workflow_id,
        RuleType = (string)(row.rule_type ?? "daily"),
        CronExpression = (string)(row.cron_expression ?? string.Empty),
        IntervalMinutes = Convert.ToInt32(row.interval_minutes ?? 60),
        Enabled = Convert.ToInt32(row.enabled) != 0,
        NextRunTime = ParseDateTime((string?)row.next_run_time),
        LastResult = (string?)row.last_result,
        CreatedAt = ParseDateTimeNotNull((string)row.created_at)
    };

    private RunRecord MapRowToRunRecord(dynamic row) => new()
    {
        Id = (string)row.id,
        WorkflowId = (string)row.workflow_id,
        WorkflowName = (string?)row.workflow_name,
        StartTime = ParseDateTimeNotNull((string)row.start_time),
        EndTime = ParseDateTimeNotNull((string)row.end_time),
        Success = Convert.ToInt32(row.success) != 0,
        FailedStepName = (string?)row.failed_step_name,
        ErrorMessage = (string?)row.error_message,
        ScreenshotPath = (string?)row.screenshot_path,
        StepRecords = Deserialize<List<RunStepRecord>>((string?)row.step_records_json) ?? new()
    };

    #endregion

    #region Serialization Helpers

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private static string? Serialize(object? value) =>
        value == null ? null : JsonSerializer.Serialize(value, JsonOpts);

    private static T? Deserialize<T>(string? json) =>
        string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json, JsonOpts);

    private static DateTime ParseDateTime(string? s) =>
        string.IsNullOrWhiteSpace(s) ? default : DateTime.Parse(s);

    private static DateTime ParseDateTimeNotNull(string s) =>
        DateTime.TryParse(s, out var dt) ? dt : DateTime.MinValue;

    #endregion
}
