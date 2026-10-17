using System.Text.Json;

namespace Virex.NET.Contracts;

/// <summary>Strict scoped ingress. Unknown, duplicate or mismatched properties are rejected before dispatch.</summary>
public static class ScopedControlJson
{
    public static ScopedControlRequest ReadRequest(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Shape(root, "expectedTarget", "command", "payload");
        if (!root.TryGetProperty("expectedTarget", out var target)) throw new JsonException("expectedTarget is required.");
        Shape(target, "appId", "instanceId", "workspaceScope");
        RequireString(target, "appId"); RequireString(target, "instanceId");
        if (!target.TryGetProperty("workspaceScope", out var scope)) throw new JsonException("workspaceScope is required.");
        Shape(scope, "workspaceId", "registrationId", "activationId");
        foreach (var name in new[] { "workspaceId", "registrationId", "activationId" })
            if (!scope.TryGetProperty(name, out var id) || id.ValueKind != JsonValueKind.String || !Guid.TryParse(id.GetString(), out var value) || value == Guid.Empty)
                throw new JsonException("A complete Workspace identity is required.");
        RequireString(root, "command");
        var request = ProtocolJson.Deserialize<ScopedControlRequest>(json)!;
        // JsonElement must outlive the parsed document.
        request.Payload = root.TryGetProperty("payload", out var payload) ? payload.Clone() : null;
        Validate(request);
        return request;
    }

    public static void Validate(ScopedControlRequest request)
    {
        if (request is null || request.ExpectedTarget?.IsValid != true) throw new JsonException("A valid expectedTarget is required.");
        var payload = request.Payload;
        switch (request.Command)
        {
            case ScopedControlCommands.Initialize:
            case ScopedControlCommands.Deinitialize:
            case ScopedControlCommands.Reset:
                if (payload is { ValueKind: not JsonValueKind.Null }) Shape(payload.Value);
                break;
            case ScopedControlCommands.Start:
                Shape(Required(payload), "requestId", "condition", "runMode", "inspectionMode", "productInfo");
                RequireString(payload!.Value, "requestId");
                if (payload.Value.TryGetProperty("productInfo", out var product) && product.ValueKind != JsonValueKind.Null) ProductShape(product);
                _ = ReadPayload<SystemStartRequest>(request); break;
            case ScopedControlCommands.Stop:
                Shape(Required(payload), "reason");
                _ = ReadPayload<SystemStopRequest>(request); break;
            case ScopedControlCommands.SetOperationMode:
                Shape(Required(payload), "mode"); RequireString(payload!.Value, "mode");
                _ = ReadPayload<SetOperationModeRequest>(request); break;
            case ScopedControlCommands.SetProductInfo:
                ProductShape(Required(payload));
                _ = ReadPayload<ProductInfo>(request); break;
            default: throw new JsonException("Unknown scoped command.");
        }
    }

    public static T ReadPayload<T>(ScopedControlRequest request) =>
        CommandPayloadJson.ReadObject<T>(Required(request.Payload).GetRawText()) ?? throw new JsonException("A command payload is required.");

    private static void ProductShape(JsonElement product)
    {
        Shape(product, "additionalFields", "lotID", "waferID", "recipe", "slot", "foupID", "chamberID", "lotIDOr", "waferIDOr", "recipeOr", "slotOr", "foupIDOr", "chamberIDOr");
        if (product.TryGetProperty("additionalFields", out var fields) && fields.ValueKind != JsonValueKind.Null)
        {
            if (fields.ValueKind != JsonValueKind.Object) throw new JsonException("additionalFields must be an object.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in fields.EnumerateObject())
                if (!seen.Add(field.Name)) throw new JsonException("Duplicate additional field: " + field.Name);
        }
    }

    private static JsonElement Required(JsonElement? payload) => payload ?? throw new JsonException("A command payload is required.");
    private static void RequireString(JsonElement value, string name)
    {
        if (!value.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(property.GetString()))
            throw new JsonException(name + " must be a nonempty string.");
    }
    private static void Shape(JsonElement value, params string[] allowed)
    {
        if (value.ValueKind != JsonValueKind.Object) throw new JsonException("A JSON object is required.");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
            if (!allowed.Contains(property.Name, StringComparer.Ordinal) || !seen.Add(property.Name))
                throw new JsonException("Unknown or duplicate scoped property: " + property.Name);
    }
}
