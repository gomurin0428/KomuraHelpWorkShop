# Existing-Code Formal Inspection Report

## Scope

Target code: `src/hhc` command-line compiler and the integration harness under
`tests/hhc.IntegrationTests`.

This inspection uses TLA+ as a bug-finding lens for the existing implementation.
The goal is not to prove the product correct, but to expose weak specifications,
missing tests, and failure states that deserve human review.

Supporting artifacts:

- `docs/formal-inspection/extraction-ledger.md`
- `docs/formal-inspection/ears-requirements.md`
- `docs/formal-inspection/current-spec.feature`
- `docs/formal-inspection/domain-model.md`
- `docs/formal-inspection/traceability.md`
- `docs/formal-inspection/recommended-tests.md`
- `docs/formal-inspection/fuzzing-seeds.md`
- `docs/formal-inspection/augmented-loop-audit.md`
- `docs/formal-inspection/completion-audit.md`
- `docs/formal-inspection/limit-completion-audit.md`
- `docs/tla/coverage-audit.md`

## Findings Fixed

| ID | Source | Problem | Remediation | Test/TLA |
| -- | -- | -- | -- | -- |
| BUG-001 | output persistence | Direct final-path CHM writing could leave a partial final output after post-create write failure. | `ChmWriter` now stages to a same-directory temp file and publishes only after staging succeeds. | `TempWriteFailurePreservesExistingOutput`, `DesiredFailureAtomicity`, `M015_temp_write_failure_leaves_partial` killed |
| BUG-002 | archive namespace | Parent-relative project files outside the HHP directory could leak sibling directory names into CHM archive paths. | Project-declared outside files now use source basename only. | `OutsideProjectPathsStayInsideArchiveNamespace`, `OutsideProjectPathUsesBasenameArchiveName`, `M016_outside_project_path_escapes_archive` killed |
| BUG-003 | CHM directory limits | A single oversized directory entry could escape the intended `CompilationException` path and throw an internal `ArgumentException`. | `ChmWriter` now checks single-entry PMGL fit before chunk building in both counting and building passes. Aggregate PMGI overflow is also regression-tested. | `OversizedDirectoryEntryFailsBeforePublishingOutput`, `AggregateDirectoryIndexTooLargeFailsBeforePublishingOutput`, UC062/UC063 directory-limit models |

## Extracted State, Events, I/O, And Failures

State:

- CLI parse mode: help, version, argument error, compile.
- Project state: not loaded, loaded, missing.
- Collection state: explicit roots, pending links, warnings, missing required files.
- Archive namespace state: none, project-relative, basename-only, escaped forbidden state.
- Metadata/package state: metadata built, CHM entries and byte package built.
- Writer state: output not published, output publication reached, valid CHM, existing output preserved, forbidden partial final output.
- Terminal result: exit code, stderr/stdout tags, warnings, CHM visibility.

Events:

- Parse CLI arguments.
- Load HHP project and detect declared language encoding.
- Collect required project files and optional linked files.
- Normalize archive paths and outside-project file names.
- Absorb link scanner read failures.
- Generate table of contents when no contents file is configured.
- Warn for unsupported HHW options.
- Build CHM metadata and package.
- Stage CHM bytes in a temporary file and publish to the final output path.

External input/output:

- Inputs: CLI arguments, HHP/HHC/HHK/HTML/CSS/binary files, filesystem state, current culture/codepage.
- Outputs: stdout summaries, stderr warnings/errors, final CHM output, same-directory temporary files.

Failures and non-features:

- Argument errors return exit code 2 before project loading.
- Compile exceptions return exit code 1.
- Link scanner read exceptions are swallowed and treated as no outgoing links.
- Other I/O exceptions become compile errors.
- No retry, cancellation, timeout, resume, or application-level output lock protocol is implemented.
- Temp cleanup is best effort; crash/power-loss durability is not verified.

## Human Review Needed

| ID | Decision needed | Current behavior |
| -- | -- | -- |
| HR-002 | Is process-level output atomicity enough? | Existing final output is preserved on staging/publish failure, but fsync/crash durability is not guaranteed. |
| HR-004 | Should unsupported HHW features remain warning-only? | They warn and compilation can still succeed. |
| HR-005 | Is absence of retry/cancellation/timeout acceptable? | Current implementation has no such protocol. |
| HR-006 | Should concurrent compiles to one output be serialized? | Current implementation relies on filesystem move/replace behavior only. |

## Compatibility Decisions Accepted

| ID | Decision | Locked behavior |
| -- | -- | -- |
| CD-001 | Match the reference compiler for link-scan read failures. | Link scanner read exceptions are absorbed without warnings and treated as no outgoing links. |
| CD-002 | Match the reference compiler for explicit outside source files. | Project-declared outside files are read when explicitly listed, but stored in the CHM under basename-only archive paths. |

## TLA+ Model

Primary inspection model: `docs/tla/inspection/ExistingCodeLoop.tla`

State variables:

- `phase`, `scenario`, `filesCollected`, `archiveNamespace`
- `metadataBuilt`, `packageBuilt`, `outputOpened`, `outputState`
- `exitCode`, `errorKind`, `warnings`
- `pendingLinks`, `linkReadAbsorbed`, `retryCount`, `transitionCount`, `visited`

Actions:

- `CliTerminal`, `LoadProject`, `CollectFiles`, `ScanLinks`
- `BuildMetadata`, `BuildPackage`, `CreateOutput`, `WriteOutput`
- `ExternalAbort`, `StayDone`

Key invariants and temporal properties:

- `StageDiscipline`
- `FinalOutcomeMatchesCurrentCode`
- `NoSuccessfulChmOnError`
- `OutputCreateFailurePreservesExistingOutput`
- `OutsideProjectPathUsesBasenameArchiveName`
- `ArchiveNamespaceNeverEscapes`
- `TerminalTransitionCountMatchesPath`
- `WriteFailureBeforePublishPreservesExistingOutput`
- `LinkReadFailureIsAbsorbedWithoutWarning`
- `UnsupportedFeaturesWarnButSucceed`
- `NoExplicitCancellationOrTimeoutPath`
- `NoRetryPolicy`
- `DesiredFailureAtomicity`
- `EventuallyDone`

TLA+ abstraction boundaries are documented in `docs/formal-inspection/domain-model.md`.

## TLC Results

Inspection model:

- Command: `python tools/run_tla_inspection_model.py`
- Result: PASS
- Models: 1
- Coverage: enabled, 1 minute interval
- Summary: `docs/tla/inspection/verification-summary.md`

Existing broader TLA suite:

- Command: `python tools/run_tla_models.py`
- Result: PASS
- Models: 107
- Suites: implementation, core, use cases

Coverage audit:

- Command: `python tools/audit_tla_coverage.py`
- Result: PASS
- Logs checked: 108
- Unexpected zero-hit or missing-coverage issues: 0

## Mutation Oracle

Command: `python tools/run_tla_mutation_oracle.py`

Summary: `docs/tla/inspection/mutation-oracle-summary.md`

Mutants: 23

| Classification | Count | Notes |
| -- | --: | -- |
| killed | 20 | TLC detected invariant, temporal, deadlock, or action-spec violations. |
| equivalent | 3 | No observable reachable-state difference in the modeled domain. |
| true survivor | 0 | None remained. |

Equivalent mutants:

- `M004_ge_to_gt_equivalent_retry_domain`: `retryCount >= 0` to `retryCount > -1`; all reachable states have `retryCount = 0`.
- `M011_cancel_timeout_transition_changed`: external abort exit code changed; the transition is unreachable because the implementation exposes no cancellation/timeout path.
- `M018_abort_action_removed_equivalent`: unreachable abort/cancel/timeout action removed from `Next`; no reachable state changes because `ExternalAbortScenarios = {}`.

Strengthened invariants:

- `DesiredFailureAtomicity` and `WriteFailureBeforePublishPreservesExistingOutput` kill partial-final-output mutations.
- `OutsideProjectPathUsesBasenameArchiveName` and `ArchiveNamespaceNeverEscapes` kill archive namespace escape mutations.
- `TerminalTransitionCountMatchesPath` kills `+`/`-` path-length update mutations.
- `NoPendingLinksAtTerminalState` kills pending-link reset deletion.
- `LinkReadFailureIsAbsorbedWithoutWarning` kills loss of the absorbed-link failure marker.
- `StageDiscipline` and `SuccessRequiresPackageAndOutputOpen` kill premature output/write transitions.
- `FinalOutcomeMatchesCurrentCode` kills success-publish update deletion and write-failure-to-success mutations.
- `EventuallyDone` with `WF_vars(Next)` kills fairness removal; `BuildPackage` action deletion is killed by deadlock detection.

## Implementation Tests

Command:

- `dotnet run --project tests\hhc.IntegrationTests\hhc.IntegrationTests.csproj`
- Current Windows run used `-p:UseAppHost=false` to avoid stale apphost file-lock churn in repeated nested `dotnet run` calls.

Result:

- 59 integration/direct tests passed.

Added or strengthened tests:

- `ChmStructuralHeaderInvariantsHold`
- `ChmDirectoryEntriesResolveExactUserContent`
- `TempWriteFailurePreservesExistingOutput`
- `OutsideProjectPathsStayInsideArchiveNamespace`
- `LinkCleaningCoversBoundaryTargets`
- `HhpParserBoundaryOptionsAreStable`
- `Utf16ProjectCompiles`
- `NoLinkScanSkipsOptionalLinkedMissingFiles`
- `UnsupportedHhwFeaturesWarnButSucceed`
- `HelpExitsZeroBeforeProjectLoading`
- `VersionExitsZeroBeforeProjectLoading`
- `MissingOutValueExitsTwo`
- `ArchivePathNormalizationPropertySeedsNeverEscape`
- `GeneratedArchivePathFuzzSeedsNeverEscape`
- `OutsideProjectBasenameCollisionsWarnAndKeepFirst`
- `LinkScannerExtractionSeedsCoverSyntax`
- `FlatLinkRewriteSeedPropertiesAreStable`
- `GeneratedFlatLinkRewriteFuzzSeedsAreIdempotent`
- `LinkScannerReadFailureIsAbsorbed`
- `PublishFailureAfterTempStagingCleansTemp`
- `OversizedDirectoryEntryFailsBeforePublishingOutput`
- `AggregateDirectoryIndexTooLargeFailsBeforePublishingOutput`
- `HhpParserEncodingAndLineEndingSeedsAreStable`
- `EncodingDetectorFallbackSeedsAreStable`
- `GeneratedInvalidUtf8FallbackSeedsSelectFallback`
- `StaleTempOutputDoesNotBlockNextCompile`
- `ProcessDeathAfterTempCreationPreservesExistingOutput`
- `ConcurrentWritersLeaveValidFinalOutput`
- `CrossProcessSameOutputCompilesLeaveValidFinalOutput`

Test wiring red/green check:

- Correct implementation: all 59 tests passed.
- Current rerun: `dotnet run --project tests\hhc.IntegrationTests\hhc.IntegrationTests.csproj -p:UseAppHost=false` passed all 59 tests. MSBuild emitted stale apphost/cache delete warnings, but the executable tests ran against the rebuilt DLL path.
- Current intentional source mutation: temporarily changed `ProjectCompiler.MakeArchiveRelative` so outside project paths reused `originalPath` instead of `Path.GetFileName(sourcePath)`.
- Expected red result: `outside project paths stay inside archive namespace` failed because the sibling temp directory name appeared in CHM bytes, and `outside project basename collisions warn and keep first` failed because basename collision detection no longer fired.
- Restored implementation: the same 59 tests passed again.
- Mutated implementation: temporarily changed `ProjectCompiler.MakeArchiveRelative` so outside project paths were not basename-only.
- Expected red result: `outside project paths stay inside archive namespace` failed by detecting the sibling temp directory name inside CHM bytes.
- Mutated implementation: temporarily disabled the aggregate PMGI size guard in `ChmWriter.BuildSinglePmgiChunk`.
- Expected red result: `aggregate directory index too large fails before publishing output` failed because an internal `ArgumentException` escaped instead of `CompilationException`.
- Mutated implementation: temporarily used the final output path as the staging path.
- Expected red result: normal successful compiles and `temp write failure preserves existing CHM output` failed, proving output-publish and preservation tests are wired to the implementation.
- Mutated implementation: temporarily disabled invalid-UTF-8 fallback in `TextEncodingDetector`.
- Expected red result: `JapaneseLanguageStoresCp932Metadata`, `HhpParserEncodingAndLineEndingSeedsAreStable`, and `EncodingDetectorFallbackSeedsAreStable` failed.
- Mutated implementation: temporarily disabled CHM content-offset advancement in `ChmWriter.AssignContentOffsets`.
- Expected red result: `ChmStructuralHeaderInvariantsHold` and `ChmDirectoryEntriesResolveExactUserContent` failed by decoding wrong payload bytes.
- Restored implementation: all 59 tests passed again.

## Dropped To Gherkin / Tests / Seeds

- BUG-001 is represented in Gherkin as "Temp-file write failure preserves the existing output" and in `TempWriteFailurePreservesExistingOutput`.
- BUG-002 is represented in Gherkin as "Outside project files are included by basename only" and in `OutsideProjectPathsStayInsideArchiveNamespace`.
- BUG-003 is represented by the CHM directory-entry limit unwanted requirement and in `OversizedDirectoryEntryFailsBeforePublishingOutput`.
- Aggregate directory overflow is represented by the CHM directory-limit unwanted requirement and in `AggregateDirectoryIndexTooLargeFailsBeforePublishingOutput`.
- CHM structural headers/chunks, PMGL directory-entry exact payload decoding, outside basename collision, bounded generated path seeds, link cleanup, link extraction syntax, flat rewrite idempotence, bounded generated flat rewrite seeds, parser boundaries, CR-only line endings, UTF-8 BOM, UTF-16 BOM, CP932 fallback, ANSI fallback selection, bounded generated invalid-UTF8 fallback seeds, stale temp non-interference, controlled process death after temp creation, bounded same-output writer/process races, no-link-scan, optional link-read absorption, and unsupported feature warnings are now covered by implementation tests.
- Remaining abstracted areas are assigned to property/fuzz/differential/stress/fault-injection checks in `domain-model.md`, `traceability.md`, and `fuzzing-seeds.md`.

## Verified Scope

Can be called verified within the stated abstraction:

- Modeled CLI/project/file/output failure stage ordering.
- Process-level final-output atomicity for temp-write and publish exceptions.
- CHM ITSF/section0/ITSP/PMGL/PMGI structural invariants and PMGL directory-entry exact payload resolution for generated small and indexed outputs.
- Outside project files do not escape the CHM archive namespace; they use basename-only archive names and duplicate basenames warn/keep first.
- Link target cleanup, extraction, and flat rewrite behavior for selected and bounded generated external/UNC/fragment/malformed-percent/HTML/CSS/HHC boundary seeds.
- Link scanner read failures are executable-test covered as silent absorption.
- Deterministic HHP parsing for duplicate, blank, quoted, unbalanced quoted, CR-only line ending, unknown section, and truthy option seeds.
- CLI help/version/argument-error short-circuit branches are executable-test covered.
- Oversized metadata, oversized directory-entry, and aggregate directory-index failures stop before output publication.
- Declared CP932, invalid-UTF8 fallback, current ANSI fallback selection, UTF-8 BOM, UTF-16 BOM, and bounded generated invalid-UTF8 fallback paths covered by implementation tests.
- Unsupported HHW feature declarations warn and do not silently disappear.
- Stale temp files do not block a later successful compile on the tested filesystem.
- Controlled process death after temp creation preserves the existing final output and leaves a stale temp file.
- Bounded in-process and two-process same-output races leave a structurally valid final CHM with exactly one winning payload on the tested filesystem.
- TLA mutation oracle has 0 true survivors in the inspection model after the 23-mutant augmented run.

Cannot yet be called verified:

- Crash consistency across power loss, fsync, or storage-controller failure.
- Startup cleanup or garbage collection of stale temp files after process death.
- Platform/high-volume concurrent compiles to the same final output beyond the bounded local-filesystem race tests.
- Full Windows/UNC/path grammar beyond selected boundary, collision, and bounded generated seed-property tests.
- Full HTML/CSS parsing grammar beyond selected and bounded generated extraction/cleanup/rewrite seed tests.
- Exhaustive encoding behavior across all invalid byte/codepage combinations beyond bounded generated seeds.
- Independent CHM-reader compatibility beyond structural header/chunk and PMGL exact-payload invariants.
- Sandboxing or blocking behavior for untrusted HHP projects; compatibility mode treats HHP files as trusted local project manifests and allows explicit outside source inclusion.
- Cancellation, timeout, and retry semantics, because the implementation has no such protocol.
