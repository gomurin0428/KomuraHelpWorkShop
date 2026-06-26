# Formal Verification Traceability

Legend:

- `IT:` integration/direct implementation test in `tests/hhc.IntegrationTests/Program.cs`.
- `TLA:` model element in `docs/tla/inspection/ExistingCodeLoop.tla`.
- `UC:` existing per-use-case TLA model under `docs/tla/usecases`.
- `Lean/Dafny target`: a candidate for future proof, not implemented in this pass.

| ID | EARS/Gherkin | TLA+ Action / Inv / Temporal | Lean/Dafny target | Implementation test | Evidence / gap |
| -- | ------------ | ---------------------------- | ----------------- | ------------------- | -------------- |
| N-001 | `N-001`, `U-001` | `CliTerminal`, `EarlyFailuresStopBeforeMetadata` | none | `HelpExitsUsageBeforeProjectLoading`, `VersionPrintsUsageBeforeProjectLoading`, `UnknownCliOptionExitsUsage` | covered. |
| N-002 | `N-002` | `LoadProject`..`WriteOutput`, `StageDiscipline` | compiler pipeline ordering | `SmallProjectHasHeaderInternalStreamsAndPmglOnly` | covered by TLA + compile smoke. |
| N-003 | `N-003`, `U-003` | `CollectFiles`, `FinalOutcomeMatchesCurrentCode` | path normalization postcondition | `MissingRequiredFileEmitsPartialChm` | covered for HHC-compatible partial output. |
| N-004 | `N-004`, `U-004` | `ScanLinks`, `LinkReadFailureIsAbsorbedWithoutWarning` | none | `NoLinkScanSkipsOptionalLinkedMissingFiles`, `FlatReplacementPrunesLosingSourceLinks`, `LinkScannerExtractionSeedsCoverSyntax`, `LinkScannerReadFailureIsAbsorbed` | covered for no-scan, replaced-source link pruning, common extraction syntax, and read-failure absorption. |
| N-005 | `N-005` | `BuildPackage`, `SuccessRequiresPackageAndOutputOpen` | CHM directory entry encoder | `SmallProjectHasHeaderInternalStreamsAndPmglOnly`, `LargeProjectUsesPmgi`, `ChmStructuralHeaderInvariantsHold`, `ChmDirectoryEntriesResolveExactUserContent` | header/offset/chunk invariants and PMGL directory-entry-to-payload resolution covered; independent-reader differential tests still recommended. |
| N-006 | `N-006`, Gherkin temp-write scenario | `WriteOutput`, `DesiredFailureAtomicity` | atomic publish wrapper contract | `TempWriteFailurePreservesExistingOutput`, `LockedOutputFileCreateExitsOne` | process-level atomicity covered; crash fsync not covered. |
| N-007 | `N-007`, `U-010` | `BuildMetadata`, `UnsupportedFeaturesWarnButSucceed` | none | `UnsupportedHhwFeaturesWarnButSucceed` | covered. |
| N-008 | `N-008` | abstracted as no optional pending links | none | `NoLinkScanSkipsOptionalLinkedMissingFiles` | covered. |
| E-001 | `E-001`, `U-001` | `CliTerminal` | CLI parser totality | `UnknownCliOptionExitsUsage`, `MissingOutValueExitsUsage` | covered for selected parse-error branches. |
| E-002 | `E-002`, `U-002` | `LoadProject`, `EarlyFailuresStopBeforeMetadata` | none | `MissingProjectExitsZero` | covered. |
| E-003 | `E-003`, `U-003` | `CollectFiles`, `FinalOutcomeMatchesCurrentCode` | none | `MissingRequiredFileEmitsPartialChm` | covered for HHC5003 partial CHM output. |
| E-004 | `E-004` | `CollectFiles`, `FinalOutcomeMatchesCurrentCode` | none | `AllowMissingDowngradesRequiredAbsence` | metadata reference edge still review-needed. |
| E-005 | `E-005` | UC023/UC024/UC096 use-case models | map silent last-wins invariant | `FlatDuplicateConflictKeepsLast`, `FlatReplacementPrunesLosingSourceLinks`, `OutsideProjectBasenameCollisionsKeepLast`, `CaseOnlyArchiveSourceCollisionIsQuiet` | covered for flat, replaced-source link pruning, outside-basename, and case-only conflicts. |
| E-006 | `E-006`, `U-005` | `CreateOutput`, `WriteOutput`, `DesiredFailureAtomicity` | atomic publish contract | `OutputPathCannotOverwriteProjectOrInputFiles`, `OutputPathCannotOverwriteReplacedFlatCollisionSource`, `UnwritableOutputTargetExitsOne`, `LockedOutputFileCreateExitsOne`, `TempWriteFailurePreservesExistingOutput` | covered for output overwrite guards and process-level failures. |
| E-007 | `E-007` | UC084 writer input-read error model | none | `LockedInputFileReadExitsOne` | covered. |
| E-008 | `E-008` | UC062, UC063, UC086 limit use cases | CHM size arithmetic | `OversizedMetadataEntryExitsOne`, `OversizedDirectoryEntryFailsBeforePublishingOutput`, `AggregateDirectoryIndexTooLargeFailsBeforePublishingOutput` | covered for metadata, single-entry PMGL limit, and aggregate PMGI overflow. |
| B-001 | `U-006` | `ArchiveNamespaceNeverEscapes` | `NormalizeForArchive` postcondition | `LinkCleaningCoversBoundaryTargets`, `ArchivePathNormalizationPropertySeedsNeverEscape`, `GeneratedArchivePathFuzzSeedsNeverEscape` | covered for deterministic and generated escaping/idempotence seeds; platform-matrix expansion recommended. |
| B-002 | Gherkin outside-path scenario | `OutsideProjectPathUsesBasenameArchiveName` | `MakeArchiveRelative` postcondition | `OutsideProjectPathsStayInsideArchiveNamespace`, `OutsideProjectBasenameCollisionsKeepLast` | covered, including silent last-wins basename collision behavior. |
| B-003 | flat archive behavior | UC040/UC041/UC097 | flat archive and payload-preservation postcondition | `FlatDuplicateConflictKeepsLast`, `FlatReplacementPrunesLosingSourceLinks`, `FlatBaseHrefScannerFlattensArchiveWithoutRewritingPayload`, `FlatUtf16RewritePreservesBom`, `FlatLinkRewriteSeedPropertiesAreStable` | covered for archive flattening, replacement reachability, payload preservation, and helper rewrite idempotence. |
| B-004 | `U-007` | UC030/UC031/UC075/UC076 | link cleaner postcondition | `LinkCleaningCoversBoundaryTargets`, `LinkScannerExtractionSeedsCoverSyntax`, `FlatLinkRewriteSeedPropertiesAreStable`, `GeneratedFlatLinkRewriteFuzzSeedsAreIdempotent` | drive-rooted local links review-needed. |
| B-005 | `U-008` | UC016/UC068/UC069/UC070 | parser determinism | `HhpParserBoundaryOptionsAreStable`, `HhpParserEncodingAndLineEndingSeedsAreStable` | covered for selected syntax, CR-only line endings, and encoding-driven parser boundaries. |
| B-006 | `U-009` | UC042..UC079 | encoding detector decision tree | `JapaneseLanguageStoresCp932Metadata`, `HhpParserEncodingAndLineEndingSeedsAreStable`, `EncodingDetectorFallbackSeedsAreStable`, `GeneratedInvalidUtf8FallbackSeedsSelectFallback`, `Utf16ProjectCompiles` | covered for CP932 fallback, ANSI fallback selection, UTF-8 BOM, UTF-16 BOM, and generated invalid UTF-8 fallback seeds; broader codepage corpus still recommended. |
| IO-001 | `U-004`, `E-007` | `ScanLinks`, UC084 | none | `LockedInputFileReadExitsOne`, `LinkScannerReadFailureIsAbsorbed` | covered for writer required-read failure and optional scan absorption. |
| IO-002 | `U-005` | `CreateOutput`, `WriteOutput` | atomic publish wrapper | output failure tests, `PublishFailureAfterTempStagingCleansTemp` | covered except crash/power loss. |
| C-001 | `U-011` | concurrency abstracted from TLA; `NoRetryPolicy` documents absence of coordination protocol | none | `ConcurrentWritersLeaveValidFinalOutput`, `CrossProcessSameOutputCompilesLeaveValidFinalOutput` | bounded local-filesystem races covered; explicit locking/product policy and platform stress still review-needed. |
| C-002 | no parallelism requirement | abstracted away | none | `ConcurrentWritersLeaveValidFinalOutput` | no in-pipeline parallelism exists; writer-level race stress covered. |
| P-001 | `U-005` | `DesiredFailureAtomicity` | atomic publish contract | `TempWriteFailurePreservesExistingOutput`, `ProcessDeathAfterTempCreationPreservesExistingOutput` | covered for process exceptions and controlled process death after temp creation; power-loss/fsync not covered. |
| P-002 | `U-013` | abstracted away except temp cleanup on exception | none | temp cleanup assertions in `TempWriteFailurePreservesExistingOutput`, `PublishFailureAfterTempStagingCleansTemp`, `StaleTempOutputDoesNotBlockNextCompile`, `ProcessDeathAfterTempCreationPreservesExistingOutput` | stale temp non-interference and process-death stale temp creation covered; cleanup/garbage collection not implemented or verified. |
| SEC-001 | `U-006` | `ArchiveNamespaceNeverEscapes` | path safety postcondition | `OutsideProjectPathsStayInsideArchiveNamespace` | accepted compatibility behavior: explicit outside source inclusion is allowed for trusted local HHP projects, while CHM archive namespace escape remains forbidden. |
| SEC-002 | threat model note | not modeled | none | none | trusted local input assumed; not verified. |
| T-001 | `EventuallyDone` | temporal property `EventuallyDone` | none | all process tests terminate | modeled termination covered; real hangs not exhaustively verified. |
| U-001 | invalid CLI unwanted | `CliTerminal` | CLI parser totality | `UnknownCliOptionExitsUsage`, `MissingOutValueExitsUsage`, help/version tests | covered for selected branches. |
| U-002 | missing project unwanted | `LoadProject` | none | `MissingProjectExitsZero` | covered. |
| U-003 | missing required unwanted | `CollectFiles` | none | `MissingRequiredFileEmitsPartialChm` | covered. |
| U-004 | link read unwanted | `ScanLinks` | none | `LinkScannerReadFailureIsAbsorbed` | covered. |
| U-005 | output failure unwanted | `DesiredFailureAtomicity` | atomic publish wrapper | output atomicity tests | covered. |
| U-006 | traversal unwanted | `ArchiveNamespaceNeverEscapes`, `OutsideProjectPathUsesBasenameArchiveName` | path postcondition | path boundary tests, outside collision test, and seed-property loop | covered for selected cases. |
| U-007 | external link unwanted | use-case TLA link models | link cleaner postcondition | link cleanup, extraction, and flat rewrite tests | covered for selected cases. |
| U-008 | parser ambiguity unwanted | use-case HHP models | parser determinism | HHP parser syntax, line-ending, and encoding-boundary tests | covered for selected cases. |
| U-009 | encoding unwanted | encoding use-case models | encoding detector | encoding integration/direct tests | covered for selected fallback/BOM cases; generated fuzz recommended. |
| U-010 | unsupported feature unwanted | `UnsupportedFeaturesWarnButSucceed` | none | `UnsupportedHhwFeaturesWarnButSucceed` | covered. |
| U-011 | retry/double execution unwanted | `NoRetryPolicy`; concurrency timing not modeled | none | `ConcurrentWritersLeaveValidFinalOutput`, `CrossProcessSameOutputCompilesLeaveValidFinalOutput` | bounded race final-output validity covered; no retry/lock protocol exists. |
| U-012 | cancel/timeout unwanted | `NoExplicitCancellationOrTimeoutPath` | none | none | documented absence only. |
| U-013 | crash restart unwanted | abstracted away | none | `ProcessDeathAfterTempCreationPreservesExistingOutput`, `StaleTempOutputDoesNotBlockNextCompile` | controlled process death and stale-temp non-interference covered; restart cleanup policy remains unverified. |

## Traceability Gaps To Track

| Gap | Why it matters | Proposed destination |
| -- | -- | -- |
| Platform-matrix and high-volume path/link/encoding fuzz | TLA abstracts byte-level parsing, and implementation tests cover deterministic plus bounded generated corpora rather than exhaustive platform/codepage matrices. | expanded property/fuzz seeds in `fuzzing-seeds.md` |
| Platform and high-volume same-output race stress | Bounded same-output race tests now pass, but TLA abstracts timing and only the current local filesystem was exercised. | platform/stress matrix or product locking decision |
| Stale temp garbage collection after process death | TLA abstracts crash; controlled process death and stale-temp non-interference are tested, but garbage collection after process death is not implemented or verified. | startup cleanup implementation/test or documented product decision |
