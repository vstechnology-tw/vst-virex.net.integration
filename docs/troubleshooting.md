# Troubleshooting

## REST Request Failed

Check:

- The simulator or production endpoint is running.
- REST base URL is correct.
- Firewall allows configured ports.
- The route is listed at [REST API](rest-api.md).

## Command Returns `invalid_state`

The command reached the service, but it is not valid in the current state.

Examples:

- `Start` is valid only in `Ready`.
- `Stop` is valid only in `Running`.
- `SetProductInfo` is valid only in `Ready`.
- `Initialize` is valid only in `Uninitialized`.
- `Deinitialize` is valid only in `Ready`.

Read `GET /api/status` first, then send the command when the state allows it.

## No Results Returned

Check:

- A run has started and completed.
- Result query filters match the ProductInfo snapshot captured when `Start` was accepted.
- Filters use `waferID`, `lotID`, or `recipe`.

## TCP Event Missing

Check:

- TCP host/port is correct.
- Client keeps the socket open.
- Each incoming frame ends with `\n`.
- Client parses the documented event names, such as `statusChanged` and `resultCreated`.

## MQTT Event Missing

Check:

- Broker host/port is correct.
- The subscription matches the root topic, such as `virex/#`.
- MQTT is only used for outgoing events.
- Client listens to documented topic names, such as `productInfoChanged`.

## Local Preview Differs From GitHub Pages

Use MkDocs locally:

```powershell
python -m mkdocs serve --dev-addr 127.0.0.1:8000
```

The GitHub Pages workflow does:

```powershell
python -m mkdocs build --strict
```
