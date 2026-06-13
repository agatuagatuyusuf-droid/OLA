using Xunit;
using SkyAuto.Infrastructure.Storage;

namespace SkyAuto.Tests;

public class SqliteStoreSettingsTests
{
    private static SqliteStore CreateStore()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), $"settings_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dataDir);
        return new SqliteStore(dataDir);
    }

    private static string GetDataDir(SqliteStore store)
    {
        // Extract dataDir from connection string pattern "Data Source=<path>/data/skyauto.db"
        return Path.GetDirectoryName(Path.GetDirectoryName(store.ConnectionString.Replace("Data Source=", "")))!;
    }

    [Fact]
    public void SaveSetting_Then_GetSetting_Returns_Value()
    {
        var store = CreateStore();
        try
        {
            store.SaveSetting("test_key", "test_value");
            var value = store.GetSetting("test_key");
            Assert.Equal("test_value", value);
        }
        finally
        {
            try { Directory.Delete(GetDataDir(store), true); } catch { }
        }
    }

    [Fact]
    public void NonExistent_Key_Returns_Empty_String()
    {
        var store = CreateStore();
        try
        {
            var value = store.GetSetting("nonexistent_key");
            Assert.Equal("", value);
        }
        finally
        {
            try { Directory.Delete(GetDataDir(store), true); } catch { }
        }
    }

    [Fact]
    public void Overwrite_Same_Key_Updates_Value()
    {
        var store = CreateStore();
        try
        {
            store.SaveSetting("overwrite_key", "original");
            store.SaveSetting("overwrite_key", "updated");

            var value = store.GetSetting("overwrite_key");
            Assert.Equal("updated", value);
        }
        finally
        {
            try { Directory.Delete(GetDataDir(store), true); } catch { }
        }
    }

    [Fact]
    public void Reopen_Store_Can_Read_Previous_Settings()
    {
        var dataDir = Path.Combine(Path.GetTempPath(), $"settings_reopen_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dataDir);

        try
        {
            var store1 = new SqliteStore(dataDir);
            store1.SaveSetting("persist_key", "persist_value");
            store1.SaveSetting("ola_path", @"C:\OLA\ola.dll");
            // Dispose store1
            store1 = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Create new store pointing to same directory
            var store2 = new SqliteStore(dataDir);

            var persistValue = store2.GetSetting("persist_key");
            Assert.Equal("persist_value", persistValue);

            var olaPath = store2.GetSetting("ola_path");
            Assert.Equal(@"C:\OLA\ola.dll", olaPath);
        }
        finally
        {
            try { Directory.Delete(dataDir, true); } catch { }
        }
    }

    [Fact]
    public void Multiple_Different_Keys_All_Return_Correct_Values()
    {
        var store = CreateStore();
        try
        {
            store.SaveSetting("key1", "value1");
            store.SaveSetting("key2", "value2");
            store.SaveSetting("key3", "value3");

            Assert.Equal("value1", store.GetSetting("key1"));
            Assert.Equal("value2", store.GetSetting("key2"));
            Assert.Equal("value3", store.GetSetting("key3"));
        }
        finally
        {
            try { Directory.Delete(GetDataDir(store), true); } catch { }
        }
    }
}
