# Alarm sources and two-stage recovery

`GET /api/alarms` returns an `AlarmCatalog` with `appId`, `instanceId`, read `timestamp`, and `sources`. Each descriptor contains an exact case-sensitive `name`, a `source` producer, and a `category`. The name is also the event name in `GET /api/events`. There is no Alarm name enum or SDK whitelist. Customer IDs, wire formatting, and whole-machine disposition belong to the consumer.

Subscribe to any required names with the existing TCP/NDJSON SDK. The callback receives an `EventEnvelope`; its owned object payload is `AlarmNotification`. Compare App/Instance and work identity before using it. Its timestamp is the occurrence time; the envelope timestamp is publication time. Optional RequestId/JobId/CaptureId/ResultId are supplied only when the actual source knows them. A runtime fault before a result commits has no ResultId.

```csharp
var catalog = await client.GetAlarmsAsync(cancellationToken);
var subscription = await client.TcpEvents.SubscribeAsync(
    catalog.Sources.Select(source => source.Name),
    evt => Console.WriteLine(evt.Payload.GetRawText()),
    scope: new EventSubscriptionScope { AppId = catalog.AppId, InstanceId = catalog.InstanceId },
    cancellationToken: cancellationToken);
try
{
    // Keep the subscription alive while consuming occurrences.
}
finally { await subscription.CloseAsync(); }
```

| Category | Public behavior |
| --- | --- |
| `fault` | Stop unfinished work, invalidate capture callbacks, and latch `Faulted`. Start/Initialize cannot bypass recovery. Other App sessions continue. |
| `defect` | Notify an actual occurrence without independently latching hardware fault. |
| `criticalDefect` | Notify separately; the Machine decides whether to stop the whole machine. |

`POST /api/system/reset` / `client.ResetAsync()` stops work, joins cancelled synthetic run tasks, and reuses Deinitialize cleanup under the same session owner. Concurrent reset callers join one attempt. Deinitialize overlapping that attempt shares its cleanup. Success returns `Uninitialized`; a repeated reset there succeeds without another cleanup. Cleanup failure returns HTTP 409, `accepted=false`, `errorCode=cleanup_failed`, and `Faulted`. A new fault during recovery survives the older attempt. Reset never resumes previous work.

Then call `POST /api/system/initialize` / `client.InitializeAsync()` explicitly. Only completed initialization returns `Ready`. Reset from Running cancels that work as well. With operation management enabled, Local refuses an external Reset with `operation_not_allowed`, without stopping work or clearing its fault/recipe. Read-only queries and mode changes remain available.

Cancellation before a client request has no server effects. Cancellation/disconnect after server admission stops the caller waiting; server cleanup still completes. The SDK preserves caller cancellation and reports HTTP command failures through `VirexClientException`. Catalog failures, malformed success payloads, and API timeout use `VirexQueryException` / `api_failed`; they do not become empty success. `VirexRestClient` borrows the caller's HttpClient. The caller owns it, and owns each subscription's close/disposal. Calls may run concurrently.

The Simulator is synthetic. Its actual built-in producers are `simulator.acquisitionFault` (`SimulateAcquisitionFaultAsync`), `simulator.persistenceFailed` (a failed dummy artifact write), and `simulator.cleanupFailed` (failed cleanup). Each is catalogued and observable over TCP. `ConfigureAlarmSourceAsync` registers a copied custom descriptor before initialization; `RaiseAlarmAsync` is its explicit synthetic occurrence producer. Names are unrestricted full strings. It rejects a stale expected JobId, an uncommitted/unknown/superseded result, or a closed host without emitting; it never substitutes a previous result. Built-in names cannot be manually injected through this method. Generic EmitEvent also refuses Alarm names so it cannot bypass the classified producer. `EmitError` is diagnostic ErrorInfo only and is not a complete classified Alarm producer.

Synthetic success and compile gates do not validate real device faults, native cleanup, a customer profile, hardware, or Machine acceptance.

ResultId is optional. With a selector, RaiseAlarmAsync requires the current committed result. Without one, an actual current-work defect/criticalDefect occurrence remains valid; it is not ResultCreated/DefectResult and never borrows a preceding result.
