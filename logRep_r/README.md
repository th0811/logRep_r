# FFXI_LogRep_r

FINAL FANTASY XI（FFXI）がゲームフォルダー内へ出力する`TEMP`ログを監視し、ローテーションで上書きされる前に継続退避するWindowsアプリです。

通常は`FFXI_LogRep_r.exe`をダブルクリックして使用するWPF GUIアプリですが、同じexeをPowerShellやコマンドプロンプトからCLIとして操作できます。

## 監視対象ファイル

ゲームが出力したログファイルを監視対象とします。  
通所は`C:\Program Files (x86)\PlayOnline\SquareEnix\FINAL FANTASY XI\TEMP`にあるかと思います。

設定した`TEMP`フォルダー直下の次のファイルを監視します。

```text
1_0.log ～ 1_19.log
2_0.log ～ 2_19.log
```

- `1_N.log`: ログウィンドウ1
- `2_N.log`: ログウィンドウ2
- `N`: ローテーションスロット番号

`2_8(1).log`など、上記形式に一致しないファイルは通常監視の対象外です。

## 動作環境

- Windows 10またはWindows 11（64bit）
- 配布用の自己完結版では.NET Runtimeの事前インストールは不要
- 開発時は.NET SDK 8.0.422（`global.json`で指定）

## インストール

1. 配布されたZIPファイルを任意のフォルダーへ展開します。
2. フォルダー内のファイルを削除せず、そのまま使用します。
3. `FFXI_LogRep_r.exe`をダブルクリックします。

インストーラーや管理者権限は不要です。

## GUIでの実行

```powershell
FFXI_LogRep_r.exe
```

初回起動後に「設定」を開き、最低限次の項目を指定します。

- FFXIの`TEMP`フォルダー
- 収集セッションの出力先フォルダー

メイン画面の「収集開始」で監視を開始し、「収集停止」で出力を確定します。

## CLI

引数を指定するとGUIを表示せずCLIモードで動作します。

### コマンド一覧

| コマンド | 説明 |
| --- | --- |
| `help` | ヘルプを表示 |
| `start` | 収集を開始。GUI起動中はIPCでGUIへ要求 |
| `stop` | 起動中GUIの収集を停止 |
| `status` | 起動中GUIまたは現在のプロセスの状態を表示 |
| `once` | 一度だけ対象ログを読み込み、completedセッションを生成 |
| `config get KEY` | 設定値を表示 |
| `config set KEY VALUE` | 設定値を保存 |
| `config path` | 使用する設定ファイルパスを表示 |

### 実行例

GUI起動:

```powershell
FFXI_LogRep_r.exe
```

一度だけ収集:

```powershell
FFXI_LogRep_r.exe once --temp-dir "C:\Program Files (x86)\PlayOnline\SquareEnix\FINAL FANTASY XI\TEMP" --output-dir "D:\FFXILogs\sessions"
```

状態確認:

```powershell
FFXI_LogRep_r.exe status
```

設定値の確認:

```powershell
FFXI_LogRep_r.exe config get polling_interval_ms
```

設定値の変更:

```powershell
FFXI_LogRep_r.exe config set polling_interval_ms 500
```

明示的な設定ファイルを使用する場合は`--config`を指定します。

```powershell
FFXI_LogRep_r.exe config get log_level --config "D:\FFXILogs\config.json"
```

## 出力構成

収集開始ごとに、出力先へ日時形式のセッションフォルダーを作成します。

```text
sessions/
└─ 20260623-213000/
   ├─ session.json
   ├─ raw_records.jsonl
   ├─ canonical_records.jsonl
   ├─ state.json
   ├─ stats.json
   └─ collector.log
```

### session.json

セッションID、開始・終了時刻、監視対象、エンコーディング、各schema version、セッション状態を保存します。

主な状態:

- `active`: 収集中
- `completed`: 正常終了し出力確定済み
- `aborted`: 異常終了または未確定

### raw_records.jsonl

TEMPログから抽出したレコードを、可能な限り一次情報を保持して保存します。

- 元バイト列の16進表現
- デコード済み本文
- 読み取り元ファイルとウィンドウ
- ファイルハッシュ
- パース状態
- 時刻・マーカー検出結果

再解析、パーサー改善、欠損調査、検証用途の正データです。

### canonical_records.jsonl

rawレコードを正規化し、ログウィンドウ間の重複イベントを統合した派生データです。DPS算出ツールなど、通常の後続解析では原則こちらを使用します。

canonicalの件数はraw以下になる場合があります。canonicalデータはrawデータの代替バックアップではありません。

### state.json

監視ファイルの更新情報、処理済みraw ID、canonicalキー、最終並び順など、重複抑止と状態管理に必要な情報を保存します。

### stats.json

raw/canonical保存件数、重複除外件数、パース・デコードエラー数、欠損警告数、最終検出時刻を保存します。

### collector.log

アプリの診断ログ用ファイルです。現在のMVPでは実行経路やログ出力状況により生成されない場合があります。収集データ本体はJSONL、状態確認は`session.json`と`stats.json`を使用してください。

## DPS算出ツールとの連携

本アプリはログの収集・退避・正規化までを担当します。DPS計算そのものは対象外であり、別アプリで行います。

## publish（exeファイルの生成）
.NET 8 SDK とWindows環境が必要です。

https://dotnet.microsoft.com/ja-jp/download/dotnet/8.0

logAnalyzerフォルダ直下で下記記載のコマンドを実行してください。

### ランタイムなし版

```powershell
dotnet publish src/FfxiTempLogCollector.App/FfxiTempLogCollector.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -p:PublishProfile=win-x64 `
  -o publish/
```

### 自己完結形式
.NET 8 SDKがインストールされていないPCでも実行可能ですが、容量が肥大化します。

```powershell
dotnet publish src/FfxiTempLogCollector.App/FfxiTempLogCollector.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishProfile=win-x64 `
  -o publish/
```

### 出力先
上記コマンドでの出力先は、次のディレクトリです。

```text
./publish
```

## ビルド（開発者向け）
```powershell
dotnet build FfxiTempLogCollector.sln
```

## 運用手順

### 通常運用

1. FFXI開始前または開始直後に本アプリを起動します。
2. メイン画面または自動開始設定で収集を開始します。
3. FFXIプレイ中はアプリを終了せず、必要に応じてタスクトレイへ格納します。
4. プレイ終了後に「収集停止」を実行します。
5. `session.json`が`completed`になったセッションを後続ツールへ渡します。

### 異常終了後

1. 対象セッションの`session.json`を確認します。
2. `active`または`aborted`の場合は未確定データとして扱います。
3. `stats.json`のエラー数・欠損警告数を確認します。
4. 必要に応じて`raw_records.jsonl`を使用して再解析します。

## 既知の制限事項

- ログ本文のすべてに時刻が含まれるわけではありません。
- canonicalの時系列順には、検出時刻やログ内ヒントによる推定が含まれます。
- FFXI TEMPログのバイナリ構造は公式な完全仕様ではなく、実ログ観測に基づく実装です。
- DPS計算は本アプリでは行いません。別アプリが必要です。
- アプリ停止中にローテーション上書きされたログは取得できません。
- 複数のFFXIクライアントを同時に監視する機能はMVP対象外です。
- 収集中のactiveセッションを読み続けるライブDPSはMVP対象外です。
- FFXIやログ形式の更新により、未知のレコードがパースエラーになる可能性があります。
- ファイル読み取り失敗は次回ポーリングで再試行しますが、上書きまでの時間によっては欠損する可能性があります。
- `collector.log`は現在のMVPでは常に生成されるとは限りません。
