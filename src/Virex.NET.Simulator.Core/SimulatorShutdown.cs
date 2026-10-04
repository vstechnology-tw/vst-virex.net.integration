using Virex.NET.Contracts;
using System.Runtime.ExceptionServices;

namespace Virex.NET.Simulator.Core;

public sealed partial class SimulatorSession
{
    private readonly object _shutdownGate = new object();
    private readonly List<Task> _runTasks = new List<Task>();
    private volatile bool _shutdownRequested;
    private Task? _shutdownTask;

    /// <summary>
    /// Permanently closes this session for its trusted host. Cancels and joins simulated
    /// runs without changing operation mode or representing an external lifecycle command.
    /// Concurrent and reentrant callers join the same operation. Cleanup and retained
    /// run failures are propagated; multiple failures are reported together.
    /// </summary>
    public Task ShutdownAsync()
    {
        TaskCompletionSource<object?> completion;
        lock (_shutdownGate)
        {
            if (_shutdownTask is not null)
                return _shutdownTask;

            completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _shutdownTask = completion.Task;
            _shutdownRequested = true;
        }

        // Publish the shared task before cleanup can synchronously invoke a host event.
        _ = CompleteShutdownAsync(completion);
        return completion.Task;
    }

    // Called only while the existing session gate owns admission.
    private void TrackRunTask(Task task)
    {
        // A later Start must not discard an earlier run failure before host shutdown observes it.
        _runTasks.RemoveAll(previous => previous.Status == TaskStatus.RanToCompletion);
        _runTasks.Add(task);
    }

    private async Task CompleteShutdownAsync(TaskCompletionSource<object?> completion)
    {
        try
        {
            await ShutdownCoreAsync().ConfigureAwait(false);
            completion.TrySetResult(null);
        }
        catch (Exception ex)
        {
            completion.TrySetException(ex);
        }
    }

    private async Task ShutdownCoreAsync()
    {
        Task[] runs = [];
        var failures = new List<Exception>();
        try
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (State == SimulatorState.Running)
                    await StopUnderGateAsync(new SystemStopRequest { Reason = "Simulator host shutting down." }).ConfigureAwait(false);
                else
                {
                    StopActiveRunTimers();
                    InvalidateCapture();
                }
            }
            finally
            {
                runs = _runTasks.ToArray();
                _gate.Release();
            }
        }
        catch (Exception ex)
        {
            failures.Add(ex);
        }

        // A run may be waiting for the gate; never join it while holding that gate.
        var joined = Task.WhenAll(runs);
        try { await joined.ConfigureAwait(false); }
        catch (Exception ex)
        {
            if (joined.Exception is { } aggregate)
                failures.AddRange(aggregate.Flatten().InnerExceptions);
            else
                failures.Add(ex);
        }

        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException("Simulator host shutdown encountered multiple failures.", failures);
    }
}
