# サンプル

サンプル プロジェクトは、公開 ProductInfo とステート マシン コントラクトのガイド付きデモンストレーションを提供します。

例を実行する前に:

1. シミュレーターを起動します。

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

2. 既定のエンドポイントをそのまま使用します。
3. **Start Servers** を押します。

## C# の例

|例 |コマンド |目的 |
| --- | --- | --- |
| `samples/csharp-sdk` | `dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj` |推奨される .NET エントリ ポイント。初期化、ProductInfo、開始、停止、結果クエリを示します。 |
| `samples/csharp-raw-rest` | `dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj` | RESTful API 状態クエリ、ProductInfo、システム コマンド、および結果クエリを直接呼び出します。 |
| `samples/csharp-raw-tcp` | `dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj` | TCP/NDJSON 初期フレームと ProductInfo 更新イベントを示します。 |
| `samples/csharp-raw-mqtt` | `dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj` | MQTT 状態、ProductInfo、実行、結果、および拒否イベントを監視します。 |

## Python の例

|例 |コマンド |目的 |
| --- | --- | --- |
| `samples/python-raw-rest` | `python samples\python-raw-rest\main.py` | Python 標準ライブラリの HTTP サポートを使用します。 |
| `samples/python-raw-tcp` | `python samples\python-raw-tcp\main.py` | TCP/NDJSON ソケットのデモ。 |
| `samples/python-raw-mqtt` | `python samples\python-raw-mqtt\main.py` | MQTT イベントの観察を示します。 |

## C++ の例

Visual Studio 開発者 PowerShell からビルド:

```powershell
cmake -S samples\cpp-raw-rest -B samples\cpp-raw-rest\build
cmake --build samples\cpp-raw-rest\build --config Release
samples\cpp-raw-rest\build\Release\cpp-raw-rest.exe
```

TCP および MQTT の例では、同じ CMake パターンを使用します。

## 期待される動作

|フロー |期待される動作 |
| --- | --- |
| SDK および RESTful API |初期化は `Uninitialized` から `Ready` に移行します。 ProductInfo の更新は `Ready` に戻ります。開始は `Running` を返します。実行の完了後に結果を照会できます。 |
| TCP |このサンプルは、`5089` に接続し、初期状態/ProductInfo フレームを読み取り、ProductInfo NDJSON フレームを送信し、更新イベントを出力します。 |
| MQTT |このサンプルは、`virex/#` をサブスクライブし、`statusChanged`、`productInfoChanged`、`runStarted`、`runCompleted`、`resultCreated`、および `commandRejected` を出力します。 |
