using System.Text.Json;

namespace Virex.NET.Contracts;

public static class VirexEventParser
{
    public static bool TryParse(string json, out VirexEvent value, out string error)
    {
        value = new VirexEvent { RawJson = json };
        error = string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("type", out var typeElement) ||
                typeElement.ValueKind != JsonValueKind.String)
            {
                error = "Event type is required.";
                return false;
            }

            var type = typeElement.GetString() ?? string.Empty;
            value.Type = type;
            switch (type)
            {
                case "statusChanged":
                    value.Status = ProtocolJson.Deserialize<SystemStatus>(json);
                    break;
                case "productInfoChanged":
                    value.ProductInfo = ProtocolJson.Deserialize<ProductInfo>(json);
                    break;
                case "runStarted":
                case "runCompleted":
                    value.Status = ProtocolJson.Deserialize<SystemStatus>(json);
                    break;
                case "resultCreated":
                    value.Result = ProtocolJson.Deserialize<ResultSummary>(json);
                    break;
                case "errorChanged":
                    value.Error = ProtocolJson.Deserialize<ErrorInfo>(json);
                    break;
                case "commandRejected":
                    value.CommandResponse = ProtocolJson.Deserialize<CommandResponse>(json);
                    break;
                default:
                    error = "Unsupported event type.";
                    return false;
            }

            return true;
        }
        catch (JsonException ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
