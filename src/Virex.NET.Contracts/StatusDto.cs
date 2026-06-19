namespace Virex.NET.Contracts;

public static class ProcessStates
{
    public const string Ready = "ready";
    public const string Capturing = "capturing";
    public const string Inspecting = "inspecting";
    public const string Saving = "saving";
}

public sealed class StatusDto
{
    public bool Initialized { get; set; }

    public string ProcessState { get; set; } = ProcessStates.Ready;

    public string Recipe { get; set; } = WaferInfo.Unknown;
}

public sealed class ErrorStatusDto
{
    public bool HasError { get; set; }

    public string? Message { get; set; }

    public bool Initialized { get; set; }

    public string ProcessState { get; set; } = ProcessStates.Ready;

    public string Recipe { get; set; } = WaferInfo.Unknown;
}

public sealed class ControlStatusDto
{
    public bool Initialized { get; set; }

    public string ProcessState { get; set; } = ProcessStates.Ready;

    public string Recipe { get; set; } = WaferInfo.Unknown;

    public string Message { get; set; } = string.Empty;
}
