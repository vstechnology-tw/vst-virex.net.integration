using System.Text.Json;
using System.Text.Json.Nodes;

namespace Virex.NET.Contracts;

public static class CommandPayloadJson
{
    public static T? ReadObject<T>(string? json, bool allowEmpty = false)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            if (allowEmpty) return default;
            throw new JsonException("A JSON request body is required.");
        }
        using var document = JsonDocument.Parse(json!);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new JsonException("The request body must be a JSON object.");
        var value = ProtocolJson.Deserialize<T>(json!);
        var product = value is ProductInfo info ? info : (value as MqttCommandRequest)?.ProductInfo;
        if (product is not null && (product.LotID is null || product.WaferID is null || product.Recipe is null
            || product.Slot is null || product.FoupID is null || product.ChamberID is null))
            throw new JsonException("ProductInfo fields must be strings, not null.");
        return value;
    }

    public static string? TryReadCorrelationId(string? json) => TryReadString(json, "correlationId");
    public static string? TryReadRequestId(string? json) => TryReadString(json, "requestId");

    public static string WithRequestId(string frame, string? requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId)) return frame;
        var payload = JsonNode.Parse(frame)!.AsObject();
        payload["requestId"] = requestId;
        return payload.ToJsonString() + "\n";
    }

    private static string? TryReadString(string? json, string property)
    {
        try
        {
            using var document = JsonDocument.Parse(json ?? "{}");
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty(property, out var id)
                && id.ValueKind == JsonValueKind.String ? id.GetString() : null;
        }
        catch (JsonException) { return null; }
    }
}
