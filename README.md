
# LogRep2への移行
本プロジェクトはLogRep2へ移行となりました。
従来セッションデータは引き続き利用可能です。

https://github.com/th0811/LogRep2


## 旧情報 概要

- logRep_r … FFXIクライアントが出力する生ログファイルを分析用にデータ変換・退避・蓄積します
- logAnalyzer ... logRep_rが出力したデータを分析します
- AssistantTool ... logRep_rの出力したデータでゲーム内ログを復元できます。その他開発者向き内部データの確認もできます。

### 使い方

`logRep_r`と`logAnalyzer`をそれぞれのフォルダにてビルドを行ってください。  
ビルドには`.NET 8 SDK`が必要です。

ビルドのやり方がわからない人はReleasesからビルド済みexeを取得してください。
https://github.com/th0811/logRep_r/releases

ゲーム内ログはデフォルトインストールした場合は
`C:\Program Files (x86)\PlayOnline\SquareEnix\FINAL FANTASY XI\TEMP`にあるかと思います。

exeファイルは好きな場所に配置してください。
`logRep_r.exe`と`logAnalyzer.exe`を同じフォルダに格納してもOKです。

`AssistantTool`はフォルダ内の`index.html`をブラウザで開いてください。

ビルドコマンド等の詳細は各フォルダ以下のREADMEを参照してください。



