using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Virex.NET.Contracts;
using Virex.NET.Simulator.Core;
using Virex.NET.Simulator.WPF.Services;

namespace Virex.NET.Simulator.WPF;

public partial class MainWindow : Window
{
    private readonly SimulatorSession _session;
    private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
    private CancellationTokenSource? _queryCancellation;
    private Task _activeQuery = Task.CompletedTask;
    private Task _activeAction = Task.CompletedTask;
    private int _queryVersion;
    private bool _busy;
    private bool _closing;
    private bool _cleanupComplete;
    private readonly List<Exception> _cleanupErrors = new List<Exception>();
    private AggregateException? _cleanupFailure;
    private static readonly Brush InactiveStateBackground = FrozenBrush(229, 231, 235);
    private static readonly Brush InactiveStateBorder = FrozenBrush(148, 163, 184);
    private static readonly Brush InactiveStateForeground = FrozenBrush(17, 24, 39);
    private static readonly Brush ActiveStateBackground = FrozenBrush(38, 136, 176);
    private static readonly Brush ActiveStateBorder = FrozenBrush(27, 99, 128);
    private static readonly Brush ActiveStateForeground = Brushes.White;
    private RestSimulatorServer? _rest;
    private TcpSimulatorServer? _tcp;
    private EmbeddedMqttBroker? _mqttBroker;
    private MqttSimulatorPublisher? _mqtt;

    private static Brush FrozenBrush(byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }

    public MainWindow() : this(new SimulatorSession(null,
        Environment.GetCommandLineArgs().Contains("--manage-operation-mode", StringComparer.Ordinal)))
    {
    }

    /// <summary>Displays the supplied session and shuts down its runs when the window closes.</summary>
    public MainWindow(SimulatorSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        InitializeComponent();
        InspectionModeBox.ItemsSource = new[] { "Omitted (legacy)", InspectionModes.CaptureOnly, InspectionModes.CaptureAndInspect };
        InspectionModeBox.SelectedIndex = 0;
        OperationModeBox.ItemsSource = new[] { OperationModes.Local, OperationModes.Remote };
        OperationModeBox.SelectedIndex = 0;
        _session.Log += OnLog;
        _session.StatusChanged += OnStatusChanged;
        _session.OperationModeChanged += OnOperationModeChanged;
        _session.CaptureReady += OnCaptureReady;
        _session.CaptureCompleted += OnCaptureCompleted;
        _session.ResultCreated += OnResultCreated;
        RefreshStatus();
        RefreshOperationMode(_session.OperationMode);
    }

    private void PostToUi(Action update)
    {
        if (_closing || Dispatcher.HasShutdownStarted) return;
        Dispatcher.BeginInvoke(new Action(() => { if (!_closing) update(); }));
    }

    private void OnLog(object? sender, string message) => PostToUi(() => AppendLog(message));
    private void OnStatusChanged(object? sender, SystemStatus status) => PostToUi(RefreshStatus);
    private void OnOperationModeChanged(object? sender, OperationModeInfo mode) => PostToUi(() => RefreshOperationMode(_session.OperationMode));
    private void OnCaptureReady(object? sender, CaptureReadyInfo info) => PostToUi(() =>
        CaptureReadyBox.Text = FormatCapture(info.JobId, info.CaptureId, info.SourceIds.Length, info.Timestamp));
    private void OnCaptureCompleted(object? sender, CaptureCompletedInfo info) => PostToUi(() =>
        CaptureCompletedBox.Text = FormatCapture(info.JobId, info.CaptureId, info.SourceIds.Length, info.Timestamp) + $"\nImageCount={info.ImageCount}");
    private void OnResultCreated(object? sender, ResultSummary info) => PostToUi(() =>
        ResultStatusText.Text = $"resultCreated: ResultId={info.ResultId}\nJobId={info.JobId}\nCaptureId={info.CaptureId}");

    private static string FormatCapture(string jobId, string captureId, int sourcesCount, string timestamp) =>
        $"JobId={jobId}\nCaptureId={captureId}\nSourcesCount={sourcesCount}\nTimestamp={timestamp}";

    private void RefreshOperationMode(OperationModeInfo mode) =>
        OperationModeText.Text = $"Applied mode={mode.Mode}; ManagementEnabled={mode.ManagementEnabled}";

    private async Task RunActionAsync(Func<CancellationToken, Task> action)
    {
        if (_busy || _closing) return;
        _busy = true;
        LifecyclePanel.IsEnabled = false;
        ApplyProductInfoButton.IsEnabled = false;
        ApplyOperationModeButton.IsEnabled = false;
        try { await action(_lifetime.Token); }
        catch (OperationCanceledException) { if (!_closing) AppendLog("Operation cancelled."); }
        catch (Exception ex) { if (!_closing) AppendLog("Operation failed: " + ex.Message); }
        finally
        {
            _busy = false;
            if (!_closing)
            {
                LifecyclePanel.IsEnabled = true;
                ApplyProductInfoButton.IsEnabled = true;
                ApplyOperationModeButton.IsEnabled = true;
            }
        }
    }

    private void ScheduleAction(Func<CancellationToken, Task> action)
    {
        if (!_busy && !_closing) _activeAction = RunActionAsync(action);
    }

    private void RunCommand(Func<CancellationToken, Task<CommandResponse>> command) =>
        ScheduleAction(async token => AppendCommandResponse(await command(token)));

    private void StartServers_Click(object sender, RoutedEventArgs e) =>
        ScheduleAction(async token =>
        {
            // Repeated starts must not replace and orphan live server instances.
            if (_rest is not null) { AppendLog("Servers are already started."); return; }
            try
            {
                _rest = new RestSimulatorServer(_session, RestPrefixBox.Text);
                await _rest.StartAsync();
                token.ThrowIfCancellationRequested();
                _tcp = new TcpSimulatorServer(_session, int.Parse(TcpPortBox.Text));
                await _tcp.StartAsync();
                token.ThrowIfCancellationRequested();
                _mqttBroker = new EmbeddedMqttBroker(MqttHostBox.Text, int.Parse(MqttPortBox.Text));
                await _mqttBroker.StartAsync();
                token.ThrowIfCancellationRequested();
                _mqtt = new MqttSimulatorPublisher(_session, MqttHostBox.Text, int.Parse(MqttPortBox.Text), MqttTopicBox.Text);
                await _mqtt.StartAsync();
                token.ThrowIfCancellationRequested();
                AppendLog("Servers started.");
            }
            catch (Exception startError)
            {
                try { await StopServersAsync(); }
                catch (Exception cleanupError) { throw new AggregateException("Server start and cleanup failed.", startError, cleanupError); }
                throw;
            }
        });

    private void StopServers_Click(object sender, RoutedEventArgs e) =>
        ScheduleAction(async _ => { await StopServersAsync(); AppendLog("Servers stopped."); });

    private void ApplyProductInfo_Click(object sender, RoutedEventArgs e)
    {
        var product = ReadProductInfo();
        RunCommand(token => _session.SetProductInfoFromSourceAsync(product, OperationSource.Local, token));
    }

    private void Initialize_Click(object sender, RoutedEventArgs e) =>
        RunCommand(token => _session.InitializeFromSourceAsync(OperationSource.Local, token));
    private void Deinitialize_Click(object sender, RoutedEventArgs e) =>
        RunCommand(token => _session.DeinitializeFromSourceAsync(OperationSource.Local, token));
    private void StartSingle_Click(object sender, RoutedEventArgs e) => Start(ControlRunModes.SingleRun);
    private void StartContinue_Click(object sender, RoutedEventArgs e) => Start(ControlRunModes.Continue);
    private void Stop_Click(object sender, RoutedEventArgs e) =>
        RunCommand(token => _session.StopFromSourceAsync(new SystemStopRequest(), OperationSource.Local, token));

    private void Start(string runMode)
    {
        if (_busy || _closing) return;
        var request = new SystemStartRequest
        {
            RunMode = runMode,
            InspectionMode = InspectionModeBox.SelectedIndex == 0 ? null : (string)InspectionModeBox.SelectedItem,
        };
        CaptureReadyBox.Text = "No event observed for this start.";
        CaptureCompletedBox.Text = "No event observed for this start.";
        ResultStatusText.Text = "No resultCreated observed for this start.";
        RunCommand(token => _session.StartFromSourceAsync(request, OperationSource.Local, token));
    }

    private void ApplyOperationMode_Click(object sender, RoutedEventArgs e)
    {
        var mode = (string)OperationModeBox.SelectedItem;
        RunCommand(async token =>
        {
            var response = await _session.SetOperationModeAsync(new SetOperationModeRequest { Mode = mode }, token);
            if (response.OperationMode is not null) RefreshOperationMode(response.OperationMode);
            return response;
        });
    }

    private void GetRecipes_Click(object sender, RoutedEventArgs e) =>
        BeginQuery("Get recipes", _ => Task.FromResult<object>(_session.GetRecipes()));
    private void GetCurrentRecipe_Click(object sender, RoutedEventArgs e) =>
        BeginQuery("Get current recipe", _ => Task.FromResult<object>(_session.GetCurrentRecipe()));
    private void GetCurrentParameters_Click(object sender, RoutedEventArgs e) =>
        BeginQuery("Get current parameters", _ => Task.FromResult<object>(_session.GetCurrentRecipeParameters()));
    private void GetOperationMode_Click(object sender, RoutedEventArgs e) =>
        BeginQuery("Get operation mode", _ =>
        {
            var mode = _session.OperationMode;
            RefreshOperationMode(mode);
            return Task.FromResult<object>(mode);
        });
    private void GetResultDetail_Click(object sender, RoutedEventArgs e)
    {
        // Preserve exact input. No trimming, latest-result lookup, or path interpretation.
        var resultId = ResultIdBox.Text;
        BeginQuery("Get result detail: " + resultId, async token => await _session.GetResultDetailAsync(resultId, token));
    }

    private void BeginQuery(string description, Func<CancellationToken, Task<object>> query)
    {
        if (_closing) return;
        _queryCancellation?.Cancel();
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _queryCancellation = cancellation;
        var version = ++_queryVersion;
        _activeQuery = Task.WhenAll(_activeQuery, RunQueryAsync(description, query, cancellation, version));
    }

    private async Task RunQueryAsync(string description, Func<CancellationToken, Task<object>> query,
        CancellationTokenSource cancellation, int version)
    {
        CancelQueryButton.IsEnabled = true;
        QueryOutputBox.Text = description + "\nLoading...";
        try
        {
            var result = await query(cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            if (!_closing && version == _queryVersion) QueryOutputBox.Text = description + "\n" + ProtocolJson.Serialize(result);
        }
        catch (OperationCanceledException)
        {
            if (!_closing && version == _queryVersion) QueryOutputBox.Text = description + "\nQuery cancelled.";
        }
        catch (SimulatorQueryException ex)
        {
            if (!_closing && version == _queryVersion)
                QueryOutputBox.Text = description + $"\nError: {ex.ErrorCode} (HTTP {ex.StatusCode})\n{ex.Message}";
        }
        catch (Exception ex)
        {
            if (!_closing && version == _queryVersion) QueryOutputBox.Text = description + "\nQuery failed: " + ex.Message;
        }
        finally
        {
            if (version == _queryVersion)
            {
                _queryCancellation = null;
                if (!_closing) CancelQueryButton.IsEnabled = false;
            }
            cancellation.Dispose();
        }
    }

    private void CancelQuery_Click(object sender, RoutedEventArgs e) => _queryCancellation?.Cancel();

    private ProductInfo ReadProductInfo() => new ProductInfo
    {
        WaferID = WaferIDBox.Text, LotID = LotIDBox.Text, Recipe = RecipeBox.Text,
        Slot = SlotBox.Text, FoupID = FoupIDBox.Text, ChamberID = ChamberIDBox.Text,
    };

    private void RefreshStatus()
    {
        StatusText.Text = $"state={_session.Status.State}";
        RefreshStateGraph(_session.Status.State);
    }

    private void RefreshStateGraph(string state)
    {
        SetStateNode(UninitializedNode, state == SystemStates.Uninitialized);
        SetStateNode(InitializingNode, state == SystemStates.Initializing);
        SetStateNode(ReadyNode, state == SystemStates.Ready);
        SetStateNode(UpdatingProductInfoNode, state == SystemStates.UpdatingProductInfo);
        SetStateNode(RunningNode, state == SystemStates.Running);
        SetStateNode(DeinitializingNode, state == SystemStates.Deinitializing);
    }

    private static void SetStateNode(Border node, bool isActive)
    {
        node.Background = isActive ? ActiveStateBackground : InactiveStateBackground;
        node.BorderBrush = isActive ? ActiveStateBorder : InactiveStateBorder;
        if (node.Child is TextBlock label) label.Foreground = isActive ? ActiveStateForeground : InactiveStateForeground;
    }

    private void AppendCommandResponse(CommandResponse response)
    {
        if (_closing) return;
        AppendLog(response.Accepted
            ? $"{response.Command} accepted; JobId={response.JobId}; CaptureId={response.CaptureId}"
            : $"{response.Command} rejected: {response.ErrorCode}, state={response.State}; {response.Message}");
    }

    private void AppendLog(string message)
    {
        LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        LogBox.ScrollToEnd();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (e.Cancel || _cleanupComplete) return;
        e.Cancel = true;
        if (_closing) return;
        _closing = true;
        try { _lifetime.Cancel(); } catch (Exception ex) { _cleanupErrors.Add(ex); }
        try { _queryCancellation?.Cancel(); } catch (Exception ex) { _cleanupErrors.Add(ex); }
        _session.Log -= OnLog;
        _session.StatusChanged -= OnStatusChanged;
        _session.OperationModeChanged -= OnOperationModeChanged;
        _session.CaptureReady -= OnCaptureReady;
        _session.CaptureCompleted -= OnCaptureCompleted;
        _session.ResultCreated -= OnResultCreated;
        _ = CloseAfterCleanupAsync();
    }

    private async Task CloseAfterCleanupAsync()
    {
        // Leave the first Closing event before calling Close again, even when cleanup is synchronous.
        await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);
        try
        {
            await ObserveCleanupAsync(() => Task.WhenAll(_activeAction, _activeQuery));
            // Trusted host cleanup closes admission without changing operation mode or source identity.
            await Task.WhenAll(ObserveCleanupAsync(_session.ShutdownAsync), ObserveCleanupAsync(StopServersAsync));
        }
        finally
        {
            if (_cleanupErrors.Count > 0)
            {
                _cleanupFailure = new AggregateException("Shutdown failed.", _cleanupErrors).Flatten();
                AppendLog(_cleanupFailure.ToString());
            }
            _lifetime.Dispose();
            _cleanupComplete = true;
            Close();
        }
    }

    private async Task ObserveCleanupAsync(Func<Task> cleanup)
    {
        Task? pending = null;
        try { pending = cleanup(); await pending; }
        catch (Exception ex)
        {
            // Await rethrows one failure; keep every owner failure from WhenAll.
            if (pending?.Exception is AggregateException aggregate)
                _cleanupErrors.AddRange(aggregate.Flatten().InnerExceptions);
            else _cleanupErrors.Add(ex);
        }
    }

    private async Task StopServersAsync()
    {
        // Release each owned server even if another server fails during shutdown.
        var mqtt = _mqtt; var broker = _mqttBroker; var tcp = _tcp; var rest = _rest;
        _mqtt = null; _mqttBroker = null; _tcp = null; _rest = null;
        var errors = new List<Exception>();
        async Task StopOne(Func<Task> stop)
        {
            try { await stop(); } catch (Exception ex) { errors.Add(ex); }
        }
        if (mqtt is not null) await StopOne(mqtt.StopAsync);
        if (broker is not null) await StopOne(broker.StopAsync);
        if (tcp is not null) await StopOne(tcp.StopAsync);
        if (rest is not null) await StopOne(rest.StopAsync);
        if (errors.Count > 0) throw new AggregateException("Server shutdown failed.", errors);
    }
}
