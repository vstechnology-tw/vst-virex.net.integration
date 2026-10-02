namespace Virex.NET.Contracts;

/// <summary>Per-start inspection selection, independent of single/continue run mode.</summary>
public static class InspectionModes
{
    public const string CaptureOnly = "captureOnly";
    public const string CaptureAndInspect = "captureAndInspect";

    /// <summary>Null preserves the recipe's existing behavior; every supplied value must be canonical.</summary>
    public static bool IsValid(string? value) =>
        value is null || value == CaptureOnly || value == CaptureAndInspect;
}
