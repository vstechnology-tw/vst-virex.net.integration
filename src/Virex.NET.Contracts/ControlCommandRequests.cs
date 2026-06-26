namespace Virex.NET.Contracts;

public static class ControlRunModes
{
    public const string SingleRun = "single";
    public const string Continue = "continue";

    public static bool TryNormalize(string? value, out string normalized)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            normalized = Continue;
            return true;
        }

        var trimmed = value!.Trim();
        if (string.Equals(trimmed, SingleRun, System.StringComparison.OrdinalIgnoreCase))
        {
            normalized = SingleRun;
            return true;
        }

        if (string.Equals(trimmed, Continue, System.StringComparison.OrdinalIgnoreCase))
        {
            normalized = Continue;
            return true;
        }

        normalized = string.Empty;
        return false;
    }
}

public sealed class ControlStartRequest
{
    public string? Condition { get; set; }

    public string? RunMode { get; set; }
}

public sealed class ControlStopRequest
{
    public string? Reason { get; set; }
}
