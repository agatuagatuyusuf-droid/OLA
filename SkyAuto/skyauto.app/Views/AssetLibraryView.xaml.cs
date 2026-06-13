using System.Windows;
using System.Windows.Controls;
using SkyAuto.Core.Models;
using SkyAuto.Infrastructure.Storage;

namespace SkyAuto.App.Views;

public partial class AssetLibraryView : UserControl
{
    private readonly SqliteStore _store;
    private List<Asset> _assets = new();

    public AssetLibraryView(SqliteStore store)
    {
        InitializeComponent();
        _store = store;
        RefreshAssets();
        AssetGrid.SelectionChanged += OnSelectionChanged;
    }

    private void RefreshAssets()
    {
        var filterText = (TypeFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "全部";
        _assets = _store.GetAllAssets().ToList();

        if (filterText != "全部")
        {
            var typeKey = Asset.AssetTypeLabels.FirstOrDefault(kv => kv.Value == filterText).Key;
            if (!string.IsNullOrEmpty(typeKey))
                _assets = _assets.Where(a => a.Type == typeKey).ToList();
        }

        AssetGrid.ItemsSource = null;
        AssetGrid.ItemsSource = _assets;
        StatusText.Text = $"共{_assets.Count} 个素材";
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        AssetActions.Visibility = AssetGrid.SelectedItem != null ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnAddAssetClick(object? sender, RoutedEventArgs e)
    {
        var asset = new Asset();

        // Create a simple dialog for adding
        var win = new Window { Title = "添加素材", Width = 400, Height = 300 };
        var stack = new StackPanel { Margin = new Thickness(15) };

        AddField(stack, "名称:", out TextBox nameBox);
        AddComboBoxField(stack, "类型:", out ComboBox typeBox);
        typeBox.ItemsSource = Asset.AssetTypes;

        AddField(stack, "文件路径:", out TextBox pathBox);
        AddField(stack, "描述:", out TextBox descBox);

        var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 15, 0, 0) };
        bool saved = false;

        var saveBtn = new Button { Content = "保存", Width = 70 };
        btns.Children.Add(saveBtn);
        saveBtn.Click += (s2, e2) =>
        {
            asset.Name = nameBox.Text?.Trim() ?? "";
            asset.Type = typeBox.SelectedItem?.ToString() ?? "variable";
            asset.FilePath = pathBox.Text?.Trim() ?? "";
            asset.Description = descBox.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(asset.Name))
            {
                MessageBox.Show("请输入素材名称", "提示");
                return;
            }

            _store.SaveAsset(asset);
            saved = true;
            win.Close();
        };

        var cancelBtn = new Button { Content = "取消", Width = 70, Margin = new Thickness(8, 0, 0, 0) };
        cancelBtn.Click += (s2, e2) => win.Close();
        btns.Children.Add(cancelBtn);
        stack.Children.Add(btns);

        win.Content = stack;
        win.ShowDialog();

        if (saved) RefreshAssets();
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

    private void OnEditAssetClick(object? sender, RoutedEventArgs e)
    {
        if (AssetGrid.SelectedItem is not Asset asset) return;

        var win = new Window { Title = "编辑素材", Width = 400, Height = 300 };
        var stack = new StackPanel { Margin = new Thickness(15) };

        AddField(stack, "名称:", out TextBox nameBox);
        nameBox.Text = asset.Name;

        AddComboBoxField(stack, "类型:", out ComboBox typeBox);
        typeBox.ItemsSource = Asset.AssetTypes;
        typeBox.SelectedItem = asset.Type;

        AddField(stack, "文件路径:", out TextBox pathBox);
        pathBox.Text = asset.FilePath;

        AddField(stack, "描述:", out TextBox descBox);
        descBox.Text = asset.Description;

        var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 15, 0, 0) };
        bool saved = false;

        var saveBtn = new Button { Content = "保存", Width = 70 };
        btns.Children.Add(saveBtn);
        saveBtn.Click += (s2, e2) =>
        {
            asset.Name = nameBox.Text?.Trim() ?? "";
            asset.Type = typeBox.SelectedItem?.ToString() ?? "variable";
            asset.FilePath = pathBox.Text?.Trim() ?? "";
            asset.Description = descBox.Text?.Trim() ?? "";

            _store.SaveAsset(asset);
            saved = true;
            win.Close();
        };

        var cancelBtn = new Button { Content = "取消", Width = 70, Margin = new Thickness(8, 0, 0, 0) };
        cancelBtn.Click += (s2, e2) => win.Close();
        btns.Children.Add(cancelBtn);
        stack.Children.Add(btns);

        win.Content = stack;
        win.ShowDialog();

        if (saved) RefreshAssets();
    }

    private void OnDeleteAssetClick(object? sender, RoutedEventArgs e)
    {
        if (AssetGrid.SelectedItem is not Asset asset) return;

        var result = MessageBox.Show($"确定要删除素材 \"{asset.Name}\" 吗？", "确认删除", MessageBoxButton.YesNo);
        if (result != MessageBoxResult.Yes) return;

        _store.DeleteAsset(asset.Id);
        RefreshAssets();
    }
}
