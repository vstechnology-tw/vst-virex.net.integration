namespace Virex.NET.Contracts;

public static class MqttTopics
{
    public const string DefaultBaseTopic = "virex";
    public const string Commands = "commands";
    public const string Responses = "responses";
    public const string StatusChanged = "statusChanged";
    public const string ProductInfoChanged = "productInfoChanged";
    public const string RunStarted = "runStarted";
    public const string RunCompleted = "runCompleted";
    public const string ResultCreated = "resultCreated";
    public const string ErrorChanged = "errorChanged";
    public const string CommandRejected = "commandRejected";
    public const string CommandStatusGet = "commands/status/get";
    public const string CommandErrorGet = "commands/error/get";
    public const string CommandProductInfoGet = "commands/product-info/get";
    public const string CommandProductInfoSet = "commands/product-info/set";
    public const string CommandSystemInitialize = "commands/system/initialize";
    public const string CommandSystemDeinitialize = "commands/system/deinitialize";
    public const string CommandSystemStart = "commands/system/start";
    public const string CommandSystemStop = "commands/system/stop";
    public const string CommandResultsQuery = "commands/results/query";

    public static string Combine(string baseTopic, string childTopic)
    {
        var root = string.IsNullOrWhiteSpace(baseTopic) ? DefaultBaseTopic : baseTopic.Trim();
        return root.TrimEnd('/') + "/" + childTopic.TrimStart('/');
    }

    public static string ResponseTopic(string baseTopic, string correlationId) =>
        Combine(baseTopic, Responses + "/" + Uri.EscapeDataString(correlationId));
}
