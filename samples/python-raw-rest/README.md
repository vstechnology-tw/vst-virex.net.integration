# Python Raw REST Sample

Minimal Python client for the public Virex.NET REST API.

## Prerequisites

- Python 3.8 or newer.
- The Virex.NET simulator running with REST enabled at `http://127.0.0.1:5088`.
- No third-party Python packages are required.

## Run

From the repository root:

```powershell
dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
```

In the simulator window, click **Start Servers**. Then open a second terminal from the repository root and run:

```powershell
python samples\python-raw-rest\main.py
```

Optional base URL:

```powershell
python samples\python-raw-rest\main.py http://127.0.0.1:5088
```

Expected result:

```text
Status: initialized=False, processState=ready, recipe=Default
WaferInfo updated through raw REST.
```
