# Exact-name event subscriptions

The REST catalog at `GET /api/events` returns `appId`, `instanceId`, and producer-supported `names`. `AppId` is a stable host identity. `InstanceId` changes when that App instance restarts. Names are case-sensitive complete strings. Neither wildcard expansion nor a fixed SDK name list is used.

```csharp
var catalog = await client.GetEventCatalogAsync(token);
var request = new SystemStartRequest { RunMode = ControlRunModes.Continue };
var subscription = await client.TcpEvents.SubscribeAsync(
    new[] { "captureReady", "captureCompleted", "resultCreated" },
    envelope => Consume(envelope),
    issue => Report(issue.Code, issue.Message),
    new EventSubscriptionScope { AppId = catalog.AppId,
        InstanceId = catalog.InstanceId, RequestId = request.RequestId }, token);
try
{
    var accepted = await client.StartAsync(request, token);
    // Acceptance and events can arrive in either order. RequestId exists before REST sends.
    // Acceptance is not capture readiness. Use the matching producer event.
}
finally
{
    await subscription.CloseAsync();
}
```

Each subscription owns one TCP connection. Send a `subscribeEvents` NDJSON object with a `requestId` and nonempty `names` array. The `eventSubscription` reply returns the active names, App identity, and a new `connectionGeneration`. The server acknowledges before sending events for the new names. A rejected request changes none of the active names. For a recognized `subscribeEvents` or `unsubscribeEvents` request with a single validation error, repeated names return `duplicate_event_subscription`, an unknown name returns `unknown_event_name`, and malformed request fields return `invalid_event_payload`. A `subscribeEvents` request for an already active name also returns `duplicate_event_subscription` if no earlier validation fails. When multiple validation errors are present, the server returns the first one it detects. A frame that is not valid JSON is not recognized as a subscription request, so no `eventSubscription` acknowledgement is sent for that frame. `unsubscribeEvents` has the same request shape and acknowledgement; removing a name which is not active returns `event_not_subscribed`.

An `event` envelope has a `name`, unique producer `eventId`, UTC `timestamp`, App identity, connection generation, object `payload`, and applicable `requestId`, `jobId`, `captureId`, `resultId`. Generic names require no SDK enum change. Each name still requires an actual producer. Simulator hosts can configure synthetic custom names with `ConfigureEvents` before use and produce them with `EmitEvent`. This is synthetic behavior and does not establish real App or equipment support.

SDK callbacks execute serially on the receive loop. UI consumers must dispatch UI work themselves. Optional exact work scope filters report mismatches as `stale_event_work`; identity/generation mismatches, unknown delivered names, malformed payloads and duplicate event IDs also report issues. `LastIssue` retains the latest diagnostic when no diagnostic callback is supplied. Duplicate detection retains the most recent 4096 IDs on this connection. No automatic reconnect or replay occurs.

`UnsubscribeAsync` waits for the acknowledgement and acts as a callback barrier for removed names. Other names remain active. `Dispose` synchronously stops delivery, including joining an already running callback from another thread. `CloseAsync` also joins the receive loop and propagates transport or consumer callback failures through `Completion`. Cancellation closes the socket, including on .NET Framework. If an unsubscribe outcome is ambiguous because of cancellation or timeout, the SDK closes the subscription. A callback may call `Dispose`; it must not synchronously wait for `CloseAsync` or `UnsubscribeAsync` from the receive loop.

Existing typed TCP streams remain available through `RunAsync` for existing samples and guards. Their initial status/product snapshots and broadcast behavior are separate from this explicitly acknowledged subscription protocol. AML-specific IDs and payload conversion belong to the consumer, outside these public contracts.
