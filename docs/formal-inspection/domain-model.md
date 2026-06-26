# State And Domain Model

This model is implementation-informed but intentionally not an implementation copy.

## Modeled State Variables

| Variable | Domain | Meaning | Ledger IDs |
| -- | -- | -- | -- |
| `scenario` | finite scenario tags | Representative normal, event, and unwanted paths. | all modeled IDs |
| `phase` | `Start`, `ProjectLoaded`, `FilesCollected`, `LinksScanned`, `MetadataBuilt`, `PackageBuilt`, `OutputOpened`, `Done` | Compiler pipeline stage. | S-001, T-001 |
| `filesCollected` | boolean | Required collection stage has run. | N-003, E-003 |
| `archiveNamespace` | `None`, `InsideRelative`, `BasenameOnly`, `Escaped` | Abstract CHM archive-path safety result. | B-001, B-002, U-006 |
| `metadataBuilt` | boolean | Metadata construction stage has run. | N-002, S-001 |
| `packageBuilt` | boolean | CHM directory/content package stage has run. | N-005, S-001 |
| `outputOpened` | boolean | Final publication stage has been reached. | N-006, E-006 |
| `outputState` | `Absent`, `Existing`, `ValidChm`, `Partial` | Observable final output state; `Partial` is a forbidden bad state. | P-001, U-005 |
| `exitCode` | `-1`, `0`, `1`, `2` | Process result abstraction. | S-002 |
| `errorKind` | finite tags | User-visible error class. | E-001..E-008 |
| `warnings` | set of tags | User-visible warning classes. | S-003, N-007 |
| `pendingLinks` | set of tags | Abstract optional links awaiting scan. | N-004, U-004 |
| `linkReadAbsorbed` | boolean | Link-scan read failure was swallowed. | IO-001, U-004 |
| `retryCount` | integer bounded to 0 | Documents no retry policy. | U-011 |
| `transitionCount` | integer `0..8` | Abstract non-stutter transition count for path-length mutation checks. | S-001, T-001 |
| `visited` | set of stage tags | Coverage/stage discipline signal. | S-001 |

## Actions

| Action | Description | Ledger IDs |
| -- | -- | -- |
| `CliTerminal` | Help/argument-error terminal behavior before project load. | N-001, E-001, U-001 |
| `LoadProject` | Project load success or missing-project failure. | E-002, U-002 |
| `CollectFiles` | Required/optional file collection, including outside-project basename policy. | N-003, E-003, E-004, B-002, U-006 |
| `ScanLinks` | Optional link scan and swallowed read failure. | N-004, U-004 |
| `BuildMetadata` | Warning accumulation and metadata stage. | N-002, N-007 |
| `BuildPackage` | Abstract CHM package construction. | N-005 |
| `CreateOutput` | Output publication setup or output-create failure. | E-006 |
| `WriteOutput` | Successful final publish or temp-write-before-publish failure. | N-006, U-005 |
| `ExternalAbort` | Deliberately unreachable cancellation/timeout placeholder. | U-012 |
| `StayDone` | Stutter after terminal state. | T-001 |

## Invariants And Temporal Properties

| Property | Purpose | Ledger IDs |
| -- | -- | -- |
| `TypeOK` | Keeps all modeled variables inside finite domains. | all modeled IDs |
| `StageDiscipline` | Metadata/package/output stages cannot happen out of order. | S-001 |
| `FinalOutcomeMatchesCurrentCode` | Terminal exit/error/warning/output/archive namespace matches current-code spec. | S-002, S-003 |
| `FatalErrorDoesNotCreateChm` | Fatal error scenarios cannot report a valid CHM. | U-003, U-005 |
| `SuccessRequiresPackageAndOutputOpen` | A valid CHM requires package construction and output publication. | N-002, N-005 |
| `EarlyFailuresStopBeforeMetadata` | CLI/project/required-file failures stop early. | U-001..U-003 |
| `OutputCreateFailurePreservesExistingOutput` | Locked/unwritable final target preserves existing output. | E-006 |
| `OutsideProjectPathUsesBasenameArchiveName` | Outside project paths are basename-only in the archive. | B-002, U-006 |
| `ArchiveNamespaceNeverEscapes` | No terminal state may have escaped archive namespace. | B-001, SEC-001 |
| `TerminalTransitionCountMatchesPath` | Terminal path length must match the selected scenario. | S-001, T-001 |
| `WriteFailureBeforePublishPreservesExistingOutput` | Temp-write failure preserves final output. | U-005 |
| `LinkReadFailureIsAbsorbedWithoutWarning` | Current link-scan read failures are silent and non-fatal. | U-004 |
| `NoPendingLinksAtTerminalState` | Link scan does not leave work pending. | N-004 |
| `UnsupportedFeaturesWarnButSucceed` | Unsupported feature declarations warn and still succeed. | N-007, U-010 |
| `NoExplicitCancellationOrTimeoutPath` | Documents absence of cancellation/timeout protocol. | U-012 |
| `NoRetryPolicy` | Documents absence of retry protocol. | U-011 |
| `DesiredFailureAtomicity` | Failed compiles must not leave partial final output. | U-005 |
| `EventuallyDone` | Every modeled behavior reaches a terminal state. | T-001 |

## External Inputs And Outputs

Inputs modeled as tags:

- CLI mode and parse errors.
- Project load success/missing.
- Required missing file.
- Optional link read failure.
- Unsupported feature declarations.
- Outside-project path.
- Output create/write failure.

Outputs modeled as tags:

- Exit code and error kind.
- Warning set.
- Final output state.
- Archive namespace safety state.
- Stage coverage.

## TLA+ Abstractions

Modeled in TLA+:

- Pipeline ordering and terminal outcomes.
- Required vs optional failure boundaries.
- Output atomicity at the final path.
- Outside-project archive namespace safety.
- Warning/error separation.
- Absence of retry/cancellation/timeout protocols.

Abstracted away from TLA+:

- Independent-reader compatibility and full CHM payload semantics. Basic ITSF/section0/ITSP/PMGL/PMGI header, offset, chunk-count, PMGL-link invariants, and PMGL directory-entry-to-payload resolution are covered by implementation tests.
- Full filesystem path grammar across Windows/UNC/network shares.
- Full HTML/CSS parsing grammar.
- Exact codepage byte sequences and Unicode normalization.
- Cross-process race timing and OS-level file replacement semantics.
- Power-loss crash consistency and fsync behavior.
- Sandboxing policy for untrusted HHP files; compatibility mode treats HHP files as trusted local project manifests.

Supplement plan for abstracted areas:

| Abstracted area | Proposed independent check |
| -- | -- |
| CHM binary layout | Structural header/chunk and PMGL exact-payload tests are added; golden-file/differential tests against known CHM readers remain recommended where available. |
| Path grammar | Property-based tests over Windows-like, UNC, rooted, parent-relative, dot-only, blank, and long paths. |
| HTML/CSS parsing | Fuzzing seeds for attributes, CSS URLs, entities, percent escapes, fragments, and malformed tags. |
| Encoding | Property tests over BOMs, invalid UTF-8, declared LCIDs, and fallback encodings. |
| Cross-process races | Bounded two-process same-output stress is added; broader platform/high-volume stress or explicit locking decision remains recommended. |
| Crash consistency | Controlled process-death-after-temp test is added; broader process-kill windows, final replace interruption, fsync/power-loss, and stale temp cleanup after process death remain recommended. |
| Trust/security policy | Compatibility decision accepted: explicit outside source files are allowed for trusted local HHP projects; document that this compiler is not a sandbox for untrusted manifests. |
