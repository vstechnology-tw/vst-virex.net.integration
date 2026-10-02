namespace Virex.NET.Contracts;

public sealed class CaptureReadyInfo
{
    public string JobId { get; set; } = string.Empty;
    public string CaptureId { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public string[] SourceIds { get; set; } = Array.Empty<string>();
}

public sealed class CaptureCompletedInfo
{
    public string JobId { get; set; } = string.Empty;
    public string CaptureId { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public string[] SourceIds { get; set; } = Array.Empty<string>();
    public int ImageCount { get; set; }
}
