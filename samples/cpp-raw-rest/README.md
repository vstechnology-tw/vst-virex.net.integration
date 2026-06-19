# C++ Raw REST Sample

Minimal Windows C++ client for the public Virex.NET REST API. It uses WinHTTP and does not require third-party packages.

## Prerequisites

- Windows.
- CMake 3.20 or newer.
- Visual Studio Build Tools or Visual Studio with the C++ desktop workload.
- The Virex.NET simulator running with REST enabled at `http://127.0.0.1:5088`.

## Build

From a Visual Studio Developer PowerShell:

```powershell
cmake -S samples\cpp-raw-rest -B samples\cpp-raw-rest\build
cmake --build samples\cpp-raw-rest\build --config Release
```

## Run

From the repository root, start the simulator:

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

In the simulator window, click **Start Servers**. Then run:

```powershell
samples\cpp-raw-rest\build\Release\cpp-raw-rest.exe
```

Optional base URL:

```powershell
samples\cpp-raw-rest\build\Release\cpp-raw-rest.exe http://127.0.0.1:5088
```

Expected result:

```text
Status: {"initialized":false,"processState":"ready","recipe":"Default"}
WaferInfo updated through raw REST.
```
