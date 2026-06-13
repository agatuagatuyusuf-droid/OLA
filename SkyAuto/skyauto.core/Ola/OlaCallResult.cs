namespace SkyAuto.Core.Ola;

public class OlaCallResult
{
    public bool Success { get; set; }
    public string FunctionKey { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string RawResult { get; set; } = string.Empty;
    public bool IsMock { get; set; }
}
