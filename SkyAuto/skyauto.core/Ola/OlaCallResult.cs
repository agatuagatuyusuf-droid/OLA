using System.Text.Json;

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
    public bool Verified { get; set; }
    public string VerifyMessage { get; set; } = string.Empty;
    public string RawResponse { get; set; } = string.Empty;
    public Dictionary<string, object> Evidence { get; set; } = new();
    public string? ScreenshotPath { get; set; }
    public bool NotVerified { get; set; }

    public string ToEvidenceJson() => JsonSerializer.Serialize(new
    {
        success = Success,
        functionKey = FunctionKey,
        message = Message,
        isMock = IsMock,
        verified = Verified,
        notVerified = NotVerified,
        verifyMessage = VerifyMessage,
        rawResponse = RawResponse,
        errorCode = ErrorCode,
        evidence = Evidence,
        screenshotPath = ScreenshotPath
    }, new JsonSerializerOptions { WriteIndented = true });
}
