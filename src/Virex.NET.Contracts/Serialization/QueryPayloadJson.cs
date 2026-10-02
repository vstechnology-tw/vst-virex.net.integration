using System.Text.Json;

namespace Virex.NET.Contracts;

/// <summary>Reads new query payloads without replacing missing nested fields with DTO defaults.</summary>
public static class QueryPayloadJson
{
    public static RecipeParameters ReadRecipeParameters(JsonElement root)
    {
        RequireObject(root);
        RequireString(Property(root, "recipe"), nonempty: true);
        RequireString(Property(root, "revision"), nonempty: true);
        var groups = RequireArray(Property(root, "groups"));
        foreach (var group in groups.EnumerateArray())
        {
            RequireObject(group);
            RequireString(Property(group, "key"), nonempty: true);
            foreach (var parameter in RequireArray(Property(group, "parameters")).EnumerateArray())
            {
                RequireObject(parameter);
                RequireString(Property(parameter, "key"), nonempty: true);
                var type = RequireString(Property(parameter, "type"));
                var value = Property(parameter, "value");
                var valid = type switch
                {
                    "string" => value.ValueKind == JsonValueKind.String,
                    "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
                    "integer" => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out _),
                    "number" => value.ValueKind == JsonValueKind.Number,
                    _ => false,
                };
                if (!valid)
                    throw new JsonException("Parameter value must match its declared scalar type.");
            }
        }
        return ProtocolJson.Deserialize<RecipeParameters>(root.GetRawText())!;
    }

    public static ResultDetail ReadResultDetail(JsonElement root)
    {
        RequireObject(root);
        var version = Property(root, "schemaVersion");
        if (version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out _))
            throw new JsonException("schemaVersion must be an integer.");
        RequireString(Property(root, "resultId"), nonempty: true);
        var summary = Property(root, "summary");
        RequireObject(summary);
        RequireString(Property(summary, "resultId"), nonempty: true);
        foreach (var finding in RequireArray(Property(root, "findings")).EnumerateArray())
        {
            RequireObject(finding);
            RequireString(Property(finding, "findingId"), nonempty: true);
            RequireString(Property(finding, "kind"), nonempty: true);
            RequireString(Property(finding, "label"));
            if (TryProperty(finding, "score", out var score) && score.ValueKind != JsonValueKind.Null)
                RequireNumber(score);
            foreach (var point in RequireArray(Property(finding, "productPolygon")).EnumerateArray())
            {
                RequireObject(point);
                RequireNumber(Property(point, "xmm"));
                RequireNumber(Property(point, "ymm"));
            }
            foreach (var id in RequireArray(Property(finding, "diagnosticImageIds")).EnumerateArray())
                RequireString(id);
        }
        return ProtocolJson.Deserialize<ResultDetail>(root.GetRawText())!;
    }

    private static void RequireObject(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
            throw new JsonException("A query object is required.");
    }

    private static JsonElement RequireArray(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array)
            throw new JsonException("A query array is required.");
        return value;
    }

    private static string RequireString(JsonElement value, bool nonempty = false)
    {
        if (value.ValueKind != JsonValueKind.String || (nonempty && string.IsNullOrWhiteSpace(value.GetString())))
            throw new JsonException("A required query string is missing or invalid.");
        return value.GetString()!;
    }

    private static void RequireNumber(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var number) || double.IsNaN(number) || double.IsInfinity(number))
            throw new JsonException("A finite query number is required.");
    }

    private static JsonElement Property(JsonElement value, string name) =>
        TryProperty(value, name, out var property) ? property : throw new JsonException("Required query property is missing: " + name);

    private static bool TryProperty(JsonElement value, string name, out JsonElement property)
    {
        // Match ProtocolJson's case-insensitive, last-property-wins deserialization.
        property = default;
        var found = false;
        foreach (var candidate in value.EnumerateObject())
        {
            if (string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                property = candidate.Value;
                found = true;
            }
        }
        return found;
    }
}
