# Fuzzing And Stress Seeds For Abstracted Regions

These seeds cover behavior intentionally abstracted away from the finite TLA+
models. They are not a substitute for the checked model; they are the next
executable destinations for byte-level parsing, platform path behavior, and
process-level races.

Current executable coverage added in this pass:

| Area | Covered by |
| -- | -- |
| CHM structural header, chunk, and exact payload seeds | `SmallProjectHasHeaderInternalStreamsAndPmglOnly`, `LargeProjectUsesPmgi`, `ChmStructuralHeaderInvariantsHold`, `ChmDirectoryEntriesResolveExactUserContent` |
| PATH deterministic escaping/idempotence seeds | `ArchivePathNormalizationPropertySeedsNeverEscape`, `GeneratedArchivePathFuzzSeedsNeverEscape` |
| PATH outside basename and collision seeds | `OutsideProjectPathsStayInsideArchiveNamespace`, `OutsideProjectBasenameCollisionsWarnAndKeepFirst` |
| LINK cleanup/extraction/flat rewrite seeds | `LinkCleaningCoversBoundaryTargets`, `LinkScannerExtractionSeedsCoverSyntax`, `FlatLinkRewriteSeedPropertiesAreStable`, `GeneratedFlatLinkRewriteFuzzSeedsAreIdempotent` |
| HHP parser syntax, line ending, and encoding decision seeds | `HhpParserBoundaryOptionsAreStable`, `HhpParserEncodingAndLineEndingSeedsAreStable` |
| ENC invalid UTF-8 fallback and BOM seeds | `JapaneseLanguageStoresCp932Metadata`, `EncodingDetectorFallbackSeedsAreStable`, `GeneratedInvalidUtf8FallbackSeedsSelectFallback`, `Utf16ProjectCompiles` |
| PERSIST stale-temp, process-death, and bounded race seeds | `StaleTempOutputDoesNotBlockNextCompile`, `ProcessDeathAfterTempCreationPreservesExistingOutput`, `ConcurrentWritersLeaveValidFinalOutput`, `CrossProcessSameOutputCompilesLeaveValidFinalOutput` |

Still not converted to normal CI tests: high-volume generated corpora beyond
the bounded seeds above, platform-matrix path behavior, broader process-kill crash
windows, stale-temp garbage collection after process death, platform-matrix
same-output race stress, and differential CHM validation against an independent
reader.

## Path Normalization Seeds

Target: `ArchivePath.NormalizeForArchive`, `ProjectCompiler.MakeArchiveRelative`,
and flat-mode link rewriting.

| Seed ID | Input shape | Expected oracle |
| -- | -- | -- |
| PATH-001 | `..`, `../`, `../../topic.html`, `a/../../../topic.html` | Archive path never starts with `..`, never contains `/../`, and never escapes archive root. |
| PATH-002 | `C:\outside\topic.html`, `D:/outside/topic.html`, `/outside/topic.html` | Project-declared outside source is stored by basename only after source resolution. |
| PATH-003 | `\\server\share\topic.html`, `//server/share/topic.html` | UNC and protocol-relative links are not collected as local link-scan inputs. |
| PATH-004 | `a//b///c.html`, `a/./b/../c.html`, `./index.html` | Normalized non-flat paths are stable under repeated normalization. |
| PATH-005 | flat mode with `a/index.html`, `b/index.html`, `A/INDEX.HTML` | First-wins duplicate policy and warning behavior are deterministic under case-insensitive comparison. |

## Link Parsing Seeds

Target: `LinkScanner.ExtractLinks` and `ArchivePath.CleanLink`.

| Seed ID | Input shape | Expected oracle |
| -- | -- | -- |
| LINK-001 | `<a href="#section">`, `<img src="">`, CSS `url(#icon)` | Fragment-only or empty targets are ignored. |
| LINK-002 | `https://example.test/a.html`, `mailto:x@y.test`, `javascript:alert(1)` | External schemes are ignored. |
| LINK-003 | `topic.html?x=1#frag`, `topic%20name.html`, `bad%zz.html` | Query/fragment removal and percent-decoding match current reviewed behavior; malformed escapes keep original spelling. |
| LINK-004 | mixed-case attributes, single quotes, whitespace around `=` | Extraction stays deterministic or unsupported syntax is documented. |
| LINK-005 | locked/unreadable HTML, CSS, HHC, HHK | Link scan returns no outgoing links and does not abort compilation. |

## HHP Parser Seeds

Target: `HhpProject.Load`.

| Seed ID | Input shape | Expected oracle |
| -- | -- | -- |
| HHP-001 | duplicate options with different casing | Last parsed value wins for the normalized option key. |
| HHP-002 | blank values: `Compiled file=`, `Title=` | Blank option values behave as omitted where current code does so. |
| HHP-003 | balanced quotes, doubled quotes, unbalanced quotes | Balanced quotes are stripped; unbalanced quotes remain literal. |
| HHP-004 | unknown sections before/after `[FILES]` | Unknown sections are retained for warning decisions and do not corrupt later sections. |
| HHP-005 | CRLF, LF, CR-only, UTF-8 BOM, UTF-16 BOM | Section and option parsing remain stable across line endings and BOMs. |

## Encoding Seeds

Target: `TextEncodingDetector` and metadata string encoding.

| Seed ID | Input shape | Expected oracle |
| -- | -- | -- |
| ENC-001 | valid UTF-8 with no language declaration | UTF-8 is selected and metadata bytes match UTF-8. |
| ENC-002 | invalid UTF-8 with `Language=0x0411 Japanese` | CP932 is selected and metadata bytes match CP932. |
| ENC-003 | invalid UTF-8 with no language declaration | Current ANSI fallback is documented; product should decide whether this is acceptable. |
| ENC-004 | UTF-16 LE/BE BOM with non-ASCII title | BOM wins over declared language. |
| ENC-005 | mixed-validity byte sequences near option delimiters | Parser must not split sections/options inconsistently or silently lose required file lines. |

## Persistence And Race Seeds

Target: process-level behavior outside the TLA crash abstraction.

| Seed ID | Input shape | Expected oracle |
| -- | -- | -- |
| PERSIST-001 | kill process after temp file creation and before final move | Existing final output remains valid; stale temp policy is either implemented or documented. |
| PERSIST-002 | locked final output during `File.Replace` | Existing output remains byte-for-byte unchanged and staged temp is cleaned up best effort. |
| PERSIST-003 | two processes compile different projects to one output | Product decision: last-writer-wins, deterministic lock failure, or explicit serialization. |
| PERSIST-004 | output directory on network/share-like path | Atomicity semantics are documented or tested on the supported platform set. |
| PERSIST-005 | antivirus-like delayed file lock on temp or final path | Failure remains a clean compile error without partial final output. |
