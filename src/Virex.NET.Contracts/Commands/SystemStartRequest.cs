namespace Virex.NET.Contracts;

public sealed class SystemStartRequest
{
    public string? Condition { get; set; }

    public string? RunMode { get; set; }

    /// <summary>Null uses existing recipe behavior; otherwise use an InspectionModes constant.</summary>
    public string? InspectionMode { get; set; }
}
