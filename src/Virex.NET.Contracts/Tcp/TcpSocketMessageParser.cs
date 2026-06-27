using System.Text.Json;

namespace Virex.NET.Contracts;

public static class TcpSocketMessageParser
{
    public static bool TryParse(string line, out TcpSocketMessage message, out string error)
    {
        message = new TcpSocketMessage { RawJson = line };
        error = string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                error = "TCP frame must be a JSON object.";
                return false;
            }

            var type = root.TryGetProperty("type", out var typeElement) &&
                       typeElement.ValueKind == JsonValueKind.String
                ? typeElement.GetString() ?? string.Empty
                : string.Empty;

            if (string.Equals(type, "start", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(type, "stop", System.StringComparison.OrdinalIgnoreCase))
            {
                message.Type = type.ToLowerInvariant();
                message.Condition = ReadOptionalString(root, "condition");
                message.RunMode = ReadRunMode(root);
                message.Reason = ReadOptionalString(root, "reason");
                return true;
            }

            if (ProductInfoJsonParser.TryParse(line, out var info, out error))
            {
                message.Type = "productInfo";
                message.ProductInfo = info;
                return true;
            }

            return false;
        }
        catch (JsonException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string? ReadOptionalString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
            return null;

        var text = value.GetString();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static string ReadRunMode(JsonElement root) =>
        ControlRunModes.TryNormalize(ReadOptionalString(root, "runMode"), out var runMode)
            ? runMode
            : string.Empty;
}
