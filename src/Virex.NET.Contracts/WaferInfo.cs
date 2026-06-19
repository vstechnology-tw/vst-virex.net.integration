namespace Virex.NET.Contracts;

public sealed class WaferInfo
{
    public const string Unknown = "-";

    public string LotId { get; set; } = string.Empty;

    public string WaferId { get; set; } = string.Empty;

    public string RecipeId { get; set; } = string.Empty;

    public string Slot { get; set; } = string.Empty;

    public string FoupId { get; set; } = string.Empty;

    public string ChamberId { get; set; } = string.Empty;

    public string LotIdOr => string.IsNullOrWhiteSpace(LotId) ? Unknown : LotId;

    public string WaferIdOr => string.IsNullOrWhiteSpace(WaferId) ? Unknown : WaferId;

    public string RecipeIdOr => string.IsNullOrWhiteSpace(RecipeId) ? Unknown : RecipeId;

    public string SlotOr => string.IsNullOrWhiteSpace(Slot) ? Unknown : Slot;

    public string FoupIdOr => string.IsNullOrWhiteSpace(FoupId) ? Unknown : FoupId;

    public string ChamberIdOr => string.IsNullOrWhiteSpace(ChamberId) ? Unknown : ChamberId;
}
