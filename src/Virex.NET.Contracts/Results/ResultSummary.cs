namespace Virex.NET.Contracts;

public sealed class ResultSummary
{
    public string ResultId { get; set; } = string.Empty;

    public string Timestamp { get; set; } = string.Empty;

    public string LotID { get; set; } = string.Empty;

    public string WaferID { get; set; } = string.Empty;

    public string Recipe { get; set; } = string.Empty;

    public string Slot { get; set; } = string.Empty;

    public string FoupID { get; set; } = string.Empty;

    public string ChamberID { get; set; } = string.Empty;

    public string Condition { get; set; } = string.Empty;

    public string OverallResult { get; set; } = string.Empty;

    public int DefectCount { get; set; }

    public string ImageRelativePath { get; set; } = string.Empty;

    public string ResultRelativePath { get; set; } = string.Empty;

    public string ImagePath { get; set; } = string.Empty;

    public string PreviewImagePath { get; set; } = string.Empty;

    public string ResultPath { get; set; } = string.Empty;
}
