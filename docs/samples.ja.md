# サンプル

サンプル プロジェクトは、公開 ProductInfo、ライフサイクル、イベント、結果コントラクトを完全な形で実行できるデモとして提供します。

例を実行する前に:

1. シミュレーターを起動します。

   ```powershell
   dotnet run --project src\Virex.NET.Simulator.WPF\Virex.NET.Simulator.WPF.csproj
   ```

2. 既定のエンドポイント設定を使用します。
3. **Start Servers** を押します。

## コピーして実行できる完全なソース

以下のリンクは省略されていない完全なファイルで、必要な `using`、`import`、`#include` を含みます。各プロジェクトまたはビルド設定と一緒に使用してください。

| 言語 / transport | 完全なソース | 依存関係とビルド |
| --- | --- | --- |
| C# SDK | [Program.cs](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-sdk/Program.cs) · [project](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-sdk/CSharpSdkSample.csproj) | .NET 8 と `Virex.NET.Client` project reference。 |
| C# raw REST | [Program.cs](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-raw-rest/Program.cs) · [project](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-raw-rest/CSharpRawRestSample.csproj) | .NET 8 と `Virex.NET.Contracts` project reference。 |
| C# raw TCP | [Program.cs](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-raw-tcp/Program.cs) · [project](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-raw-tcp/CSharpRawTcpSample.csproj) | .NET 8 と `Virex.NET.Contracts` project reference。 |
| C# raw MQTT | [Program.cs](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-raw-mqtt/Program.cs) · [project](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/csharp-raw-mqtt/CSharpRawMqttSample.csproj) | .NET 8、MQTTnet、`Virex.NET.Contracts` project reference。 |
| Python raw REST | [main.py](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/python-raw-rest/main.py) | Python 標準ライブラリのみ。 |
| Python raw TCP | [main.py](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/python-raw-tcp/main.py) | Python 標準ライブラリのみ。 |
| Python raw MQTT | [main.py](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/python-raw-mqtt/main.py) | Python 標準ライブラリのみ。サンプルが必要な MQTT 交換を直接実装します。 |
| C++ raw REST | [main.cpp](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/cpp-raw-rest/main.cpp) · [CMakeLists.txt](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/cpp-raw-rest/CMakeLists.txt) | Windows SDK WinHTTP (`winhttp.lib`)。`SendRequest` は `main.cpp` 内のヘルパーで、第三者ライブラリではありません。 |
| C++ raw TCP | [main.cpp](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/cpp-raw-tcp/main.cpp) · [CMakeLists.txt](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/cpp-raw-tcp/CMakeLists.txt) | Windows SDK Winsock2 (`ws2_32.lib`)。 |
| C++ raw MQTT | [main.cpp](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/cpp-raw-mqtt/main.cpp) · [CMakeLists.txt](https://github.com/vstechnology-tw/vst-virex.net.integration/blob/main/samples/cpp-raw-mqtt/CMakeLists.txt) | Windows SDK Winsock2 (`ws2_32.lib`)、外部 MQTT ライブラリ不要。 |

RESTful API、TCP、MQTT のリファレンスページの language tabs は、各操作を説明する短いリクエスト断片です。直接貼り付けて実行する場合は、上記の完全なソースを使用してください。

## C# の例

|例 |コマンド |目的 |
| --- | --- | --- |
| `samples/csharp-sdk` | `dotnet run --project samples\csharp-sdk\CSharpSdkSample.csproj` |推奨される .NET エントリ ポイント。初期化、ProductInfo、開始、停止、結果クエリを示します。 |
| `samples/csharp-raw-rest` | `dotnet run --project samples\csharp-raw-rest\CSharpRawRestSample.csproj` | RESTful API 状態クエリ、ProductInfo、システム コマンド、および結果クエリを直接呼び出します。 |
| `samples/csharp-raw-tcp` | `dotnet run --project samples\csharp-raw-tcp\CSharpRawTcpSample.csproj` | TCP/NDJSON 初期フレーム、コマンド、イベントフレームを示します。 |
| `samples/csharp-raw-mqtt` | `dotnet run --project samples\csharp-raw-mqtt\CSharpRawMqttSample.csproj` | MQTT イベントを監視し、対応するコマンド/クエリを発行します。 |

## Python の例

|例 |コマンド |目的 |
| --- | --- | --- |
| `samples/python-raw-rest` | `python samples\python-raw-rest\main.py` | Python 標準ライブラリの HTTP サポートを使用します。 |
| `samples/python-raw-tcp` | `python samples\python-raw-tcp\main.py` | TCP/NDJSON ソケットのコマンドとクエリのデモ。 |
| `samples/python-raw-mqtt` | `python samples\python-raw-mqtt\main.py` | MQTT イベント観察とコマンド/クエリ topic のデモ。 |

## C++ の例

Visual Studio 開発者 PowerShell からビルド:

```powershell
cmake -S samples\cpp-raw-rest -B samples\cpp-raw-rest\build
cmake --build samples\cpp-raw-rest\build --config Release
samples\cpp-raw-rest\build\Release\cpp-raw-rest.exe
```

TCP および MQTT の例では同じ CMake パターンを使用します。C++ ソースには必要な Windows ヘッダーと library directive があり、REST の `SendRequest` はソース内に定義されています。

## 期待される動作

|フロー |期待される動作 |
| --- | --- | --- |
| SDK および RESTful API |初期化は `Uninitialized` から `Ready` に移行します。 ProductInfo の更新は `Ready` に戻ります。開始は `Running` を返します。実行の完了後に結果を照会できます。 |
| TCP |サンプルは `5089` に接続し、初期状態/ProductInfo フレームを読み取り、ProductInfo/start/stop フレームを送信し、`imageGrabbed` と `resultCreated` を含むイベントフレームを監視します。 |
| MQTT |サンプルは `virex/#` をサブスクライブし、`imageGrabbed` を含むイベントを表示し、`commands/status/get` を発行して対応する `responses/{correlationId}` payload を表示します。 |