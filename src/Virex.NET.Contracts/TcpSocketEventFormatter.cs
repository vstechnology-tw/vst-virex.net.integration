using System;

namespace Virex.NET.Contracts;

public static class TcpSocketEventFormatter
{
    public static string FormatStatus(StatusDto status) =>
        Format(new
        {
            type = "status",
            status.Initialized,
            status.ProcessState,
            status.Recipe,
        });

    public static string FormatWaferInfo(WaferInfo info) =>
        Format(new
        {
            type = "waferInfo",
            info.LotId,
            info.WaferId,
            info.RecipeId,
            info.Slot,
            info.FoupId,
            info.ChamberId,
        });

    public static string FormatResult(ResultSummaryDto summary) =>
        Format(new
        {
            type = "result",
            summary.ResultId,
            summary.Timestamp,
            summary.LotId,
            summary.WaferId,
            summary.RecipeId,
            summary.Slot,
            summary.FoupId,
            summary.ChamberId,
            summary.OverallResult,
            summary.DefectCount,
            summary.ImageRelativePath,
            summary.ResultRelativePath,
            summary.ImagePath,
            summary.PreviewImagePath,
            summary.ResultPath,
        });

    public static string FormatError(ErrorStatusDto error) =>
        Format(new
        {
            type = "error",
            message = error.Message,
            error.Initialized,
            error.ProcessState,
            error.Recipe,
            timestamp = DateTimeOffset.Now,
        });

    public static string FormatCommand(string type) => Format(new { type });

    private static string Format(object payload) => ProtocolJson.Serialize(payload) + "\n";
}
