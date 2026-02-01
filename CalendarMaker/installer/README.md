# CalendarMaker インストーラ（Windows）

このプロジェクトは WPF アプリ（`net8.0-windows`）です。別PCへ配布する最も簡単な方法は、`dotnet publish` の出力を Windows インストーラにまとめることです。

## オプションA（推奨）: Inno Setup インストーラ

### ビルドに必要なもの（ビルドPC）
- Windows
- .NET 8 SDK（`dotnet`）
- Inno Setup 6（`ISCC.exe` が使えること）

### ビルド手順
リポジトリ直下で実行:
```powershell
.\installer\build-installer.ps1 -Configuration Release -Runtime win-x64
```

実行時に「このシステムではスクリプトの実行が無効」等が出る場合は、プロセス限定で回避できます:
```powershell
powershell -ExecutionPolicy Bypass -File .\installer\build-installer.ps1 -Configuration Release -Runtime win-x64
```

`ISCC.exe` が見つからない場合は、Inno Setup 6 をインストールするか、パスを指定してください:
```powershell
.\installer\build-installer.ps1 -Configuration Release -Runtime win-x64 -IsccPath "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
```

インストーラのバージョン文字列は `CalendarMaker\CalendarMaker.csproj` の `<Version>...</Version>` で指定できます。

出力:
- 発行（publish）出力: `dist\publish\win-x64\`
- インストーラ exe: `dist\installer\CalendarMaker-Setup-<version>.exe`

### 別PCへのインストール
生成された `...Setup-<version>.exe` を対象PCへコピーして実行してください。

もし「必要なDLLが見つからない」等で起動に失敗する場合は、対象PCに **Microsoft Visual C++ Redistributable 2015-2022 (x64)** のインストールが必要なことがあります。

## オプションB: ポータブル（インストーラなし）
`dist\publish\win-x64\` フォルダ一式を別PCへコピーして、`CalendarMaker.exe` を直接起動することもできます。
