# Capture readiness and completion

`captureReady` permits an external scanner to begin the identified capture: **every source in sourceIds has successfully completed preparation and is armed to accept acquisition for that job/capture**. It is separate from lifecycle Ready or runStarted. A failed preparation must reject Start with capture_preparation_failed (REST 503) and emit no readiness permission. A source that cannot establish this guarantee must not emit captureReady.

`captureCompleted` means acquisition for **all sources in the ready set has explicitly ended**, with their images handed to the application. It is emitted once for that capture. It does not wait for inspection, result creation or artifact storage and does not promise a successful result. Individual imageGrabbed events, image counts or a first source finishing do not establish whole-capture completion. runCompleted is lifecycle behavior and cannot substitute for either new event.

| Field | captureReady | captureCompleted |
| --- | --- | --- |
| jobId | Required, server-assigned ID for one accepted Start | Same job |
| captureId | Required acquisition group ID | Same capture |
| timestamp | ISO 8601 time readiness was established | Time all source acquisition ended |
| sourceIds | Nonempty, unique set of prepared sources | Same complete source set |
| imageCount | Absent | Total individual images, at least one per source |

Start replies (REST/MQTT) add jobId and the first captureId. Each imageGrabbed and resultCreated can add jobId; existing captureId still links images and exact ResultId artifacts. A continuous Start uses one jobId and distinct captureIds, announcing readiness for each capture. A subsequent Start always uses a new jobId. Existing image payloads without jobId remain parseable, and SDK method signatures and runMode/inspectionMode semantics remain unchanged.

Example event sequence: captureReady(job-A, cap-1, sources A/B) → imageGrabbed(A,frame-1) → imageGrabbed(A,frame-2) → imageGrabbed(B,frame-1) → captureCompleted(job-A,cap-1,3 images) → later resultCreated(job-A,cap-1). CaptureOnly emits acquisition events without resultCreated. All source-end callbacks are required independently of frame counts; duplicate frames/end callbacks are ignored, and unknown sources, old job/capture IDs, cancellation or source faults cannot advance a capture to completion. An already completed acquisition remains completed if later inspection or storage fails.

TCP NDJSON uses type captureReady/captureCompleted, with fields flattened in the event. MQTT uses corresponding child topics under the configured base topic; SDK attaches type from the topic and returns VirexEvent.CaptureReady / CaptureCompleted. The Simulator queues MQTT events in emission order. Parsers reject new events with missing IDs, invalid timestamp, missing/duplicate sources or an invalid image count. Old events retain their prior parsing.

These are live events, with no replay or retained scan permission. Subscribe before Start and match the returned job/capture. Consumers must deduplicate by jobId/captureId, ignore late events from cancelled/old work, and invalidate scan permission on Stop, acquisition fault, disconnection or source readiness loss. A new event must never reactivate an ended job. If connection state is uncertain, stop/reconcile before requesting a new job rather than using an old permission.

Simulator Core defaults to one synthetic source (`simulator`) and one frame; ConfigureCaptureSimulationAsync can select multiple synthetic source IDs, frames per source, or preparation failure while inactive. Its callback accounting requires explicit per-source completion and rejects duplicates/late/cancelled callbacks. Dummy image/result artifacts preserve existing paths; multi-source event images are synthetic and artifacts are representative simulator output.

**Hardware acceptance is separate:** App #283 must prove the supported source actually completed setup and is armed before readiness, signal loss/fault and invalidate the job, and publish completion only after all relevant source acquisitions end, independently of inspection. Simulator PASS does not establish camera, external-trigger, scan timing or customer-equipment readiness. No customer protocol names or private acquisition implementation are included here.
