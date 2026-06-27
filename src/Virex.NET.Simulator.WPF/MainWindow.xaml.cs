using System.Windows;
using Virex.NET.Contracts;
using Virex.NET.Simulator.Core;
using Virex.NET.Simulator.WPF.Services;

namespace Virex.NET.Simulator.WPF;

public partial class MainWindow : Window
{
    private readonly SimulatorSession _session = new SimulatorSession();
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

    private async void StartCycle_Click(object sender, RoutedEventArgs e) =>
        AppendCommandResponse(await _session.StartAsync(new SystemStartRequest()));

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
