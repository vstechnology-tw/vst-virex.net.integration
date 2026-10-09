namespace Virex.NET.Contracts;

public sealed class SystemStartRequest
{
    /// <summary>Created before sending so racing TCP events can be correlated with REST acceptance.</summary>
    public string RequestId { get; set; } = Guid.NewGuid().ToString("N");
    public string? Condition { get; set; }

    public string? RunMode { get; set; }

    /// <summary>Null uses existing recipe behavior; otherwise use an InspectionModes constant.</summary>
    public string? InspectionMode { get; set; }
}
