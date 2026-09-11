using Virex.NET.Contracts;

namespace Virex.NET.Client;

/// <summary>A TCP or MQTT operation received an explicit negative response.</summary>
public sealed class VirexCommandException : Exception
{
    public VirexCommandException(CommandResponse response)
        : base(response?.Message ?? "The Virex command was rejected.")
    {
        Response = response ?? throw new ArgumentNullException(nameof(response));
    }

    public VirexCommandException(MqttCommandResponse response)
        : base(response?.Message ?? response?.CommandResponse?.Message ?? "The MQTT operation was rejected.")
    {
        MqttResponse = response ?? throw new ArgumentNullException(nameof(response));
        Response = response.CommandResponse;
    }

    public CommandResponse? Response { get; }
    public MqttCommandResponse? MqttResponse { get; }
    public string? ErrorCode => Response?.ErrorCode ?? MqttResponse?.ErrorCode;
}
