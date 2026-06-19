using System.Text.Json;

namespace Virex.NET.Contracts;

public sealed class TcpSocketMessage
{
    public string Type { get; set; } = string.Empty;

    public WaferInfo? WaferInfo { get; set; }

    public string RawJson { get; set; } = string.Empty;
}

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
                return true;
            }

            if (WaferInfoJsonParser.TryParse(line, out var info, out error))
            {
                message.Type = "waferInfo";
                message.WaferInfo = info;
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
}
