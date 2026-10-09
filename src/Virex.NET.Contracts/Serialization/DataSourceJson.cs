using System.Text.Json;

namespace Virex.NET.Contracts;

/// <summary>Shared JSON type rules. Null/undefined is a missing value, never a successful value.</summary>
public static class DataSourceJson
{
    public static DataSourceValue ReadValue(JsonElement root)
    {
        RequireIdentity(root);
        RequiredString(root, "name");
        RequiredString(root, "state");
        var kind = RequiredString(root, "kind");
        var type = RequiredString(root, "type");
        var value = Property(root, "value");
        if (!MatchesType(type, value)) throw new JsonException("Source value does not match its declared type.");
        if (kind == DataSourceKinds.Result)
        {
            RequiredString(root, "resultId"); RequiredString(root, "jobId"); RequiredString(root, "captureId");
        }
        else if (kind != DataSourceKinds.Live || HasProperty(root, "resultId"))
            throw new JsonException("Invalid source kind or live result identity.");
        foreach (var field in new[] { "requestId", "jobId", "captureId" })
            if (HasProperty(root, field)) RequiredString(root, field);
        var result = ProtocolJson.Deserialize<DataSourceValue>(root.GetRawText())!;
        result.Value = value.Clone();
        return result;
    }

    public static DataSourceCatalog ReadCatalog(JsonElement root)
    {
        RequireIdentity(root);
        var sources = Property(root, "sources");
        if (sources.ValueKind != JsonValueKind.Array) throw new JsonException("A source array is required.");
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var source in sources.EnumerateArray())
        {
            if (!names.Add(RequiredString(source, "name"))) throw new JsonException("Duplicate source name.");
            if (RequiredString(source, "kind") is not DataSourceKinds.Live and not DataSourceKinds.Result || !IsType(RequiredString(source, "type")))
                throw new JsonException("Invalid source kind or type.");
            if (RequiredString(source, "availability") is not DataSourceAvailability.Available and not DataSourceAvailability.NoCurrentResult
                and not DataSourceAvailability.Unavailable and not DataSourceAvailability.MissingValue)
                throw new JsonException("Invalid source availability.");
            if (RequiredString(source, "kind") == DataSourceKinds.Live && RequiredString(source, "availability") == DataSourceAvailability.NoCurrentResult)
                throw new JsonException("Live sources cannot depend on the current result.");
        }
        return ProtocolJson.Deserialize<DataSourceCatalog>(root.GetRawText())!;
    }

    public static ResultReference ReadResultReference(JsonElement root)
    {
        RequireIdentity(root);
        RequiredString(root, "jobId"); RequiredString(root, "captureId"); RequiredString(root, "resultId");
        if (HasProperty(root, "requestId")) RequiredString(root, "requestId");
        return ProtocolJson.Deserialize<ResultReference>(root.GetRawText())!;
    }

    private static void RequireIdentity(JsonElement root)
    {
        RequiredString(root, "appId"); RequiredString(root, "instanceId");
        var timestamp = Property(root, "timestamp");
        if (timestamp.ValueKind != JsonValueKind.String || !timestamp.TryGetDateTimeOffset(out var time) || time == default)
            throw new JsonException("A valid read timestamp is required.");
    }

    private static string RequiredString(JsonElement root, string name)
    {
        var value = Property(root, name);
        if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw new JsonException("A nonempty query string is required: " + name);
        return value.GetString()!;
    }

    private static JsonElement Property(JsonElement root, string name)
    {
        if (root.ValueKind != JsonValueKind.Object) throw new JsonException("A query object is required.");
        var found = false;
        var value = default(JsonElement);
        foreach (var candidate in root.EnumerateObject())
        {
            if (!string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase)) continue;
            if (found) throw new JsonException("Duplicate query property: " + name);
            found = true; value = candidate.Value;
        }
        return found ? value : throw new JsonException("Required query property is missing: " + name);
    }

    private static bool HasProperty(JsonElement root, string name) => root.EnumerateObject().Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    public static bool IsType(string type) => type is "string" or "boolean" or "integer" or "number" or "object" or "array";

    public static bool MatchesType(string type, JsonElement value) => type switch
    {
        "string" => value.ValueKind == JsonValueKind.String,
        "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
        "integer" => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out _),
        "number" => value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number) && !double.IsNaN(number) && !double.IsInfinity(number),
        "object" => value.ValueKind == JsonValueKind.Object,
        "array" => value.ValueKind == JsonValueKind.Array,
        _ => false,
    };
}
