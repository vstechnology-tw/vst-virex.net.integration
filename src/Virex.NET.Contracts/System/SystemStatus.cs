namespace Virex.NET.Contracts;

public sealed class SystemStatus
{
    public string State { get; set; } = SystemStates.Uninitialized;
}
