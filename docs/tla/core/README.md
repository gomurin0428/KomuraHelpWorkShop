# TLA+ Core Models

These hand-written models verify Komura HHC's abstract compiler contracts. They
are intentionally independent from the generated Gherkin use-case smoke models
under `docs/tla/usecases`.

| Model | Purpose | Key contracts |
| --- | --- | --- |
| `CompilerPipeline` | End-to-end compiler state machine | CLI terminal modes do not compile, required-file failures block CHM creation, writer failures do not create CHM files, successful compiles pass through collection, metadata, and writer stages. |
| `CliParsing` | CLI parser abstraction | Help/version short-circuit later arguments, argument errors stop before compile, repeated `--out` uses the last value, compile modes require one project. |
| `FileCollection` | Recursive file collection | External and fragment-only links are ignored, missing required files emit HHC5003 while collection completes, duplicate archive-path conflicts replace silently and scan replacement links, Flat storage uses filename-only archive paths, link cycles terminate. |
| `ArchivePathNormalization` | Path/link cleaning abstraction | Empty, fragment, external, UNC, and CHM links are ignored; query/fragment stripping, percent decoding, malformed percent retention, dot segments, Flat mode, and outside-project paths are finite and deterministic. |
| `TextEncodingDetection` | Text encoding and LCID fallback | BOM priority, strict UTF-8, invalid UTF-8 fallback, LCID parsing with and without `0x`, current ANSI fallback, and DBCS flag classification. |
| `LinkScannerBehavior` | Link extraction and Flat rewrite | HTML/CSS/HHC/HHK targets, read-error absorption, non-scannable files, ignored external/fragment targets, and Flat filename rewrites with suffix preservation. |
| `ChmDirectory` | CHM directory abstraction | Internal streams and user files are reachable on success, PMGI appears exactly for multi-PMGL directories, entry/directory limit failures do not create CHM files. |
| `ChmMetadataWriter` | CHM metadata writer abstraction | Optional `#SYSTEM` entries, generated contents metadata, DBCS/non-DBCS flags, string table deduplication, oversized metadata, input read failure, and locked-output preservation. |

Run only the core models:

```powershell
python .\tools\run_tla_core_models.py
```

Run the core models and generated use-case models:

```powershell
python .\tools\run_tla_models.py
```

The core models abstract filesystem contents, text encodings, and CHM bytes.
Concrete byte-level checks live in the C# integration test harness.
