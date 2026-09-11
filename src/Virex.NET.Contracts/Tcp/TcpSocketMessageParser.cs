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

            if (root.TryGetProperty("type", out var suppliedType) && suppliedType.ValueKind != JsonValueKind.String)
            {
                error = "TCP command type must be a string.";
                return false;
            }

            message.RequestId = ReadOptionalString(root, "requestId");
            var type = root.TryGetProperty("type", out var typeElement) &&
                       typeElement.ValueKind == JsonValueKind.String
                ? typeElement.GetString() ?? string.Empty
                : string.Empty;

            if (string.Equals(type, "initialize", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(type, "deinitialize", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(type, "start", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(type, "stop", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(type, "status", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(type, "error", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(type, "getProductInfo", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(type, "results", System.StringComparison.OrdinalIgnoreCase))
            {
                message.Type = NormalizeType(type);
                foreach (var property in new[] { "condition", "runMode", "reason", "lotID", "waferID", "recipe", "requestId" })
                {
                    if (root.TryGetProperty(property, out var element)
                        && element.ValueKind != JsonValueKind.String && element.ValueKind != JsonValueKind.Null)
                    {
                        error = property + " must be a string.";
                        return false;
                    }
                }
                message.Condition = ReadOptionalString(root, "condition");
                message.RunMode = ReadRunMode(root);
                message.Reason = ReadOptionalString(root, "reason");
                message.LotID = ReadOptionalString(root, "lotID");
                message.WaferID = ReadOptionalString(root, "waferID");
                message.Recipe = ReadOptionalString(root, "recipe");
                return true;
            }

            if (!string.IsNullOrWhiteSpace(type)
                && !string.Equals(type, "productInfo", System.StringComparison.OrdinalIgnoreCase)
                // SDK <= 2.2.2 used the event formatter for this one command.
                && !string.Equals(type, "productInfoChanged", System.StringComparison.OrdinalIgnoreCase))
            {
                error = "Unsupported TCP command type.";
                return false;
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
            : ReadOptionalString(root, "runMode")!;

    private static string NormalizeType(string type) =>
        string.Equals(type, "getProductInfo", System.StringComparison.OrdinalIgnoreCase)
            ? "getProductInfo"
            : type.ToLowerInvariant();
}
