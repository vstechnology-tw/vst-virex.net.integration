namespace Virex.NET.Contracts;

/// <summary>Exact producer names available through the ordinary event subscription API.</summary>
public sealed class AlarmCatalog
{
    public string AppId { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public AlarmSourceDescriptor[] Sources { get; set; } = [];
}

/// <summary>Name is the exact event name; Source identifies its producer. Neither is a customer alarm ID.</summary>
public sealed class AlarmSourceDescriptor
{
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

/// <summary>Behavior categories, independent of configurable producer names and customer mappings.</summary>
public static class AlarmCategories
{
    public const string Fault = "fault";
    public const string Defect = "defect";
    public const string CriticalDefect = "criticalDefect";
    public static bool IsValid(string category) => category is Fault or Defect or CriticalDefect;
}

/// <summary>An occurrence snapshot. Optional work identifiers are present only when known to the producer.</summary>
public sealed class AlarmNotification
{
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string State { get; set; } = SystemStates.Uninitialized;
    public DateTimeOffset Timestamp { get; set; }
    public string AppId { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public string? RequestId { get; set; }
    public string? JobId { get; set; }
    public string? CaptureId { get; set; }
    public string? ResultId { get; set; }
}
