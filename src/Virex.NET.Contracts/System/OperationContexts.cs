namespace Virex.NET.Contracts;

/// <summary>Accepted Workspace operation context, separate from the public control mode.</summary>
public static class OperationContexts
{
    public const string Local = "Local";
    public const string Remote = "Remote";

    public static bool IsValid(string? value) => value is Local or Remote;
}
