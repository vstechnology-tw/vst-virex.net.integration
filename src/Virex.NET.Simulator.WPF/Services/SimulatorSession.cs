using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Virex.NET.Contracts;

namespace Virex.NET.Simulator.WPF.Services;

public sealed class SimulatorSession
{
    private readonly object _gate = new object();
    private readonly List<ResultSummaryDto> _results = new List<ResultSummaryDto>();
    private CancellationTokenSource? _cycleCts;
    private int _resultSequence;

    public event EventHandler<StatusDto>? StatusChanged;
    public event EventHandler<WaferInfo>? WaferInfoChanged;
    public event EventHandler<ResultSummaryDto>? ResultCreated;
    public event EventHandler<ErrorStatusDto>? ErrorChanged;
    public event EventHandler<string>? Log;

    public StatusDto Status { get; private set; } = new StatusDto
    {
        Initialized = false,
        ProcessState = ProcessStates.Ready,
        Recipe = "Default",
    };

    public ErrorStatusDto Error { get; private set; } = new ErrorStatusDto
    {
        HasError = false,
        Initialized = false,
        ProcessState = ProcessStates.Ready,
        Recipe = "Default",
    };

    public WaferInfo WaferInfo { get; private set; } = new WaferInfo
    {
        LotId = "LOT-001",
        WaferId = "W01",
        RecipeId = "RCP-A",
        Slot = "1",
        FoupId = "FOUP-A",
        ChamberId = "CH-1",
    };

    public ResultSummaryDto[] Results
    {
        get
        {
            lock (_gate)
            {
                return _results.ToArray();
            }
        }
    }

    public ControlResponse Initialize(string recipe)
    {
        if (Status.Initialized)
            return Control("Already initialized.", 409);

        SetStatus(true, ProcessStates.Ready, string.IsNullOrWhiteSpace(recipe) ? "Default" : recipe);
        LogMessage("Initialized.");
        return Control("Initialized.");
    }

    public ControlResponse Terminate()
    {
        if (Status.ProcessState != ProcessStates.Ready)
            return Control("Cannot terminate while process is active.", 409);

        SetStatus(false, ProcessStates.Ready, Status.Recipe);
        LogMessage("Terminated.");
        return Control("Terminated.");
    }

    public async Task<ControlResponse> StartCycleAsync(string resultPathPrefix)
    {
        if (!Status.Initialized)
            return Control("not_initialized", 409);

        if (Status.ProcessState != ProcessStates.Ready)
            return Control("process_active", 409);

        _cycleCts = new CancellationTokenSource();
        var token = _cycleCts.Token;
        try
        {
            SetStatus(true, ProcessStates.Capturing, Status.Recipe);
            await Task.Delay(800, token).ConfigureAwait(false);
            SetStatus(true, ProcessStates.Inspecting, Status.Recipe);
            await Task.Delay(1000, token).ConfigureAwait(false);
            SetStatus(true, ProcessStates.Saving, Status.Recipe);
            await Task.Delay(500, token).ConfigureAwait(false);
            EmitResult(resultPathPrefix);
            SetStatus(true, ProcessStates.Ready, Status.Recipe);
            return Control("Cycle completed.");
        }
        catch (OperationCanceledException)
        {
            SetStatus(Status.Initialized, ProcessStates.Ready, Status.Recipe);
            return Control("Stopped.");
        }
    }

    public ControlResponse Stop()
    {
        if (!Status.Initialized)
            return Control("not_initialized", 409);

        if (Status.ProcessState == ProcessStates.Ready)
            return Control("not_running", 409);

        _cycleCts?.Cancel();
        SetStatus(true, ProcessStates.Ready, Status.Recipe);
        LogMessage("Stopped.");
        return Control("Stopped.");
    }

    public void UpdateWaferInfo(WaferInfo info, string source)
    {
        WaferInfo = info;
        LogMessage($"WaferInfo updated from {source}: {info.LotIdOr}/{info.WaferIdOr}");
        WaferInfoChanged?.Invoke(this, info);
    }

    public ResultSummaryDto EmitResult(string resultPathPrefix)
    {
        var id = $"{WaferInfo.LotIdOr}-{WaferInfo.WaferIdOr}-{WaferInfo.SlotOr}-{DateTime.Now:yyyyMMdd_HHmmss}_{Interlocked.Increment(ref _resultSequence):000}";
        var relativeRoot = $"{DateTime.Now:yyyyMMdd}/{WaferInfo.LotIdOr}";
        var imageRelative = $"{relativeRoot}/{id}-image.tiff";
        var resultRelative = $"{relativeRoot}/{id}-result.json";
        var result = new ResultSummaryDto
        {
            ResultId = id,
            Timestamp = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"),
            LotId = WaferInfo.LotIdOr,
            WaferId = WaferInfo.WaferIdOr,
            RecipeId = string.IsNullOrWhiteSpace(WaferInfo.RecipeId) ? Status.Recipe : WaferInfo.RecipeId,
            Slot = WaferInfo.SlotOr,
            FoupId = WaferInfo.FoupIdOr,
            ChamberId = WaferInfo.ChamberIdOr,
            OverallResult = "OK",
            DefectCount = 0,
            DieCount = 100,
            ImageRelativePath = imageRelative,
            ResultRelativePath = resultRelative,
            ImagePath = JoinExternalPath(resultPathPrefix, imageRelative),
            ResultPath = JoinExternalPath(resultPathPrefix, resultRelative),
        };

        lock (_gate)
        {
            _results.Insert(0, result);
            if (_results.Count > 100)
                _results.RemoveAt(_results.Count - 1);
        }

        LogMessage("Result emitted: " + id);
        ResultCreated?.Invoke(this, result);
        return result;
    }

    public void EmitError(string message)
    {
        Error = new ErrorStatusDto
        {
            HasError = true,
            Message = message,
            Initialized = Status.Initialized,
            ProcessState = Status.ProcessState,
            Recipe = Status.Recipe,
        };
        LogMessage("Error emitted: " + message);
        ErrorChanged?.Invoke(this, Error);
    }

    private ControlResponse Control(string message, int statusCode = 200) =>
        new ControlResponse
        {
            StatusCode = statusCode,
            Body = new ControlStatusDto
            {
                Initialized = Status.Initialized,
                ProcessState = Status.ProcessState,
                Recipe = Status.Recipe,
                Message = message,
            },
        };

    private void SetStatus(bool initialized, string processState, string recipe)
    {
        Status = new StatusDto
        {
            Initialized = initialized,
            ProcessState = processState,
            Recipe = string.IsNullOrWhiteSpace(recipe) ? "Default" : recipe,
        };
        Error.Initialized = Status.Initialized;
        Error.ProcessState = Status.ProcessState;
        Error.Recipe = Status.Recipe;
        LogMessage($"Status: initialized={Status.Initialized}, processState={Status.ProcessState}, recipe={Status.Recipe}");
        StatusChanged?.Invoke(this, Status);
    }

    private void LogMessage(string message) => Log?.Invoke(this, message);

    public void WriteLog(string message) => LogMessage(message);

    private static string JoinExternalPath(string prefix, string relative)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return relative;

        var separator = ContainsDirectorySeparator(prefix) ? "\\" : "/";
        return prefix.Trim().TrimEnd('\\', '/') + separator + relative.Replace("/", separator);
    }

    private static bool ContainsDirectorySeparator(string value)
    {
#if NET48
        return value.Contains("\\");
#else
        return value.Contains('\\');
#endif
    }
}
