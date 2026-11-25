# MyRobot 日本語フォントMOD

[MyRobot](https://store.steampowered.com/app/3597510/MyRobot/) で日本語表示にしたとき、文字が「□」になる問題を直す BepInEx 用 MOD です。
<img width="1832" height="2290" alt="compare_all" src="https://github.com/user-attachments/assets/bcb9eca2-489f-40fc-a01f-0c3b5c60d1d7" />

- メインフォント: コーポレート・ロゴ（ラウンド）Bold ver3
- 予備フォント: Noto Sans JP Bold (メインフォントに無い JIS 第3・第4水準の漢字など。生成AIの会話向け)
- ゲーム本体のファイルは書き換えません

## ダウンロード

[Releases](../../releases) から zip をダウンロードしてください。

| ファイル | 内容 |
| --- | --- |
| `MyRobotJP_vX.Y.Z_with_BepInEx.zip` | BepInEx 同梱の全部入り版 (はじめての方はこちら) |
| `MyRobotJP_vX.Y.Z.zip` | MOD のみ (BepInEx 5 を導入済みの方向け) |

## 入れ方

1. Steam で MyRobot を右クリック > 管理 > ローカルファイルを閲覧 でゲームフォルダを開きます。
2. zip の中身をそのままゲームフォルダ (`Robot.exe` がある場所) に展開します。
3. ゲームを起動し、言語を日本語にします。

外すときは `BepInEx/plugins/MyRobotJP` フォルダを削除します。

## 設定

一度起動すると `BepInEx/config/myrobot.japanesefont.cfg` ができます。

| 項目 | 説明 |
| --- | --- |
| `FontFile` | メインフォント (`fonts` フォルダ内のファイル名) |
| `ExtraFallbackFonts` | 予備フォント (カンマ区切り、空欄で無効) |
| `ReplaceAllTextsInJapanese` | 日本語のとき、すべての文字をメインフォントにする |
| `AddAsFallback` | 他の言語でも、元のフォントに無い文字をこのフォントで表示する |
| `DebugLog` | 置き換えた文字の情報をログに出す (不具合調査用) |

## しくみ

ゲームの `Lexicon.GetFontForSystemLanguage` (言語ごとにフォントを選ぶ処理) を Harmony でフックし、日本語のときに同梱フォントを返します。フォントは起動時に `TMP_FontAsset.CreateFontAsset` で .otf から直接作るため、AssetBundle は不要です。描画用のマテリアルはゲーム本来の日本語フォントのものを引き継ぐので、ゲーム内モニターなど特殊な画面でも表示されます。

## ビルド

.NET SDK 8 以降が必要です。

1. `lib/Managed/` にゲームの `Robot_Data/Managed/` 内の DLL をコピーします (再配布はしないでください)。
2. `lib/BepInEx/` に BepInEx 5 の `BepInEx/core/` 内の DLL をコピーします。
3. `dotnet build src/MyRobotJP -c Release`

## リリースの作り方

1. ビルドした `MyRobotJP.dll` を `dist/` に置き、`release-notes/vX.Y.Z.md` を書きます。
2. `vX.Y.Z` タグを push するか、Actions の Release ワークフローを手動実行 (tag を入力) すると、zip を作って Release に添付します。

## ライセンス

- ソースコード: MIT License ([LICENSE](LICENSE))
- フォント: SIL Open Font License 1.1 (`fonts/` 内のライセンス文書)
- BepInEx (Releases の同梱版): LGPL-2.1

このMODは非公式です。ゲームの開発元とは関係ありません。

## 作者・連絡先

作者: Kulur ・ Steam: https://steamcommunity.com/id/Kulur/
不具合報告は [Issues](../../issues) か Steam プロフィールからお願いします。
