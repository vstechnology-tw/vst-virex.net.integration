namespace Virex.NET.Contracts;

public sealed class TcpSocketMessage
{
    public string Type { get; set; } = string.Empty;

    public string? Condition { get; set; }

    public string RunMode { get; set; } = ControlRunModes.Continue;

    public string? Reason { get; set; }

    public ProductInfo? ProductInfo { get; set; }

    public string RawJson { get; set; } = string.Empty;
}
