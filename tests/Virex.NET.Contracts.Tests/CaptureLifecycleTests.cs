using System.Collections.Concurrent;
using System.Text.Json;
using Virex.NET.Contracts;
using Virex.NET.Simulator.Core;

namespace Virex.NET.Contracts.Tests;

public sealed class CaptureLifecycleTests
{
    [Fact]
    public void SourceCallbacksRequireAllReadyExplicitEndsAndRejectDuplicatesAndLateWork()
    {
        var sources = new[] { "a", "b" };
        var cycle = new SimulatorCaptureCycle("job", "capture", sources);
        sources[0] = "mutated";
        Assert.True(cycle.MarkSourceReady("a", out var ready));
        Assert.Null(ready);
        Assert.False(cycle.MarkSourceReady("a", out ready));
        Assert.Null(ready);
        Assert.False(cycle.AddImage(Image("job", "capture", "a", "1")));
        Assert.False(cycle.MarkSourceReady("unknown", out ready));
        Assert.True(cycle.MarkSourceReady("b", out ready));
        Assert.Equal(new[] { "a", "b" }, ready!.SourceIds);
        ready.SourceIds[0] = "changed";
        Assert.True(cycle.AddImage(Image("job", "capture", "a", "1")));
        Assert.False(cycle.AddImage(Image("job", "capture", "a", "1")));
        Assert.True(cycle.AddImage(Image("job", "capture", "a", "2")));
        Assert.True(cycle.CompleteSource("job", "capture", "a", out var completed));
        Assert.Null(completed);
        Assert.False(cycle.CompleteSource("job", "capture", "a", out completed));
        Assert.False(cycle.AddImage(Image("job", "capture", "a", "3")));
        Assert.False(cycle.CompleteSource("job", "capture", "b", out completed));
        Assert.True(cycle.AddImage(Image("job", "capture", "b", "1")));
        Assert.True(cycle.AddImage(Image("job", "capture", "b", "2")));
        Assert.False(cycle.CompleteSource("old-job", "capture", "b", out completed));
        Assert.True(cycle.CompleteSource("job", "capture", "b", out completed));
        Assert.Equal(4, completed!.ImageCount);
        Assert.Equal(new[] { "a", "b" }, completed.SourceIds);
        Assert.False(cycle.CompleteSource("job", "capture", "b", out completed));
        Assert.Null(completed);
        Assert.False(cycle.AddImage(Image("job", "capture", "b", "late")));
        var next = new SimulatorCaptureCycle("new-job", "new-capture", ["a", "b"]);
        next.MarkSourceReady("a", out _); next.MarkSourceReady("b", out _);
        Assert.False(next.AddImage(Image("job", "capture", "a", "late")));
        Assert.False(next.CompleteSource("job", "capture", "b", out completed));
        Assert.Null(completed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PreparationFailureOrPartialCancellationCannotProduceReadinessOrCompletion(bool prepared)
    {
        var cycle = new SimulatorCaptureCycle("job", "capture", ["a", "b"]);
        cycle.MarkSourceReady("a", out var ready);
        Assert.Null(ready);
        if (prepared)
        {
            cycle.MarkSourceReady("b", out ready);
            Assert.NotNull(ready);
            Assert.True(cycle.AddImage(Image("job", "capture", "a", "1")));
            Assert.True(cycle.CompleteSource("job", "capture", "a", out var partial));
            Assert.Null(partial);
        }
        cycle.Cancel();
        Assert.False(cycle.MarkSourceReady("b", out ready));
        Assert.Null(ready);
        Assert.False(cycle.AddImage(Image("job", "capture", "b", "late")));
        Assert.False(cycle.CompleteSource("job", "capture", "b", out var completed));
        Assert.Null(completed);
    }

    [Theory]
    [InlineData("tcp")]
    [InlineData("mqtt")]
    public async Task SdkObservesReadyAllImagesCompletionThenResultForOneJob(string transport)
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        await fixture.Session.ConfigureCaptureSimulationAsync(["a", "b"], framesPerSource: 2);
        await fixture.Client.InitializeAsync();
        var observed = new ConcurrentQueue<VirexEvent>();
        var resultReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var captureOnlyReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var captureOnly = false;
        var connected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnEvent(object? sender, VirexEvent value)
        {
            if (value.OperationMode is not null) connected.TrySetResult();
            if (value.Type is "captureReady" or "imageGrabbed" or "captureCompleted" or "resultCreated") observed.Enqueue(value);
            if (value.Result is not null) resultReceived.TrySetResult();
            if (value.CaptureCompleted is not null && captureOnly) captureOnlyReceived.TrySetResult();
        }
        fixture.Client.TcpEvents.EventReceived += OnEvent;
        fixture.Client.MqttEvents.EventReceived += OnEvent;
        using var stop = new CancellationTokenSource();
        var watcher = transport == "tcp" ? fixture.Client.TcpEvents.RunAsync(stop.Token) : fixture.Client.MqttEvents.RunAsync(stop.Token);
        try
        {
            for (var attempt = 0; attempt < 30 && !connected.Task.IsCompleted; attempt++)
            {
                await fixture.Client.SetOperationModeAsync(attempt % 2 == 0 ? OperationModes.Remote : OperationModes.Local);
                await Task.WhenAny(connected.Task, Task.Delay(100));
            }
            await connected.Task.WaitAsync(TimeSpan.FromSeconds(3));
            var resultsAtCompletion = new List<int>();
            fixture.Session.CaptureCompleted += (_, _) => resultsAtCompletion.Add(fixture.Session.Results.Length);
            var start = await fixture.Client.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.Continue });
            await fixture.Session.RunCompletedAsync();
            await resultReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var events = observed.ToArray();
            Assert.Equal(new[] { "captureReady", "imageGrabbed", "imageGrabbed", "imageGrabbed", "imageGrabbed", "captureCompleted", "resultCreated" }, events.Select(x => x.Type));
            var ready = events[0].CaptureReady!;
            Assert.Equal(start.JobId, ready.JobId);
            Assert.Equal(start.CaptureId, ready.CaptureId);
            Assert.Equal(new[] { "a", "b" }, ready.SourceIds);
            foreach (var image in events.Where(x => x.ImageGrabbed is not null).Select(x => x.ImageGrabbed!))
            {
                Assert.Equal(ready.JobId, image.JobId);
                Assert.Equal(ready.CaptureId, image.CaptureId);
                Assert.NotNull(image.FrameId);
                Assert.Contains(image.SourceId, ready.SourceIds);
            }
            Assert.Equal(ready.JobId, events[5].CaptureCompleted!.JobId);
            Assert.Equal(4, events[5].CaptureCompleted!.ImageCount);
            Assert.Equal(ready.JobId, events[6].Result!.JobId);
            Assert.Equal(ready.CaptureId, events[6].Result!.CaptureId);
            Assert.Equal(0, Assert.Single(resultsAtCompletion));
            observed.Clear();
            captureOnly = true;
            Assert.True((await fixture.Client.MqttCommands.StartWithOptionsAsync(new SystemStartRequest { InspectionMode = InspectionModes.CaptureOnly, RunMode = ControlRunModes.Continue })).Accepted);
            await fixture.Session.RunCompletedAsync();
            await captureOnlyReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var onlyEvents = observed.ToArray();
            Assert.Equal(6, onlyEvents.Length);
            Assert.Equal("captureCompleted", onlyEvents[^1].Type);
            Assert.NotEqual(ready.JobId, onlyEvents[0].CaptureReady!.JobId);
            Assert.NotEqual(ready.CaptureId, onlyEvents[0].CaptureReady!.CaptureId);
            Assert.Single(fixture.Session.Results);
        }
        finally
        {
            stop.Cancel();
            try { await watcher; } catch (OperationCanceledException) { }
        }
    }

    [Fact]
    public async Task ContinuousCapturesKeepOneJobAndDistinctCapturesUntilStop()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        await fixture.Session.InitializeAsync();
        await fixture.Session.ConfigureCaptureSimulationAsync(["a", "b"]);
        var complete = new List<CaptureCompletedInfo>();
        var twice = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Session.CaptureCompleted += (_, value) =>
        {
            complete.Add(value);
            if (complete.Count == 2) twice.TrySetResult();
        };
        var start = await fixture.Client.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.Continue });
        await twice.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await fixture.Client.StopAsync();
        Assert.Equal(2, complete.Count);
        Assert.All(complete, value => Assert.Equal(start.JobId, value.JobId));
        Assert.Equal(2, complete.Select(x => x.CaptureId).Distinct().Count());
        Assert.Equal(start.CaptureId, complete[0].CaptureId);
        Assert.Equal(2, fixture.Session.Results.Length);
        await Task.Delay(1200);
        Assert.Equal(2, complete.Count);
        Assert.Equal(SimulatorState.Ready, fixture.Session.State);
    }

    [Theory]
    [InlineData("rest")]
    [InlineData("tcp")]
    [InlineData("mqtt")]
    public async Task PreparationFailureRejectsStartWithoutScanPermission(string transport)
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        await fixture.Session.InitializeAsync();
        await fixture.Session.ConfigureCaptureSimulationAsync(["a", "b"], failPreparation: true);
        var ready = 0;
        var completed = 0;
        fixture.Session.CaptureReady += (_, _) => ready++;
        fixture.Session.CaptureCompleted += (_, _) => completed++;
        var rejected = await fixture.StartAsync(transport);
        Assert.False(rejected.Accepted);
        Assert.Equal(CommandErrorCodes.CapturePreparationFailed, rejected.ErrorCode);
        Assert.Equal(SimulatorState.Ready, fixture.Session.State);
        Assert.Equal(0, ready);
        Assert.Equal(0, completed);
        Assert.Empty(fixture.Session.Results);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelOrAcquisitionFaultInvalidatesOldCaptureAndDoesNotCompleteIt(bool fault)
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        await fixture.Session.InitializeAsync();
        await fixture.Session.ConfigureCaptureSimulationAsync(["a", "b"], 2);
        var ready = new List<CaptureReadyInfo>();
        var completed = new List<CaptureCompletedInfo>();
        fixture.Session.CaptureReady += (_, value) => ready.Add(value);
        fixture.Session.CaptureCompleted += (_, value) => completed.Add(value);
        var first = await fixture.Client.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.Continue });
        if (fault)
        {
            await fixture.Session.SimulateAcquisitionFaultAsync("simulated source fault");
            await fixture.Session.InitializeAsync();
        }
        else await fixture.Client.StopAsync();
        Assert.Empty(completed);
        Assert.Empty(fixture.Session.Results);
        var second = await fixture.Client.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.Continue });
        await fixture.Session.RunCompletedAsync();
        await Task.Delay(1200); // Let old cancelled timer callbacks reach their former deadline.
        var actual = Assert.Single(completed);
        Assert.Equal(second.JobId, actual.JobId);
        Assert.NotEqual(first.JobId, actual.JobId);
        Assert.NotEqual(first.CaptureId, actual.CaptureId);
        Assert.Equal(2, ready.Count);
        Assert.Single(fixture.Session.Results);
        Assert.False((await fixture.Session.RunCompletedAsync()).Accepted);
        Assert.Single(completed);
    }

    [Fact]
    public void EventParsingIsStrictForNewEventsAndPreservesOldImages()
    {
        var ready = new CaptureReadyInfo { JobId = "job", CaptureId = "cap", Timestamp = "2026-10-02T00:00:00.000+00:00", SourceIds = ["a", "b"] };
        Assert.True(VirexEventParser.TryParse(TcpSocketEventFormatter.FormatCaptureReady(ready), out var value, out _));
        Assert.Equal("job", value.CaptureReady!.JobId);
        Assert.True(VirexEventParser.TryParse(TcpSocketEventFormatter.FormatCaptureCompleted(new CaptureCompletedInfo { JobId = ready.JobId, CaptureId = ready.CaptureId, Timestamp = ready.Timestamp, SourceIds = ready.SourceIds, ImageCount = 4 }), out value, out _));
        Assert.Equal(4, value.CaptureCompleted!.ImageCount);
        var json = TcpSocketEventFormatter.FormatCaptureReady(ready);
        foreach (var malformed in new[]
        {
            "[]", "null", json.Replace("\"jobId\":\"job\",", ""), json.Replace("\"captureId\":\"cap\"", "\"captureId\":null"),
            json.Replace("[\"a\",\"b\"]", "[\"a\",null]"), json.Replace("[\"a\",\"b\"]", "[\"a\",\"a\"]"),
            json.Replace("[\"a\",\"b\"]", "[]"), json.Replace(ProtocolJson.Serialize(ready.Timestamp), "\"bad\""),
            json.Replace("captureReady", "captureCompleted"),
            json.Replace("captureReady", "captureCompleted").Replace("}", ",\"imageCount\":true}"),
            json.Replace("captureReady", "captureCompleted").Replace("}", ",\"imageCount\":1}"),
        }) Assert.False(VirexEventParser.TryParse(malformed, out _, out _), malformed);
        Assert.True(VirexEventParser.TryParse("{\"type\":\"imageGrabbed\",\"captureId\":\"old\"}", out value, out _));
        Assert.Null(value.ImageGrabbed!.JobId);
        Assert.Null(value.CaptureCompleted);
    }

    private static ImageGrabbedInfo Image(string job, string capture, string source, string frame) =>
        new ImageGrabbedInfo { JobId = job, CaptureId = capture, SourceId = source, FrameId = frame };
}
