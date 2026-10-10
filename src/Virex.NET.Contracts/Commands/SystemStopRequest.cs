namespace Virex.NET.Contracts;

public sealed class SystemStopRequest
{
    public string? Reason { get; set; }

    /// <summary>Install an atomic Start admission barrier. Only a successful explicit Reset releases it.</summary>
    public bool RequireReset { get; set; }

    /// <summary>Exact ordinal Start IDs to revoke for this App instance, including across Reset/Initialize.
    /// At most two nonblank, unique IDs. An empty set covers a fault with no known request.</summary>
    public string[] CancelStartRequestIds { get; set; } = Array.Empty<string>();

    public string? ExpectedAppId { get; set; }
    public string? ExpectedInstanceId { get; set; }
}
