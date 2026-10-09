using Virex.NET.Client;
using Virex.NET.Contracts;

namespace Virex.NET.Samples;

public static class CommittedResultReader
{
    public static async Task<DataSourceValue> ReadAsync(VirexClient client, CommandResponse accepted,
        EventEnvelope resultEvent, string sourceName, CancellationToken cancellationToken = default)
    {
        if (!accepted.Accepted || string.IsNullOrWhiteSpace(resultEvent.ResultId)
            || string.IsNullOrWhiteSpace(accepted.AppId) || string.IsNullOrWhiteSpace(accepted.InstanceId)
            || string.IsNullOrWhiteSpace(accepted.JobId) || string.IsNullOrWhiteSpace(accepted.CaptureId)
            || resultEvent.AppId != accepted.AppId || resultEvent.InstanceId != accepted.InstanceId
            || resultEvent.RequestId != accepted.RequestId || resultEvent.JobId != accepted.JobId
            || resultEvent.CaptureId != accepted.CaptureId)
            throw new InvalidOperationException("Result event does not belong to the accepted work.");

        var value = await client.ReadDataSourceAsync(sourceName, resultEvent.ResultId, cancellationToken);
        if (value.Kind != DataSourceKinds.Result || value.ResultId != resultEvent.ResultId
            || value.AppId != accepted.AppId || value.InstanceId != accepted.InstanceId
            || value.RequestId != accepted.RequestId || value.JobId != accepted.JobId || value.CaptureId != accepted.CaptureId)
            throw new InvalidOperationException("Committed value does not belong to the accepted work.");
        return value;
    }
}
