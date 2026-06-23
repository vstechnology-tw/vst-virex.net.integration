namespace Virex.NET.Client;

public sealed class VirexClientOptions
{
    public string RestBaseUrl { get; set; } = "http://127.0.0.1:5088";

    public string TcpHost { get; set; } = "127.0.0.1";

    public int TcpPort { get; set; } = 5089;

    public string MqttHost { get; set; } = "127.0.0.1";

    public int MqttPort { get; set; } = 1883;

    public string MqttTopic { get; set; } = "virex";

    public int TimeoutMs { get; set; } = 5000;

    public int TcpFrameTimeoutMs { get; set; } = 5000;
}
