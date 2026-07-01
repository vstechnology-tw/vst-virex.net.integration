namespace Virex.NET.Contracts;

public sealed class MqttCommandResponse
{
    public string CorrelationId { get; set; } = string.Empty;

    public string Topic { get; set; } = string.Empty;

    public bool Accepted { get; set; } = true;

    public string? ErrorCode { get; set; }

    public string? Message { get; set; }

    public SystemStatus? Status { get; set; }

    public ErrorInfo? Error { get; set; }

    public ProductInfo? ProductInfo { get; set; }

    public CommandResponse? CommandResponse { get; set; }

    public ResultList? Results { get; set; }
}
