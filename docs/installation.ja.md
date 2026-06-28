# インストール/ダウンロード

このページでは、公開 Virex.NET 統合ツールを入手する方法について説明します。

- GitHub リリースから Windows シミュレーターをダウンロードします。
- NuGet から C# SDK パッケージをインストールします。
- 検証プロセスの次のステップを選択します。

## GitHub Releases: 事前に構築されたシミュレーター EXE

現在のシミュレーター リリースは `v2.0.3` です。

[Virex.NET Simulator v2.0.3 をダウンロードする](https://github.com/vstechnology-tw/vst-virex.net.integration/releases/tag/v2.0.3)

各 ZIP には `Virex.NET.Simulator.WPF.exe` が含まれています。ターゲット環境に応じてパッケージを選択してください。

|ファイル |ランタイム |いつ使用するか |
| --- | --- | --- |
| `Virex.NET.Simulator-v2.0.3-net48-win-x64.zip` | .NET Framework 4.8 |ターゲット Windows マシンに既に .NET Framework 4.8 がインストールされている場合に使用します。 |
| `Virex.NET.Simulator-v2.0.3-net8.0-windows-win-x64-self-contained.zip` | .NET 8 ウィンドウ |ランタイムを含む最新の Windows ビルドが必要な場合に使用します。 |
| `Virex.NET.Simulator-v2.0.3-net10.0-windows-win-x64-self-contained.zip` | .NET 10 ウィンドウ |サポートされている最新の Windows ターゲットを検証する必要がある場合に使用します。 |

ダウンロード後:

1. ZIP を解凍します。
2. `Virex.NET.Simulator.WPF.exe`を実行します。
3. テスト環境で別のポートが必要でない限り、既定のエンドポイントをそのまま使用します。
4. SDK または元のプロトコルの例を実行する前に、**Start Servers** を押します。

シミュレーターのボタン、既定のエンドポイント、および操作手順については、[シミュレーターガイド](simulator.ja.md) を参照してください。

## NuGet パッケージ

|パッケージ |目的 |
| --- | --- |
| [Virex.NET.Contracts](https://www.nuget.org/packages/Virex.NET.Contracts/) |公開 C# データモデル、REST ルート、MQTT トピック名、TCP/NDJSON フォーマットおよび解析ツールを提供します。 |
| [Virex.NET.Client](https://www.nuget.org/packages/Virex.NET.Client/) | C# REST、TCP、および MQTT の SDK ラッパー。 |

SDK をインストールします。

```powershell
dotnet add package Virex.NET.Client --version 2.0.3
```

共有契約パッケージのみをインストールします。

```powershell
dotnet add package Virex.NET.Contracts --version 2.0.3
```

`Virex.NET.Client` および `Virex.NET.Contracts` は `netstandard2.0` をターゲットとします。

## 次のステップ

- 最初の統合検証フロー: [クイックスタート](quick-start.ja.md) を参照してください。
- C# SDK の使用法を完了します。[C# SDK ガイド](csharp-sdk.ja.md) を参照してください。
- 他の言語またはRaw プロトコルの例: [サンプル](samples.ja.md) を参照してください。
