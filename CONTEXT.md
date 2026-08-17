# Virex.NET Integration Event Context

This context defines the public vocabulary for pushed integration events and simulator artifacts in the Virex.NET Integration Kit.

## Public integration events

**ImageGrabbed**:
An event indicating that one image acquisition completed successfully, before artifact persistence. It has capture metadata but no image or result path, and it does not mean that inspection has completed.
_Avoid_: ImageProcessed, ResultCreated

**Capture**:
One successful image acquisition associated with a single inspection attempt.
_Avoid_: Run, Result

**Capture identity**:
The stable public identity used to associate an acquired image with the result created from the same capture.
_Avoid_: Result identity, timestamp-only correlation

**Result**:
A public summary produced after inspection for a capture. It may include the outcome and references to the related artifacts.
_Avoid_: Image, Capture

## Simulator artifacts

**Dummy artifact**:
A simulator-generated image or result file that represents the public artifact contract for integration testing. It is not a production inspection output or a private algorithm result.
_Avoid_: Real inspection data, private result

**Simulator result root**:
The `results` folder below the simulator execution directory where dummy image, preview, and result artifacts are persisted.
_Avoid_: Source repository path, production result store

**Persisted artifact**:
A simulator-created file that is available through the paths in `Result` after the capture event.
_Avoid_: Capture payload path
