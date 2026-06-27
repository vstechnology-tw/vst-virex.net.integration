using System.Text.Json;

namespace Virex.NET.Contracts;

public static class ProductInfoJsonParser
{
    public static bool TryParse(string json, out ProductInfo info, out string error)
    {
        info = new ProductInfo();
        error = string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                error = "ProductInfo frame must be a JSON object.";
                return false;
            }

            info.LotID = GetString(root, "lotID");
            info.WaferID = GetString(root, "waferID");
            info.Recipe = GetString(root, "recipe");
            info.Slot = GetString(root, "slot");
            info.FoupID = GetString(root, "foupID");
            info.ChamberID = GetString(root, "chamberID");
            return HasAny(info);
        }
        catch (JsonException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string GetString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
            return string.Empty;

        if (value.ValueKind == JsonValueKind.String)
            return value.GetString() ?? string.Empty;

        if (value.ValueKind == JsonValueKind.Number)
            return value.GetRawText();

        return string.Empty;
    }

    private static bool HasAny(ProductInfo info) =>
        !string.IsNullOrWhiteSpace(info.LotID) ||
        !string.IsNullOrWhiteSpace(info.WaferID) ||
        !string.IsNullOrWhiteSpace(info.Recipe) ||
        !string.IsNullOrWhiteSpace(info.Slot) ||
        !string.IsNullOrWhiteSpace(info.FoupID) ||
        !string.IsNullOrWhiteSpace(info.ChamberID);
}
