using System.Text.Json;

namespace Virex.NET.Contracts;

/// <summary>Validates Alarm catalog payloads without restricting producer names.</summary>
public static class AlarmJson
{
    public static AlarmCatalog ReadCatalog(JsonElement root)
    {
        RequiredString(root, "appId"); RequiredString(root, "instanceId");
        var timestamp = Property(root, "timestamp");
        if (timestamp.ValueKind != JsonValueKind.String || !timestamp.TryGetDateTimeOffset(out var time) || time == default)
            throw new JsonException("A valid Alarm catalog timestamp is required.");
        var sources = Property(root, "sources");
        if (sources.ValueKind != JsonValueKind.Array) throw new JsonException("An Alarm source array is required.");
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var source in sources.EnumerateArray())
        {
            if (!names.Add(RequiredString(source, "name"))) throw new JsonException("Duplicate Alarm producer name.");
            RequiredString(source, "source");
            if (!AlarmCategories.IsValid(RequiredString(source, "category"))) throw new JsonException("Invalid Alarm category.");
        }
        return ProtocolJson.Deserialize<AlarmCatalog>(root.GetRawText())!;
    }

    private static string RequiredString(JsonElement root, string name)
    {
        var value = Property(root, name);
        if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw new JsonException("A nonempty Alarm string is required: " + name);
        return value.GetString()!;
    }

    private static JsonElement Property(JsonElement root, string name)
    {
        if (root.ValueKind != JsonValueKind.Object) throw new JsonException("An Alarm object is required.");
        var found = false;
        var value = default(JsonElement);
        foreach (var property in root.EnumerateObject())
        {
            if (!string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) continue;
            if (found) throw new JsonException("Duplicate Alarm property: " + name);
            found = true; value = property.Value;
        }
        return found ? value : throw new JsonException("Required Alarm property is missing: " + name);
    }
}
