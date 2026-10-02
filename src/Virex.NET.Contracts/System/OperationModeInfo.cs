namespace Virex.NET.Contracts;

public static class OperationModes
{
    public const string Local = "local";
    public const string Remote = "remote";

    public static bool IsValid(string? mode) => mode is Local or Remote;
}

public sealed class OperationModeInfo
{
    public string Mode { get; set; } = OperationModes.Local;
    public bool ManagementEnabled { get; set; }
}

public sealed class SetOperationModeRequest
{
    public string? Mode { get; set; }
}
