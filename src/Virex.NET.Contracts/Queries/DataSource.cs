using System.Text.Json;

namespace Virex.NET.Contracts;

/// <summary>Exact source names supported by this App instance. Availability concerns the current work.</summary>
public sealed class DataSourceCatalog
{
    public string AppId { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public DataSourceDescriptor[] Sources { get; set; } = [];
}

public sealed class DataSourceDescriptor
{
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Availability { get; set; } = string.Empty;
}

/// <summary>Owned JSON snapshot. Timestamp is the read time; result values require an explicit ResultId.</summary>
public sealed class DataSourceValue
{
    public string AppId { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string? RequestId { get; set; }
    public string? JobId { get; set; }
    public string? CaptureId { get; set; }
    public string? ResultId { get; set; }
    public JsonElement Value { get; set; }
}

/// <summary>The committed current-work identity only. Pass ResultId explicitly when reading a result source.</summary>
public sealed class ResultReference
{
    public string AppId { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string? RequestId { get; set; }
    public string JobId { get; set; } = string.Empty;
    public string CaptureId { get; set; } = string.Empty;
    public string ResultId { get; set; } = string.Empty;
}

public static class DataSourceKinds
{
    public const string Live = "live";
    public const string Result = "result";
}

public static class DataSourceAvailability
{
    public const string Available = "available";
    public const string NoCurrentResult = "no_current_result";
    public const string Unavailable = "source_unavailable";
    public const string MissingValue = "source_value_missing";
}
