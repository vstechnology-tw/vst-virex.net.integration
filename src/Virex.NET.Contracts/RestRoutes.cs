namespace Virex.NET.Contracts;

public static class RestRoutes
{
    public const string Health = "/health";
    public const string OpenApiJson = "/openapi/v1.json";
    public const string Scalar = "/scalar";
    public const string ApiStatus = "/api/status";
    public const string ApiError = "/api/error";
    public const string ApiWaferInfo = "/api/wafer-info";
    public const string ApiControlInitialize = "/api/control/initialize";
    public const string ApiControlTerminate = "/api/control/terminate";
    public const string ApiControlStart = "/api/control/start";
    public const string ApiControlStop = "/api/control/stop";
    public const string ApiResults = "/api/results";
}

public static class MqttTopics
{
    public const string DefaultBaseTopic = "Virex.NET";
    public const string Status = "status";
    public const string WaferInfo = "wafer-info";
    public const string Result = "result";
    public const string Error = "error";

    public static string Combine(string baseTopic, string childTopic)
    {
        var root = string.IsNullOrWhiteSpace(baseTopic) ? DefaultBaseTopic : baseTopic.Trim();
        return root.TrimEnd('/') + "/" + childTopic.TrimStart('/');
    }
}
