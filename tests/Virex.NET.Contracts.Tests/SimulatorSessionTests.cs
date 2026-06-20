using Virex.NET.Contracts;
using Virex.NET.Simulator.WPF.Services;

namespace Virex.NET.Contracts.Tests;

public sealed class SimulatorSessionTests
{
    [Fact]
    public async Task StartCycleBeforeInitializeReturnsNotInitialized()
    {
        var session = new SimulatorSession();

        var response = await session.StartCycleAsync("results");

        Assert.Equal(409, response.StatusCode);
        Assert.Equal("not_initialized", response.Body.Message);
        Assert.False(session.Status.Initialized);
        Assert.Equal(ProcessStates.Ready, session.Status.ProcessState);
    }

    [Fact]
    public void UpdateWaferInfoLogsAllWaferInfoFields()
    {
        var session = new SimulatorSession();
        var messages = new List<string>();
        session.Log += (_, message) => messages.Add(message);

        session.UpdateWaferInfo(
            new WaferInfo
            {
                LotId = "LOT-A",
                WaferId = "W07",
                RecipeId = "RCP-42",
                Slot = "7",
                FoupId = "FOUP-Z",
                ChamberId = "CH-B",
            },
            "TCP");

        Assert.Contains(
            "WaferInfo updated from TCP: lotId=LOT-A, waferId=W07, recipeId=RCP-42, slot=7, foupId=FOUP-Z, chamberId=CH-B",
            messages);
    }
}
