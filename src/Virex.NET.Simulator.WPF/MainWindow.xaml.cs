using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Virex.NET.Contracts;
using Virex.NET.Simulator.Core;
using Virex.NET.Simulator.WPF.Services;

namespace Virex.NET.Simulator.WPF;

public partial class MainWindow : Window
{
    private readonly SimulatorSession _session = new SimulatorSession();
    private static readonly Brush InactiveStateBackground = new SolidColorBrush(Color.FromRgb(229, 231, 235));
    private static readonly Brush InactiveStateBorder = new SolidColorBrush(Color.FromRgb(148, 163, 184));
    private static readonly Brush InactiveStateForeground = new SolidColorBrush(Color.FromRgb(17, 24, 39));
    private static readonly Brush ActiveStateBackground = new SolidColorBrush(Color.FromRgb(38, 136, 176));
    private static readonly Brush ActiveStateBorder = new SolidColorBrush(Color.FromRgb(27, 99, 128));
    private static readonly Brush ActiveStateForeground = Brushes.White;
    private RestSimulatorServer? _rest;
    private TcpSimulatorServer? _tcp;
    private EmbeddedMqttBroker? _mqttBroker;
    private MqttSimulatorPublisher? _mqtt;

    public MainWindow()
    {
        InitializeComponent();
        _session.Log += (_, message) => Dispatcher.Invoke(() => AppendLog(message));
        _session.StatusChanged += (_, _) => Dispatcher.Invoke(RefreshStatus);
        RefreshStatus();
    }

    private async void StartServers_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _rest = new RestSimulatorServer(_session, RestPrefixBox.Text);
            await _rest.StartAsync();

            _tcp = new TcpSimulatorServer(_session, int.Parse(TcpPortBox.Text));
            await _tcp.StartAsync();

            _mqttBroker = new EmbeddedMqttBroker(MqttHostBox.Text, int.Parse(MqttPortBox.Text));
            await _mqttBroker.StartAsync();
            AppendLog($"MQTT broker listening at {MqttHostBox.Text}:{MqttPortBox.Text}.");

            _mqtt = new MqttSimulatorPublisher(
                _session,
                MqttHostBox.Text,
                int.Parse(MqttPortBox.Text),
                MqttTopicBox.Text);
            await _mqtt.StartAsync();

            AppendLog("Servers started.");
        }
        catch (Exception ex)
        {
            await StopServersAsync();
            AppendLog("Start failed: " + ex.Message);
        }
    }

    private async void StopServers_Click(object sender, RoutedEventArgs e)
    {
        await StopServersAsync();
        AppendLog("Servers stopped.");
    }

    private async void ApplyProductInfo_Click(object sender, RoutedEventArgs e)
    {
        var response = await _session.SetProductInfoAsync(ReadProductInfo());
        AppendCommandResponse(response);
    }

    private async void Initialize_Click(object sender, RoutedEventArgs e) =>
        AppendCommandResponse(await _session.InitializeAsync());

    private async void Deinitialize_Click(object sender, RoutedEventArgs e) =>
        AppendCommandResponse(await _session.DeinitializeAsync());

    private async void StartSingle_Click(object sender, RoutedEventArgs e) =>
        AppendCommandResponse(await _session.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.SingleRun }));

    private async void StartContinue_Click(object sender, RoutedEventArgs e) =>
        AppendCommandResponse(await _session.StartAsync(new SystemStartRequest { RunMode = ControlRunModes.Continue }));

    private async void Stop_Click(object sender, RoutedEventArgs e) =>
        AppendCommandResponse(await _session.StopAsync());

    private ProductInfo ReadProductInfo() =>
        new ProductInfo
        {
            WaferID = WaferIDBox.Text,
            LotID = LotIDBox.Text,
            Recipe = RecipeBox.Text,
            Slot = SlotBox.Text,
            FoupID = FoupIDBox.Text,
            ChamberID = ChamberIDBox.Text,
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

        if (node.Child is TextBlock label)
            label.Foreground = isActive ? ActiveStateForeground : InactiveStateForeground;
    }

    private void AppendCommandResponse(CommandResponse response)
    {
        if (!response.Accepted)
            AppendLog($"{response.Command} rejected: {response.ErrorCode}, state={response.State}");
    }

    private void AppendLog(string message)
    {
        LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        LogBox.ScrollToEnd();
    }

    protected override async void OnClosed(EventArgs e)
    {
        await StopServersAsync();
        base.OnClosed(e);
    }

    private async Task StopServersAsync()
    {
        if (_mqtt is not null)
            await _mqtt.StopAsync();
        if (_mqttBroker is not null)
            await _mqttBroker.StopAsync();
        if (_tcp is not null)
            await _tcp.StopAsync();
        if (_rest is not null)
            await _rest.StopAsync();
        _mqtt = null;
        _mqttBroker = null;
        _tcp = null;
        _rest = null;
    }
}
