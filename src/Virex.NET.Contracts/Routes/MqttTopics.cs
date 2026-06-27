namespace Virex.NET.Contracts;

public static class MqttTopics
{
    public const string DefaultBaseTopic = "virex";
    public const string StatusChanged = "statusChanged";
    public const string ProductInfoChanged = "productInfoChanged";
    public const string RunStarted = "runStarted";
    public const string RunCompleted = "runCompleted";
    public const string ResultCreated = "resultCreated";
    public const string ErrorChanged = "errorChanged";
    public const string CommandRejected = "commandRejected";

    public static string Combine(string baseTopic, string childTopic)
    {
        var root = string.IsNullOrWhiteSpace(baseTopic) ? DefaultBaseTopic : baseTopic.Trim();
        return root.TrimEnd('/') + "/" + childTopic.TrimStart('/');
    }
}
