namespace Virex.NET.Contracts;

public sealed class SystemStartRequest
{
    /// <summary>Created before sending so racing TCP events can be correlated with REST acceptance.</summary>
    public string RequestId { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>Optional product context frozen atomically when Start is accepted; null retains current product behavior.</summary>
    public ProductInfo? ProductInfo { get; set; }

    public string? Condition { get; set; }

    public string? RunMode { get; set; }

    /// <summary>Null uses existing recipe behavior; otherwise use an InspectionModes constant.</summary>
    public string? InspectionMode { get; set; }
}
