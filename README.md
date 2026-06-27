# 概要

- logRep_r … FFXIクライアントが出力する生ログファイルを分析用にデータ変換・退避・蓄積します
- logAnalyzer ... logRep_rが出力したデータを分析します
- AssistantTool ... logRep_rの出力したデータでゲーム内ログを復元できます。その他開発者向き内部データの確認もできます。

## 使い方

`logRep_r`と`logAnalyzer`をそれぞれのフォルダにてビルドを行ってください。  
ビルドには`.NET 8 SDK`が必要です。

ビルドのやり方がわからない人はReleasesからビルド済みexeを取得してください。

exeファイルは好きな場所に配置してください。
`logRep_r.exe`と`logAnalyzer.exe`を同じフォルダに格納してもOKです。

`AssistantTool`はフォルダ内の`index.html`をブラウザで開いてください。

ビルドコマンド等の詳細は各フォルダ以下のREADMEを参照してください。



