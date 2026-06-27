# FFXI_LogRep_r

FINAL FANTASY XI（FFXI）がゲームフォルダー内へ出力する`TEMP`ログを監視し、ローテーションで上書きされる前に継続退避するWindowsアプリです。

通常は`FFXI_LogRep_r.exe`をダブルクリックして使用するWPF GUIアプリですが、同じexeをPowerShellやコマンドプロンプトからCLIとして操作できます。

## 設計方針

本アプリは次の優先順位で設計されています。

1. 常時退避を主目的とする
2. 常時マージを補助目的とする

`raw_records.jsonl`には再解析可能な一次退避データを保存します。`canonical_records.jsonl`には、ログウィンドウ1と2に重複して出力されたイベントなどを統合した派生データを保存します。

canonicalデータだけでは将来のパーサー改善や不具合調査に必要な情報が失われる可能性があるため、原則としてraw出力も有効にしてください。

本アプリはFFXI側のログファイルを読み取り専用で扱います。元のログファイルを編集、削除、移動、リネーム、ロックしません。

## 監視対象ファイル

設定した`TEMP`フォルダー直下の次のファイルを監視します。

```text
1_0.log ～ 1_19.log
2_0.log ～ 2_19.log
```

- `1_N.log`: ログウィンドウ1
- `2_N.log`: ログウィンドウ2
- `N`: ローテーションスロット番号

`2_8(1).log`など、上記形式に一致しないファイルは通常監視の対象外です。

### 読み込み順

アプリを途中から起動した場合、ローテーション番号だけではファイルの新旧を判断できません。そのため、初回収集では存在する対象ファイルを次の順に処理します。

1. ファイル更新日時（UTC）の古い順
2. 更新日時が同じ場合はファイル名順

初回処理後は、各ポーリングで変更を検出したファイルだけを処理します。同一ポーリング内で複数ファイルが変更されていた場合も、更新日時の古い順、次にファイル名順で処理します。

更新日時はコピーや復元操作で変わる場合があるため、厳密なゲーム内時系列を保証するものではありません。

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

### 設定画面

設定画面では次の項目を変更できます。

- TEMPフォルダー
- 出力先フォルダー
- ポーリング間隔（250～5000ms）
- ログウィンドウ1、2の監視有無
- raw、canonical出力の有無
- 起動時自動収集
- 収集開始後のタスクトレイ格納
- 最小化時と閉じるボタン押下時の動作
- タスクトレイ通知
- ログレベル

ポーリング間隔、ログレベル、タスクトレイ関連設定は収集中でも即時反映されます。TEMPフォルダー、出力先、監視ウィンドウ、raw/canonical出力の変更は次回収集開始時に反映されます。

### 起動時自動収集

`auto_start_collection_on_launch`を有効にすると、アプリ起動後に自動で収集を開始します。

これはWindowsログオン時の自動起動やWindowsサービス登録を行う設定ではありません。Windows起動時にアプリ自体を開始する必要がある場合は、別途ショートカットやタスクスケジューラを設定してください。

### タスクトレイ

設定に応じて、収集中の閉じる操作や最小化操作でウィンドウをタスクトレイへ格納できます。

トレイアイコンの右クリックメニューから次を操作できます。

- メイン画面を表示
- 収集開始
- 収集停止
- 出力先を開く
- 設定
- アプリ終了

収集開始、収集停止、重大エラー、出力失敗の通知は`show_tray_notifications`で無効化できます。

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

## config.json

設定はJSON形式で保存されます。通常はexeと同じフォルダーの`config.json`へ保存されます。
検索優先順位は次のとおりです。

1. CLIの`--config`で指定したパス
2. exeと同じフォルダーの`config.json`

未指定で設定を保存する場合も、exeと同じフォルダーの`config.json`へ保存されます。

同梱の`config.example.json`に全設定項目の例があります。手動設定する場合はコピーして`config.json`へ名前を変更してください。通常はGUI設定画面または`config set`を使用してください。

主要設定:

| 設定キー | 説明 |
| --- | --- |
| `temp_dir` | FFXIのTEMPフォルダー |
| `output_dir` | セッション出力先。既定値 `sessions` はexeと同じフォルダー基準 |
| `polling_interval_ms` | 監視間隔。250～5000ms |
| `watch_window1`, `watch_window2` | ログウィンドウの監視有無 |
| `raw_output`, `canonical_output` | 各JSONL出力の有無 |
| `auto_start_collection_on_launch` | アプリ起動後の自動収集 |
| `minimize_button_behavior` | `normal`または`tray` |
| `close_button_behavior` | `confirm_exit`、`tray_when_collecting`、`always_tray` |
| `show_tray_notifications` | トレイ通知の有無 |
| `log_level` | `debug`、`info`、`warning`、`error` |

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

DPS算出ツールとの連携前提:

- 原則として`canonical_records.jsonl`をロードする
- `raw_records.jsonl`は再解析、詳細確認、検証に使用する
- MVPでは`completed`セッションを通常ロード対象とする
- `active`または`aborted`セッションは、追記中、欠損、未確定の可能性がある
- `session.json`と各レコードの`schema_version`で将来互換性を管理する
- 未知の追加フィールドは無視できる実装を推奨する

ログ本文に時刻がないレコードでは、`first_seen_at`などを補助情報として利用します。その場合の時刻は厳密なゲーム内イベント時刻ではありません。


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
