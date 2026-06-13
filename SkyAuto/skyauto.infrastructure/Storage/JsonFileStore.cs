using System.Text.Encodings.Web;
using System.Text.Json;
using SkyAuto.Core.Models;

namespace SkyAuto.Infrastructure.Storage;

public class JsonFileStore : IWorkflowStore, IAssetStore, IScheduleStore, IRunRecordStore
{
    private readonly string _dataDir;
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true, PropertyNamingPolicy = null, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, PropertyNameCaseInsensitive = true };

    public JsonFileStore(string dataDir)
    {
        _dataDir = dataDir;
        EnsureDirectories();
    }

    private void EnsureDirectories()
    {
        Directory.CreateDirectory(Path.Combine(_dataDir, "workflows"));
        Directory.CreateDirectory(Path.Combine(_dataDir, "assets"));
        Directory.CreateDirectory(Path.Combine(_dataDir, "schedules"));
        Directory.CreateDirectory(Path.Combine(_dataDir, "run_records"));
    }

    // === Workflows ===
    public List<Workflow> GetAllWorkflows() => LoadList<Workflow>("workflows");

    public Workflow? GetWorkflow(string id) => LoadSingle<Workflow>("workflows", id);

    public void SaveWorkflow(Workflow wf)
    {
        wf.UpdatedAt = DateTime.Now;
        SaveSingle("workflows", wf.Id, wf);
    }

    public void DeleteWorkflow(string id)
    {
        var path = GetFilePath("workflows", id + ".json");
        if (File.Exists(path)) File.Delete(path);
    }

    // === Assets ===
    public List<Asset> GetAllAssets() => LoadList<Asset>("assets");

    public Asset? GetAsset(string id) => LoadSingle<Asset>("assets", id);

    public void SaveAsset(Asset asset) => SaveSingle("assets", asset.Id, asset);

    public void DeleteAsset(string id)
    {
        var path = GetFilePath("assets", id + ".json");
        if (File.Exists(path)) File.Delete(path);
    }

    // === Schedules ===
    public List<Schedule> GetAllSchedules() => LoadList<Schedule>("schedules");

    public Schedule? GetSchedule(string id) => LoadSingle<Schedule>("schedules", id);

    public void SaveSchedule(Schedule schedule) => SaveSingle("schedules", schedule.Id, schedule);

    public void DeleteSchedule(string id)
    {
        var path = GetFilePath("schedules", id + ".json");
        if (File.Exists(path)) File.Delete(path);
    }

    // === Run Records ===
    public List<RunRecord> GetAllRunRecords() => LoadList<RunRecord>("run_records");

    public void SaveRunRecord(RunRecord record) => SaveSingle("run_records", record.Id, record);

    public List<RunRecord> GetRunRecordsForWorkflow(string workflowId)
        => LoadList<RunRecord>("run_records").Where(r => r.WorkflowId == workflowId).ToList();

    // === JSON Import/Export ===
    public string ExportWorkflowToJson(Workflow wf) => JsonSerializer.Serialize(wf, JsonOpts);

    public Workflow? ImportWorkflowFromJson(string json)
        => JsonSerializer.Deserialize<Workflow>(json, JsonOpts);

    // === Helpers ===
    private List<T> LoadList<T>(string subDir) where T : class
    {
        var dir = GetFilePath(subDir, "");
        if (!Directory.Exists(dir)) return new List<T>();

        var results = new List<T>();
        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var item = JsonSerializer.Deserialize<T>(json, JsonOpts);
                if (item != null) results.Add(item);
            }
            catch { /* skip invalid files */ }
        }
        return results;
    }

    private T? LoadSingle<T>(string subDir, string id) where T : class
    {
        var path = GetFilePath(subDir, id + ".json");
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, JsonOpts);
        }
        catch { return null; }
    }

    private void SaveSingle(string subDir, string id, object obj)
    {
        var path = GetFilePath(subDir, id + ".json");
        var json = JsonSerializer.Serialize(obj, JsonOpts);
        File.WriteAllText(path, json);
    }

    private string GetFilePath(string subDir, string fileName) => Path.Combine(_dataDir, subDir, fileName);
}

public interface IWorkflowStore
{
    List<Workflow> GetAllWorkflows();
    Workflow? GetWorkflow(string id);
    void SaveWorkflow(Workflow wf);
    void DeleteWorkflow(string id);
    string ExportWorkflowToJson(Workflow wf);
    Workflow? ImportWorkflowFromJson(string json);
}

public interface IAssetStore
{
    List<Asset> GetAllAssets();
    Asset? GetAsset(string id);
    void SaveAsset(Asset asset);
    void DeleteAsset(string id);
}

public interface IScheduleStore
{
    List<Schedule> GetAllSchedules();
    Schedule? GetSchedule(string id);
    void SaveSchedule(Schedule schedule);
    void DeleteSchedule(string id);
}

public interface IRunRecordStore
{
    List<RunRecord> GetAllRunRecords();
    void SaveRunRecord(RunRecord record);
    List<RunRecord> GetRunRecordsForWorkflow(string workflowId);
}
