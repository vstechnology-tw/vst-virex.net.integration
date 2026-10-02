namespace Virex.NET.Contracts;

/// <summary>Version 1 of the Integration result detail envelope, independent of file schema versions.</summary>
public sealed class ResultDetail
{
    public const int CurrentSchemaVersion = 1;
    public int SchemaVersion { get; set; }
    public string ResultId { get; set; } = string.Empty;
    public ResultSummary Summary { get; set; } = new ResultSummary();
    public ResultFinding[] Findings { get; set; } = [];
}

/// <summary>Public finding projection. Polygon coordinates are in the public product frame.</summary>
public sealed class ResultFinding
{
    public string FindingId { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public double? Score { get; set; }
    public ProductPoint[] ProductPolygon { get; set; } = [];
    public string[] DiagnosticImageIds { get; set; } = [];
}

public sealed class ProductPoint
{
    public double Xmm { get; set; }
    public double Ymm { get; set; }
}
