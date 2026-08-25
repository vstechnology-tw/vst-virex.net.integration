using System.Text.Json;
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

    [Fact]
    public async Task ConcurrentFaultAndStartShareOneSerializedRecoveryTransition()
    {
        var session = new SimulatorSession();
        Assert.True((await session.InitializeAsync()).Accepted);
        Assert.True((await session.StartAsync(new SystemStartRequest
        {
            RunMode = ControlRunModes.Continue,
        })).Accepted);
        session.ConfigureDeinitializeFailures(1, "Camera close failed.");

        var firstFault = session.SimulateAcquisitionFaultAsync("Camera acquisition failed.");
        var joinedFault = session.SimulateAcquisitionFaultAsync("A later fault must not overwrite recovery.");
        var concurrentStart = session.StartAsync(new SystemStartRequest
        {
            RunMode = ControlRunModes.Continue,
        });

        Assert.Same(firstFault, joinedFault);
        Assert.False((await firstFault).Accepted);
        Assert.False((await concurrentStart).Accepted);
        Assert.Equal(SimulatorState.Deinitializing, session.State);
        Assert.Equal("Camera acquisition failed.", session.Status.RecoveryDetails);
    }
    [Fact]
    public async Task AcquisitionFaultMakesDeinitializeRetryableUntilCleanupSucceeds()
    {
        var session = new SimulatorSession();
        await session.InitializeAsync();
        await session.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.Continue });
        session.ConfigureDeinitializeFailures(1);

        var firstAttempt = await session.SimulateAcquisitionFaultAsync("camera disconnected");

        Assert.False(firstAttempt.Accepted);
        Assert.Equal(CommandErrorCodes.RequiresDeinitialize, firstAttempt.ErrorCode);
        Assert.Equal(RecoveryActions.Deinitialize, firstAttempt.RecoveryAction);
        Assert.Equal("Acquisition", firstAttempt.RecoverySource);
        Assert.Equal("Deinitializing", firstAttempt.RecoveryPhase);
        Assert.Equal("camera disconnected", firstAttempt.RecoveryDetails);
        Assert.NotNull(firstAttempt.RecoveryStartedAt);
        Assert.Equal(SystemStates.Deinitializing, firstAttempt.State);
        Assert.Equal(SystemStates.Deinitializing, session.Status.State);
        Assert.Equal(RecoveryActions.Deinitialize, session.Status.RecoveryAction);
        Assert.Equal("Acquisition", session.Status.RecoverySource);
        Assert.Equal("Deinitializing", session.Status.RecoveryPhase);
        Assert.Equal(CommandErrorCodes.RequiresDeinitialize, session.Status.ErrorCode);
        Assert.True(session.Error.HasError);
        Assert.Equal("camera disconnected", session.Error.Message);
        Assert.Equal(CommandErrorCodes.RequiresDeinitialize, session.Error.ErrorCode);
        Assert.Equal(RecoveryActions.Deinitialize, session.Error.RecoveryAction);

        var retry = await session.DeinitializeAsync();

        Assert.True(retry.Accepted);
        Assert.Equal(SystemStates.Uninitialized, retry.State);
        Assert.Equal(SystemStates.Uninitialized, session.Status.State);
        Assert.False(session.Error.HasError);
        Assert.Null(session.Error.RecoveryAction);
    }

    [Fact]
    public void RecoveryDetailsAreSanitizedBeforeTheyReachThePublicContract()
    {
        var session = new SimulatorSession();

        session.EmitError("native cleanup failed\npassword=secret C:\\recipes\\private.json");

        Assert.Equal("native cleanup failed password=[redacted] [path]", session.Error.Message);
        Assert.Equal("simulated_error", session.Error.ErrorCode);
        Assert.Null(session.Error.RecoveryAction);
        Assert.Null(session.Error.RecoveryDetails);
    }

    [Fact]
    public async Task ConcurrentDeinitializeCallsJoinOneRecoveryAttemptAndACompletedFailureCanRetry()
    {
        var session = new SimulatorSession();
        await session.InitializeAsync();
        session.ConfigureDeinitializeFailures(1, "native cleanup failed");
        var deinitializing = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        session.StatusChanged += (_, status) =>
        {
            if (status.State == SystemStates.Deinitializing)
                deinitializing.TrySetResult(null);
        };

        var first = session.DeinitializeAsync();
        await deinitializing.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var second = session.DeinitializeAsync();

        Assert.Same(first, second);
        var failed = await first;
        Assert.False(failed.Accepted);
        Assert.NotNull(failed.RecoveryStartedAt);
        Assert.Equal("native cleanup failed", failed.RecoveryDetails);
        Assert.Equal(SystemStates.Deinitializing, session.Status.State);

        var retry = await session.DeinitializeAsync();

        Assert.True(retry.Accepted);
        Assert.Equal(SystemStates.Uninitialized, retry.State);
        Assert.Null(retry.RecoveryAction);
        Assert.Null(retry.RecoveryStartedAt);
    }

    [Fact]
    public async Task SingleRunPublishesImageGrabbedBeforeResultAndPersistsArtifacts()
    {
        var root = Path.Combine(Path.GetTempPath(), "virex-image-grabbed-" + Guid.NewGuid().ToString("N"));

        try
        {
            var session = new SimulatorSession(root);
            var notifications = new List<string>();
            ImageGrabbedInfo? imageGrabbed = null;
            ResultSummary? result = null;

            session.ImageGrabbed += (_, value) =>
            {
                notifications.Add("imageGrabbed");
                imageGrabbed = value;
            };
            session.ResultCreated += (_, value) =>
            {
                notifications.Add("resultCreated");
                result = value;
            };

            await session.InitializeAsync();
            await session.SetProductInfoAsync(new ProductInfo
            {
                WaferID = "W01",
                LotID = "LOT-1",
                Recipe = "RCP-A",
                Slot = "1",
            });
            await session.StartAsync(new SystemStartRequest
            {
                Condition = "golden-sample",
                RunMode = ControlRunModes.SingleRun,
            });

            var completed = await session.RunCompletedAsync();

            Assert.True(completed.Accepted);
            Assert.Equal(SystemStates.Ready, session.Status.State);
            Assert.Equal(2, notifications.Count);
            Assert.Equal("imageGrabbed", notifications[0]);
            Assert.Equal("resultCreated", notifications[1]);
            Assert.NotNull(imageGrabbed);
            Assert.NotNull(result);
            Assert.Equal(result!.CaptureId, imageGrabbed!.CaptureId);
            Assert.False(string.IsNullOrWhiteSpace(imageGrabbed.Timestamp));
            Assert.True(Path.IsPathFullyQualified(result.ImagePath));
            Assert.True(File.Exists(result.ImagePath));
            Assert.True(File.Exists(result.PreviewImagePath));
            Assert.True(File.Exists(result.ResultPath));
            var imageBytes = File.ReadAllBytes(result.ImagePath);
            var previewBytes = File.ReadAllBytes(result.PreviewImagePath);
            Assert.Equal((byte)0x42, imageBytes[0]);
            Assert.Equal((byte)0x4D, imageBytes[1]);
            Assert.Equal((byte)0xFF, previewBytes[0]);
            Assert.Equal((byte)0xD8, previewBytes[1]);

            using var resultDocument = JsonDocument.Parse(File.ReadAllText(result.ResultPath));
            Assert.Equal(result.CaptureId, resultDocument.RootElement.GetProperty("captureId").GetString());
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ArtifactPersistenceFailureKeepsImageGrabbedAndSuppressesResult()
    {
        var root = Path.GetTempFileName();

        try
        {
            var session = new SimulatorSession(root);
            var imageGrabbedCount = 0;
            var resultCreatedCount = 0;
            ErrorInfo? error = null;

            session.ImageGrabbed += (_, _) => imageGrabbedCount++;
            session.ResultCreated += (_, _) => resultCreatedCount++;
            session.ErrorChanged += (_, value) => error = value;

            await session.InitializeAsync();
            await session.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.SingleRun });

            var completed = await session.RunCompletedAsync();

            Assert.True(completed.Accepted);
            Assert.Equal(SystemStates.Ready, session.Status.State);
            Assert.Equal(1, imageGrabbedCount);
            Assert.Equal(0, resultCreatedCount);
            Assert.NotNull(error);
            Assert.True(error!.HasError);
        }
        finally
        {
            if (File.Exists(root))
                File.Delete(root);
        }
    }
}
