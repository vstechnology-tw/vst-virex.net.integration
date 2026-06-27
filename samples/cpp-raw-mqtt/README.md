# C++ Raw MQTT Sample

Demonstrates MQTT event subscription for the current public protocol.

Run the simulator first and press **Start Servers**.

Build from Visual Studio Developer PowerShell:

```powershell
cmake -S samples\cpp-raw-mqtt -B samples\cpp-raw-mqtt\build
cmake --build samples\cpp-raw-mqtt\build --config Release
samples\cpp-raw-mqtt\build\Release\cpp-raw-mqtt.exe
```

The sample subscribes to `virex/#` and prints received event topics.
