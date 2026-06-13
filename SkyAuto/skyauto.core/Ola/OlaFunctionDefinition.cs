namespace SkyAuto.Core.Ola;

public class OlaFunctionDefinition
{
    public string FunctionKey { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ChineseName { get; set; } = string.Empty;
    public string RawFunctionName { get; set; } = string.Empty; // e.g., ola_find_image
    public List<OlaFunctionParameterDefinition> Parameters { get; set; } = new();
    public bool Implemented { get; set; }
    public bool RealOlaConnected { get; set; }
}

public class OlaFunctionParameterDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public string? DefaultValue { get; set; }
}

public class OlaFunctionTestResult
{
    public string FunctionKey { get; set; } = string.Empty;
    public DateTime TestedAt { get; set; }
    public bool Success { get; set; }
    public string Status { get; set; } = "pending"; // pending, mock_pass, real_pass, real_fail
    public string Message { get; set; } = string.Empty;
    public bool IsMock { get; set; }
}

public class OlaFunctionStatusRecord
{
    public string FunctionKey { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ChineseName { get; set; } = string.Empty;
    public string RawFunctionName { get; set; } = string.Empty;
    public string ParametersJson { get; set; } = "[]";
    public bool Implemented { get; set; }
    public bool RealOlaConnected { get; set; }
    public bool Tested { get; set; }
    public string TestStatus { get; set; } = "pending"; // pending, mock_pass, real_pass, real_fail, not_implemented
    public string? TestMessage { get; set; }
    public DateTime? LastTestedAt { get; set; }
}
