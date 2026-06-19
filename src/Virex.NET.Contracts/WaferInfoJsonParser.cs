using System;
using System.Text.Json;

namespace Virex.NET.Contracts;

public static class WaferInfoJsonParser
{
    public static bool TryParse(string json, out WaferInfo info, out string error)
    {
        info = new WaferInfo();
        error = string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                error = "WaferInfo payload must be a JSON object.";
                return false;
            }

            var lotId = GetString(root, "lotId");
            var waferId = GetString(root, "waferId");
            var foupId = GetString(root, "foupId");
            var chamberId = GetString(root, "chamberId");
            var slot = GetSlot(root);

            if (string.IsNullOrWhiteSpace(lotId))
            {
                error = "lotId is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(waferId))
            {
                error = "waferId is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(slot))
            {
                error = "slot must be a non-empty string or number.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(foupId))
            {
                error = "foupId is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(chamberId))
            {
                error = "chamberId is required.";
                return false;
            }

            info = new WaferInfo
            {
                LotId = lotId,
                WaferId = waferId,
                RecipeId = GetString(root, "recipeId"),
                Slot = slot,
                FoupId = foupId,
                ChamberId = chamberId,
            };
            return true;
        }
        catch (JsonException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static string GetSlot(JsonElement root)
    {
        if (!root.TryGetProperty("slot", out var value))
            return string.Empty;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.ToString(),
            _ => string.Empty,
        };
    }
}
