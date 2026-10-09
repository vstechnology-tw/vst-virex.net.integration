using System.Text.Json;

namespace Virex.NET.Contracts;

public static class EventEnvelopeJson
{
    public static bool TryParse(string json, out EventEnvelope value, out string error)
    {
        value = new EventEnvelope();
        error = string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw new JsonException("Event must be an object.");
            foreach (var field in new[] { "type", "name", "eventId", "appId", "instanceId", "connectionGeneration", "timestamp" })
                if (!root.TryGetProperty(field, out var item) || item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString()))
                    throw new JsonException("Event field is required: " + field);
            foreach (var field in new[] { "requestId", "jobId", "captureId", "resultId" })
                if (root.TryGetProperty(field, out var item) && (item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString())))
                    throw new JsonException("Invalid event correlation: " + field);
            if (root.GetProperty("type").GetString() != "event" || !root.GetProperty("timestamp").TryGetDateTimeOffset(out _)
                || !root.TryGetProperty("payload", out var payload) || payload.ValueKind != JsonValueKind.Object)
                throw new JsonException("Invalid event type, timestamp or object payload.");
            value = ProtocolJson.Deserialize<EventEnvelope>(json)!;
            return true;
        }
        catch (JsonException ex) { error = ex.Message; return false; }
    }
}
