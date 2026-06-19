using System;
using System.Threading.Tasks;
using System.Windows;
using Virex.NET.Contracts;
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

    private void ApplyWaferInfo_Click(object sender, RoutedEventArgs e)
    {
        _session.UpdateWaferInfo(ReadWaferInfo(), "UI");
    }

    private void Initialize_Click(object sender, RoutedEventArgs e) => _session.Initialize("Default");

    private void Terminate_Click(object sender, RoutedEventArgs e) => _session.Terminate();

    private async void StartCycle_Click(object sender, RoutedEventArgs e) =>
        await _session.StartCycleAsync(ResultPrefixBox.Text);

    private void Stop_Click(object sender, RoutedEventArgs e) => _session.Stop();

    private void EmitResult_Click(object sender, RoutedEventArgs e) => _session.EmitResult(ResultPrefixBox.Text);

    private void EmitError_Click(object sender, RoutedEventArgs e) => _session.EmitError("Simulator injected error.");

    private WaferInfo ReadWaferInfo() =>
        new WaferInfo
        {
            LotId = LotIdBox.Text,
            WaferId = WaferIdBox.Text,
            RecipeId = RecipeIdBox.Text,
            Slot = SlotBox.Text,
            FoupId = FoupIdBox.Text,
            ChamberId = ChamberIdBox.Text,
        };

    private void RefreshStatus()
    {
        var status = _session.Status;
        StatusText.Text = $"initialized={status.Initialized}, processState={status.ProcessState}, recipe={status.Recipe}";
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
