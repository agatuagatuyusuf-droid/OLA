namespace SkyAuto.Core.Ola;

public class OlaFunctionInfo
{
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<OlaParamInfo> Parameters { get; set; } = new();
}

public class OlaParamInfo
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = "string"; // string, int, bool, file, image_asset
    public string? DefaultValue { get; set; }
}
