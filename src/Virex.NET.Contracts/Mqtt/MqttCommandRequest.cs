namespace Virex.NET.Contracts;

public sealed class MqttCommandRequest
{
    public string CorrelationId { get; set; } = string.Empty;

    public string? LotID { get; set; }

    public string? WaferID { get; set; }

    public string? Recipe { get; set; }

    public string? Condition { get; set; }

    public string? RunMode { get; set; }

    public string? Reason { get; set; }

    public ProductInfo? ProductInfo { get; set; }
}
