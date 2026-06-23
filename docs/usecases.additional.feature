Feature: Additional Komura HHC edge cases covered by TLA+
  These scenarios extend the reader-facing behavior specification with edge
  cases that are projected into the generated TLA+ use-case and implementation
  conformance models.

  Rule: CLI parsing preserves short-circuit and option precedence behavior

    Scenario: UC064 help short-circuits later arguments
      When the user runs hhc with a help option and additional arguments
      Then help is printed
      And the project is not loaded
      And no CHM file is created

    Scenario: UC065 version short-circuits project loading
      When the user runs hhc with --version and an invalid project path
      Then version information is printed
      And the project is not loaded
      And no CHM file is created

    Scenario: UC066 repeated output option uses the last value
      Given a valid HHP project
      When the user supplies --out more than once
      Then the final --out value is used as the CHM output path

    Scenario: UC067 output option before project path is accepted
      Given a valid HHP project
      When the user supplies --out before the HHP path
      Then the requested output path is used

  Rule: HHP options and project paths handle edge values

    Scenario: UC068 blank Compiled file falls back to project stem
      Given the HHP has an empty Compiled file option
      When the user compiles the project
      Then the CHM output path is derived from the HHP file name

    Scenario: UC069 unbalanced quoted option value is kept literally
      Given the HHP title starts with an unmatched quote
      When the user compiles the project
      Then the title metadata keeps the literal unmatched quote

    Scenario: UC070 truthy Flat option values enable flat archive mode
      Given the HHP has Flat=on
      When the user compiles nested files
      Then the CHM archive stores file names without directory prefixes

    Scenario: UC071 dot project path is ignored after archive normalization
      Given the HHP lists a project path that normalizes to an empty archive path
      When the user compiles the project
      Then that path is skipped without becoming a CHM entry

    Scenario: UC072 rooted project file outside the project stores the file name
      Given the HHP lists an absolute file path outside the project directory
      When the user compiles the project
      Then the CHM archive path is the source file name

    Scenario: UC073 relative project file outside the project stores the file name
      Given the HHP lists ../shared/page.html
      When the user compiles the project
      Then the CHM archive path is the source file name

  Rule: Link scanning ignores unsafe targets and absorbs recoverable failures

    Scenario: UC074 empty and fragment-only links are ignored
      Given an HTML file has empty and fragment-only links
      When links are scanned
      Then no extra CHM entries or missing-file warnings are produced

    Scenario: UC075 network-share style links are ignored
      Given an HTML file links to a UNC or protocol-relative target
      When links are scanned
      Then the target is not collected as a local file

    Scenario: UC076 malformed percent escapes keep original spelling
      Given an HTML file links to bad%ZZ.html
      When the linked file exists
      Then bad%ZZ.html is collected using the original spelling

    Scenario: UC077 link scanner read failures are absorbed
      Given a scannable file cannot be read during link scanning
      When the project is compiled
      Then compilation continues as if the file had no outgoing links

  Rule: Encoding and metadata fallback paths stay deterministic

    Scenario: UC078 invalid UTF-8 without BOM falls back to ANSI
      Given a text file has invalid UTF-8 bytes and no BOM
      When the project is compiled
      Then the selected fallback ANSI encoding is used

    Scenario: UC079 Language LCID can omit the 0x prefix
      Given the HHP has Language=0411 Japanese
      When the project is compiled
      Then Japanese LCID and CP932 metadata behavior are selected

    Scenario: UC080 projects with no HTML file omit the default topic
      Given a project lists no HTML files
      When generated contents are created
      Then no default topic is written to metadata

    Scenario: UC081 flat mode normalizes contents and index option archive paths
      Given Flat mode is enabled
      And Contents file and Index file include directories
      When metadata is built
      Then the contents and index metadata paths use flat file names

    Scenario: UC082 duplicate metadata strings are reused in the CHM string table
      Given metadata contains repeated strings
      When CHM metadata streams are built
      Then the string table reuses the existing offset

  Rule: Additional failure paths do not create or corrupt CHM output

    Scenario: UC083 input text read failure during collection aborts compilation
      Given a required text input cannot be read while collecting files
      When the user compiles the project
      Then compilation exits with an error
      And no CHM file is created

    Scenario: UC084 writer input file read failure exits without creating CHM
      Given a binary input cannot be read by the writer
      When the user compiles the project
      Then compilation exits with an error
      And no CHM file is created

    Scenario: UC085 locked existing output is preserved
      Given the output CHM already exists and cannot be opened for writing
      When the user compiles the project
      Then compilation exits with an error
      And the existing output bytes are preserved

    Scenario: UC086 oversized metadata entry fails before CHM creation
      Given metadata would exceed a #SYSTEM entry size limit
      When the user compiles the project
      Then compilation exits with an error
      And no CHM file is created

    Scenario: UC087 generated contents escapes HTML-sensitive topic names
      Given generated contents include topic names with HTML-sensitive characters
      When the generated contents file is embedded
      Then the generated Local and Name values are escaped

  Rule: PR review edge cases remain protected

    Scenario: UC088 local base href resolves fragment-only links
      Given an HTML file declares a local base href pointing at topics/chapter.html
      And the file contains a fragment-only link to #intro
      When links are scanned for inclusion
      Then topics/chapter.html is collected as the linked topic

    Scenario: UC089 generated TOC escapes reserved Local URLs
      Given generated contents include topic archive names with # or literal percent escapes
      When the compiler writes the generated contents file
      Then generated Local values are URL-escaped so they still address the embedded topic filenames

    Scenario: UC090 project path HTML entities remain literal
      Given the HHP [FILES] section lists docs/a&amp;b.html
      When the project file path is normalized
      Then docs/a&amp;b.html is kept as a literal filesystem entry

    Scenario: UC091 external base href prevents flat local rewrites
      Given Flat mode is enabled
      And a page declares an external base href
      When the page is rewritten for flat archives
      Then relative references under that external base are left unchanged

    Scenario: UC092 arbitrary absolute URI schemes are external
      Given a page links to cid:, urn:, irc:, or smb: targets
      When links are scanned or flat-rewritten
      Then those targets are treated as external links and are not collected or rewritten as local files

    Scenario: UC093 decoded NUL links are rejected before path resolution
      Given a page contains a percent-encoded NUL in a local-looking link
      When links are scanned
      Then the target is rejected before filesystem path resolution and compilation continues

    Scenario: UC094 internal stream archive path collisions fail
      Given a project lists a user file whose archive path matches a reserved CHM internal stream
      When the writer prepares CHM directory entries
      Then compilation fails instead of silently dropping the user payload

    Scenario: UC095 output paths cannot overwrite project or input files
      Given the configured CHM output path equals the project file or a collected input file
      When compilation validates the output path
      Then compilation fails before writing output bytes

    Scenario: UC096 case-only source collisions warn
      Given two source files differ only by case but normalize to the same CHM archive path
      When files are collected on a case-sensitive filesystem
      Then the compiler warns about the duplicate archive path and keeps the first payload

    Scenario: UC097 flat rewrite preserves reserved filename escapes
      Given Flat mode rewrites a local URL whose basename contains an encoded reserved character
      When the archive path is flattened
      Then the decoded basename selects the embedded file and the rewritten URL preserves the reserved-character escape
