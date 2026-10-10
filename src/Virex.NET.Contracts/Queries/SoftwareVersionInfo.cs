namespace Virex.NET.Contracts;

/// <summary>The software version reported by the addressed App, including its build suffix when present.</summary>
public sealed class SoftwareVersionInfo
{
    public string Version { get; set; } = string.Empty;
}
