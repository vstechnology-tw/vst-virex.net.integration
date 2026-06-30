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
}
