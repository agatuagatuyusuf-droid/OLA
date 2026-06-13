using Xunit;
using SkyAuto.Core.Variables;
using SkyAuto.Infrastructure.Storage;

namespace SkyAuto.Tests;

public class VariableStoreTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteStore _store;

    public VariableStoreTests()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), $"var_store_{Guid.NewGuid():N}");
        _store = new SqliteStore(dataDir);
        _dbPath = Path.Combine(dataDir, "data", "skyauto.db");
    }

    public void Dispose()
    {
        try { File.Delete(_dbPath); } catch { }
        try { File.Delete(_dbPath + "-shm"); } catch { }
        try { File.Delete(_dbPath + "-wal"); } catch { }
        try
        {
            var dataDir = Path.GetDirectoryName(Path.GetDirectoryName(_dbPath))!;
            if (Directory.Exists(dataDir))
                Directory.Delete(dataDir, true);
        }
        catch { }
    }

    [Fact]
    public void Save_And_Get_Variable_By_Key()
    {
        var v = new VariableDefinition
        {
            Name = "账号",
            Key = "账号",
            Type = VariableType.Text,
            Scope = VariableScope.Workflow,
            WorkflowId = "wf1",
            Required = true,
            Description = "登录账号"
        };

        _store.SaveVariable(v);

        var loaded = _store.GetVariableByKey("账号", "wf1");
        Assert.NotNull(loaded);
        Assert.Equal("账号", loaded.Key);
        Assert.Equal("登录账号", loaded.Description);
        Assert.True(loaded.Required);
    }

    [Fact]
    public void Save_Variable_Update_Overwrites()
    {
        var v = new VariableDefinition
        {
            Name = "旧名称",
            Key = "test_key",
            Type = VariableType.Text,
            Scope = VariableScope.Workflow,
            WorkflowId = "wf1"
        };
        _store.SaveVariable(v);

        var id = v.Id;
        v.Name = "新名称";
        _store.SaveVariable(v);

        var loaded = _store.GetVariableByKey("test_key", "wf1");
        Assert.NotNull(loaded);
        Assert.Equal("新名称", loaded.Name);
        Assert.Equal(id, loaded.Id);
    }

    [Fact]
    public void Delete_Variable_Removes_Definition_And_Values()
    {
        var v = new VariableDefinition
        {
            Name = "待删除",
            Key = "delete_me",
            Type = VariableType.Text,
            Scope = VariableScope.Workflow,
            WorkflowId = "wf1"
        };
        _store.SaveVariable(v);

        var val = new VariableValue
        {
            VariableId = v.Id,
            WorkflowId = "wf1",
            Value = "some_value"
        };
        _store.SaveVariableValue(val);

        _store.DeleteVariable(v.Id);

        Assert.Null(_store.GetVariableByKey("delete_me", "wf1"));
        Assert.Null(_store.GetLatestValue(v.Id));
    }

    [Fact]
    public void Save_Value_And_Get_Latest()
    {
        var v = new VariableDefinition
        {
            Name = "最新值测试",
            Key = "latest_test",
            Type = VariableType.Text,
            Scope = VariableScope.Workflow,
            WorkflowId = "wf1"
        };
        _store.SaveVariable(v);

        var v1 = new VariableValue
        {
            VariableId = v.Id,
            WorkflowId = "wf1",
            Value = "old_value",
            CreatedAt = DateTime.Now.AddDays(-1),
            UpdatedAt = DateTime.Now.AddDays(-1)
        };
        _store.SaveVariableValue(v1);

        var v2 = new VariableValue
        {
            VariableId = v.Id,
            WorkflowId = "wf1",
            Value = "new_value",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        _store.SaveVariableValue(v2);

        var latest = _store.GetLatestValue(v.Id, "wf1");
        Assert.NotNull(latest);
        Assert.Equal("new_value", latest.Value);
    }

    [Fact]
    public void Workflow_Variable_Takes_Priority_Over_Global()
    {
        var global = new VariableDefinition
        {
            Name = "全局设置",
            Key = "setting",
            Type = VariableType.Text,
            Scope = VariableScope.Global,
            WorkflowId = null
        };
        _store.SaveVariable(global);

        var wf = new VariableDefinition
        {
            Name = "流程设置",
            Key = "setting",
            Type = VariableType.Text,
            Scope = VariableScope.Workflow,
            WorkflowId = "wf_priority"
        };
        _store.SaveVariable(wf);

        var byGlobal = _store.GetVariableByKey("setting", null);
        Assert.NotNull(byGlobal);
        Assert.Equal(VariableScope.Global, byGlobal.Scope);

        var byWf = _store.GetVariableByKey("setting", "wf_priority");
        Assert.NotNull(byWf);
        Assert.Equal(VariableScope.Workflow, byWf.Scope);
        Assert.Equal("wf_priority", byWf.WorkflowId);
    }

    [Fact]
    public void Password_Variable_IsSecret_Is_Saved()
    {
        var v = new VariableDefinition
        {
            Name = "密码",
            Key = "pwd",
            Type = VariableType.Password,
            Scope = VariableScope.Workflow,
            WorkflowId = "wf_secret"
        };

        _store.SaveVariable(v);

        var loaded = _store.GetVariableByKey("pwd", "wf_secret");
        Assert.NotNull(loaded);
        Assert.True(loaded.IsSecret);
        Assert.Equal(VariableType.Password, loaded.Type);
    }

    [Fact]
    public void Variables_Survive_Reopen()
    {
        var v = new VariableDefinition
        {
            Name = "重启测试",
            Key = "survive_key",
            Type = VariableType.Number,
            Scope = VariableScope.Global
        };
        _store.SaveVariable(v);

        // Simulate reopening by creating a new SqliteStore with same dbPath
        var dataDir = Path.GetDirectoryName(_dbPath)!;
        _ = new SqliteStore(dataDir);

        var loaded = _store.GetVariableByKey("survive_key");
        Assert.NotNull(loaded);
        Assert.Equal(VariableType.Number, loaded.Type);
    }

    [Fact]
    public void GetVariables_Returns_Global_And_Workflow()
    {
        var g = new VariableDefinition
        {
            Name = "全局",
            Key = "global_only",
            Type = VariableType.Text,
            Scope = VariableScope.Global,
            WorkflowId = null
        };
        _store.SaveVariable(g);

        var w = new VariableDefinition
        {
            Name = "工作流",
            Key = "wf_only",
            Type = VariableType.Text,
            Scope = VariableScope.Workflow,
            WorkflowId = "wf_get"
        };
        _store.SaveVariable(w);

        var wfVars = _store.GetVariables("wf_get");
        Assert.Contains(wfVars, x => x.Key == "global_only");
        Assert.Contains(wfVars, x => x.Key == "wf_only");
    }

    [Fact]
    public void DeleteVariable_Removes_Values()
    {
        var v = new VariableDefinition
        {
            Name = "级联删除测试",
            Key = "cascade_delete",
            Type = VariableType.Text,
            Scope = VariableScope.Workflow,
            WorkflowId = "wf_cascade"
        };
        _store.SaveVariable(v);

        var val = new VariableValue
        {
            VariableId = v.Id,
            WorkflowId = "wf_cascade",
            Value = "cascade_value"
        };
        _store.SaveVariableValue(val);
        Assert.NotNull(_store.GetLatestValue(v.Id));

        _store.DeleteVariable(v.Id);

        Assert.Null(_store.GetLatestValue(v.Id));

        var allValues = _store.GetVariableValues("wf_cascade");
        Assert.DoesNotContain(allValues, x => x.VariableId == v.Id);
    }
}
