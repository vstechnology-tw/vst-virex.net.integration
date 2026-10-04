using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MQTTnet;
using MQTTnet.Client;
using Virex.NET.Simulator.Core;
using Virex.NET.Simulator.WPF;

namespace Virex.NET.Contracts.Tests;

public sealed class SimulatorWindowTests
{
    private static readonly AsyncLocal<StageTrace?> CurrentTrace = new();
    private static void Stage(string stage) => CurrentTrace.Value?.Record(stage);
    [Theory]
    [InlineData(false, null)]
    [InlineData(false, InspectionModes.CaptureOnly)]
    [InlineData(false, InspectionModes.CaptureAndInspect)]
    [InlineData(true, null)]
    [InlineData(true, InspectionModes.CaptureOnly)]
    [InlineData(true, InspectionModes.CaptureAndInspect)]
    public Task BothStartButtonsUseTheSelectedInspectionModeAndRealCaptureEvents(bool continuous, string? inspectionMode) => OnStaAsync(async () =>
    {
        await using var fixture = new WindowFixture();
        await fixture.Session.ConfigureCaptureSimulationAsync(new[] { "source-a", "source-b" }, framesPerSource: 2);
        await fixture.InitializeThroughUiAsync();
        var selector = fixture.Control<ComboBox>("InspectionModeBox");
        Assert.Equal(0, selector.SelectedIndex);
        selector.SelectedIndex = inspectionMode is null ? 0 : inspectionMode == InspectionModes.CaptureOnly ? 1 : 2;
        CaptureReadyInfo? ready = null;
        CaptureCompletedInfo? completed = null;
        fixture.Session.CaptureReady += (_, value) => ready = value;
        fixture.Session.CaptureCompleted += (_, value) => completed = value;
        fixture.Click(continuous ? "StartContinueButton" : "StartSingleButton");
        await WaitUntilAsync(() => ready is not null && fixture.Text("CaptureReadyBox").Contains(ready.CaptureId));
        Assert.Equal(SimulatorState.Running, fixture.Session.State);
        Assert.Contains(continuous ? ControlRunModes.Continue : ControlRunModes.SingleRun, fixture.Text("LogBox"));
        Assert.True((await fixture.Session.RunCompletedAsync()).Accepted);
        await WaitUntilAsync(() => completed is not null && fixture.Text("CaptureCompletedBox").Contains(completed.CaptureId));
        Assert.Equal(ready!.JobId, completed!.JobId);
        Assert.Equal(ready.CaptureId, completed.CaptureId);
        Assert.Equal(4, completed.ImageCount);
        foreach (var box in new[] { "CaptureReadyBox", "CaptureCompletedBox" })
        {
            Assert.Contains("JobId=" + ready.JobId, fixture.Text(box));
            Assert.Contains("CaptureId=" + ready.CaptureId, fixture.Text(box));
            Assert.Contains("SourcesCount=2", fixture.Text(box));
        }
        Assert.Contains("Timestamp=" + ready.Timestamp, fixture.Text("CaptureReadyBox"));
        Assert.Contains("Timestamp=" + completed.Timestamp, fixture.Text("CaptureCompletedBox"));
        Assert.Contains("ImageCount=4", fixture.Text("CaptureCompletedBox"));
        if (inspectionMode == InspectionModes.CaptureOnly)
        {
            Assert.Empty(fixture.Session.Results);
            Assert.Contains("No resultCreated observed", fixture.Text("ResultStatusText"));
        }
        else
        {
            var result = Assert.Single(fixture.Session.Results);
            await WaitUntilAsync(() => fixture.Text("ResultStatusText").Contains(result.ResultId));
            Assert.Equal(ready.JobId, result.JobId);
            Assert.Equal(ready.CaptureId, result.CaptureId);
        }
    });

    [Fact]
    public Task QueryButtonsShowPublicSnapshotsAndExactResultFailuresWithoutChangingState() => OnStaAsync(async () =>
    {
        await using var fixture = new WindowFixture();
        fixture.Click("GetRecipesButton");
        Assert.Contains("RCP-A", fixture.Text("QueryOutputBox"));
        foreach (var button in new[] { "GetCurrentRecipeButton", "GetCurrentParametersButton" })
        {
            fixture.Click(button);
            Assert.Contains(QueryErrorCodes.NoCurrentRecipe, fixture.Text("QueryOutputBox"));
            Assert.Contains("HTTP 409", fixture.Text("QueryOutputBox"));
        }
        Assert.Equal(SimulatorState.Uninitialized, fixture.Session.State);
        await fixture.InitializeThroughUiAsync();
        fixture.Click("GetCurrentRecipeButton");
        Assert.Contains(fixture.Session.ProductInfo.RecipeOr, fixture.Text("QueryOutputBox"));
        fixture.Click("GetCurrentParametersButton");
        Assert.Contains("cycleDelayMs", fixture.Text("QueryOutputBox"));
        fixture.Click("StartSingleButton");
        await WaitUntilAsync(() => fixture.Session.State == SimulatorState.Running);
        await fixture.Session.RunCompletedAsync();
        var first = Assert.Single(fixture.Session.Results);
        fixture.Click("StartSingleButton");
        await WaitUntilAsync(() => fixture.Session.State == SimulatorState.Running);
        await fixture.Session.RunCompletedAsync();
        Assert.Equal(2, fixture.Session.Results.Length);
        fixture.Control<TextBox>("ResultIdBox").Text = first.ResultId;
        fixture.Click("GetResultDetailButton");
        await WaitUntilAsync(() => fixture.Text("QueryOutputBox").Contains("schemaVersion"));
        using var output = JsonDocument.Parse(fixture.Text("QueryOutputBox").Split('\n', 2)[1]);
        Assert.Equal(first.ResultId, output.RootElement.GetProperty("resultId").GetString());
        foreach (var id in new[] { "missing", " " + first.ResultId, first.ResultId + " " })
        {
            fixture.Control<TextBox>("ResultIdBox").Text = id;
            fixture.Click("GetResultDetailButton");
            Assert.Contains(QueryErrorCodes.ResultNotFound, fixture.Text("QueryOutputBox"));
            Assert.Contains("HTTP 404", fixture.Text("QueryOutputBox"));
        }
        fixture.Control<TextBox>("ResultIdBox").Text = "";
        fixture.Click("GetResultDetailButton");
        Assert.Contains(QueryErrorCodes.InvalidQuery, fixture.Text("QueryOutputBox"));
        Assert.Equal(SimulatorState.Ready, fixture.Session.State);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task AppliedModeIsObservedAndLocalButtonsRespectOptInAuthority(bool managed) => OnStaAsync(async () =>
    {
        await using var fixture = new WindowFixture(managed);
        fixture.Click("GetOperationModeButton");
        Assert.Contains("Applied mode=local", fixture.Text("OperationModeText"));
        Assert.Contains("ManagementEnabled=" + managed, fixture.Text("OperationModeText"));
        await fixture.InitializeThroughUiAsync();
        fixture.Control<ComboBox>("OperationModeBox").SelectedItem = OperationModes.Remote;
        fixture.Click("ApplyOperationModeButton");
        await WaitUntilAsync(() => fixture.Text("OperationModeText").Contains("Applied mode=remote"));
        Assert.Equal(OperationModes.Remote, fixture.Session.OperationMode.Mode);
        Assert.Equal(managed, fixture.Session.OperationMode.ManagementEnabled);
        Assert.Equal(SimulatorState.Ready, fixture.Session.State);
        fixture.Click("GetOperationModeButton");
        Assert.Contains("remote", fixture.Text("QueryOutputBox"));
        fixture.Click("StartContinueButton");
        if (managed)
        {
            await WaitUntilAsync(() => fixture.Text("LogBox").Contains(CommandErrorCodes.OperationNotAllowed));
            Assert.Equal(SimulatorState.Ready, fixture.Session.State);
        }
        else
        {
            await WaitUntilAsync(() => fixture.Session.State == SimulatorState.Running);
            fixture.Click("StopButton");
            await WaitUntilAsync(() => fixture.Session.State == SimulatorState.Ready);
        }
        // An external host change must update the observed applied mode too.
        await fixture.Session.SetOperationModeAsync(new SetOperationModeRequest { Mode = OperationModes.Local });
        await WaitUntilAsync(() => fixture.Text("OperationModeText").Contains("Applied mode=local"));
        fixture.Click("StartContinueButton");
        await WaitUntilAsync(() => fixture.Session.State == SimulatorState.Running);
        await fixture.Session.SetOperationModeAsync(new SetOperationModeRequest { Mode = OperationModes.Remote });
        fixture.Click("StopButton");
        if (managed)
        {
            await WaitUntilAsync(() => fixture.Text("LogBox").Contains("Stop rejected: " + CommandErrorCodes.OperationNotAllowed));
            Assert.Equal(SimulatorState.Running, fixture.Session.State);
        }
        else await WaitUntilAsync(() => fixture.Session.State == SimulatorState.Ready);
    });

    [Fact]
    public Task PreparationFailureDoesNotFabricateCaptureEventsOrResults() => OnStaAsync(async () =>
    {
        await using var fixture = new WindowFixture();
        await fixture.InitializeThroughUiAsync();
        await fixture.Session.ConfigureCaptureSimulationAsync(new[] { "source-a" }, failPreparation: true);
        fixture.Control<ComboBox>("InspectionModeBox").SelectedItem = InspectionModes.CaptureOnly;
        fixture.Click("StartSingleButton");
        await WaitUntilAsync(() => fixture.Text("LogBox").Contains(CommandErrorCodes.CapturePreparationFailed));
        Assert.Equal(SimulatorState.Ready, fixture.Session.State);
        Assert.Contains("No event observed", fixture.Text("CaptureReadyBox"));
        Assert.Contains("No event observed", fixture.Text("CaptureCompletedBox"));
        Assert.Empty(fixture.Session.Results);
    });

    [Fact]
    public Task NewQueryAndCancellationPreventStaleDetailOutput() => OnStaAsync(async () =>
    {
        await using var fixture = new WindowFixture();
        await fixture.InitializeThroughUiAsync();
        fixture.Click("StartSingleButton");
        await WaitUntilAsync(() => fixture.Session.State == SimulatorState.Running);
        await fixture.Session.RunCompletedAsync();
        var result = Assert.Single(fixture.Session.Results);
        // Force an asynchronous multi-buffer read without changing the public detail schema.
        var original = await File.ReadAllTextAsync(result.ResultPath);
        await File.WriteAllTextAsync(result.ResultPath, original.Insert(1, "\"padding\":\"" + new string('x', 8 * 1024 * 1024) + "\","));
        fixture.Control<TextBox>("ResultIdBox").Text = result.ResultId;
        fixture.Click("GetResultDetailButton");
        Assert.True(fixture.Control<Button>("CancelQueryButton").IsEnabled);
        fixture.Click("CancelQueryButton");
        await WaitUntilAsync(() => fixture.Text("QueryOutputBox").Contains("Query cancelled."));
        fixture.Click("GetResultDetailButton");
        fixture.Click("GetRecipesButton");
        await WaitUntilAsync(() => !fixture.Control<Button>("CancelQueryButton").IsEnabled);
        await Task.Delay(100);
        Assert.StartsWith("Get recipes\n", fixture.Text("QueryOutputBox"));
        Assert.DoesNotContain("schemaVersion", fixture.Text("QueryOutputBox"));
    });

    [Fact]
    public Task MinimumWindowSizeKeepsQueriesCaptureAndStateTabsAccessible() => OnStaAsync(async () =>
    {
        await using var fixture = new WindowFixture();
        await fixture.InitializeThroughUiAsync();
        fixture.Click("StartSingleButton");
        await WaitUntilAsync(() => fixture.Session.State == SimulatorState.Running);
        Stage("layout.run-completion.before");
        await fixture.Session.RunCompletedAsync();
        Stage("layout.run-completion.after");
        await WaitUntilAsync(() => fixture.Text("CaptureCompletedBox").Contains("CaptureId="));
        fixture.Click("GetCurrentParametersButton");
        var window = fixture.Window;
        window.Width = window.MinWidth;
        window.Height = window.MinHeight;
        var tabs = fixture.Control<TabControl>("DetailsTabs");
        var directory = EvidenceDirectory;
        Directory.CreateDirectory(directory);
        var observations = new List<object>();
        // Pixel rendering is optional evidence. Some headless hosts cannot flush WPF rendering.
        var captureRendering = Environment.GetEnvironmentVariable("VIREX_UI_RENDER_CAPTURE") == "1";
        for (var index = 0; index < tabs.Items.Count; index++)
        {
            Stage($"layout.tab-{index}.select.before");
            tabs.SelectedIndex = index;
            Stage($"layout.tab-{index}.update.before");
            window.UpdateLayout();
            Stage($"layout.tab-{index}.update.after");
            Assert.True(tabs.ActualWidth > 300);
            Assert.True(tabs.ActualHeight > 150);
            var controlName = index == 0 ? "GetRecipesButton" : index == 1 ? "CaptureCompletedBox" : "ReadyNode";
            var control = fixture.Control<FrameworkElement>(controlName);
            Assert.True(control.IsVisible);
            Assert.True(control.ActualWidth > 0);
            Assert.True(control.ActualHeight > 0);
            control.BringIntoView();
            Stage($"layout.tab-{index}.viewport.before");
            window.UpdateLayout();
            FrameworkElement viewport = tabs;
            for (DependencyObject? parent = VisualTreeHelper.GetParent(control); parent is not null; parent = VisualTreeHelper.GetParent(parent))
                if (parent is ScrollContentPresenter presenter) { viewport = presenter; break; }
            var bounds = control.TransformToAncestor(viewport).TransformBounds(new Rect(control.RenderSize));
            Assert.True(bounds.Left >= -1 && bounds.Top >= -1 && bounds.Right <= viewport.ActualWidth + 1 && bounds.Bottom <= viewport.ActualHeight + 1,
                $"{controlName} bounds {bounds} exceed viewport {viewport.ActualWidth}x{viewport.ActualHeight}.");
            Stage($"layout.tab-{index}.viewport.after");
            observations.Add(new
            {
                Tab = ((TabItem)tabs.Items[index]).Header,
                WindowWidth = window.ActualWidth, WindowHeight = window.ActualHeight,
                TabsWidth = tabs.ActualWidth, TabsHeight = tabs.ActualHeight,
                Control = controlName, control.IsVisible, control.ActualWidth, control.ActualHeight,
                RenderCaptureRequested = captureRendering,
                Bounds = bounds.ToString(), ViewportWidth = viewport.ActualWidth, ViewportHeight = viewport.ActualHeight,
            });
            if (captureRendering)
            {
                var content = (FrameworkElement)window.Content;
                var bitmap = new RenderTargetBitmap((int)content.ActualWidth, (int)content.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                Stage($"layout.tab-{index}.render.before");
                bitmap.Render(content);
                Stage($"layout.tab-{index}.render.after");
                var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
                bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
                Assert.Contains(pixels.Where((_, position) => position % 4 == 3), alpha => alpha != 0);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var file = File.Create(Path.Combine(directory, "minimum-tab-" + index + ".png"));
                encoder.Save(file);
            }
            Stage($"layout.tab-{index}.bounds-write.before");
            await File.WriteAllTextAsync(Path.Combine(directory, "layout-observations.json"), JsonSerializer.Serialize(observations));
            Stage($"layout.tab-{index}.bounds-write.after");
        }
        await File.WriteAllTextAsync(Path.Combine(directory, "layout-observations.json"), JsonSerializer.Serialize(observations));
    });

    [Fact]
    public Task ClosingManagedRemoteContinuousRunPreservesModeAndJoinsSession() => OnStaAsync(async () =>
    {
        await using var fixture = new WindowFixture(true);
        await fixture.InitializeThroughUiAsync();
        await fixture.Session.SetOperationModeAsync(new SetOperationModeRequest { Mode = OperationModes.Remote });
        Assert.True((await fixture.Session.StartFromSourceAsync(new SystemStartRequest
        {
            RunMode = ControlRunModes.Continue, InspectionMode = InspectionModes.CaptureOnly,
        }, OperationSource.External)).Accepted);
        await WaitUntilAsync(() => fixture.Text("CaptureReadyBox").Contains("CaptureId="));
        await fixture.CloseAsync();
        Assert.Equal(OperationModes.Remote, fixture.Session.OperationMode.Mode);
        Assert.True(fixture.Session.OperationMode.ManagementEnabled);
        Assert.Equal(SimulatorState.Ready, fixture.Session.State);
        await Task.Delay(1200);
        Assert.Empty(fixture.Session.Results);
        Assert.False((await fixture.Session.StartFromSourceAsync(new SystemStartRequest(), OperationSource.External)).Accepted);
    });

    [Fact]
    public Task ClosingCancelsPendingUiWorkAndDetachesSessionEvents() => OnStaAsync(async () =>
    {
        await using var fixture = new WindowFixture();
        fixture.Click("InitializeButton");
        Assert.False(fixture.Control<Button>("InitializeButton").IsEnabled);
        // Queries remain available while lifecycle work is pending.
        fixture.Click("GetRecipesButton");
        Assert.Contains("RCP-A", fixture.Text("QueryOutputBox"));
        await fixture.CloseAsync();
        var status = fixture.Text("StatusText");
        var log = fixture.Text("LogBox");
        var mode = fixture.Text("OperationModeText");
        Assert.False((await fixture.Session.InitializeFromSourceAsync(OperationSource.Local)).Accepted);
        fixture.Session.WriteLog("A host event after the window closed.");
        await Task.Delay(50);
        Assert.Equal(status, fixture.Text("StatusText"));
        Assert.Equal(log, fixture.Text("LogBox"));
        Assert.Equal(mode, fixture.Text("OperationModeText"));
    });

    [Fact]
    public async Task WindowStateBrushesAreFrozenAndUsableAcrossIndependentStaOwners()
    {
        Brush? firstBrush = null;
        var firstThread = 0;
        await OnStaAsync(async () =>
        {
            await using var fixture = new WindowFixture();
            firstThread = Environment.CurrentManagedThreadId;
            firstBrush = fixture.Control<Border>("UninitializedNode").Background;
            Assert.True(firstBrush.IsFrozen);
        });
        await OnStaAsync(async () =>
        {
            await using var fixture = new WindowFixture();
            Assert.NotEqual(firstThread, Environment.CurrentManagedThreadId);
            Assert.Same(firstBrush, fixture.Control<Border>("UninitializedNode").Background);
            await fixture.InitializeThroughUiAsync();
            foreach (var name in new[] { "UninitializedNode", "InitializingNode", "ReadyNode", "UpdatingProductInfoNode", "RunningNode", "DeinitializingNode" })
            {
                var node = fixture.Control<Border>(name);
                Assert.True(node.Background.IsFrozen);
                Assert.True(node.BorderBrush.IsFrozen);
                Assert.True(((TextBlock)node.Child).Foreground.IsFrozen);
            }
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task ClosingStartedServersWithActiveClientsJoinsOwnersAndPreservesAllFailures(bool faultUiWork) => OnStaAsync(async () =>
    {
        await using var fixture = new WindowFixture(true);
        await fixture.InitializeThroughUiAsync();
        var ports = ConfigureEndpoints(fixture);
        fixture.Click("StartServersButton");
        await WaitUntilAsync(() => fixture.Text("LogBox").Contains("Servers started."));
        using var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:" + ports.RestPort + "/") };
        using (var rejected = await http.PostAsync(RestRoutes.ApiSystemStart, new StringContent("{}", Encoding.UTF8, "application/json")))
            Assert.Contains(CommandErrorCodes.OperationNotAllowed, await rejected.Content.ReadAsStringAsync());
        await fixture.Session.SetOperationModeAsync(new SetOperationModeRequest { Mode = OperationModes.Remote });
        using (var started = await http.PostAsync(RestRoutes.ApiSystemStart,
            new StringContent(ProtocolJson.Serialize(new SystemStartRequest { RunMode = ControlRunModes.Continue, InspectionMode = InspectionModes.CaptureOnly }), Encoding.UTF8, "application/json")))
            Assert.True(ProtocolJson.Deserialize<CommandResponse>(await started.Content.ReadAsStringAsync())!.Accepted);

        using var tcp = new TcpClient();
        await tcp.ConnectAsync(IPAddress.Loopback, ports.Tcp);
        using var reader = new StreamReader(tcp.GetStream(), Encoding.UTF8, false, 4096, true);
        Assert.Contains("status", (await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5)))!);
        Assert.NotNull(await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5)));
        // Keep a real accepted TCP handler in a partial-frame read during Close.
        await tcp.GetStream().WriteAsync(Encoding.UTF8.GetBytes("{\"type\":"));

        var factory = new MqttFactory();
        using var mqtt = factory.CreateMqttClient();
        var responseReceived = new TaskCompletionSource<MqttCommandResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var mqttDisconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        mqtt.ApplicationMessageReceivedAsync += e =>
        {
            responseReceived.TrySetResult(ProtocolJson.Deserialize<MqttCommandResponse>(Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.ToArray()))!);
            return Task.CompletedTask;
        };
        mqtt.DisconnectedAsync += _ => { mqttDisconnected.TrySetResult(); return Task.CompletedTask; };
        await mqtt.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", ports.Mqtt).Build());
        await mqtt.SubscribeAsync(factory.CreateSubscribeOptionsBuilder().WithTopicFilter(f => f.WithTopic(MqttTopics.ResponseTopic("ui-close", "mode"))).Build());
        await mqtt.PublishAsync(new MqttApplicationMessageBuilder().WithTopic(MqttTopics.Combine("ui-close", MqttTopics.CommandOperationModeGet))
            .WithPayload("{\"correlationId\":\"mode\"}").Build());
        Assert.Equal(OperationModes.Remote, (await responseReceived.Task.WaitAsync(TimeSpan.FromSeconds(5))).OperationMode!.Mode);

        // Keep an accepted REST handler waiting for the rest of the request body.
        using var pendingHttp = new TcpClient();
        var requestAccepted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnLog(object? sender, string message)
        {
            if (message == "REST POST " + RestRoutes.ApiProductInfo) requestAccepted.TrySetResult();
        }
        fixture.Session.Log += OnLog;
        try
        {
            await pendingHttp.ConnectAsync(IPAddress.Loopback, ports.RestPort);
            await pendingHttp.GetStream().WriteAsync(Encoding.ASCII.GetBytes("POST " + RestRoutes.ApiProductInfo + " HTTP/1.1\r\nHost: 127.0.0.1\r\nContent-Type: application/json\r\nContent-Length: 10000\r\n\r\n{"));
            await requestAccepted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally { fixture.Session.Log -= OnLog; }
        if (faultUiWork)
        {
            SetField(fixture.Window, "_activeAction", Task.FromException(new InvalidOperationException("action-owner-failure")));
            SetField(fixture.Window, "_activeQuery", Task.FromException(new InvalidOperationException("query-owner-failure")));
        }
        Stage("active-endpoints.close.before");
        await fixture.CloseAsync();
        Stage("active-endpoints.close.after");
        Assert.Equal(OperationModes.Remote, fixture.Session.OperationMode.Mode);
        Assert.True(fixture.Session.OperationMode.ManagementEnabled);
        Assert.Equal(SimulatorState.Ready, fixture.Session.State);
        AssertSessionUnsubscribed(fixture.Session);
        foreach (var name in new[] { "_rest", "_tcp", "_mqtt", "_mqttBroker" }) Assert.Null(Field<object?>(fixture.Window, name));
        var cleanupFailure = Field<AggregateException?>(fixture.Window, "_cleanupFailure");
        if (faultUiWork)
        {
            Assert.NotNull(cleanupFailure);
            Assert.Contains(cleanupFailure.InnerExceptions, error => error.Message == "action-owner-failure");
            Assert.Contains(cleanupFailure.InnerExceptions, error => error.Message == "query-owner-failure");
        }
        else Assert.Null(cleanupFailure);
        await mqttDisconnected.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await DrainToEndAsync(tcp.GetStream());
        await DrainToEndAsync(pendingHttp.GetStream());
        AssertPortsReleased(ports);
        var lateLogs = new List<string>();
        void AfterClose(object? sender, string message) { lock (lateLogs) lateLogs.Add(message); }
        fixture.Session.Log += AfterClose;
        try
        {
            await Task.Delay(100);
            lock (lateLogs) Assert.Empty(lateLogs);
            Assert.Empty(fixture.Session.Results);
        }
        finally { fixture.Session.Log -= AfterClose; }
    });

    [Fact]
    public Task ClosingAfterPartialServerStartReleasesEveryCreatedOwner() => OnStaAsync(async () =>
    {
        await using var fixture = new WindowFixture();
        var ports = ConfigureEndpoints(fixture);
        using var occupied = new TcpListener(IPAddress.Any, ports.Tcp);
        occupied.Start();
        fixture.Click("StartServersButton");
        await WaitUntilAsync(() => fixture.Text("LogBox").Contains("Operation failed:"));
        await fixture.CloseAsync();
        AssertSessionUnsubscribed(fixture.Session);
        foreach (var name in new[] { "_rest", "_tcp", "_mqtt", "_mqttBroker" }) Assert.Null(Field<object?>(fixture.Window, name));
        occupied.Stop();
        AssertPortsReleased(ports);
    });

    [Fact]
    public Task ClosingDuringPendingServerStartCancelsAndReleasesEveryCreatedOwner() => OnStaAsync(async () =>
    {
        await using var fixture = new WindowFixture();
        var ports = ConfigureEndpoints(fixture);
        fixture.Click("StartServersButton");
        Assert.False(Field<Task>(fixture.Window, "_activeAction").IsCompleted);
        await fixture.CloseAsync();
        Assert.True(Field<Task>(fixture.Window, "_activeAction").IsCompleted);
        AssertSessionUnsubscribed(fixture.Session);
        foreach (var name in new[] { "_rest", "_tcp", "_mqtt", "_mqttBroker" }) Assert.Null(Field<object?>(fixture.Window, name));
        AssertPortsReleased(ports);
    });

    [Fact]
    public Task ClosingDuringPendingResultDetailQueryCancelsAndJoinsTheQuery() => OnStaAsync(async () =>
    {
        await using var fixture = new WindowFixture();
        await fixture.InitializeThroughUiAsync();
        fixture.Click("StartSingleButton");
        await WaitUntilAsync(() => fixture.Session.State == SimulatorState.Running);
        await fixture.Session.RunCompletedAsync();
        var result = Assert.Single(fixture.Session.Results);
        var original = await File.ReadAllTextAsync(result.ResultPath);
        await File.WriteAllTextAsync(result.ResultPath, original.Insert(1, "\"padding\":\"" + new string('x', 8 * 1024 * 1024) + "\","));
        fixture.Control<TextBox>("ResultIdBox").Text = result.ResultId;
        fixture.Click("GetResultDetailButton");
        var query = Field<Task>(fixture.Window, "_activeQuery");
        Assert.False(query.IsCompleted);
        await fixture.CloseAsync();
        Assert.True(query.IsCompletedSuccessfully);
        AssertSessionUnsubscribed(fixture.Session);
        var output = fixture.Text("QueryOutputBox");
        await Task.Delay(50);
        Assert.Equal(output, fixture.Text("QueryOutputBox"));
    });

    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
    private static void SetField(object owner, string name, object value) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(owner, value);
    private static void AssertSessionUnsubscribed(SimulatorSession session)
    {
        foreach (var name in new[] { "Log", "StatusChanged", "OperationModeChanged", "CaptureReady", "CaptureCompleted", "ProductInfoChanged", "ImageGrabbed", "ResultCreated", "ErrorChanged", "CommandRejected" })
            Assert.Null(Field<Delegate?>(session, name));
    }
    private static (int RestPort, int Tcp, int Mqtt) ConfigureEndpoints(WindowFixture fixture)
    {
        var reserved = Enumerable.Range(0, 3).Select(_ => new TcpListener(IPAddress.Loopback, 0)).ToArray();
        try
        {
            foreach (var listener in reserved) listener.Start();
            var ports = reserved.Select(listener => ((IPEndPoint)listener.LocalEndpoint).Port).ToArray();
            fixture.Control<TextBox>("RestPrefixBox").Text = "http://127.0.0.1:" + ports[0] + "/";
            fixture.Control<TextBox>("TcpPortBox").Text = ports[1].ToString();
            fixture.Control<TextBox>("MqttPortBox").Text = ports[2].ToString();
            fixture.Control<TextBox>("MqttTopicBox").Text = "ui-close";
            return (ports[0], ports[1], ports[2]);
        }
        finally { foreach (var listener in reserved) listener.Stop(); }
    }
    private static void AssertPortsReleased((int RestPort, int Tcp, int Mqtt) ports)
    {
        foreach (var port in new[] { ports.RestPort, ports.Tcp, ports.Mqtt })
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
        }
    }
    private static async Task DrainToEndAsync(Stream stream)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var bytes = new byte[4096];
        try { while (await stream.ReadAsync(bytes, timeout.Token) != 0) { } }
        catch (IOException) { /* A reset also proves the accepted socket was released. */ }
    }

    private static string EvidenceDirectory => Environment.GetEnvironmentVariable("VIREX_UI_EVIDENCE_DIR")
        ?? Path.Combine(Path.GetTempPath(), "integration-simulator-closeout-20261004", "ui-validation-astra");

    private static async Task OnStaAsync(Func<Task> test, [CallerMemberName] string testName = "")
    {
        var trace = new StageTrace(testName);
        var watch = Stopwatch.StartNew();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher? ownerDispatcher = null;
        Exception? failure = null;
        var actionEnded = false;
        var thread = new Thread(() =>
        {
            CurrentTrace.Value = trace;
            try
            {
                var dispatcher = Dispatcher.CurrentDispatcher;
                Volatile.Write(ref ownerDispatcher, dispatcher);
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
                dispatcher.BeginInvoke(new Action(async () =>
                {
                    try { trace.Record("action.before"); await test(); trace.Criterion("passed"); }
                    catch (Exception ex) { failure = ex; trace.Criterion("failed", ex); }
                    finally
                    {
                        actionEnded = true;
                        trace.Record("dispatcher.shutdown-request");
                        dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                    }
                }));
                trace.Record("dispatcher.run.before");
                Dispatcher.Run();
                trace.Record("dispatcher.run.returned");
            }
            catch (Exception ex) { failure = ex; trace.Criterion("failed", ex); }
            finally { completion.TrySetResult(); }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        try
        {
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(20));
            // Completion covers Dispatcher.Run returning; join covers the STA owner itself exiting.
            var remaining = TimeSpan.FromSeconds(20) - watch.Elapsed;
            if (!thread.Join(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero))
                throw new TimeoutException("The STA owner did not exit within the original 20-second limit.");
            trace.OwnerEnded();
            if (!actionEnded && failure is null) throw new InvalidOperationException("Dispatcher exited before the test action completed.");
            if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
        }
        catch (TimeoutException ex)
        {
            trace.Criterion("timeout", ex);
            var dispatcher = Volatile.Read(ref ownerDispatcher);
            try { dispatcher?.BeginInvokeShutdown(DispatcherPriority.Background); trace.Record("timeout.shutdown-request"); }
            catch (Exception shutdownError) { trace.Record("timeout.shutdown-request.failed: " + shutdownError); }
            trace.Record("timeout.owner-alive=" + thread.IsAlive);
            throw new TimeoutException("UI criterion timed out. " + trace.Snapshot(), ex);
        }
        finally
        {
            if (!thread.IsAlive) trace.OwnerEnded();
            trace.Save();
        }
    }

    private sealed class StageTrace(string testName)
    {
        private readonly object _gate = new();
        private readonly Queue<object> _stages = new();
        private readonly Stopwatch _watch = Stopwatch.StartNew();
        private readonly string _path = Path.Combine(EvidenceDirectory, testName + "-" + Guid.NewGuid().ToString("N") + ".json");
        private string _criterion = "running";
        private string? _error;
        private bool _ownerEnded;
        public void Record(string stage)
        {
            lock (_gate)
            {
                if (_stages.Count == 128) _stages.Dequeue();
                _stages.Enqueue(new { Stage = stage, Thread = Environment.CurrentManagedThreadId, ElapsedMs = _watch.ElapsedMilliseconds });
            }
        }
        public void Criterion(string outcome, Exception? error = null)
        {
            lock (_gate) { _criterion = outcome; _error = error?.ToString(); }
            Record("criterion." + outcome);
        }
        public void OwnerEnded() { lock (_gate) _ownerEnded = true; }
        public string Snapshot()
        {
            lock (_gate) return JsonSerializer.Serialize(new { Test = testName, Criterion = _criterion, Error = _error, OwnerEnded = _ownerEnded, Stages = _stages.ToArray() });
        }
        public void Save()
        {
            Directory.CreateDirectory(EvidenceDirectory);
            File.WriteAllText(_path, Snapshot());
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var watch = Stopwatch.StartNew();
        while (!condition())
        {
            if (watch.Elapsed > TimeSpan.FromSeconds(5)) throw new TimeoutException("The UI did not reach the expected state.");
            await Task.Delay(10);
        }
    }

    private sealed class WindowFixture : IAsyncDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "virex-ui-" + Guid.NewGuid().ToString("N"));
        private readonly MainWindow _window;
        private readonly TaskCompletionSource _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public WindowFixture(bool managed = false)
        {
            Stage("fixture.session.before");
            Session = new SimulatorSession(_root, managed);
            Stage("fixture.window-construct.before");
            _window = new MainWindow(Session) { ShowInTaskbar = false, ShowActivated = false };
            Stage("fixture.window-construct.after");
            _window.Closed += (_, _) => _closed.TrySetResult();
            Stage("fixture.show.before");
            _window.Show();
            Stage("fixture.show.after");
        }

        public SimulatorSession Session { get; }
        public MainWindow Window => _window;
        public T Control<T>(string name) where T : FrameworkElement => (T)_window.FindName(name);
        public string Text(string name) => Control<FrameworkElement>(name) switch
        {
            TextBox box => box.Text,
            TextBlock block => block.Text,
            _ => throw new ArgumentException("Not a text control.", nameof(name)),
        };
        public void Click(string name)
        {
            Stage("click." + name + ".before");
            Control<Button>(name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Stage("click." + name + ".after");
        }
        public async Task InitializeThroughUiAsync()
        {
            Stage("fixture.initialize.before");
            Click("InitializeButton");
            await WaitUntilAsync(() => Session.State == SimulatorState.Ready && Control<Button>("InitializeButton").IsEnabled);
            Stage("fixture.initialize.after");
        }
        public async Task CloseAsync()
        {
            if (_closed.Task.IsCompleted) return;
            Stage("fixture.close.before; action=" + Field<Task>(_window, "_activeAction").Status + "; query=" + Field<Task>(_window, "_activeQuery").Status);
            _window.Close();
            Stage("fixture.close.requested");
            await _closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Stage("fixture.close.after");
        }
        public async ValueTask DisposeAsync()
        {
            await CloseAsync();
            Stage("fixture.session-shutdown.before");
            await Session.ShutdownAsync();
            Stage("fixture.session-shutdown.after");
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }
    }
}
