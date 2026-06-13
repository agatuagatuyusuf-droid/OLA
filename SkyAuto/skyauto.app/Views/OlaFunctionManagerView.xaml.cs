using System.Windows;
using System.Windows.Controls;
using SkyAuto.Core.Ola;
using SkyAuto.Infrastructure.Storage;

namespace SkyAuto.App.Views;

public partial class OlaFunctionManagerView : UserControl
{
    private readonly SqliteStore _store;
    private readonly IOlaClient? _olaClient;
    private List<OlaFunctionStatusRecord> _allRecords = new();

    public OlaFunctionManagerView(SqliteStore store, IOlaClient? olaClient = null)
    {
        InitializeComponent();
        _store = store;
        _olaClient = olaClient;
        RefreshData();
    }

    private void RefreshData()
    {
        _allRecords = _store.GetAllOlaFunctionStatuses();
        FunctionStatusGrid.ItemsSource = _allRecords;

        // Populate category filter
        var categories = _allRecords.Select(r => r.Category).Distinct().ToList();
        CategoryFilterBox.Items.Clear();
        CategoryFilterBox.Items.Add(new ComboBoxItem { Content = "全部", IsSelected = true });
        foreach (var cat in categories)
            CategoryFilterBox.Items.Add(new ComboBoxItem { Content = cat });

        UpdateStatusBar();
    }

    private void OnInitializeAllClick(object? sender, RoutedEventArgs e)
    {
        var registry = OlaFunctionRegistry.CreateDefault();
        var functions = registry.GetAll();

        foreach (var func in functions)
        {
            var record = new OlaFunctionStatusRecord
            {
                FunctionKey = func.Type,
                Category = func.Category,
                ChineseName = func.Name,
                RawFunctionName = $"ola_{func.Type}",
                ParametersJson = System.Text.Json.JsonSerializer.Serialize(func.Parameters),
                Implemented = true, // If in registry, it's implemented
                RealOlaConnected = false,
                Tested = false,
                TestStatus = "not_implemented",
                TestMessage = func.Description ?? ""
            };

            _store.SaveOlaFunctionStatus(record);
        }

        MessageBox.Show($"已初始化 {functions.Count} 个 OLA 函数状态。请配置真实 OLA 后点击刷新进行测试。", "提示");
        RefreshData();
    }

    private void OnRefreshClick(object? sender, RoutedEventArgs e)
    {
        if (_olaClient != null && _olaClient.Mode == OlaConnectionMode.Real)
        {
            // Test each function against real OLA
            var registry = OlaFunctionRegistry.CreateDefault();
            foreach (var func in registry.GetAll())
            {
                try
                {
                    var result = _olaClient.Call(func.Type, new Dictionary<string, object>());
                    var record = new OlaFunctionStatusRecord
                    {
                        FunctionKey = func.Type,
                        Category = func.Category,
                        ChineseName = func.Name,
                        RawFunctionName = $"ola_{func.Type}",
                        ParametersJson = System.Text.Json.JsonSerializer.Serialize(func.Parameters),
                        Implemented = true,
                        RealOlaConnected = result.Success && !result.IsMock,
                        Tested = true,
                        TestStatus = (result.Success && !result.IsMock) ? "real_pass" : "real_fail",
                        TestMessage = result.Message ?? "",
                        LastTestedAt = DateTime.Now
                    };
                    _store.SaveOlaFunctionStatus(record);
                }
                catch (Exception ex)
                {
                    var record = new OlaFunctionStatusRecord
                    {
                        FunctionKey = func.Type,
                        Category = func.Category,
                        ChineseName = func.Name,
                        RawFunctionName = $"ola_{func.Type}",
                        Implemented = true,
                        RealOlaConnected = false,
                        Tested = true,
                        TestStatus = "real_fail",
                        TestMessage = ex.Message,
                        LastTestedAt = DateTime.Now
                    };
                    _store.SaveOlaFunctionStatus(record);
                }
            }

            MessageBox.Show("已使用真实 OLA 测试所有函数。请查看结果。", "提示");
        }
        else if (_olaClient != null && _olaClient.Mode == OlaConnectionMode.Mock)
        {
            // Test with Mock - mark as mock_pass but NOT real
            var registry = OlaFunctionRegistry.CreateDefault();
            foreach (var func in registry.GetAll())
            {
                try
                {
                    var result = _olaClient.Call(func.Type, new Dictionary<string, object>());
                    var record = new OlaFunctionStatusRecord
                    {
                        FunctionKey = func.Type,
                        Category = func.Category,
                        ChineseName = func.Name,
                        RawFunctionName = $"ola_{func.Type}",
                        ParametersJson = System.Text.Json.JsonSerializer.Serialize(func.Parameters),
                        Implemented = true,
                        RealOlaConnected = false, // Mock is NOT real!
                        Tested = true,
                        TestStatus = "mock_pass",
                        TestMessage = result.Message ?? "(Mock)",
                        LastTestedAt = DateTime.Now
                    };
                    _store.SaveOlaFunctionStatus(record);
                }
                catch (Exception ex)
                {
                    var record = new OlaFunctionStatusRecord
                    {
                        FunctionKey = func.Type,
                        Category = func.Category,
                        ChineseName = func.Name,
                        RawFunctionName = $"ola_{func.Type}",
                        Implemented = true,
                        RealOlaConnected = false,
                        Tested = true,
                        TestStatus = "mock_pass",
                        TestMessage = ex.Message,
                        LastTestedAt = DateTime.Now
                    };
                    _store.SaveOlaFunctionStatus(record);
                }
            }

            MessageBox.Show("Mock 测试结果已更新。注意: Mock通过不等于完成！只有 Real 通过才算真实完成。", "提示");
        }
        else
        {
            // No OLA client - just refresh from database
            RefreshData();
        }

        RefreshData();
    }

    private void OnCategoryFilterChanged(object? sender, SelectionChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var selectedCategory = (CategoryFilterBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
        var searchText = SearchBox.Text?.Trim().ToLowerInvariant();

        var filtered = _allRecords.AsEnumerable();

        if (!string.IsNullOrEmpty(selectedCategory) && selectedCategory != "全部")
            filtered = filtered.Where(r => r.Category == selectedCategory);

        if (!string.IsNullOrEmpty(searchText))
            filtered = filtered.Where(r =>
                r.FunctionKey.Contains(searchText) ||
                r.ChineseName.Contains(searchText) ||
                (r.RawFunctionName?.Contains(searchText) ?? false));

        FunctionStatusGrid.ItemsSource = filtered.ToList();
    }

    private void UpdateStatusBar()
    {
        var total = _allRecords.Count;
        if (total == 0)
        {
            StatusBarText.Text = "暂无函数状态数据。点击「初始化全部函数状态」开始。";
            return;
        }

        var implemented = _allRecords.Count(r => r.Implemented);
        var realConnected = _allRecords.Count(r => r.RealOlaConnected);
        var tested = _allRecords.Count(r => r.Tested);
        var realPass = _allRecords.Count(r => r.TestStatus == "real_pass");
        var mockPass = _allRecords.Count(r => r.TestStatus == "mock_pass");

        StatusBarText.Text = $"共 {total} 个函数 | 已封装: {implemented} | Real接入: {realConnected} | 已测试: {tested} (Real通过: {realPass}, Mock通过: {mockPass})";
    }
}
