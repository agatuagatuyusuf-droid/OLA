namespace SkyAuto.Core.Ola;

public class OlaRuntimeStatus
{
    public OlaConnectionMode Mode { get; set; } = OlaConnectionMode.NotConfigured;
    public string PluginPath { get; set; } = string.Empty;
    public bool Initialized { get; set; }
    public string InitError { get; set; } = string.Empty;
    public string MachineCode { get; set; } = string.Empty;
    public DateTime? LastConnectedAt { get; set; }
    public DateTime? LastDisconnecedAt { get; set; }

    public bool IsReal => Mode == OlaConnectionMode.Real && Initialized;
}
