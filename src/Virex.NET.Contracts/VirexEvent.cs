using System.Text.Json;

namespace Virex.NET.Contracts;

public sealed class VirexEvent
{
    public string Type { get; set; } = string.Empty;

    public string RawJson { get; set; } = string.Empty;

    public StatusDto? Status { get; set; }

    public WaferInfo? WaferInfo { get; set; }

    public ResultSummaryDto? Result { get; set; }

    public ErrorStatusDto? Error { get; set; }
}

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
                case "status":
                    value.Status = ProtocolJson.Deserialize<StatusDto>(json);
                    break;
                case "waferInfo":
                    value.WaferInfo = ProtocolJson.Deserialize<WaferInfo>(json);
                    break;
                case "result":
                    value.Result = ProtocolJson.Deserialize<ResultSummaryDto>(json);
                    break;
                case "error":
                    value.Error = ProtocolJson.Deserialize<ErrorStatusDto>(json);
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
