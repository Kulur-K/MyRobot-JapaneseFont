MyRobot 日本語フォントMOD v1.2.0
================================

日本語表示のとき、文字が□になる問題を直し、フォントを
「コーポレート・ロゴ（ラウンド）Bold ver3」に変えるMODです。
生成AIの会話などで、このフォントに無い漢字(JIS第3・第4水準など)が出たときは、
予備フォント「Noto Sans JP Bold」で表示します。

■ 必要なもの
- BepInEx 5.4.23 (Windows x64)
  「with_BepInEx」版のzipには同梱されています。

■ 入れ方
1. zipの中身を、ゲームフォルダ(Robot.exe があるフォルダ)にそのまま展開します。
   Steamで MyRobot を右クリック > 管理 > ローカルファイルを閲覧 で開けます。
2. ゲームを起動し、設定で言語を日本語にします。

■ 外し方
BepInEx/plugins/MyRobotJP フォルダを削除します。
BepInEx ごと外す場合は、BepInEx フォルダ、winhttp.dll、doorstop_config.ini、
.doorstop_version も削除します。

■ 設定
一度起動すると BepInEx/config/myrobot.japanesefont.cfg ができます。
- ReplaceAllTextsInJapanese : 日本語のとき、すべての文字をこのフォントにする (true/false)
- AddAsFallback             : 他の言語でも、元のフォントに無い文字をこのフォントで表示する
- FontFile                  : fonts フォルダ内の別のフォントに差し替えることもできます
- ExtraFallbackFonts        : 予備フォント(カンマ区切り)。空欄にすると予備フォントを使いません

■ 作者・連絡先
作者: Kulur
Steam: https://steamcommunity.com/id/Kulur/
不具合報告や感想は、上のSteamプロフィールからお願いします。

■ ライセンス
- フォント: コーポレート・ロゴ（ラウンド）ver3 は SIL Open Font License 1.1 です。
  BepInEx/plugins/MyRobotJP/fonts 内のライセンス文書をご覧ください。
- フォント: Noto Sans JP は SIL Open Font License 1.1 です (OFL_NotoSansJP.txt)。
- BepInEx: LGPL-2.1 (https://github.com/BepInEx/BepInEx)
- このMODは非公式です。ゲームの開発元とは関係ありません。
