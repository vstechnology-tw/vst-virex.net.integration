namespace Virex.NET.Contracts;

public sealed class ImageGrabbedInfo
{
    public string CaptureId { get; set; } = string.Empty;

    public string Timestamp { get; set; } = string.Empty;

    public string LotID { get; set; } = string.Empty;

    public string WaferID { get; set; } = string.Empty;

    public string Recipe { get; set; } = string.Empty;

    public string Slot { get; set; } = string.Empty;

    public string FoupID { get; set; } = string.Empty;

    public string ChamberID { get; set; } = string.Empty;
}
