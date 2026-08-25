using System.Text.Json;
using Virex.NET.Contracts;

namespace Virex.NET.Contracts.Tests;

public sealed class ProtocolContractTests
{
    [Fact]
    public void StatusFrameUsesStateOnlyEventShape()
    {
        var frame = TcpSocketEventFormatter.FormatStatus(new SystemStatus { State = SystemStates.Ready });

        using var doc = JsonDocument.Parse(frame);
        Assert.Equal("statusChanged", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal(SystemStates.Ready, doc.RootElement.GetProperty("state").GetString());
    }

    [Fact]
    public void RecoveryActionIsAnOptionalPublicContractField()
    {
        var statusJson = ProtocolJson.Serialize(new SystemStatus
        {
            State = SystemStates.Deinitializing,
            RecoveryAction = RecoveryActions.Deinitialize,
        });
        var responseJson = ProtocolJson.Serialize(new CommandResponse
        {
            Accepted = false,
            State = SystemStates.Deinitializing,
            Command = "Stop",
            ErrorCode = CommandErrorCodes.RequiresDeinitialize,
            RecoveryAction = RecoveryActions.Deinitialize,
            Message = "Deinitialize is required.",
        });

        using var status = JsonDocument.Parse(statusJson);
        using var response = JsonDocument.Parse(responseJson);
        Assert.Equal(RecoveryActions.Deinitialize, status.RootElement.GetProperty("recoveryAction").GetString());
        Assert.Equal(CommandErrorCodes.RequiresDeinitialize, response.RootElement.GetProperty("errorCode").GetString());
        Assert.Equal(RecoveryActions.Deinitialize, response.RootElement.GetProperty("recoveryAction").GetString());
    }

    [Fact]
    public void RecoveryProjectionFieldsAreSharedByStatusErrorAndCommands()
    {
        var startedAt = new DateTimeOffset(2026, 8, 7, 1, 2, 3, TimeSpan.Zero);
        var statusJson = ProtocolJson.Serialize(new SystemStatus
        {
            State = SystemStates.Deinitializing,
            RecoveryAction = RecoveryActions.Deinitialize,
            ErrorCode = CommandErrorCodes.RequiresDeinitialize,
            RecoveryStartedAt = startedAt,
            RecoverySource = "Cam01",
            RecoveryPhase = "Deinitializing",
            RecoveryDetails = "native close failed",
        });
        var errorJson = ProtocolJson.Serialize(new ErrorInfo
        {
            HasError = true,
            State = SystemStates.Deinitializing,
            ErrorCode = CommandErrorCodes.RequiresDeinitialize,
            RecoveryAction = RecoveryActions.Deinitialize,
            RecoveryStartedAt = startedAt,
            RecoverySource = "Cam01",
            RecoveryPhase = "Deinitializing",
            RecoveryDetails = "native close failed",
        });
        var responseJson = ProtocolJson.Serialize(new CommandResponse
        {
            Accepted = false,
            State = SystemStates.Deinitializing,
            Command = "Deinitialize",
            ErrorCode = CommandErrorCodes.RequiresDeinitialize,
            RecoveryAction = RecoveryActions.Deinitialize,
            RecoveryStartedAt = startedAt,
            RecoverySource = "Cam01",
            RecoveryPhase = "Deinitializing",
            RecoveryDetails = "native close failed",
        });

        using var status = JsonDocument.Parse(statusJson);
        using var error = JsonDocument.Parse(errorJson);
        using var response = JsonDocument.Parse(responseJson);
        foreach (var document in new[] { status, error, response })
        {
            Assert.Equal("Cam01", document.RootElement.GetProperty("recoverySource").GetString());
            Assert.Equal("Deinitializing", document.RootElement.GetProperty("recoveryPhase").GetString());
            Assert.Equal("native close failed", document.RootElement.GetProperty("recoveryDetails").GetString());
            Assert.NotEqual(JsonValueKind.Null, document.RootElement.GetProperty("recoveryStartedAt").ValueKind);
        }

        Assert.Equal(CommandErrorCodes.RequiresDeinitialize, error.RootElement.GetProperty("errorCode").GetString());
        Assert.Equal(CommandErrorCodes.RequiresDeinitialize, status.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public void ProductInfoParserAcceptsNumberOrStringSlot()
    {
        Assert.True(ProductInfoJsonParser.TryParse(
            """{"waferID":"W01","lotID":"LOT-1","recipe":"RCP-A","slot":1,"foupID":"CAR-A","chamberID":"CH-1"}""",
            out var numberSlot,
            out _));
        Assert.Equal("1", numberSlot.Slot);

        Assert.True(ProductInfoJsonParser.TryParse(
            """{"waferID":"W01","lotID":"LOT-1","recipe":"RCP-A","slot":"A01","foupID":"CAR-A","chamberID":"CH-1"}""",
            out var stringSlot,
            out _));
        Assert.Equal("A01", stringSlot.Slot);
    }

    [Fact]
    public void ProductInfoSerializesFixedPublicFields()
    {
        var json = ProtocolJson.Serialize(new ProductInfo
        {
            LotID = "LOT-001",
            WaferID = "W01",
            Recipe = "RCP-A",
            Slot = "1",
            FoupID = "FOUP-A",
            ChamberID = "CH-1",
        });

        using var doc = JsonDocument.Parse(json);
        Assert.Equal("LOT-001", doc.RootElement.GetProperty("lotID").GetString());
        Assert.Equal("W01", doc.RootElement.GetProperty("waferID").GetString());
        Assert.Equal("RCP-A", doc.RootElement.GetProperty("recipe").GetString());
        Assert.Equal("1", doc.RootElement.GetProperty("slot").GetString());
        Assert.Equal("FOUP-A", doc.RootElement.GetProperty("foupID").GetString());
        Assert.Equal("CH-1", doc.RootElement.GetProperty("chamberID").GetString());
    }

    [Fact]
    public void TcpMessageParserRoutesCommandsAndProductInfo()
    {
        Assert.True(TcpSocketMessageParser.TryParse("""{"type":"initialize"}""", out var initialize, out _));
        Assert.Equal("initialize", initialize.Type);

        Assert.True(TcpSocketMessageParser.TryParse("""{"type":"deinitialize"}""", out var deinitialize, out _));
        Assert.Equal("deinitialize", deinitialize.Type);

        Assert.True(TcpSocketMessageParser.TryParse("""{"type":"start"}""", out var start, out _));
        Assert.Equal("start", start.Type);
        Assert.Null(start.Condition);
        Assert.Equal(ControlRunModes.Continue, start.RunMode);

        Assert.True(TcpSocketMessageParser.TryParse(
            """{"type":"productInfo","waferID":"W01","lotID":"LOT-1","recipe":"RCP-A","slot":"1","foupID":"CAR-A","chamberID":"CH-1"}""",
            out var product,
            out _));
        Assert.Equal("productInfo", product.Type);
        Assert.Equal("W01", product.ProductInfo?.WaferID);
    }

    [Fact]
    public void TcpMessageParserRoutesRestEquivalentQueryFrames()
    {
        Assert.True(TcpSocketMessageParser.TryParse("""{"type":"status"}""", out var status, out _));
        Assert.Equal("status", status.Type);

        Assert.True(TcpSocketMessageParser.TryParse("""{"type":"error"}""", out var error, out _));
        Assert.Equal("error", error.Type);

        Assert.True(TcpSocketMessageParser.TryParse("""{"type":"getProductInfo"}""", out var productInfo, out _));
        Assert.Equal("getProductInfo", productInfo.Type);

        Assert.True(TcpSocketMessageParser.TryParse(
            """{"type":"results","lotID":"LOT-001","waferID":"W01","recipe":"RCP-A"}""",
            out var results,
            out _));
        Assert.Equal("results", results.Type);
        Assert.Equal("LOT-001", results.LotID);
        Assert.Equal("W01", results.WaferID);
        Assert.Equal("RCP-A", results.Recipe);
    }

    [Fact]
    public void MqttTopicsExposeRestEquivalentCommandTopics()
    {
        Assert.Equal("commands/status/get", MqttTopics.CommandStatusGet);
        Assert.Equal("commands/error/get", MqttTopics.CommandErrorGet);
        Assert.Equal("commands/product-info/get", MqttTopics.CommandProductInfoGet);
        Assert.Equal("commands/product-info/set", MqttTopics.CommandProductInfoSet);
        Assert.Equal("commands/system/initialize", MqttTopics.CommandSystemInitialize);
        Assert.Equal("commands/system/deinitialize", MqttTopics.CommandSystemDeinitialize);
        Assert.Equal("commands/system/start", MqttTopics.CommandSystemStart);
        Assert.Equal("commands/system/stop", MqttTopics.CommandSystemStop);
        Assert.Equal("commands/results/query", MqttTopics.CommandResultsQuery);
        Assert.Equal("virex/responses/abc-123", MqttTopics.ResponseTopic("virex", "abc-123"));
    }

    [Fact]
    public void EventParserReadsNewEventNames()
    {
        var json = TcpSocketEventFormatter.FormatStatus(new SystemStatus { State = SystemStates.Running });

        Assert.True(VirexEventParser.TryParse(json, out var value, out _));
        Assert.Equal("statusChanged", value.Type);
        Assert.Equal(SystemStates.Running, value.Status?.State);
    }

    [Fact]
    public void ResultEventIncludesStartCondition()
    {
        var frame = TcpSocketEventFormatter.FormatResult(new ResultSummary { Condition = "golden-sample" });

        using var doc = JsonDocument.Parse(frame);
        Assert.Equal("resultCreated", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("golden-sample", doc.RootElement.GetProperty("condition").GetString());
    }
    [Fact]
    public void RecoverySanitizerUsesOneBoundedPublicPolicy()
    {
        var sanitized = RecoveryMessageSanitizer.Sanitize(
            "native cleanup failed\npassword=secret C:\\recipes\\private.json /var/log/virex\u0001 at Driver.Close()");
        var bounded = RecoveryMessageSanitizer.Sanitize(new string('x', 600));

        Assert.Equal(
            "native cleanup failed password=[redacted] [path] [path]",
            sanitized);
        Assert.NotNull(bounded);
        Assert.Equal(512, bounded.Length);
        Assert.EndsWith("...", bounded, StringComparison.Ordinal);
    }

    [Fact]
    public void OlderClientShapeIgnoresAdditiveRecoveryFields()
    {
        var json = ProtocolJson.Serialize(new SystemStatus
        {
            State = SystemStates.Deinitializing,
            RecoveryAction = RecoveryActions.Deinitialize,
            RecoveryStartedAt = DateTimeOffset.Parse("2026-08-07T00:00:00Z"),
            RecoverySource = "Cam01",
            RecoveryPhase = "Deinitializing",
            RecoveryDetails = "native close failed",
        });

        var legacy = JsonSerializer.Deserialize<LegacySystemStatus>(json, ProtocolJson.Options);

        Assert.NotNull(legacy);
        Assert.Equal(SystemStates.Deinitializing, legacy.State);
    }

    private sealed class LegacySystemStatus
    {
        public string State { get; set; } = string.Empty;
    }

    [Fact]
    public void ImageGrabbedFrameIncludesMetadataWithoutPath()
    {
        var frame = TcpSocketEventFormatter.FormatImageGrabbed(new ImageGrabbedInfo
        {
            CaptureId = "capture-1",
            Timestamp = "2026-08-17T10:00:00.000+08:00",
            LotID = "LOT-001",
            WaferID = "W01",
            Recipe = "RCP-A",
            Slot = "1",
            FoupID = "FOUP-A",
            ChamberID = "CH-1",
        });

        using var doc = JsonDocument.Parse(frame);
        Assert.Equal("imageGrabbed", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("capture-1", doc.RootElement.GetProperty("captureId").GetString());
        Assert.False(doc.RootElement.TryGetProperty("imagePath", out _));
        Assert.False(doc.RootElement.TryGetProperty("resultPath", out _));

        Assert.True(VirexEventParser.TryParse(frame, out var value, out _));
        Assert.Equal("capture-1", value.ImageGrabbed?.CaptureId);
        Assert.Equal("W01", value.ImageGrabbed?.WaferID);
    }
}
