using System.Text.Json;

namespace Virex.NET.Contracts;

/// <summary>Stop-specific receiver shape checks and typed SDK validation. Invalid JSON throws JsonException;
/// invalid typed input throws ArgumentException. Validation has no command effects. Returned requests own
/// their ID array, so callers cannot alter an admitted request by mutating the original array.</summary>
public static class StopPayloadJson
{
    private static readonly HashSet<string> Fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { "reason", "requireReset", "cancelStartRequestIds", "expectedAppId", "expectedInstanceId" };

    public static SystemStopRequest ReadObject(string? json, bool allowEmpty = false)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            if (allowEmpty) return new SystemStopRequest();
            throw new JsonException("A Stop JSON object is required.");
        }
        using var document = JsonDocument.Parse(json!);
        var root = document.RootElement;
        ValidateShape(root, Fields);
        var hasExpected = false;
        foreach (var field in root.EnumerateObject())
        {
            if (field.Name.Equals("requireReset", StringComparison.OrdinalIgnoreCase))
            {
                if (field.Value.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
                    throw new JsonException("requireReset must be a boolean.");
            }
            else if (field.Name.Equals("cancelStartRequestIds", StringComparison.OrdinalIgnoreCase))
            {
                if (field.Value.ValueKind != JsonValueKind.Array || field.Value.EnumerateArray().Any(x => x.ValueKind != JsonValueKind.String))
                    throw new JsonException("cancelStartRequestIds must be an array of strings.");
            }
            else
            {
                if (!field.Name.Equals("reason", StringComparison.OrdinalIgnoreCase)) hasExpected = true;
                if (field.Value.ValueKind is not JsonValueKind.String and not JsonValueKind.Null)
                    throw new JsonException("Stop text fields must be strings or null.");
            }
        }
        var request = ProtocolJson.Deserialize<SystemStopRequest>(json!)!;
        if (!request.RequireReset && hasExpected)
            throw new JsonException("Expected identity fields require recovery Stop.");
        try { return ValidateAndSnapshot(request); }
        catch (ArgumentException ex) { throw new JsonException(ex.Message, ex); }
    }

    public static SystemStopRequest ValidateAndSnapshot(SystemStopRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        var ids = request.CancelStartRequestIds?.ToArray();
        if (ids is null || ids.Length > 2 || ids.Any(string.IsNullOrWhiteSpace)
            || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
            throw new ArgumentException("CancelStartRequestIds must contain at most two unique nonblank exact IDs.", nameof(request));
        if (request.RequireReset)
        {
            if (string.IsNullOrWhiteSpace(request.ExpectedAppId) || string.IsNullOrWhiteSpace(request.ExpectedInstanceId))
                throw new ArgumentException("Recovery Stop requires a complete nonblank expected App/instance pair.", nameof(request));
        }
        else if (ids.Length != 0 || request.ExpectedAppId is not null || request.ExpectedInstanceId is not null)
            throw new ArgumentException("Recovery IDs or expected identity require RequireReset=true.", nameof(request));
        return new SystemStopRequest
        {
            Reason = request.Reason,
            RequireReset = request.RequireReset,
            CancelStartRequestIds = ids,
            ExpectedAppId = request.ExpectedAppId,
            ExpectedInstanceId = request.ExpectedInstanceId,
        };
    }

    /// <summary>Reads a successful recovery Stop proof. Missing/false proof, duplicate case aliases,
    /// wrong identity, recovery action or nonquiescent result state throw JsonException. Successful
    /// states are Ready, Uninitialized or Faulted, with the barrier still requiring Reset.
    /// This cannot establish that an older
    /// receiver supports the request; callers must check its capability before sending.</summary>
    public static CommandResponse ReadRecoveryResponse(string json, SystemStopRequest request)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new JsonException("Recovery Stop response must be an object.");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in root.EnumerateObject())
            if (!seen.Add(field.Name)) throw new JsonException("Duplicate recovery Stop response field.");
        foreach (var name in new[] { "accepted", "recoveryStopApplied", "command", "appId", "instanceId", "recoveryAction", "state" })
            if (!seen.Contains(name)) throw new JsonException("Missing recovery Stop proof field: " + name);
        var response = ProtocolJson.Deserialize<CommandResponse>(json)!;
        if (!response.Accepted || !response.RecoveryStopApplied || response.Command != "Stop"
            || !string.Equals(response.AppId, request.ExpectedAppId, StringComparison.Ordinal)
            || !string.Equals(response.InstanceId, request.ExpectedInstanceId, StringComparison.Ordinal)
            || response.RecoveryAction != RecoveryActions.Reset)
            throw new JsonException("Recovery Stop response does not prove the requested barrier and identity.");
        _ = QueryPayloadJson.ReadSystemStatus(root);
        if (response.State is not (SystemStates.Ready or SystemStates.Uninitialized or SystemStates.Faulted))
            throw new JsonException("Recovery Stop response does not prove quiescent work and joined cleanup.");
        return response;
    }

    /// <summary>TCP/MQTT Stop support ordinary Stop only. Recovery fields, including false/null fields,
    /// unknown keys and duplicate case aliases are rejected before deserialization or effects.</summary>
    public static void ValidateOrdinaryTransport(JsonElement root, bool mqtt)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "reason", mqtt ? "correlationId" : "type" };
        if (!mqtt) allowed.Add("requestId");
        ValidateShape(root, allowed);
        foreach (var field in root.EnumerateObject())
            if (field.Value.ValueKind is not JsonValueKind.String and not JsonValueKind.Null)
                throw new JsonException("Ordinary Stop transport fields must be strings or null.");
    }

    private static void ValidateShape(JsonElement root, HashSet<string> allowed)
    {
        if (root.ValueKind != JsonValueKind.Object) throw new JsonException("Stop must be a JSON object.");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in root.EnumerateObject())
            if (!allowed.Contains(field.Name) || !seen.Add(field.Name))
                throw new JsonException("Unknown or duplicate Stop field: " + field.Name);
    }
}
