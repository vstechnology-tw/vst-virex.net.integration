# C++ Raw RESTful API Sample

Demonstrates the current RESTful API using a C++ sample.

Run the simulator first and press **Start Servers**.

Build from Visual Studio Developer PowerShell:

```powershell
cmake -S samples\cpp-raw-rest -B samples\cpp-raw-rest\build
cmake --build samples\cpp-raw-rest\build --config Release
samples\cpp-raw-rest\build\Release\cpp-raw-rest.exe
```

`SendRequest` is a helper defined in `main.cpp`; it is not a function from an external library. The source includes the Windows SDK WinHTTP headers and CMake links `winhttp.lib`.

The sample calls status, initialize, ProductInfo update, start, stop, and results endpoints.
