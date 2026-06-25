Feature: Existing-code formal inspection specification for Komura HHC
  This feature records the behavior inferred from the current implementation.
  It is not an assertion that every behavior is desirable. Scenarios marked
  "human review" require a product or compatibility decision.
  Silent link-scan read absorption and explicit outside source inclusion are
  accepted compatibility targets for the reference compiler behavior.

  Rule: CLI terminal modes stop before project loading

    Scenario: Help exits successfully without compiling
      When hhc is invoked with no arguments or a help option
      Then the process exits with code 0
      And no HHP project is loaded
      And no CHM file is created

    Scenario: Argument errors stop before compiling
      When hhc receives an unknown option, a missing --out value, no project path, or multiple project paths
      Then the process exits with code 2
      And no HHP project is loaded
      And no CHM file is created

    Scenario: Version exits successfully without compiling
      When hhc is invoked with --version and an invalid project path
      Then the process exits with code 0
      And no HHP project is loaded
      And no CHM file is created

  Rule: Project loading and collection determine the early failure boundary

    Scenario: Missing project fails before collection
      When the HHP project file does not exist
      Then the process exits with code 1
      And file collection, metadata construction, and CHM writing do not run

    Scenario: Missing required files fail unless allow-missing is enabled
      Given a required HHP, contents, index, default topic, or listed file is absent
      When --allow-missing is not enabled
      Then the process exits with code 1
      And warnings collected before the failure are printed
      And metadata construction and CHM writing do not run

    Scenario: Missing optional linked files only warn
      Given a linked file discovered during link scanning is absent
      When the project is compiled
      Then a warning is printed
      And compilation can still succeed

  Rule: Link scanner read failures are intentionally absorbed

    Scenario: Unreadable scannable files are treated as having no outgoing links
      Given an HTML, CSS, HHC, or HHK file cannot be read during link scanning
      When links are extracted
      Then no exception is propagated from the link scanner
      And no outgoing links are collected from that file
      And compilation may continue
      And no warning is required for the absorbed link-scan read failure

  Rule: Paths are normalized before becoming CHM archive names

    Scenario: Outside project files are included by basename only
      Given an HHP project lists a parent-relative file outside the project directory
      And the HHP project lists an absolute file outside the project directory
      When the project is compiled
      Then the files may be read from their source locations
      And each outside file is stored under its basename in the CHM archive
      And parent or sibling directory names are not used as CHM archive directories
      And explicit outside source inclusion is allowed as a compatibility behavior

    Scenario: Outside project basename collisions warn and keep the first source
      Given two explicit outside project files have the same basename
      When the project is compiled
      Then the first source is stored under that basename in the CHM archive
      And the second source is omitted
      And a duplicate archive path warning is printed

    Scenario: Link cleanup rejects non-local targets
      Given a discovered link is external, UNC, empty, or fragment-only
      When link cleanup runs
      Then the link is not collected as a local input file

    Scenario: Malformed percent escapes keep their original spelling
      Given a discovered local link contains malformed percent escaping
      When link cleanup runs
      Then the original link spelling is retained for local path handling

    Scenario: Link extraction handles common HTML and CSS syntaxes
      Given an HTML or CSS file contains mixed-case attributes, single-quoted values, unquoted values, HHC Local params, CSS imports, and CSS urls
      When link scanning runs
      Then each supported raw link target is extracted for later cleanup

    Scenario: Flat link rewriting is idempotent
      Given local HTML, HHC, CSS import, and CSS url targets contain nested directories
      When flat-mode link rewriting runs
      Then local targets are rewritten to basenames while query and fragment suffixes are preserved
      And external targets are left unchanged
      And rewriting the same text again does not change it

  Rule: HHP parsing is deterministic for boundary syntax

    Scenario: Duplicate and ambiguous HHP options are parsed deterministically
      Given an HHP file has duplicate options, blank values, balanced quotes, and unbalanced quotes
      When the project is loaded
      Then duplicate options keep the last value
      And blank option values behave as omitted
      And balanced quotes are stripped
      And unbalanced quotes remain literal
      But human review is required to decide whether these compatibility rules match HTML Help Workshop exactly

    Scenario: Declared language and BOM encoding decisions are deterministic
      Given a project file is invalid UTF-8 and declares Language=0411
      When the project is loaded
      Then CP932 is selected for text decoding
      And CR-only line endings are parsed as separate lines
      Given another project file has a UTF-16 BOM and also declares Language=0x0411
      When that project is loaded
      Then the UTF-16 BOM wins over the declared language

  Rule: Output writing is atomic at the final CHM path

    Scenario: Output publish failure preserves the existing output
      Given an existing output file cannot be replaced
      When CHM writing reaches final publication
      Then the process exits with code 1
      And the existing output file is preserved

    Scenario: Temp-file write failure preserves the existing output
      Given CHM bytes are staged in a temporary file in the output directory
      When writing the temporary file fails before final publication
      Then the process exits with code 1
      And the existing output file is preserved
      And no partial final CHM output remains

    Scenario: Final publish failure cleans staged temp output
      Given CHM bytes have been fully staged in a temporary file
      And the existing final output cannot be replaced
      When final publication fails
      Then the process exits with code 1
      And the existing output file is preserved
      And the staged temporary output is removed

    Scenario: Stale temp output does not block a later compile
      Given a same-directory temporary output file from an earlier run still exists
      When the project is compiled again
      Then the process exits with code 0
      And a structurally valid final CHM output is published
      And the stale temporary file is left unchanged

    Scenario: Process death after temp creation preserves existing final output
      Given an existing final CHM output
      And a compiler process creates a same-directory temporary output
      When that process dies before final publication and cleanup
      Then the existing final output remains byte-for-byte unchanged
      And a stale temporary file remains

    Scenario: Same-output concurrent publication leaves one valid winner
      Given two compiler processes publish different payloads to the same output path
      When both processes run concurrently on the tested local filesystem
      Then at least one process exits successfully
      And the final output is a structurally valid CHM
      And the final output contains exactly one payload

  Rule: Unsupported HHW-compatible features degrade with warnings

    Scenario: Unsupported project options warn without failing
      Given Full-text search, Binary TOC, Binary Index, MERGE FILES, or WINDOWS custom settings are present
      When the project is otherwise compilable
      Then the process exits with code 0
      And a warning is printed for each unsupported feature
      And the generated CHM omits or downgrades that feature

  Rule: CHM package limits fail before final publication

    Scenario: CHM structural header invariants hold
      Given a generated CHM uses PMGL-only or PMGI-indexed directory blocks
      When its ITSF, section 0, ITSP, PMGI, and PMGL headers are inspected
      Then section offsets, file size, directory length, chunk count, and PMGL links are internally consistent

    Scenario: CHM directory entries resolve exact uncompressed payloads
      Given a generated CHM contains text and binary user files
      When PMGL directory entries are decoded
      Then each decoded archive path points to the exact expected content bytes
      And PMGI-indexed CHM files also resolve first and last user-file payloads correctly

    Scenario: Oversized directory entry fails as a compilation error
      Given a CHM archive path is too large for a PMGL block
      When CHM directory construction runs
      Then compilation exits with an error
      And no new final CHM output is published

    Scenario: Aggregate directory index overflow fails as a compilation error
      Given multiple CHM archive paths individually fit in PMGL blocks
      But their aggregate PMGI index exceeds the supported directory block
      When CHM directory construction runs
      Then compilation exits with an error
      And no new final CHM output is published

  Rule: No retry, cancellation, or timeout protocol is implemented

    Scenario: I/O errors are handled as immediate failures
      Given an I/O operation fails outside LinkScanner.ExtractLinks
      When the exception reaches Program.Main
      Then the process exits with code 1
      And no retry is attempted
      But human review is required to decide whether long-running compiles need cancellation or timeout support
