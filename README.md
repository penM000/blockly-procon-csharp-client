# U15長野プロコンサーバー C# クライアント
Combine Google Blockly and Procon Game Server csharp client

U15長野プロコンサーバー C# クライアントはC#で[U15長野プロコンサーバー（blockly）](https://github.com/kuropengin/blockly-procon)に接続するためのライブラリです。
## 機能
U15長野プロコンサーバー（blockly）では、従来の[AsahikawaProcon-Server](https://github.com/hal1437/AsahikawaProcon-Server)がTCPセッションを利用していたのに対し、
[socket.io](https://socket.io/)を利用しています。
このライブラリは、従来のTCPライブラリと関数の互換性があり、「program.cs」を置き換えることで、動作させることができます。

## 動作環境

**動作確認済み環境* 
+ windows10(1909)
+ visual studio community 2019 16.4.1

## セットアップ
+ step.0 接続先サーバーで観戦モードを開く
+ step.1 意の場所でGitのリポジトリをクローン、もしくはzipを展開
+ step.2 プログラミングコンテスト.sln を開く
+ step.3 「client.cs」の「server_address」「room_name」が接続先サーバーと一致しているか確認
+ step.4 ビルド＆実行


# 使用ライブラリ

+ [DynamicJson.1.2.0.0](https://www.nuget.org/packages/DynamicJson/1.2.0)
+ [EngineIoClientDotNet.0.9.22](https://www.nuget.org/packages/EngineIoClientDotNet/0.9.22)
+ [Newtonsoft.Json.8.0.1](https://www.nuget.org/packages/Newtonsoft.Json/8.0.1)
+ [SocketIoClientDotNet.0.9.13](https://www.nuget.org/packages/SocketIoClientDotNet/0.9.13)
+ [WebSocket4Net.0.14.1](https://www.nuget.org/packages/WebSocket4Net/0.14.1)
