namespace Virex.NET.Contracts;

public sealed class ResultList
{
    public ResultSummary[] Items { get; set; } = [];

    public int Count { get; set; }
}
