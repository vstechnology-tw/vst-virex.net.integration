using Stateless;
using Virex.NET.Contracts;

namespace Virex.NET.Simulator.Core;

public sealed class SimulatorSession
{
    private static readonly TimeSpan StatePreviewDelay = TimeSpan.FromSeconds(1);
    private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
    private readonly List<ResultSummary> _results = new List<ResultSummary>();
    private readonly StateMachine<SimulatorState, SimulatorTrigger> _machine;
    private int _resultSequence;
    private ProductInfo? _activeRunProductInfo;
    private string _activeRunCondition = string.Empty;
    private CancellationTokenSource? _singleRunCompletion;
    private CancellationTokenSource? _continuousRun;

    public SimulatorSession()
    {
        State = SimulatorState.Uninitialized;
        ProductInfo = new ProductInfo
        {
            LotID = "LOT-001",
            WaferID = "W01",
            Recipe = "RCP-A",
            Slot = "1",
            FoupID = "FOUP-A",
            ChamberID = "CH-1",
        };

        _machine = new StateMachine<SimulatorState, SimulatorTrigger>(
            () => State,
            state => State = state);
        ConfigureStateMachine();
    }

    public event EventHandler<SystemStatus>? StatusChanged;
    public event EventHandler<ProductInfo>? ProductInfoChanged;
    public event EventHandler<ResultSummary>? ResultCreated;
    public event EventHandler<ErrorInfo>? ErrorChanged;
    public event EventHandler<CommandResponse>? CommandRejected;
    public event EventHandler<string>? Log;

    public SimulatorState State { get; private set; }

    public SystemStatus Status => new SystemStatus { State = SimulatorStateNames.ToDto(State) };

    public ErrorInfo Error { get; private set; } = new ErrorInfo
    {
        HasError = false,
        State = SystemStates.Uninitialized,
    };

    public ProductInfo ProductInfo { get; private set; }

    public ResultSummary[] Results
    {
        get
        {
            lock (_results)
            {
                return _results.ToArray();
            }
        }
    }

    public Task<CommandResponse> InitializeAsync(CancellationToken cancellationToken = default) =>
        new InitializeSystemCommandHandler(this).Handle(new InitializeSystemCommand(), cancellationToken).AsTask();

    public Task<CommandResponse> DeinitializeAsync(CancellationToken cancellationToken = default) =>
        new DeinitializeSystemCommandHandler(this).Handle(new DeinitializeSystemCommand(), cancellationToken).AsTask();

    public Task<CommandResponse> SetProductInfoAsync(ProductInfo productInfo, CancellationToken cancellationToken = default) =>
        new SetProductInfoCommandHandler(this).Handle(new SetProductInfoCommand(productInfo), cancellationToken).AsTask();

    public Task<CommandResponse> StartAsync(SystemStartRequest request, CancellationToken cancellationToken = default) =>
        new StartSystemCommandHandler(this).Handle(new StartSystemCommand(request), cancellationToken).AsTask();

    public Task<CommandResponse> StopAsync(SystemStopRequest? request = null, CancellationToken cancellationToken = default) =>
        new StopSystemCommandHandler(this).Handle(new StopSystemCommand(request ?? new SystemStopRequest()), cancellationToken).AsTask();

    public Task<CommandResponse> RunCompletedAsync(CancellationToken cancellationToken = default) =>
        HandleCompletionAsync("RunCompleted", SimulatorTrigger.RunCompleted, cancellationToken);

    public void EmitError(string message)
    {
        Error = new ErrorInfo
        {
            HasError = true,
            Message = message,
            State = SimulatorStateNames.ToDto(State),
        };
        LogMessage("Error emitted: " + message);
        ErrorChanged?.Invoke(this, Error);
    }

    public ResultSummary[] QueryResults(string? lotID, string? waferID, string? recipe) =>
        Results
            .Where(x => Match(x.LotID, lotID) && Match(x.WaferID, waferID) && Match(x.Recipe, recipe))
            .Take(100)
            .ToArray();

    public void WriteLog(string message) => LogMessage(message);

    internal async Task<CommandResponse> HandleInitializeAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!CanFire(SimulatorTrigger.Initialize))
                return Reject("Initialize");

            await FireAsync(SimulatorTrigger.Initialize).ConfigureAwait(false);
            await DelayForStatePreviewAsync(cancellationToken).ConfigureAwait(false);
            await FireAsync(SimulatorTrigger.InitializationCompleted).ConfigureAwait(false);
            return Accept("Initialize", "Initialized.");
        }
        finally
        {
            _gate.Release();
        }
    }

    internal async Task<CommandResponse> HandleDeinitializeAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!CanFire(SimulatorTrigger.Deinitialize))
                return Reject("Deinitialize");

            await FireAsync(SimulatorTrigger.Deinitialize).ConfigureAwait(false);
            await DelayForStatePreviewAsync(cancellationToken).ConfigureAwait(false);
            await FireAsync(SimulatorTrigger.DeinitializationCompleted).ConfigureAwait(false);
            return Accept("Deinitialize", "Deinitialized.");
        }
        finally
        {
            _gate.Release();
        }
    }

    internal async Task<CommandResponse> HandleSetProductInfoAsync(ProductInfo productInfo, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!CanFire(SimulatorTrigger.SetProductInfo))
                return Reject("SetProductInfo");

            await FireAsync(SimulatorTrigger.SetProductInfo).ConfigureAwait(false);
            ProductInfo = productInfo.Snapshot();
            ProductInfoChanged?.Invoke(this, ProductInfo);
            LogMessage("ProductInfo updated: " + FormatProductInfoForLog(ProductInfo));
            await DelayForStatePreviewAsync(cancellationToken).ConfigureAwait(false);
            await FireAsync(SimulatorTrigger.ProductInfoUpdateCompleted).ConfigureAwait(false);
            return Accept("SetProductInfo", "ProductInfo updated.");
        }
        finally
        {
            _gate.Release();
        }
    }

    internal async Task<CommandResponse> HandleStartAsync(SystemStartRequest request, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!CanFire(SimulatorTrigger.Start))
                return Reject("Start");

            if (!ControlRunModes.TryNormalize(request.RunMode, out var runMode))
                return Reject("Start", CommandErrorCodes.InvalidRunMode, "Invalid run mode.");

            var condition = NormalizeCondition(request.Condition);
            if (condition.Length > 0)
                LogMessage("Start condition: " + condition);
            LogMessage("Start run mode: " + runMode);

            _activeRunProductInfo = ProductInfo.Snapshot();
            _activeRunCondition = condition;
            await FireAsync(SimulatorTrigger.Start).ConfigureAwait(false);
            if (runMode == ControlRunModes.SingleRun)
            {
                _singleRunCompletion = new CancellationTokenSource();
                _ = CompleteRunAfterDelayAsync(_singleRunCompletion.Token);
            }
            else
            {
                _continuousRun = new CancellationTokenSource();
                _ = EmitContinuousResultsAsync(_continuousRun.Token);
            }

            return Accept("Start", "Started.");
        }
        finally
        {
            _gate.Release();
        }
    }

    internal async Task<CommandResponse> HandleStopAsync(SystemStopRequest request, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!CanFire(SimulatorTrigger.Stop))
                return Reject("Stop");

            StopActiveRunTimers();
            await FireAsync(SimulatorTrigger.Stop).ConfigureAwait(false);
            LogMessage(string.IsNullOrWhiteSpace(request.Reason) ? "Stopped." : "Stopped. reason=" + request.Reason);
            return Accept("Stop", "Stopped.");
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<CommandResponse> HandleCompletionAsync(string command, SimulatorTrigger trigger, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!CanFire(trigger))
                return Reject(command);

            if (trigger == SimulatorTrigger.RunCompleted)
            {
                StopActiveRunTimers();
                EmitResult(string.Empty);
            }

            await FireAsync(trigger).ConfigureAwait(false);
            return Accept(command, command + ".");
        }
        finally
        {
            _gate.Release();
        }
    }

    private void ConfigureStateMachine()
    {
        _machine.Configure(SimulatorState.Uninitialized)
            .Permit(SimulatorTrigger.Initialize, SimulatorState.Initializing);

        _machine.Configure(SimulatorState.Initializing)
            .Permit(SimulatorTrigger.InitializationCompleted, SimulatorState.Ready);

        _machine.Configure(SimulatorState.Ready)
            .Permit(SimulatorTrigger.SetProductInfo, SimulatorState.UpdatingProductInfo)
            .Permit(SimulatorTrigger.Start, SimulatorState.Running)
            .Permit(SimulatorTrigger.Deinitialize, SimulatorState.Deinitializing);

        _machine.Configure(SimulatorState.UpdatingProductInfo)
            .Permit(SimulatorTrigger.ProductInfoUpdateCompleted, SimulatorState.Ready);

        _machine.Configure(SimulatorState.Running)
            .Permit(SimulatorTrigger.Stop, SimulatorState.Ready)
            .Permit(SimulatorTrigger.RunCompleted, SimulatorState.Ready);

        _machine.Configure(SimulatorState.Deinitializing)
            .Permit(SimulatorTrigger.DeinitializationCompleted, SimulatorState.Uninitialized);

        _machine.OnTransitionCompleted(t =>
        {
            LogMessage("Status: state=" + SimulatorStateNames.ToDto(t.Destination));
            Error.State = SimulatorStateNames.ToDto(t.Destination);
            StatusChanged?.Invoke(this, Status);
        });
    }

    private bool CanFire(SimulatorTrigger trigger) => _machine.CanFire(trigger);

    private Task FireAsync(SimulatorTrigger trigger) => _machine.FireAsync(trigger);

    private static Task DelayForStatePreviewAsync(CancellationToken cancellationToken) =>
        Task.Delay(StatePreviewDelay, cancellationToken);

    private CommandResponse Accept(string command, string message) =>
        new CommandResponse
        {
            Accepted = true,
            Command = command,
            State = SimulatorStateNames.ToDto(State),
            Message = message,
        };

    private CommandResponse Reject(string command, string errorCode = CommandErrorCodes.InvalidState, string? message = null)
    {
        var response = new CommandResponse
        {
            Accepted = false,
            Command = command,
            State = SimulatorStateNames.ToDto(State),
            ErrorCode = errorCode,
            Message = message ?? "Command is not valid in the current state.",
        };
        CommandRejected?.Invoke(this, response);
        return response;
    }

    private ResultSummary EmitResult(string resultPathPrefix)
    {
        var info = _activeRunProductInfo?.Snapshot() ?? ProductInfo.Snapshot();
        var condition = _activeRunCondition;
        var now = DateTime.Now;
        var id = $"{info.LotIDOr}-{info.WaferIDOr}-{info.SlotOr}-{now:yyyyMMdd_HHmmss}_{Interlocked.Increment(ref _resultSequence):000}";
        var relativeRoot = $"{now:yyyyMMdd}/{info.LotIDOr}";
        var artifactName = $"{now:yyyyMMdd_HHmmss}_{info.WaferIDOr}";
        var imageRelative = $"{relativeRoot}/{artifactName}.tiff";
        var previewImageRelative = $"{relativeRoot}/{artifactName}.jpg";
        var resultRelative = $"{relativeRoot}/{artifactName}.json";
        var result = new ResultSummary
        {
            ResultId = id,
            Timestamp = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"),
            LotID = info.LotIDOr,
            WaferID = info.WaferIDOr,
            Recipe = info.RecipeOr,
            Slot = info.SlotOr,
            FoupID = info.FoupIDOr,
            ChamberID = info.ChamberIDOr,
            Condition = condition,
            OverallResult = "OK",
            DefectCount = 0,
            ImageRelativePath = imageRelative,
            ResultRelativePath = resultRelative,
            ImagePath = JoinExternalPath(resultPathPrefix, imageRelative),
            PreviewImagePath = JoinExternalPath(resultPathPrefix, previewImageRelative),
            ResultPath = JoinExternalPath(resultPathPrefix, resultRelative),
        };

        lock (_results)
        {
            _results.Insert(0, result);
            if (_results.Count > 100)
                _results.RemoveAt(_results.Count - 1);
        }

        LogMessage("Result emitted: " + id);
        ResultCreated?.Invoke(this, result);
        return result;
    }

    private async Task CompleteRunAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(StatePreviewDelay, cancellationToken).ConfigureAwait(false);
            await RunCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private async Task EmitContinuousResultsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(StatePreviewDelay, cancellationToken).ConfigureAwait(false);
                if (cancellationToken.IsCancellationRequested || State != SimulatorState.Running)
                    return;

                EmitResult(string.Empty);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void StopActiveRunTimers()
    {
        var singleRunCompletion = _singleRunCompletion;
        if (singleRunCompletion is not null)
        {
            _singleRunCompletion = null;
            singleRunCompletion.Cancel();
            singleRunCompletion.Dispose();
        }

        var continuousRun = _continuousRun;
        if (continuousRun is not null)
        {
            _continuousRun = null;
            continuousRun.Cancel();
            continuousRun.Dispose();
        }
    }

    private void LogMessage(string message) => Log?.Invoke(this, message);

    private static bool Match(string value, string? filter) =>
        string.IsNullOrWhiteSpace(filter) || string.Equals(value, filter, StringComparison.OrdinalIgnoreCase);

    private static string FormatProductInfoForLog(ProductInfo info) =>
        $"lotID={info.LotIDOr}, waferID={info.WaferIDOr}, recipe={info.RecipeOr}, slot={info.SlotOr}, foupID={info.FoupIDOr}, chamberID={info.ChamberIDOr}";

    private static string NormalizeCondition(string? condition) =>
        string.IsNullOrWhiteSpace(condition) ? string.Empty : condition!.Trim();

    private static string JoinExternalPath(string prefix, string relative)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return relative;

        var separator = ContainsDirectorySeparator(prefix) ? "\\" : "/";
        return prefix.Trim().TrimEnd('\\', '/') + separator + relative.Replace("/", separator);
    }

    private static bool ContainsDirectorySeparator(string value) => value.Contains("\\");
}
