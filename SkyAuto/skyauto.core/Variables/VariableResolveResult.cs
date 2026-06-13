namespace SkyAuto.Core.Variables;

public class VariableResolveResult
{
    public bool Success { get; set; }
    public string OriginalText { get; set; } = string.Empty;
    public string ResolvedText { get; set; } = string.Empty;
    public List<string> MissingKeys { get; set; } = new();
    public List<string> UsedKeys { get; set; } = new();
    public string? ErrorMessage { get; set; }
}
