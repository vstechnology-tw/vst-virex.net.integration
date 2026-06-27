namespace Virex.NET.Contracts;

public static class TcpSocketEventFormatter
{
    public static string FormatStatus(SystemStatus status) =>
        Format(new { type = "statusChanged", status.State });

    public static string FormatProductInfo(ProductInfo info) =>
        Format(new
        {
            type = "productInfoChanged",
            info.LotID,
            info.WaferID,
            info.Recipe,
            info.Slot,
            info.FoupID,
            info.ChamberID,
        });

    public static string FormatRunStarted(SystemStatus status) =>
        Format(new { type = "runStarted", status.State });

    public static string FormatRunCompleted(SystemStatus status) =>
        Format(new { type = "runCompleted", status.State });

    public static string FormatResult(ResultSummary summary) =>
        Format(new
        {
            type = "resultCreated",
            summary.ResultId,
            summary.Timestamp,
            summary.LotID,
            summary.WaferID,
            summary.Recipe,
            summary.Slot,
            summary.FoupID,
            summary.ChamberID,
            summary.Condition,
            summary.OverallResult,
            summary.DefectCount,
            summary.ImageRelativePath,
            summary.ResultRelativePath,
            summary.ImagePath,
            summary.PreviewImagePath,
            summary.ResultPath,
        });

    public static string FormatError(ErrorInfo error) =>
        Format(new
        {
            type = "errorChanged",
            message = error.Message,
            error.State,
            timestamp = DateTimeOffset.Now,
        });

    public static string FormatCommandRejected(CommandResponse response) =>
        Format(new
        {
            type = "commandRejected",
            response.Accepted,
            response.State,
            response.Command,
            response.ErrorCode,
            response.Message,
        });

    public static string FormatCommand(string type) => Format(new { type });

    public static string FormatStartCommand(string? condition = null, string? runMode = null) =>
        Format(new
        {
            type = "start",
            condition = string.IsNullOrWhiteSpace(condition) ? null : condition,
            runMode = ControlRunModes.TryNormalize(runMode, out var normalizedRunMode)
                ? normalizedRunMode
                : runMode,
        });

    public static string FormatStopCommand(string? reason = null) =>
        Format(new
        {
            type = "stop",
            reason = string.IsNullOrWhiteSpace(reason) ? null : reason,
        });

    private static string Format(object payload) => ProtocolJson.Serialize(payload) + "\n";
}
