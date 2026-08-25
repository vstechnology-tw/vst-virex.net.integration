using Stateless;
using Virex.NET.Contracts;

namespace Virex.NET.Simulator.Core;

public sealed class SimulatorSession
{
    private static readonly TimeSpan StatePreviewDelay = TimeSpan.FromSeconds(1);
    private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
    private readonly object _deinitializationGate = new object();
    private readonly List<ResultSummary> _results = new List<ResultSummary>();
    private readonly StateMachine<SimulatorState, SimulatorTrigger> _machine;
    private int _resultSequence;
    private ProductInfo? _activeRunProductInfo;
    private string _activeRunCondition = string.Empty;
    private readonly string _resultRootDirectory;
    private CancellationTokenSource? _singleRunCompletion;
    private CancellationTokenSource? _continuousRun;
    private int _deinitializeFailuresRemaining;
    private string? _deinitializeFailureMessage;
    private string? _pendingDeinitializationError;
    private Task<CommandResponse>? _activeDeinitialization;
    private DateTimeOffset? _recoveryStartedAt;
    private string? _recoverySource;
    private string? _recoveryPhase;
    private string? _recoveryDetails;
    private string? _recoveryErrorCode;

    public SimulatorSession(string? resultRootDirectory = null)
    {
        var root = string.IsNullOrWhiteSpace(resultRootDirectory)
            ? Path.Combine(AppContext.BaseDirectory, "results")
            : resultRootDirectory!;
        _resultRootDirectory = Path.GetFullPath(root);

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
    public event EventHandler<ImageGrabbedInfo>? ImageGrabbed;
    public event EventHandler<ResultSummary>? ResultCreated;
    public event EventHandler<ErrorInfo>? ErrorChanged;
    public event EventHandler<CommandResponse>? CommandRejected;
    public event EventHandler<string>? Log;

    public SimulatorState State { get; private set; }

    public SystemStatus Status => new SystemStatus
    {
        State = SimulatorStateNames.ToDto(State),
        RecoveryAction = RecoveryActionFor(State),
        ErrorCode = RecoveryActionFor(State) is null ? null : _recoveryErrorCode,
        RecoveryStartedAt = RecoveryActionFor(State) is null ? null : _recoveryStartedAt,
        RecoverySource = RecoveryActionFor(State) is null ? null : _recoverySource,
        RecoveryPhase = RecoveryActionFor(State) is null ? null : _recoveryPhase,
        RecoveryDetails = RecoveryActionFor(State) is null ? null : _recoveryDetails,
    };

    public ErrorInfo Error { get; private set; } = new ErrorInfo
    {
        HasError = false,
        State = SystemStates.Uninitialized,
        RecoveryAction = null,
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

    public Task<CommandResponse> DeinitializeAsync(CancellationToken cancellationToken = default)
    {
        lock (_deinitializationGate)
        {
            if (_activeDeinitialization is { IsCompleted: false } activeDeinitialization)
                return activeDeinitialization;

            _activeDeinitialization = new DeinitializeSystemCommandHandler(this)
                .Handle(new DeinitializeSystemCommand(), CancellationToken.None)
                .AsTask();
            return _activeDeinitialization;
        }
    }

    public void ConfigureDeinitializeFailures(int attempts, string? message = null)
    {
        if (attempts < 0)
            throw new ArgumentOutOfRangeException(nameof(attempts));

        Volatile.Write(ref _deinitializeFailuresRemaining, attempts);
        _deinitializeFailureMessage = message;
    }

    public Task<CommandResponse> SimulateAcquisitionFaultAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("A fault message is required.", nameof(message));

        lock (_deinitializationGate)
        {
            if (_activeDeinitialization is { IsCompleted: false } activeDeinitialization)
                return activeDeinitialization;

            _activeDeinitialization = SimulateAcquisitionFaultCoreAsync(message, cancellationToken);
            return _activeDeinitialization;
        }
    }

    private async Task<CommandResponse> SimulateAcquisitionFaultCoreAsync(
        string message,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (State == SimulatorState.Running)
            {
                StopActiveRunTimers();
                await FireAsync(SimulatorTrigger.Stop).ConfigureAwait(false);
                LogMessage("Stopped. reason=Simulated acquisition fault");
            }

            if (State != SimulatorState.Ready)
                return Reject("SimulateAcquisitionFault");

            _pendingDeinitializationError = message;
            BeginRecovery(
                message,
                "Acquisition",
                "Deinitializing",
                CommandErrorCodes.RequiresDeinitialize);
            LogMessage("Acquisition fault simulated: " + message);
            return await HandleDeinitializeUnderGateAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }
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
        var safeMessage = RecoveryMessageSanitizer.Sanitize(message) ?? "Simulated error.";
        if (State == SimulatorState.Deinitializing)
            BeginRecovery(safeMessage, "Simulator", "Error", "simulated_error");
        else
            ClearRecovery();
        Error = new ErrorInfo
        {
            HasError = true,
            Message = safeMessage,
            State = SimulatorStateNames.ToDto(State),
            RecoveryAction = RecoveryActionFor(State),
            ErrorCode = "simulated_error",
            RecoveryStartedAt = RecoveryActionFor(State) is null ? null : _recoveryStartedAt,
            RecoverySource = RecoveryActionFor(State) is null ? null : _recoverySource,
            RecoveryPhase = RecoveryActionFor(State) is null ? null : _recoveryPhase,
            RecoveryDetails = RecoveryActionFor(State) is null ? null : _recoveryDetails,
        };
        LogMessage("Error emitted: " + safeMessage);
        LogEvent("errorChanged", Error);
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
            return await HandleDeinitializeUnderGateAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<CommandResponse> HandleDeinitializeUnderGateAsync(
        CancellationToken cancellationToken)
    {
        if (!CanFire(SimulatorTrigger.Deinitialize))
            return Reject("Deinitialize");

        var failureMessage = RecoveryMessageSanitizer.Sanitize(
            _pendingDeinitializationError
            ?? _deinitializeFailureMessage
            ?? "Simulated deinitialization failed.")
            ?? "Simulated deinitialization failed.";
        var hasPendingFailure = HasPendingDeinitializeFailure();
        if (hasPendingFailure)
        {
            BeginRecovery(
                failureMessage,
                string.IsNullOrWhiteSpace(_pendingDeinitializationError) ? "Simulator" : "Acquisition",
                "Deinitializing",
                CommandErrorCodes.RequiresDeinitialize);
        }

        await FireAsync(SimulatorTrigger.Deinitialize).ConfigureAwait(false);
        if (hasPendingFailure)
            SetError(failureMessage);
        await DelayForStatePreviewAsync(cancellationToken).ConfigureAwait(false);

        if (TryConsumeDeinitializeFailure())
        {
            SetError(failureMessage);
            return Reject(
                "Deinitialize",
                CommandErrorCodes.RequiresDeinitialize,
                failureMessage);
        }

        ClearRecovery();
        await FireAsync(SimulatorTrigger.DeinitializationCompleted).ConfigureAwait(false);
        _pendingDeinitializationError = null;
        SetError(null);
        return Accept("Deinitialize", "Deinitialized.");
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
            LogEvent("productInfoChanged", ProductInfo);
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
                if (EmitResult() is null)
                {
                    await FireAsync(SimulatorTrigger.Stop).ConfigureAwait(false);
                    return Accept(command, "Run completed with errors.");
                }
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
            .PermitReentry(SimulatorTrigger.Deinitialize)
            .Permit(SimulatorTrigger.DeinitializationCompleted, SimulatorState.Uninitialized);

        _machine.OnTransitionCompleted(t =>
        {
            var status = Status;
            LogMessage("Status: state=" + SimulatorStateNames.ToDto(t.Destination));
            LogEvent("statusChanged", status);
            if (t.Destination == SimulatorState.Running)
                LogEvent("runStarted", status);
            if (t.Source == SimulatorState.Running && t.Destination == SimulatorState.Ready)
                LogEvent("runCompleted", status);
            Error.State = SimulatorStateNames.ToDto(t.Destination);
            Error.RecoveryAction = RecoveryActionFor(t.Destination);
            Error.ErrorCode = Error.HasError ? _recoveryErrorCode : null;
            Error.RecoveryStartedAt = RecoveryActionFor(t.Destination) is null ? null : _recoveryStartedAt;
            Error.RecoverySource = RecoveryActionFor(t.Destination) is null ? null : _recoverySource;
            Error.RecoveryPhase = RecoveryActionFor(t.Destination) is null ? null : _recoveryPhase;
            Error.RecoveryDetails = RecoveryActionFor(t.Destination) is null ? null : _recoveryDetails;
            StatusChanged?.Invoke(this, status);
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
            RecoveryAction = RecoveryActionFor(State),
            RecoveryStartedAt = RecoveryActionFor(State) is null ? null : _recoveryStartedAt,
            RecoverySource = RecoveryActionFor(State) is null ? null : _recoverySource,
            RecoveryPhase = RecoveryActionFor(State) is null ? null : _recoveryPhase,
            RecoveryDetails = RecoveryActionFor(State) is null ? null : _recoveryDetails,
            Message = message,
        };

    private CommandResponse Reject(string command, string errorCode = CommandErrorCodes.InvalidState, string? message = null)
    {
        var response = new CommandResponse
        {
            Accepted = false,
            Command = command,
            State = SimulatorStateNames.ToDto(State),
            RecoveryAction = RecoveryActionFor(State),
            ErrorCode = errorCode,
            RecoveryStartedAt = RecoveryActionFor(State) is null ? null : _recoveryStartedAt,
            RecoverySource = RecoveryActionFor(State) is null ? null : _recoverySource,
            RecoveryPhase = RecoveryActionFor(State) is null ? null : _recoveryPhase,
            RecoveryDetails = RecoveryActionFor(State) is null ? null : _recoveryDetails,
            Message = message ?? "Command is not valid in the current state.",
        };
        LogEvent("commandRejected", response);
        CommandRejected?.Invoke(this, response);
        return response;
    }

    private static string? RecoveryActionFor(SimulatorState state) =>
        state == SimulatorState.Deinitializing ? RecoveryActions.Deinitialize : null;

    private void SetError(string? message)
    {
        var hasError = !string.IsNullOrWhiteSpace(message);
        Error = new ErrorInfo
        {
            HasError = hasError,
            Message = hasError ? message : null,
            State = SimulatorStateNames.ToDto(State),
            RecoveryAction = RecoveryActionFor(State),
            ErrorCode = hasError ? _recoveryErrorCode : null,
            RecoveryStartedAt = RecoveryActionFor(State) is null ? null : _recoveryStartedAt,
            RecoverySource = RecoveryActionFor(State) is null ? null : _recoverySource,
            RecoveryPhase = RecoveryActionFor(State) is null ? null : _recoveryPhase,
            RecoveryDetails = RecoveryActionFor(State) is null ? null : _recoveryDetails,
        };
        ErrorChanged?.Invoke(this, Error);
    }

    private void BeginRecovery(
        string? details,
        string source,
        string phase,
        string errorCode)
    {
        _recoveryStartedAt ??= DateTimeOffset.UtcNow;
        _recoverySource = source;
        _recoveryPhase = phase;
        _recoveryDetails = RecoveryMessageSanitizer.Sanitize(details);
        _recoveryErrorCode = errorCode;
    }

    private void ClearRecovery()
    {
        _recoveryStartedAt = null;
        _recoverySource = null;
        _recoveryPhase = null;
        _recoveryDetails = null;
        _recoveryErrorCode = null;
    }


    private bool HasPendingDeinitializeFailure() =>
        !string.IsNullOrWhiteSpace(_pendingDeinitializationError)
        || Volatile.Read(ref _deinitializeFailuresRemaining) > 0;

    private bool TryConsumeDeinitializeFailure()
    {
        while (true)
        {
            var remaining = Volatile.Read(ref _deinitializeFailuresRemaining);
            if (remaining <= 0)
                return false;

            if (Interlocked.CompareExchange(
                    ref _deinitializeFailuresRemaining,
                    remaining - 1,
                    remaining) == remaining)
            {
                return true;
            }
        }
    }

    private ResultSummary? EmitResult()
    {
        var info = _activeRunProductInfo?.Snapshot() ?? ProductInfo.Snapshot();
        var condition = _activeRunCondition;
        var now = DateTime.Now;
        var lotID = SanitizePathSegment(info.LotIDOr, "LOT-UNKNOWN");
        var waferID = SanitizePathSegment(info.WaferIDOr, "WAFER-UNKNOWN");
        var slot = SanitizePathSegment(info.SlotOr, "SLOT-UNKNOWN");
        var sequence = Interlocked.Increment(ref _resultSequence);
        var captureId = $"{lotID}-{waferID}-{slot}-{now:yyyyMMdd_HHmmss}_{sequence:000}";
        var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
        var imageGrabbed = new ImageGrabbedInfo
        {
            CaptureId = captureId,
            Timestamp = timestamp,
            LotID = info.LotIDOr,
            WaferID = info.WaferIDOr,
            Recipe = info.RecipeOr,
            Slot = info.SlotOr,
            FoupID = info.FoupIDOr,
            ChamberID = info.ChamberIDOr,
        };
        LogEvent("imageGrabbed", imageGrabbed);

        ImageGrabbed?.Invoke(this, imageGrabbed);

        var relativeRoot = Path.Combine(now.ToString("yyyyMMdd"), lotID);
        var artifactName = captureId;
        var imageRelative = ToPortablePath(Path.Combine(relativeRoot, artifactName + ".bmp"));
        var previewImageRelative = ToPortablePath(Path.Combine(relativeRoot, artifactName + ".jpg"));
        var resultRelative = ToPortablePath(Path.Combine(relativeRoot, artifactName + ".json"));
        var imagePath = Path.Combine(_resultRootDirectory, relativeRoot, artifactName + ".bmp");
        var previewImagePath = Path.Combine(_resultRootDirectory, relativeRoot, artifactName + ".jpg");
        var resultPath = Path.Combine(_resultRootDirectory, relativeRoot, artifactName + ".json");
        var result = new ResultSummary
        {
            ResultId = captureId,
            CaptureId = captureId,
            Timestamp = timestamp,
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
            ImagePath = imagePath,
            PreviewImagePath = previewImagePath,
            ResultPath = resultPath,
        };

        try
        {
            WriteArtifacts(result, imagePath, previewImagePath, resultPath);
        }
        catch (Exception ex)
        {
            EmitError("Failed to persist simulator artifacts for capture " + captureId + ": " + ex.Message);
            return null;
        }

        lock (_results)
        {
            _results.Insert(0, result);
            if (_results.Count > 100)
                _results.RemoveAt(_results.Count - 1);
        }
        LogEvent("resultCreated", result);

        LogMessage("Result emitted: " + captureId);
        ResultCreated?.Invoke(this, result);
        return result;
    }

    private static void WriteArtifacts(ResultSummary result, string imagePath, string previewImagePath, string resultPath)
    {
        var directory = Path.GetDirectoryName(imagePath);
        if (string.IsNullOrWhiteSpace(directory))
            throw new IOException("Simulator result directory could not be resolved.");

        Directory.CreateDirectory(directory);
        File.WriteAllBytes(imagePath, CreateDummyBmp());
        File.WriteAllBytes(previewImagePath, CreateDummyJpeg());
        File.WriteAllText(resultPath, ProtocolJson.Serialize(result));
    }

    private static byte[] CreateDummyBmp() =>
        new byte[]
        {
            0x42, 0x4D, 0x3A, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x36, 0x00, 0x00, 0x00, 0x28, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x18, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x13, 0x0B,
            0x00, 0x00, 0x13, 0x0B, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF,
            0xFF, 0x00,
        };

    private static byte[] CreateDummyJpeg() =>
        Convert.FromBase64String("/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAP//////////////////////////////////////////////////////////////////////////////////////2wBDAf//////////////////////////////////////////////////////////////////////////////////////wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAX/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIQAxAAAAH/AP/EABQQAQAAAAAAAAAAAAAAAAAAABD/2gAIAQEAAQUCf//EABQRAQAAAAAAAAAAAAAAAAAAABD/2gAIAQMBAT8Bf//EABQRAQAAAAAAAAAAAAAAAAAAABD/2gAIAQIBAT8Bf//EABQQAQAAAAAAAAAAAAAAAAAAABD/2gAIAQEAAT8hH//Z");

    private static string ToPortablePath(string path) =>
        path.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');

    private static string SanitizePathSegment(string value, string fallback)
    {
        var candidate = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        foreach (var invalid in Path.GetInvalidFileNameChars())
            candidate = candidate.Replace(invalid, '_');

        candidate = candidate.Replace('/', '_').Replace('\\', '_');
        return string.IsNullOrWhiteSpace(candidate) ? fallback : candidate;
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

                if (EmitResult() is null)
                {
                    await StopAsync(new SystemStopRequest { Reason = "Simulator artifact persistence failed." }, cancellationToken).ConfigureAwait(false);
                    return;
                }
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

    private void LogEvent<T>(string eventType, T payload) =>
        LogMessage($"Event: {eventType} payload={ProtocolJson.Serialize(payload)}");

    private static bool Match(string value, string? filter) =>
        string.IsNullOrWhiteSpace(filter) || string.Equals(value, filter, StringComparison.OrdinalIgnoreCase);

    private static string FormatProductInfoForLog(ProductInfo info) =>
        $"lotID={info.LotIDOr}, waferID={info.WaferIDOr}, recipe={info.RecipeOr}, slot={info.SlotOr}, foupID={info.FoupIDOr}, chamberID={info.ChamberIDOr}";

    private static string NormalizeCondition(string? condition) =>
        string.IsNullOrWhiteSpace(condition) ? string.Empty : condition!.Trim();

}
