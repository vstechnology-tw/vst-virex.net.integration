namespace Virex.NET.Contracts;

public sealed class ProductInfo
{
    public const string Unknown = "UNKNOWN";

    public string LotID { get; set; } = Unknown;

    public string WaferID { get; set; } = Unknown;

    public string Recipe { get; set; } = Unknown;

    public string Slot { get; set; } = Unknown;

    public string FoupID { get; set; } = Unknown;

    public string ChamberID { get; set; } = Unknown;

    /// <summary>Optional application-defined string fields. Missing and empty values remain distinct.</summary>
    public IReadOnlyDictionary<string, string>? AdditionalFields { get; set; }

    public string LotIDOr => string.IsNullOrWhiteSpace(LotID) ? Unknown : LotID;

    public string WaferIDOr => string.IsNullOrWhiteSpace(WaferID) ? Unknown : WaferID;

    public string RecipeOr => string.IsNullOrWhiteSpace(Recipe) ? Unknown : Recipe;

    public string SlotOr => string.IsNullOrWhiteSpace(Slot) ? Unknown : Slot;

    public string FoupIDOr => string.IsNullOrWhiteSpace(FoupID) ? Unknown : FoupID;

    public string ChamberIDOr => string.IsNullOrWhiteSpace(ChamberID) ? Unknown : ChamberID;

    public ProductInfo Snapshot() =>
        new ProductInfo
        {
            LotID = LotID,
            WaferID = WaferID,
            Recipe = Recipe,
            Slot = Slot,
            FoupID = FoupID,
            ChamberID = ChamberID,
            AdditionalFields = AdditionalFields is null ? null : new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(
                AdditionalFields.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)),
        };
}
