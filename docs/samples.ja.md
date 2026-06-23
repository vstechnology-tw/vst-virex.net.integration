# Samples

Sample projects は guided demos です。Tester にどの simulator button を押すか、どの state を検証するか、どの output を期待するかを案内します。

Sample を実行する前に:

1. Simulator を起動します。

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

2. Default endpoints のままにします。
3. **Start Servers** を押します。

## C# Samples

| Sample | Command | Purpose |
| --- | --- | --- |
| `samples/csharp-sdk` | `dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj` | 推奨される .NET entry point。`not_initialized`、**Initialize**、WaferInfo、start、result query を示します。 |
| `samples/csharp-raw-rest` | `dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj` | Direct REST status、WaferInfo、start、result query。 |
| `samples/csharp-raw-tcp` | `dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj` | TCP/NDJSON initial frames と WaferInfo update event。 |
| `samples/csharp-raw-mqtt` | `dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj` | MQTT status、wafer-info、result、error event observation。 |

## Python Samples

| Sample | Command | Purpose |
| --- | --- | --- |
| `samples/python-raw-rest` | `python samples\python-raw-rest\main.py` | Python standard library HTTP support を使った REST calls。 |
| `samples/python-raw-tcp` | `python samples\python-raw-tcp\main.py` | TCP/NDJSON socket demo。 |
| `samples/python-raw-mqtt` | `python samples\python-raw-mqtt\main.py` | MQTT event observation demo。 |

## C++ Samples

Visual Studio Developer PowerShell から C++ samples を build します。

```powershell
cmake -S samples\cpp-raw-rest -B samples\cpp-raw-rest\build
cmake --build samples\cpp-raw-rest\build --config Release
samples\cpp-raw-rest\build\Release\cpp-raw-rest.exe

cmake -S samples\cpp-raw-tcp -B samples\cpp-raw-tcp\build
cmake --build samples\cpp-raw-tcp\build --config Release
samples\cpp-raw-tcp\build\Release\cpp-raw-tcp.exe

cmake -S samples\cpp-raw-mqtt -B samples\cpp-raw-mqtt\build
cmake --build samples\cpp-raw-mqtt\build --config Release
samples\cpp-raw-mqtt\build\Release\cpp-raw-mqtt.exe
```

## Expected Guided Behavior

| Flow | Expected behavior |
| --- | --- |
| SDK and REST | **Initialize** 前は start が `HTTP 409 not_initialized` を返します。**Initialize** 後、sample は WaferInfo を更新し、cycle を開始し、results を query します。 |
| TCP | Sample は port `5089` に接続し、initial status と wafer-info frames を読み取り、WaferInfo NDJSON frame を送信し、simulator から返る update event を出力します。 |
| MQTT | Sample は `virex/#` を subscribe します。実行中に **Apply WaferInfo**、**Initialize**、**Start Cycle**、**Emit Fake Result**、**Emit Error** を押して matching events を確認します。 |

## Troubleshooting

| Symptom | Resolution |
| --- | --- |
| Server not started | Samples を実行する前に simulator で **Start Servers** を押します。 |
| `not_initialized` | SDK と REST guided demos では **Initialize** 前の expected behavior です。**Initialize** を押して続行します。 |
| No MQTT events | Base topic `virex`、broker `127.0.0.1:1883`、sample が listening 中であることを確認します。 |
| No result returned | **Start Cycle** または **Emit Fake Result** を押し、matching WaferInfo fields で query します。 |
