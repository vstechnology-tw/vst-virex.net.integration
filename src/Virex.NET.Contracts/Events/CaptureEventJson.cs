using System.Globalization;
using System.Text.Json;

namespace Virex.NET.Contracts;

public static class CaptureEventJson
{
    public static CaptureReadyInfo ReadReady(string json)
    {
        Validate(json, false);
        return ProtocolJson.Deserialize<CaptureReadyInfo>(json)!;
    }

    public static CaptureCompletedInfo ReadCompleted(string json)
    {
        Validate(json, true);
        return ProtocolJson.Deserialize<CaptureCompletedInfo>(json)!;
    }

    private static void Validate(string json, bool completed)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new JsonException("Capture event must be an object.");
        foreach (var property in new[] { "jobId", "captureId", "timestamp" })
            if (!root.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
                throw new JsonException(property + " is required.");
        if (!DateTimeOffset.TryParse(root.GetProperty("timestamp").GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            throw new JsonException("Invalid capture timestamp.");
        if (!root.TryGetProperty("sourceIds", out var sources) || sources.ValueKind != JsonValueKind.Array || sources.GetArrayLength() == 0)
            throw new JsonException("Capture sources are required.");
        var unique = new HashSet<string>(StringComparer.Ordinal);
        foreach (var source in sources.EnumerateArray())
            if (source.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(source.GetString()) || !unique.Add(source.GetString()!))
                throw new JsonException("Capture sources must be unique, nonblank strings.");
        if (completed && (!root.TryGetProperty("imageCount", out var count) || count.ValueKind != JsonValueKind.Number || !count.TryGetInt32(out var images) || images < unique.Count))
            throw new JsonException("Completion requires at least one image per source.");
    }
}
