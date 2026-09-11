# Command completion and errors (2.2.3)

REST returns an HTTP response for every completed request. Invalid JSON or run
mode returns 400, invalid lifecycle state returns 409, and an operation that
cannot complete returns 503. The C# REST client throws `VirexClientException`
with the status and response body for non-success responses.

MQTT requests use `correlationId` and receive a response on
`responses/{correlationId}` under the configured root topic. A request with an
invalid typed payload still receives a negative response if its correlationId
can be read. If the JSON itself is unreadable, a rejection event is emitted;
there is no reliable correlation topic without an identifiable request ID.
Failed SDK queries throw `VirexCommandException`, rather than returning empty
results or a default state. Malformed or missing response payloads also fail.

TCP query frames (`status`, `error`, `getProductInfo`, `results`) have direct
responses. The 2.2.3 SDK includes an optional `requestId`; the server echoes it
on query responses and query rejection events. The SDK ignores rejections for
other request IDs. Successful queries remain compatible with older servers;
older servers that cannot correlate a rejection may cause a bounded timeout.
`TimeoutMs` bounds a query, while caller cancellation remains cancellation.

TCP control methods named `Send*Async` only send a frame. Observe the completion
or `commandRejected` event to know the server outcome. The supported commands
are initialize, deinitialize, productInfo, start and stop. New clients send
`type: productInfo`; the known `productInfoChanged` command alias remains
accepted for SDK <= 2.2.2. Other unknown types cannot update product data.

`errorChanged` describes an active runtime error and always includes `hasError`.
Validation failures and failed operations emit `commandRejected` with the current
state and available recovery context. A failed command does not imply that the
system became Uninitialized. Query failures must not be interpreted as empty
successful results.

ProductInfo completes before `productInfoChanged` is emitted. Start is rejected
while ProductInfo is updating. An accepted Start retains that product snapshot
for its results. Stop requires Running; a service with source preparation may
also accept Stop to cancel an outstanding Start before it enters Running.
Deinitialize requires Ready or an active/retryable deinitialization.

`imageGrabbed.captureId` identifies the acquisition group and is shared by related
result summaries. Multi-source image events may share a captureId and include
optional `sourceId` and `frameId` to identify the individual image. Consumers must
ignore unknown optional fields. Results saved before any image was acquired may
have an empty captureId. No image/result paths are promised by imageGrabbed; use
resultCreated or the results query after persistence.

The ProductInfo schema uses six string fields. Older senders may omit fields and
receive protocol defaults, but explicit null/object/array values are not valid
string values. Send all six fields for portable integration.
