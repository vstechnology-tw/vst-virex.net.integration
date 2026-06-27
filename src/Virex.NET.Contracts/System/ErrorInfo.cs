namespace Virex.NET.Contracts;

public sealed class ErrorInfo
{
    public bool HasError { get; set; }

    public string? Message { get; set; }

    public string State { get; set; } = SystemStates.Uninitialized;
}
