# Emit ImageGrabbed Before Artifact Persistence

For v2.1.1, the public `imageGrabbed` event is emitted immediately after image acquisition with capture metadata and no paths. The simulator then persists dummy image, preview, and result artifacts below its execution-directory `results` root and publishes `resultCreated` with the paths; if persistence fails, the already-valid capture event remains, `errorChanged` is emitted, and `resultCreated` is suppressed.
