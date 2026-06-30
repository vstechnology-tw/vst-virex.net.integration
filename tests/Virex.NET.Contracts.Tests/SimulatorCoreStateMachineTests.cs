using Virex.NET.Contracts;
using Virex.NET.Simulator.Core;

namespace Virex.NET.Contracts.Tests;

public sealed class SimulatorCoreStateMachineTests
{
    [Fact]
    public async Task InitializeWaitsForCompletionAndReturnsReady()
    {
        var session = new SimulatorSession();

        var response = await session.InitializeAsync();

        Assert.True(response.Accepted);
        Assert.Equal(SystemStates.Ready, response.State);
        Assert.Equal(SystemStates.Ready, session.Status.State);
    }

    [Fact]
    public async Task StartReturnsRunningAndRunCompletedReturnsReady()
    {
        var session = new SimulatorSession();
        await session.InitializeAsync();
        await session.SetProductInfoAsync(new ProductInfo { WaferID = "W01", LotID = "LOT-1", Recipe = "RCP-A", Slot = "1" });

        var start = await session.StartAsync(new SystemStartRequest { Condition = "golden-sample", RunMode = ControlRunModes.SingleRun });

        Assert.True(start.Accepted);
        Assert.Equal(SystemStates.Running, start.State);

        var completed = await session.RunCompletedAsync();

        Assert.True(completed.Accepted);
        Assert.Equal(SystemStates.Ready, completed.State);
        Assert.Single(session.Results);
        Assert.Equal("W01", session.Results[0].WaferID);
        Assert.Equal("golden-sample", session.Results[0].Condition);
        Assert.EndsWith(".bmp", session.Results[0].ImageRelativePath);
        Assert.EndsWith(".bmp", session.Results[0].ImagePath);
    }

    [Fact]
    public async Task SingleRunCompletesAutomatically()
    {
        var session = new SimulatorSession();
        await session.InitializeAsync();
        await session.SetProductInfoAsync(new ProductInfo { WaferID = "W01", LotID = "LOT-1", Recipe = "RCP-A", Slot = "1" });

        var start = await session.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.SingleRun });

        Assert.True(start.Accepted);
        Assert.Equal(SystemStates.Running, start.State);

        await Task.Delay(TimeSpan.FromMilliseconds(1200));

        Assert.Equal(SystemStates.Ready, session.Status.State);
        Assert.Single(session.Results);
    }

    [Fact]
    public async Task ContinueRunStaysRunningAndEmitsResultsUntilStopped()
    {
        var session = new SimulatorSession();
        await session.InitializeAsync();
        await session.SetProductInfoAsync(new ProductInfo { WaferID = "W01", LotID = "LOT-1", Recipe = "RCP-A", Slot = "1" });

        var start = await session.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.Continue });

        Assert.True(start.Accepted);
        Assert.Equal(SystemStates.Running, start.State);

        await Task.Delay(TimeSpan.FromMilliseconds(2200));

        Assert.Equal(SystemStates.Running, session.Status.State);
        Assert.True(session.Results.Length >= 2);

        var stop = await session.StopAsync();

        Assert.True(stop.Accepted);
        Assert.Equal(SystemStates.Ready, session.Status.State);
    }

    [Fact]
    public async Task ProductInfoIsRejectedWhileRunning()
    {
        var session = new SimulatorSession();
        await session.InitializeAsync();
        await session.SetProductInfoAsync(new ProductInfo { WaferID = "W01" });
        await session.StartAsync(new SystemStartRequest());

        var response = await session.SetProductInfoAsync(new ProductInfo { WaferID = "W002" });

        Assert.False(response.Accepted);
        Assert.Equal(CommandErrorCodes.InvalidState, response.ErrorCode);
        Assert.Equal(SystemStates.Running, response.State);
    }

    [Fact]
    public async Task DeinitializeWaitsForCompletionAndReturnsUninitialized()
    {
        var session = new SimulatorSession();
        await session.InitializeAsync();

        var response = await session.DeinitializeAsync();

        Assert.True(response.Accepted);
        Assert.Equal(SystemStates.Uninitialized, response.State);
        Assert.Equal(SystemStates.Uninitialized, session.Status.State);
    }
}
