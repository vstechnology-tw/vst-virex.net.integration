namespace Virex.NET.Contracts;

public sealed class ResultSummaryDto
{
    public string ResultId { get; set; } = string.Empty;

    public string Timestamp { get; set; } = string.Empty;

    public string LotId { get; set; } = string.Empty;

    public string WaferId { get; set; } = string.Empty;

    public string RecipeId { get; set; } = string.Empty;

    public string Slot { get; set; } = string.Empty;

    public string FoupId { get; set; } = string.Empty;

    public string ChamberId { get; set; } = string.Empty;

    public string OverallResult { get; set; } = string.Empty;

    public int DefectCount { get; set; }

    public int DieCount { get; set; }

    public string ImageRelativePath { get; set; } = string.Empty;

    public string ResultRelativePath { get; set; } = string.Empty;

    public string ImagePath { get; set; } = string.Empty;

    public string ResultPath { get; set; } = string.Empty;
}

public sealed class ResultListDto
{
    public ResultSummaryDto[] Items { get; set; } = [];

    public int Count { get; set; }
}
