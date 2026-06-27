namespace Virex.NET.Contracts;

public sealed class SystemStartRequest
{
    public string? Condition { get; set; }

    public string? RunMode { get; set; }
}
