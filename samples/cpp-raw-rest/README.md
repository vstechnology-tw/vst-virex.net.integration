# C++ Raw REST Sample

Demonstrates the current REST API using a C++ sample.

Run the simulator first and press **Start Servers**.

Build from Visual Studio Developer PowerShell:

```powershell
cmake -S samples\cpp-raw-rest -B samples\cpp-raw-rest\build
cmake --build samples\cpp-raw-rest\build --config Release
samples\cpp-raw-rest\build\Release\cpp-raw-rest.exe
```

The sample calls status, initialize, ProductInfo update, start, stop, and results endpoints.
