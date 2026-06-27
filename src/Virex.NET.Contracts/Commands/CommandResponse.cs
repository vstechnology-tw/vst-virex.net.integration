namespace Virex.NET.Contracts;

public sealed class CommandResponse
{
    public bool Accepted { get; set; }

    public string State { get; set; } = SystemStates.Uninitialized;

    public string Command { get; set; } = string.Empty;

    public string? ErrorCode { get; set; }

    public string Message { get; set; } = string.Empty;
}
