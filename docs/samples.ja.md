# サンプル

サンプル プロジェクトはガイド付きデモです。テスターに、どのシミュレーター ボタンを押すか、どの状態をテストしているか、どの出力を期待すべきかを示します。

サンプルを実行する前に:

1. シミュレーターを起動します。

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

2. 既定のエンドポイントを使用します。
3. **Start Servers** を押します。

## C# サンプル

| サンプル | コマンド | 目的 |
| --- | --- | --- |
| `samples/csharp-sdk` | `dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj` | 推奨される .NET の入口です。`not_initialized`、**Initialize**、WaferInfo、開始、結果クエリを示します。 |
| `samples/csharp-raw-rest` | `dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj` | REST による状態、WaferInfo、開始、結果クエリを直接示します。 |
| `samples/csharp-raw-tcp` | `dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj` | TCP/NDJSON の初期フレームと WaferInfo 更新イベントを確認します。 |
| `samples/csharp-raw-mqtt` | `dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj` | MQTT の status、wafer-info、result、error イベント観測を確認します。 |

## Python サンプル

| サンプル | コマンド | 目的 |
| --- | --- | --- |
| `samples/python-raw-rest` | `python samples\python-raw-rest\main.py` | Python 標準ライブラリの HTTP 機能を使った REST 呼び出し。 |
| `samples/python-raw-tcp` | `python samples\python-raw-tcp\main.py` | TCP/NDJSON ソケット デモ。 |
| `samples/python-raw-mqtt` | `python samples\python-raw-mqtt\main.py` | MQTT イベント観測デモ。 |

## C++ サンプル

Visual Studio Developer PowerShell から C++ サンプルをビルドします。

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

## 想定されるガイド付き動作

| フロー | 想定動作 |
| --- | --- |
| SDK と REST | **Initialize** 前は start が `HTTP 409 not_initialized` を返します。**Initialize** 後、サンプルは WaferInfo を更新し、サイクルを開始し、結果をクエリします。 |
| TCP | サンプルはポート `5089` に接続し、初期 status フレームと wafer-info フレームを読み取り、WaferInfo NDJSON フレームを送信して、シミュレーターから返された更新イベントを出力します。 |
| MQTT | サンプルは `virex/#` を購読します。実行中に **Apply WaferInfo**、**Initialize**、**Start Cycle**、**Emit Fake Result**、**Emit Error** を押すと、対応するイベントを確認できます。 |

## トラブルシューティング

| 症状 | 解決方法 |
| --- | --- |
| サーバーが開始されていない | サンプルを実行する前に、シミュレーターで **Start Servers** を押します。 |
| `not_initialized` | SDK と REST のガイド付きデモでは、**Initialize** 前の想定動作です。**Initialize** を押して続行します。 |
| MQTT イベントがない | ベース トピック `virex`、ブローカー `127.0.0.1:1883`、サンプルがまだ待ち受け中であることを確認します。 |
| 結果が返らない | **Start Cycle** または **Emit Fake Result** を押してから、一致する WaferInfo フィールドでクエリします。 |
