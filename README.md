# Komura HTML Help Compiler

Microsoft HTML Help Workshop の `hhc.exe` と同じ用途の、ローカル実装の CHM コンパイラです。
`.hhp` を読み、`.html`、`.hhc`、`.hhk`、CSS、画像などを CHM に格納します。

## Build

```powershell
dotnet build .\src\hhc\hhc.csproj
```

Debug ビルドの実行ファイルは次に生成されます。

```text
src\hhc\bin\Debug\net8.0\hhc.exe
```

## Usage

```powershell
.\src\hhc\bin\Debug\net8.0\hhc.exe .\examples\basic\help.hhp
```

主なオプション:

- `--out <file.chm>`: `[OPTIONS] Compiled file` を上書きします。
- `--no-link-scan`: `[FILES]` と明示された TOC/Index/Default topic のみ格納します。
- `--allow-missing`: 入力ファイル欠落を警告にして CHM を生成します。
- `--verbose`: 収集したファイルを表示します。

## Current Compatibility

対応済み:

- HHP の `[OPTIONS]` と `[FILES]` の読み取り
- `Compiled file`, `Contents file`, `Index file`, `Default topic`, `Title`, `Language`, `Flat`
- `.hhc`/`.hhk` の `Local` param、HTML の `href`/`src`、CSS の `url(...)` と `@import` からのリンク収集
- ITSF v3 / ITSP / PMGL / PMGI ディレクトリ生成
- 非圧縮 CHM コンテナ生成
- `#SYSTEM`, `#WINDOWS`, `#STRINGS`, `#ITBITS`, `::DataSpace/NameList` の生成
- `Contents file` 未指定時の簡易 TOC 自動生成
- `Language` の LCID に基づく HHC/HHK と内部文字列の ANSI コードページ変換
- `Flat=Yes` 指定時の HTML/CSS/HHC/HHK 内ローカル参照のファイル名化

未対応または簡略化:

- LZX 圧縮
- Full-text search index (`$FIftiMain`)
- Binary TOC / Binary Index
- CHI / CHW / merged help collection generation

このため、最初の版は「開ける・配布できる CHM」を優先した互換コンパイラです。検索インデックスや圧縮サイズまで `hhc.exe` と同等にしたい場合は、次に LZX と内部検索インデックスを追加します。
