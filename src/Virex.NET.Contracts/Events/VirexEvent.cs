namespace Virex.NET.Contracts;

public sealed class VirexEvent
{
    public string Type { get; set; } = string.Empty;

    public string RawJson { get; set; } = string.Empty;

    public SystemStatus? Status { get; set; }

    public ProductInfo? ProductInfo { get; set; }

    public ResultSummary? Result { get; set; }

    public ErrorInfo? Error { get; set; }

    public CommandResponse? CommandResponse { get; set; }
}
