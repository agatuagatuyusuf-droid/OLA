namespace SkyAuto.Core.Models;

public class Asset
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "image"; // image, ocr_region, color_point, file, account_var, variable
    public string FilePath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public static readonly string[] AssetTypes =
    [
        "image", "ocr_region", "color_point", "file", "account_var", "variable"
    ];

    public static readonly Dictionary<string, string> AssetTypeLabels = new()
    {
        ["image"] = "图片模板",
        ["ocr_region"] = "OCR 区域",
        ["color_point"] = "找色点",
        ["file"] = "文件",
        ["account_var"] = "账号变量",
        ["variable"] = "普通变量"
    };
}
