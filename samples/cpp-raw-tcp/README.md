# C++ Raw TCP Sample

Demonstrates TCP/NDJSON frames for the current public protocol.

Run the simulator first and press **Start Servers**.

Build from Visual Studio Developer PowerShell:

```powershell
cmake -S samples\cpp-raw-tcp -B samples\cpp-raw-tcp\build
cmake --build samples\cpp-raw-tcp\build --config Release
samples\cpp-raw-tcp\build\Release\cpp-raw-tcp.exe
```

Expected events include `statusChanged`, `productInfoChanged`, `runStarted`, `runCompleted`, and `resultCreated`.
