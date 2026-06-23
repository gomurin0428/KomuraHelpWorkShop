Feature: Komura HTML Help Compiler のユースケース
  HTML Help Workshop の hhc.exe と同じ用途で使えるローカル CHM コンパイラとして、
  利用者は HHP プロジェクト、HTML、CSS、HHC、HHK、画像などをまとめて
  Windows HTML Help Viewer で開ける CHM ファイルを生成できる。

  Background:
    Given hhc コマンドが実行できる
    And テスト用の一時ディレクトリがある

  Rule: コマンドラインから基本情報を確認できる

    Scenario: 引数なしで実行するとヘルプを表示する
      When 利用者が hhc を引数なしで実行する
      Then 標準出力にバナーと Usage が表示される
      And 終了コードは 0 である
      And CHM ファイルは生成されない

    Scenario Outline: ヘルプオプションでヘルプを表示する
      When 利用者が hhc を "<option>" 付きで実行する
      Then 標準出力にバナーと Usage が表示される
      And 終了コードは 0 である

      Examples:
        | option |
        | -h     |
        | --help |
        | /?     |

    Scenario: バージョン情報を表示する
      When 利用者が hhc を "--version" 付きで実行する
      Then 標準出力に "Komura HTML Help Compiler" とバージョン番号が表示される
      And 終了コードは 0 である

  Rule: 不正なコマンドライン引数を利用者に知らせる

    Scenario: 未知のオプションを指定した場合は引数エラーにする
      When 利用者が hhc を "--unknown help.hhp" 付きで実行する
      Then 標準エラーに "unknown option" が表示される
      And 終了コードは 2 である
      And CHM ファイルは生成されない

    Scenario: 出力先オプションに値がない場合は引数エラーにする
      When 利用者が hhc を "help.hhp --out" 付きで実行する
      Then 標準エラーに "--out requires a path" が表示される
      And 終了コードは 2 である

    Scenario: プロジェクトパスがない場合は引数エラーにする
      When 利用者が hhc を "--verbose" 付きで実行する
      Then 標準エラーに "missing .hhp project path" が表示される
      And 終了コードは 2 である

    Scenario: 複数の HHP プロジェクトを同時に指定した場合は引数エラーにする
      When 利用者が hhc を "a.hhp b.hhp" 付きで実行する
      Then 標準エラーに "only one .hhp project" が表示される
      And 終了コードは 2 である

  Rule: HHP プロジェクトを読み込んで CHM を生成できる

    Scenario: Compiled file を持つ標準的な HHP をコンパイルする
      Given HHP の [OPTIONS] に "Compiled file=help.chm" がある
      And HHP の [OPTIONS] に "Contents file=toc.hhc" がある
      And HHP の [OPTIONS] に "Index file=index.hhk" がある
      And HHP の [OPTIONS] に "Default topic=index.html" がある
      And HHP の [FILES] に "index.html" と "topics/intro.html" がある
      And 参照されるファイルがすべて存在する
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then HHP と同じディレクトリに "help.chm" が生成される
      And 標準出力にバナー、Compiled 行、Files 行が表示される
      And 終了コードは 0 である

    Scenario: --out で出力先を上書きする
      Given HHP の [OPTIONS] に "Compiled file=help.chm" がある
      And 参照されるファイルがすべて存在する
      When 利用者が hhc を "help.hhp --out dist/output.chm" 付きで実行する
      Then HHP ディレクトリ基準の "dist/output.chm" が生成される
      And HHP に書かれた "help.chm" は出力先として使われない
      And 終了コードは 0 である

    Scenario: -o で絶対パスの出力先を指定する
      Given HHP が有効である
      And 指定した絶対パスの親ディレクトリがまだ存在しない
      When 利用者が hhc を "help.hhp -o <absolute-output-path>" 付きで実行する
      Then 指定した絶対パスに CHM が生成される
      And 必要な親ディレクトリが作成される
      And 終了コードは 0 である

    Scenario: Compiled file が省略された場合は HHP 名から出力名を決める
      Given HHP の [OPTIONS] に Compiled file がない
      And HHP ファイル名は "manual.hhp" である
      And HHP が有効である
      When 利用者が hhc を "manual.hhp" 付きで実行する
      Then HHP と同じディレクトリに "manual.chm" が生成される
      And 終了コードは 0 である

    Scenario: 存在しない HHP はコンパイルエラーにする
      When 利用者が hhc を "missing.hhp" 付きで実行する
      Then 標準エラーに "Project file not found" が表示される
      And 終了コードは 1 である

  Rule: HHP の構文を HTML Help Workshop 互換に扱う

    Scenario: 空行とセミコロンコメントを無視する
      Given HHP に空行と ";" で始まるコメント行がある
      And HHP が有効である
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then コメント行はオプションやファイルとして扱われない
      And CHM が生成される

    Scenario: セクション名とオプション名の大文字小文字を区別しない
      Given HHP に "[options]" と "[files]" がある
      And HHP に "compiled FILE=help.chm" がある
      And HHP が有効である
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then "help.chm" が生成される

    Scenario: 一重引用符と二重引用符で囲まれた値を読み取る
      Given HHP の [OPTIONS] に "Title='Quoted Title'" がある
      And HHP の [FILES] に "\"index.html\"" がある
      And "index.html" が存在する
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM のタイトルは "Quoted Title" になる
      And "index.html" がユーザーファイルとして格納される

    Scenario: 同じオプションが複数回ある場合は後の値を使う
      Given HHP の [OPTIONS] に "Title=Old Title" がある
      And HHP の [OPTIONS] に "Title=New Title" が後からある
      And HHP が有効である
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM のタイトルは "New Title" になる

    Scenario: セクションより前の行は無視する
      Given HHP の最初のセクションより前に任意のテキスト行がある
      And HHP が有効である
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then セクション外の行はコンパイル結果に影響しない
      And CHM が生成される

  Rule: 必須ファイルと既定トピックを収集する

    Scenario: [FILES]、Contents file、Index file、Default topic を必須ファイルとして格納する
      Given HHP の [FILES] に "index.html" がある
      And HHP の [OPTIONS] に "Contents file=toc.hhc" がある
      And HHP の [OPTIONS] に "Index file=index.hhk" がある
      And HHP の [OPTIONS] に "Default topic=topics/start.html" がある
      And 参照されるファイルがすべて存在する
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM には "index.html"、"toc.hhc"、"index.hhk"、"topics/start.html" が格納される
      And 終了コードは 0 である

    Scenario: Default topic が省略された場合は [FILES] の最初の HTML を既定トピックにする
      Given HHP の [OPTIONS] に Default topic がない
      And HHP の [FILES] に "readme.txt" がある
      And HHP の [FILES] に "index.html" が後からある
      And HHP が有効である
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM の既定トピックは "index.html" になる

    Scenario: Contents file が省略された場合は簡易目次を自動生成する
      Given HHP の [OPTIONS] に Contents file がない
      And HHP の [FILES] に "index.html" と "topics/usage.html" がある
      And 参照されるファイルがすべて存在する
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM には "Table of Contents.hhc" が格納される
      And 自動生成された目次には HTML ファイルへの Local が含まれる
      And 標準エラーに "No Contents file was specified" の警告が表示される
      And 終了コードは 0 である

    Scenario: 必須ファイルが欠けている場合は既定で失敗する
      Given HHP の [FILES] に "missing.html" がある
      And "missing.html" が存在しない
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then 標準エラーに "file not found" の警告が表示される
      And 標準エラーに "Missing required files" のエラーが表示される
      And 終了コードは 1 である
      And CHM ファイルは生成されない

    Scenario: --allow-missing 指定時は必須ファイル欠落を警告にして続行する
      Given HHP の [FILES] に "index.html" と "missing.html" がある
      And "index.html" は存在する
      And "missing.html" は存在しない
      When 利用者が hhc を "help.hhp --allow-missing" 付きで実行する
      Then 標準エラーに "file not found" の警告が表示される
      And CHM には "index.html" が格納される
      And CHM には "missing.html" が格納されない
      And 終了コードは 0 である

    Scenario: 同じアーカイブパスを指す同一ファイルは重複格納しない
      Given HHP の [FILES] に "index.html" がある
      And HHP の [OPTIONS] に "Default topic=index.html" がある
      And "index.html" が存在する
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM 内の "index.html" は 1 件だけである
      And 重複警告は表示されない

    Scenario: 別ファイルが同じアーカイブパスになる場合は先勝ちで警告する
      Given Flat が有効である
      And HHP の [FILES] に "a/index.html" と "b/index.html" がある
      And 両方のファイルが存在する
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then 標準エラーに "duplicate archive path" の警告が表示される
      And CHM 内の "index.html" は先に収集されたファイルである

    Scenario: --verbose 指定時は収集したファイルを表示する
      Given HHP が有効である
      When 利用者が hhc を "help.hhp --verbose" 付きで実行する
      Then 標準エラーに "add:" で始まるファイル収集ログが表示される
      And 各ログには CHM 内パスと元ファイルパスが含まれる
      And 終了コードは 0 である

  Rule: HTML、CSS、HHC、HHK からリンク先ファイルを収集する

    Scenario: HTML の href と src からリンク先を再帰的に収集する
      Given HHP の [FILES] に "index.html" がある
      And "index.html" には href="topics/intro.html" と src="images/logo.png" がある
      And "topics/intro.html" には href="../styles/site.css" がある
      And 参照されるファイルがすべて存在する
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM には "index.html"、"topics/intro.html"、"images/logo.png"、"styles/site.css" が格納される

    Scenario: CSS の @import と url(...) からリンク先を収集する
      Given HHP の [FILES] に "styles/site.css" がある
      And "styles/site.css" には "@import \"theme.css\"" がある
      And "styles/site.css" には "url('../images/bg.png')" がある
      And 参照されるファイルがすべて存在する
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM には "styles/site.css"、"styles/theme.css"、"images/bg.png" が格納される

    Scenario: HHC と HHK の Local param からトピックを収集する
      Given HHP の [OPTIONS] に "Contents file=toc.hhc" がある
      And HHP の [OPTIONS] に "Index file=index.hhk" がある
      And "toc.hhc" には "<param name=\"Local\" value=\"topics/usage.html\">" がある
      And "index.hhk" には "<param name=\"Local\" value=\"topics/reference.html\">" がある
      And 参照されるファイルがすべて存在する
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM には "topics/usage.html" と "topics/reference.html" が格納される

    Scenario: --no-link-scan 指定時は明示されたファイルだけを格納する
      Given HHP の [FILES] に "index.html" がある
      And "index.html" には href="linked.html" がある
      And "linked.html" が存在する
      When 利用者が hhc を "help.hhp --no-link-scan" 付きで実行する
      Then CHM には "index.html" が格納される
      And CHM には "linked.html" が格納されない
      And 終了コードは 0 である

    Scenario Outline: 外部リンクと CHM 内ジャンプはローカルファイルとして収集しない
      Given HHP の [FILES] に "index.html" がある
      And "index.html" には href="<target>" がある
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then "<target>" はローカルファイルとして収集されない
      And そのリンクが存在しなくても欠落警告は表示されない

      Examples:
        | target                         |
        | http://example.com             |
        | https://example.com            |
        | ftp://example.com/file.txt     |
        | mailto:help@example.com        |
        | javascript:void(0)             |
        | data:image/png;base64,AAAA     |
        | about:blank                    |
        | news:example                   |
        | tel:+810000000000              |
        | mk:@MSITStore:other.chm::/a.htm |
        | ms-its:other.chm::/a.htm       |
        | its:other.chm::/a.htm          |
        | #section                       |
        | //cdn.example.com/app.js       |

    Scenario: クエリ文字列とフラグメントはリンク収集時に除去する
      Given HHP の [FILES] に "index.html" がある
      And "index.html" には href="topics/usage.html?print=1#top" がある
      And "topics/usage.html" が存在する
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM には "topics/usage.html" が格納される
      And "topics/usage.html?print=1#top" という名前のファイルは要求されない

    Scenario: HTML エンティティと URL エンコードをデコードしてリンクを解決する
      Given HHP の [FILES] に "index.html" がある
      And "index.html" には href="topics/a%20b.html?x=1&amp;y=2" がある
      And "topics/a b.html" が存在する
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM には "topics/a b.html" が格納される

    Scenario: リンク先の相対パスは参照元ファイルのディレクトリを基準に解決する
      Given HHP の [FILES] に "topics/intro.html" がある
      And "topics/intro.html" には src="../images/logo.png" がある
      And "images/logo.png" が存在する
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM には "images/logo.png" が格納される

    Scenario: ルート相対リンクは HHP ディレクトリを基準に解決する
      Given HHP の [FILES] に "topics/intro.html" がある
      And "topics/intro.html" には src="/images/logo.png" がある
      And HHP ディレクトリ直下に "images/logo.png" が存在する
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM には "images/logo.png" が格納される

    Scenario: 任意のスキャン対象外ファイルはリンク抽出しない
      Given HHP の [FILES] に "downloads/manual.pdf" がある
      And "downloads/manual.pdf" の内容に "linked.html" という文字列がある
      And "linked.html" が存在する
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM には "downloads/manual.pdf" が格納される
      And CHM には "linked.html" が格納されない

    Scenario: 任意リンク先が欠けている場合は警告するが失敗しない
      Given HHP の [FILES] に "index.html" がある
      And "index.html" には href="missing-linked.html" がある
      And "missing-linked.html" が存在しない
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then 標準エラーに "file not found" の警告が表示される
      And CHM は生成される
      And 終了コードは 0 である

  Rule: CHM 内パスを正規化し、Flat モードを扱う

    Scenario: 通常モードでは HHP ディレクトリからの相対パスを CHM 内パスにする
      Given HHP の [FILES] に "topics/usage.html" がある
      And "topics/usage.html" が存在する
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM には "topics/usage.html" が格納される

    Scenario: パス区切り文字、先頭の ./、中間の . と .. を正規化する
      Given HHP の [FILES] に ".\\topics\\..\\index.html" がある
      And "index.html" が存在する
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM には "index.html" が格納される

    Scenario: HHP ディレクトリ外の相対リンクは参照元の CHM 内ディレクトリを基準に格納する
      Given HHP の [FILES] に "topics/intro.html" がある
      And "topics/intro.html" には href="../../shared/page.html" がある
      And 参照元から見た "../../shared/page.html" が存在する
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM 内のリンク先パスは参照元ディレクトリを基準に正規化される

    Scenario: Flat=Yes ではフォルダを落としてファイル名だけで格納する
      Given HHP の [OPTIONS] に "Flat=Yes" がある
      And HHP の [FILES] に "topics/usage.html" と "styles/help.css" がある
      And 参照されるファイルがすべて存在する
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM には "usage.html" と "help.css" が格納される
      And CHM には "topics/usage.html" と "styles/help.css" は格納されない

    Scenario: Flat=Yes では HTML、CSS、HHC、HHK のローカル参照をファイル名に書き換える
      Given HHP の [OPTIONS] に "Flat=Yes" がある
      And HHP の [FILES] に "index.html"、"styles/site.css"、"toc.hhc" がある
      And "index.html" には href="topics/usage.html#top" と src="images/logo.png?size=small" がある
      And "styles/site.css" には "url('../images/bg.png')" がある
      And "toc.hhc" には "<param name=\"Local\" value=\"topics/usage.html\">" がある
      And 参照されるファイルがすべて存在する
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM 内の "index.html" のリンクは "usage.html#top" と "logo.png?size=small" になる
      And CHM 内の "site.css" の URL は "bg.png" になる
      And CHM 内の "toc.hhc" の Local は "usage.html" になる

  Rule: エンコーディングと言語設定を扱う

    Scenario Outline: BOM があるテキストファイルは BOM に従って読む
      Given "<file>" が "<encoding>" の BOM 付きで保存されている
      And HHP がそのファイルを参照している
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then そのファイルは文字化けせずに読み取られる
      And 終了コードは 0 である

      Examples:
        | file       | encoding    |
        | index.html | UTF-8       |
        | index.html | UTF-16 LE   |
        | index.html | UTF-16 BE   |

    Scenario: BOM がなく UTF-8 として妥当なテキストは UTF-8 として読む
      Given HHP と HTML が BOM なし UTF-8 で保存されている
      And HHP が有効である
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then HHP と HTML は UTF-8 として読み取られる
      And 終了コードは 0 である

    Scenario: Language の LCID から HHC、HHK、内部文字列の ANSI コードページを決める
      Given HHP の [OPTIONS] に "Language=0x0411 Japanese" がある
      And HHP の [OPTIONS] に "Title=日本語ヘルプ" がある
      And HHP の [OPTIONS] に "Contents file=toc.hhc" がある
      And "toc.hhc" に日本語の Name がある
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then HHC と CHM 内部文字列は日本語 LCID に対応する ANSI コードページで格納される
      And CHM の LCID は 0x0411 である

    Scenario: Language が HHP の先頭読み取りにも使われる
      Given HHP は UTF-8 として妥当でない日本語 ANSI で保存されている
      And HHP の [OPTIONS] に "Language=0x0411 Japanese" がある
      And HHP が有効である
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then HHP は日本語 ANSI として読み取られる
      And CHM が生成される

    Scenario: Language が無効または省略された場合は現在のカルチャを既定にする
      Given HHP の [OPTIONS] に有効な Language がない
      And HHP が有効である
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM の LCID は実行環境の現在カルチャに基づく
      And 読み取りのフォールバックは実行環境の ANSI コードページに基づく

    Scenario Outline: DBCS 言語では CHM 内部フラグに DBCS を設定する
      Given HHP の [OPTIONS] に "Language=<language>" がある
      And HHP が有効である
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM の #SYSTEM には DBCS 言語として記録される

      Examples:
        | language                  |
        | 0x0411 Japanese           |
        | 0x0804 Chinese Simplified |
        | 0x0404 Chinese Traditional |
        | 0x0412 Korean             |

  Rule: CHM メタデータと内部ファイルを生成する

    Scenario: Title、Default topic、Contents file、Index file を CHM メタデータへ反映する
      Given HHP の [OPTIONS] に "Title=Product Help" がある
      And HHP の [OPTIONS] に "Default topic=index.html" がある
      And HHP の [OPTIONS] に "Contents file=toc.hhc" がある
      And HHP の [OPTIONS] に "Index file=index.hhk" がある
      And HHP が有効である
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM のタイトルは "Product Help" になる
      And CHM の既定トピックは "index.html" になる
      And CHM の目次ファイルは "toc.hhc" になる
      And CHM の索引ファイルは "index.hhk" になる

    Scenario: Title が省略された場合は HHP ファイル名をタイトルにする
      Given HHP の [OPTIONS] に Title がない
      And HHP ファイル名は "manual.hhp" である
      And HHP が有効である
      When 利用者が hhc を "manual.hhp" 付きで実行する
      Then CHM のタイトルは "manual" になる

    Scenario: Default Window が省略された場合は main を使う
      Given HHP の [OPTIONS] に Default Window がない
      And HHP が有効である
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM の既定ウィンドウ名は "main" になる

    Scenario: Default Window と Default Font を指定できる
      Given HHP の [OPTIONS] に "Default Window=custom" がある
      And HHP の [OPTIONS] に "Default Font=MS UI Gothic, 9" がある
      And HHP が有効である
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM の既定ウィンドウ名は "custom" になる
      And CHM の既定フォント情報は "MS UI Gothic, 9" になる

    Scenario: CHM には HTML Help Viewer が読む内部ストリームを含める
      Given HHP が有効である
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM には "::DataSpace/NameList" が含まれる
      And CHM には "/#SYSTEM" が含まれる
      And CHM には "/#WINDOWS" が含まれる
      And CHM には "/#STRINGS" が含まれる
      And CHM には "/#ITBITS" が含まれる
      And CHM には収集したユーザーファイルが含まれる

    Scenario: 小規模なプロジェクトは PMGL ディレクトリだけで書き込む
      Given CHM ディレクトリが 1 ブロックに収まるプロジェクトがある
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM は ITSF v3 と ITSP ディレクトリを持つ
      And CHM ディレクトリには PMGL ブロックがある
      And PMGI インデックスブロックは不要である

    Scenario: 大きいプロジェクトは PMGI インデックスを生成する
      Given CHM ディレクトリが複数の PMGL ブロックを必要とするプロジェクトがある
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM ディレクトリには PMGI インデックスブロックがある
      And すべての PMGL ブロックが参照できる

    Scenario: このバージョンではユーザーファイルを非圧縮で格納する
      Given HHP が有効である
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then CHM の DataSpace は "Uncompressed" である
      And ユーザーファイルは LZX 圧縮されない

  Rule: 未対応の HTML Help Workshop 機能を警告として扱う

    Scenario: Full-text search が有効でも検索インデックスなしで生成する
      Given HHP の [OPTIONS] に "Full-text search=Yes" がある
      And HHP が有効である
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then 標準エラーに "Full-text search index generation is not implemented" の警告が表示される
      And CHM は検索インデックスなしで生成される
      And 終了コードは 0 である

    Scenario: Binary TOC が有効でも HHC ソースを格納する
      Given HHP の [OPTIONS] に "Binary TOC=Yes" がある
      And HHP の [OPTIONS] に "Contents file=toc.hhc" がある
      And HHP が有効である
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then 標準エラーに "Binary TOC is not implemented" の警告が表示される
      And CHM には "toc.hhc" が格納される
      And 終了コードは 0 である

    Scenario: Binary Index が有効でも HHK ソースを格納する
      Given HHP の [OPTIONS] に "Binary Index=Yes" がある
      And HHP の [OPTIONS] に "Index file=index.hhk" がある
      And HHP が有効である
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then 標準エラーに "Binary Index is not implemented" の警告が表示される
      And CHM には "index.hhk" が格納される
      And 終了コードは 0 である

    Scenario: [MERGE FILES] はマージ済み CHM を生成せず警告する
      Given HHP に [MERGE FILES] セクションがある
      And HHP が有効である
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then 標準エラーに "[MERGE FILES]" の警告が表示される
      And 個別のマージ済みヘルプコレクションは生成されない
      And CHM は生成される

    Scenario: [WINDOWS] セクションは解析せず既定ウィンドウ定義を生成する
      Given HHP に [WINDOWS] セクションがある
      And HHP が有効である
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then 標準エラーに "[WINDOWS] custom settings are not parsed" の警告が表示される
      And CHM には生成された既定の "/#WINDOWS" が含まれる
      And 終了コードは 0 である

  Rule: 異常系を安全に報告する

    Scenario: 出力ファイルを書き込めない場合はコンパイルエラーにする
      Given HHP が有効である
      And 出力先パスには書き込み権限がない
      When 利用者が hhc を "help.hhp --out <unwritable-output-path>" 付きで実行する
      Then 標準エラーに "error:" が表示される
      And 終了コードは 1 である

    Scenario: 1 件のディレクトリエントリが大きすぎる場合はエラーにする
      Given CHM ディレクトリブロックに収まらない長さのアーカイブパスを持つファイルがある
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then 標準エラーに "Directory entry is too large" が表示される
      And 終了コードは 1 である

    Scenario: このコンパイラ版の上限を超える巨大ディレクトリはエラーにする
      Given PMGI 1 ブロックに収まらない数のディレクトリエントリを持つプロジェクトがある
      When 利用者が hhc を "help.hhp" 付きで実行する
      Then 標準エラーに "directory is too large for this compiler version" が表示される
      And 終了コードは 1 である
