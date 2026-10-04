using Virex.NET.Contracts;
using Virex.NET.Simulator.Core;

namespace Virex.NET.Contracts.Tests;

public sealed class SimulatorShutdownTests
{
    [Theory]
    [InlineData(false, OperationModes.Local, ControlRunModes.SingleRun)]
    [InlineData(false, OperationModes.Remote, ControlRunModes.Continue)]
    [InlineData(true, OperationModes.Local, ControlRunModes.SingleRun)]
    [InlineData(true, OperationModes.Local, ControlRunModes.Continue)]
    [InlineData(true, OperationModes.Remote, ControlRunModes.SingleRun)]
    [InlineData(true, OperationModes.Remote, ControlRunModes.Continue)]
    public async Task HostShutdownJoinsRunsWithoutChangingModeOrCommandAuthority(bool managed, string mode, string runMode)
    {
        var root = Path.Combine(Path.GetTempPath(), "virex-host-shutdown-" + Guid.NewGuid().ToString("N"));
        var session = new SimulatorSession(root, managed);
        var captures = 0;
        session.CaptureCompleted += (_, _) => Interlocked.Increment(ref captures);
        try
        {
            Assert.True((await session.InitializeFromSourceAsync(OperationSource.Local)).Accepted);
            Assert.True((await session.SetOperationModeAsync(new SetOperationModeRequest { Mode = mode })).Accepted);
            var allowedSource = mode == OperationModes.Remote ? OperationSource.External : OperationSource.Local;
            if (managed)
            {
                var deniedSource = allowedSource == OperationSource.Local ? OperationSource.External : OperationSource.Local;
                Assert.Equal(CommandErrorCodes.OperationNotAllowed,
                    (await session.StartFromSourceAsync(new SystemStartRequest(), deniedSource)).ErrorCode);
            }

            Assert.True((await session.StartFromSourceAsync(new SystemStartRequest
            {
                RunMode = runMode,
                InspectionMode = InspectionModes.CaptureOnly,
            }, allowedSource)).Accepted);
            Assert.Equal(SimulatorState.Running, session.State);

            var shutdown = session.ShutdownAsync();
            Assert.Same(shutdown, session.ShutdownAsync());
            await shutdown.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(SimulatorState.Ready, session.State);
            Assert.Equal(mode, session.OperationMode.Mode);
            Assert.Equal(managed, session.OperationMode.ManagementEnabled);
            Assert.Empty(session.Results);
            var completedCaptures = Volatile.Read(ref captures);
            await Task.Delay(1100); // Cross the original one-second synthetic capture cadence.
            Assert.Equal(completedCaptures, Volatile.Read(ref captures));
            Assert.False(Directory.Exists(root));

            foreach (var source in new[] { OperationSource.Local, OperationSource.External })
            {
                Assert.Equal(CommandErrorCodes.InvalidState,
                    (await session.StartFromSourceAsync(new SystemStartRequest(), source)).ErrorCode);
                Assert.Equal(CommandErrorCodes.InvalidState,
                    (await session.InitializeFromSourceAsync(source)).ErrorCode);
            }
            Assert.Equal(CommandErrorCodes.InvalidState,
                (await session.SetOperationModeAsync(new SetOperationModeRequest { Mode = OperationModes.Local })).ErrorCode);
            Assert.Equal(mode, session.OperationMode.Mode);
        }
        finally
        {
            await session.ShutdownAsync();
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task HostShutdownWaitsForAdmittedInitializationAndRejectsQueuedStarts()
    {
        var session = new SimulatorSession(null, true);
        var initializing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        session.StatusChanged += (_, status) =>
        {
            if (status.State == SystemStates.Initializing) initializing.TrySetResult();
        };
        var initialization = session.InitializeFromSourceAsync(OperationSource.Local);
        await initializing.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var shutdown = session.ShutdownAsync();
        Assert.False(shutdown.IsCompleted);
        Assert.Equal(CommandErrorCodes.InvalidState,
            (await session.StartFromSourceAsync(new SystemStartRequest(), OperationSource.Local)).ErrorCode);
        Assert.True((await initialization).Accepted);
        await shutdown.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(SimulatorState.Ready, session.State);
        Assert.Same(shutdown, session.ShutdownAsync());
    }

    [Fact]
    public async Task HostShutdownPublishesItsTaskBeforeAReentrantStateCallback()
    {
        var session = new SimulatorSession(null, true);
        Assert.True((await session.InitializeFromSourceAsync(OperationSource.Local)).Accepted);
        Assert.True((await session.SetOperationModeAsync(new SetOperationModeRequest { Mode = OperationModes.Remote })).Accepted);
        Assert.True((await session.StartFromSourceAsync(new SystemStartRequest
        {
            RunMode = ControlRunModes.Continue, InspectionMode = InspectionModes.CaptureOnly,
        }, OperationSource.External)).Accepted);

        Task? reentrant = null;
        var readyEvents = 0;
        session.StatusChanged += (_, status) =>
        {
            if (status.State != SystemStates.Ready) return;
            Interlocked.Increment(ref readyEvents);
            reentrant = session.ShutdownAsync(); // The callback returns without waiting.
        };
        var captures = 0;
        session.CaptureCompleted += (_, _) => Interlocked.Increment(ref captures);
        var shutdown = session.ShutdownAsync();
        await shutdown.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.NotNull(reentrant);
        Assert.Same(shutdown, reentrant);
        Assert.Same(shutdown, session.ShutdownAsync());
        Assert.Equal(1, Volatile.Read(ref readyEvents));
        Assert.Equal(OperationModes.Remote, session.OperationMode.Mode);
        Assert.True(session.OperationMode.ManagementEnabled);
        Assert.Equal(SimulatorState.Ready, session.State);
        var captured = Volatile.Read(ref captures);
        await Task.Delay(1100);
        Assert.Equal(captured, Volatile.Read(ref captures));
        Assert.Empty(session.Results);
    }

    [Fact]
    public async Task HostShutdownRetainsAnEarlierRunFailureAfterAnotherStart()
    {
        var session = new SimulatorSession();
        var failure = new InvalidOperationException("A simulated capture subscriber failed.");
        await FaultContinuousRunAsync(session, failure);
        Assert.True((await session.StopAsync()).Accepted);
        Assert.True((await session.StartAsync(new SystemStartRequest
        {
            RunMode = ControlRunModes.Continue, InspectionMode = InspectionModes.CaptureOnly,
        })).Accepted);

        var shutdown = session.ShutdownAsync();
        var reported = await Assert.ThrowsAsync<InvalidOperationException>(() => shutdown.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Same(failure, reported);
        Assert.Same(shutdown, session.ShutdownAsync());
        Assert.Equal(SimulatorState.Ready, session.State);
        Assert.Empty(session.Results);
    }

    [Fact]
    public async Task HostShutdownPreservesBothStopAndRunFailures()
    {
        var session = new SimulatorSession();
        var runFailure = new InvalidOperationException("A simulated capture subscriber failed.");
        var stopFailure = new InvalidOperationException("A simulated shutdown subscriber failed.");
        await FaultContinuousRunAsync(session, runFailure);
        session.StatusChanged += (_, status) =>
        {
            if (status.State == SystemStates.Ready) throw stopFailure;
        };

        var shutdown = session.ShutdownAsync();
        var reported = await Assert.ThrowsAsync<AggregateException>(() => shutdown.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Contains(runFailure, reported.Flatten().InnerExceptions);
        Assert.Contains(stopFailure, reported.Flatten().InnerExceptions);
        Assert.Equal(2, reported.Flatten().InnerExceptions.Count);
        Assert.Same(shutdown, session.ShutdownAsync());
        Assert.Equal(SimulatorState.Ready, session.State);
        Assert.Empty(session.Results);
    }

    private static async Task FaultContinuousRunAsync(SimulatorSession session, Exception failure)
    {
        Assert.True((await session.InitializeAsync()).Accepted);
        EventHandler<CaptureCompletedInfo> failCapture = (_, _) => throw failure;
        session.CaptureCompleted += failCapture;
        try
        {
            Assert.True((await session.StartAsync(new SystemStartRequest
            {
                RunMode = ControlRunModes.Continue, InspectionMode = InspectionModes.CaptureOnly,
            })).Accepted);
            // Observe the real background task, so the following Start cannot race fault completion.
            var field = typeof(SimulatorSession).GetField("_runTasks", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            var run = Assert.Single((List<Task>)field.GetValue(session)!);
            var reported = await Assert.ThrowsAsync<InvalidOperationException>(() => run.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.Same(failure, reported);
        }
        finally { session.CaptureCompleted -= failCapture; }
    }
}
