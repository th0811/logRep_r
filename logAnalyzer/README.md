# FFXI_LogAnalyzer

`FFXI_LogAnalyzer` は、FFXIログ収集アプリが出力したセッションフォルダを読み込み、指定した分析区間のダメージ、DPS、命中率、通常攻撃クリティカル率、技/魔法/通常攻撃別の統計を確認するWindows GUIアプリです。

## 目的

このアプリは、ログ収集アプリによって正規化された `canonical_records.jsonl` を分析するためのビューア兼集計ツールです。戦闘ログを読み解きやすい単位にまとめ、actor別サマリ、action別集計、未解析ログを表示します。

## 入力データ

分析対象は、ログ収集アプリが生成したセッションフォルダです。セッションフォルダには少なくとも次のファイルが必要です。

```text
session.json
canonical_records.jsonl
stats.json
```

`canonical_records.jsonl` はログ収集アプリがFFXI TEMPログを読み取り、重複排除や正規化を行った結果です。`FFXI_LogAnalyzer` はこのファイルを読み込んで分析します。

## 対象外

MVPでは次の機能は対象外です。

- FFXI TEMPログファイルの直接読み込み
- ログ収集機能
- `canonical_records.jsonl` の生成
- ログウィンドウ1/2の選択
- CSV/Excelエクスポート
- リアルタイム更新
- actor名の別名統合
- ペット、フェイス、召喚獣などの帰属補正
- 高度なバフ/デバフ判定
- 全ログ表現を網羅した完全な解析ルール

## セッションフォルダの読み込み

1. アプリを起動します。
2. `セッションフォルダを開く` を選択します。
3. ログ収集アプリが出力したセッションフォルダを選択します。
4. `session.json`、`canonical_records.jsonl`、`stats.json` が読み込まれます。
5. marker一覧とセッション情報が表示されます。

`session.json` の `status` が `completed` 以外の場合、ログ欠損や未確定データを含む可能性があります。MVPでは警告を表示し、ユーザーが確認した場合のみ読み込みます。

## 分析区間

分析区間は開始ポイントと終了ポイントで指定します。

- 開始ポイント: `ログ先頭` または開始marker
- 終了ポイント: 終了markerまたは `ログ最後尾`

markerを使う場合、開始markerより後ろにある終了markerを選択します。marker行自体は集計対象から除外され、開始markerと終了markerの間にある通常ログが分析されます。

`ログ先頭` を選んだ場合は最初の分析対象レコードを含みます。`ログ最後尾` を選んだ場合は最後の分析対象レコードを含みます。

## DPS時間信頼度

DPS算出に使う分析時間には信頼度があります。

| 信頼度 | 意味 |
| --- | --- |
| `exact` | 開始/終了の両方が秒精度の時刻から確定できた状態 |
| `minute` | 分精度の時刻を含むため、分単位で概算した状態 |
| `estimated` | ログ先頭/ログ最後尾、近傍時刻、`first_seen_at` などから推定した状態 |
| `unknown` | 分析時間を確定できず、DPSを算出できない状態 |

`unknown` の場合、総ダメージなどの集計は表示できますが、DPSは表示できません。

## 結果表示

### actor別サマリ

ログ上で検出された行動主体名ごとに集計します。MVPではactor名の別名統合や所属補正は行いません。

主な表示項目:

- actor名
- 総与ダメージ
- DPS
- DPS時間信頼度
- 通常攻撃命中率
- 通常攻撃クリティカル率
- 総使用回数
- 総命中回数
- 総非命中回数
- unknown件数

全actorは初期表示されます。actorごとの表示/非表示を切り替えると、結果画面の表示だけがフィルタされます。MVPでは集計結果自体は保持し、画面表示のみを除外します。

### 技/魔法/通常攻撃別集計

actor、action_name、action_typeごとに集計します。

主な表示項目:

- actor名
- action_name
- action_type
- 使用回数
- 命中回数
- 非命中回数
- unknown件数
- 命中率
- 総ダメージ
- 最大ダメージ
- 最小ダメージ
- 平均ダメージ

技/魔法ごとの最大、最小、平均ダメージは、1使用あたりの合計ではなく、ログに表示された1ダメージ結果行ごとの値を対象にします。

### 通常攻撃クリティカル率

通常攻撃クリティカル率は、通常攻撃の命中回数のうち、クリティカルとして判定された命中回数の割合です。

```text
通常攻撃クリティカル率 = 通常攻撃クリティカル命中回数 / 通常攻撃命中回数
```

通常攻撃のミスとunknownは分母に含めません。

### 命中率

命中率は、命中と非命中の合計に対する命中の割合です。

```text
命中率 = 命中回数 / (命中回数 + 非命中回数)
```

unknownは命中率の分母と分子の両方から除外します。

### 未解析ログ

actorまたはactionを特定できないログ、またはhit_statusやaction_typeがunknownになったログは、未解析ログ一覧で確認できます。

表示項目:

- action_group_key
- order範囲
- event_group
- visible_text一覧
- 解析不能理由

未解析ログはDPS集計から除外されます。

## ビルド

.NET 8 SDK とWindows環境が必要です。

```powershell
dotnet build .\FFXI_LogAnalyzer.sln
```

## テスト

```powershell
dotnet test .\FFXI_LogAnalyzer.sln
```

統合テストでは `tests/FFXI_LogAnalyzer.Tests/fixtures/sample_session/` のサンプルセッションを相対パスで参照し、セッション読み込みから集計までを検証します。

## publish

Windows x64向けの自己完結形式で配布物を作成する例です。

```powershell
dotnet publish src/FFXI_LogAnalyzer.App/FFXI_LogAnalyzer.App.csproj -c Release -r win-x64 --self-contained true
```

出力先は通常、次のディレクトリです。

```text
src/FFXI_LogAnalyzer.App/bin/Release/net8.0-windows/win-x64/publish/
```

配布時はpublish出力ディレクトリ一式を配布してください。
