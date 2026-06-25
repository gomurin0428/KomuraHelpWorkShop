# Augmented Formal-Methods Loop Audit

This file maps the augmented Japanese instruction set to the current `src/hhc`
inspection artifacts. It records applicability decisions; the example checklist
items are not adopted mechanically when the compiler has no corresponding
protocol or state.

## Scope Classification

| Category | Applies | Evidence and decision |
| -- | -- | -- |
| State transitions | yes | CLI/project/collection/link/metadata/package/output/terminal phases are modeled in `ExistingCodeLoop.tla`. |
| Concurrency/asynchrony | partial | No internal async pipeline exists. Same-output writer races are tested with bounded in-process and two-process integration tests. |
| Retry/cancel/timeout | no implemented protocol | Absence is explicit in `NoRetryPolicy`, `NoExplicitCancellationOrTimeoutPath`, and `U-011`/`U-012`; product review is still required. |
| External I/O | yes | Filesystem reads, output-directory creation, temp-file staging, replace/move, and locked-output failures are modeled or tested. |
| Persistence/recovery | partial | Final-output preservation, stale-temp non-interference, and process death after temp creation are tested; fsync/power loss and temp GC are not verified. |
| Security/integrity | partial | Archive namespace safety is modeled/tested. Explicit outside source inclusion is accepted for reference-compiler compatibility; signature/auth and sandboxing untrusted HHP manifests are outside current implementation. |
| Protocol/file format | yes | HHP parsing and CHM structural/directory payload behavior are covered by use-case TLA and integration tests. |
| UI/user operation order | no | This is a CLI compiler; no UI states were found. |
| Windows/OS API | partial | Path, lock, replace/move, and current ANSI fallback are relevant. COM/service/registry/device checks are not applicable. |
| Pure function only | no | The target has significant state, I/O, and file-format behavior, so TLA+ is appropriate for workflow-level checks. |

## Multi-Perspective Extraction

| Perspective | Added or confirmed IDs | Non-applicable or delegated area |
| -- | -- | -- |
| State machine | `S-001`, `S-002`, `T-001`, `U-001`..`U-003` | Infinite/hanging real process behavior is delegated to integration/stress tests. |
| Abnormal paths | `E-001`..`E-008`, `U-001`..`U-010` | Retry exhaustion is not applicable because no retry protocol exists. |
| External I/O | `IO-001`, `IO-002`, `P-001`, `P-002`, `U-005` | Power-loss and storage-controller failures are delegated to fault-injection/platform tests. |
| Concurrency/timing | `C-001`, `U-011`, `U-012` | No application-level lock exists; product must decide lock vs last-writer-wins. |
| Security/integrity | `SEC-001`, `SEC-002`, `B-001`, `B-002`, `U-006` | Signature/auth/tenant/owner checks are absent, not silently modeled as verified. |
| Persistence/crash recovery | `P-001`, `P-002`, `U-013` | Startup stale-temp cleanup is not implemented or verified. |
| Compatibility/config | `B-005`, `B-006`, `N-007`, `U-008`..`U-010` | Full HTML Help Workshop compatibility remains a differential-test target. |
| Resource/long-running | `E-008`, CHM limit tests | Load/soak/resource-exhaustion testing remains outside the finite model. |
| Observability | `S-003`, warnings/errors in `FinalOutcomeMatchesCurrentCode` | Structured logs/metrics are not implemented. |
| Testability | injectable temp writer and direct parser/path/encoding tests | Broader failpoints are proposed in `fuzzing-seeds.md`. |

## Derived Requirements Matrix

| Derived ID | Parent | Perspective | Question | Expected safe behavior | TLA+ target | Test target |
| -- | -- | -- | -- | -- | -- | -- |
| U-001 | N-001/E-001 | invalid input | Invalid CLI, duplicate project args, or missing option value? | Stop before project load and output creation. | `CliTerminal`, `EarlyFailuresStopBeforeMetadata` | CLI integration tests |
| U-002 | N-002/E-002 | failure | Project file missing? | Stop before collection/metadata/output. | `LoadProject` | `MissingProjectExitsOne` |
| U-003 | N-003/E-003 | failure | Required input missing without `--allow-missing`? | No valid CHM is published. | `CollectFiles`, `NoSuccessfulChmOnError` | `MissingRequiredFileFailsByDefault` |
| U-004 | N-004 | external I/O failure | Optional link-scan read fails? | Compatibility behavior absorbs it as no outgoing links without warning. | `ScanLinks`, `LinkReadFailureIsAbsorbedWithoutWarning` | `LinkScannerReadFailureIsAbsorbed` |
| U-005 | N-006/E-006 | external I/O failure | Temp write, final move, or replace fails? | No partial final CHM; existing output preserved where present. | `WriteOutput`, `DesiredFailureAtomicity` | output failure tests |
| U-006 | N-003/B-002 | security/path traversal | Project path resolves outside HHP dir? | Archive namespace stays basename-only and never escapes. | `ArchiveNamespaceNeverEscapes`, `OutsideProjectPathUsesBasenameArchiveName` | path property/integration tests |
| U-007 | N-004/B-004 | invalid input | External, UNC, empty, or fragment-only link? | Do not collect it as a local file. | link use-case TLA | link cleanup tests |
| U-008 | B-005 | compatibility | Duplicate/blank/quoted HHP syntax? | Parser remains deterministic. | HHP use-case TLA | HHP parser tests |
| U-009 | B-006 | encoding boundary | Invalid UTF-8 with declared language? | Use declared fallback encoding; broaden with fuzzing. | encoding use-case TLA | encoding direct/integration tests |
| U-010 | N-007 | degradation | Unsupported HHW options present? | Emit explicit warnings and succeed only if otherwise compilable. | `UnsupportedFeaturesWarnButSucceed` | unsupported-feature integration test |
| U-011 | C-001 | duplicate execution | Two compilers write same final output? | Bounded tested claim: final CHM is structurally valid and has one winner. Product locking policy remains open. | abstracted from TLA | race/stress tests |
| U-012 | T-001 | cancel/timeout | External cancel or timeout requested? | No application-level transition exists; do not claim verified cancel/timeout behavior. | `ExternalAbortScenarios = {}` | product decision / future tests |
| U-013 | P-002 | crash restart | Process dies after temp creation? | Existing final output remains unchanged in controlled case; temp GC is not verified. | crash abstracted | process-death/stale-temp tests |

## Augmented 3.1-3.16 Applicability Ledger

| Instruction subsection | Applicability to `src/hhc` | Current evidence | Remaining boundary |
| -- | -- | -- | -- |
| 3.1 input/parameter | yes | `U-001`, `U-002`, CLI/HHP parser Gherkin, UC001..UC017, parser/encoding/path tests | Full CLI option compatibility with HTML Help Workshop remains differential-test work. |
| 3.2 state/lifecycle | yes | `ExistingCodeLoop.tla`, `StageDiscipline`, `TerminalTransitionCountMatchesPath`, UC lifecycle models | Infinite real-process hangs are outside finite TLC. |
| 3.3 ordering/timing | yes | Early-failure stop-point invariants, coverage audit, red/green tests for output publish ordering | Wall-clock timing and scheduler fairness beyond bounded tests are not verified. |
| 3.4 concurrency/race | partial | `ConcurrentWritersLeaveValidFinalOutput`, `CrossProcessSameOutputCompilesLeaveValidFinalOutput`, `U-011` | No explicit output lock exists; high-volume/platform race policy remains a product decision. |
| 3.5 external I/O/OS/network | yes for filesystem, no network | File read/write/move/replace failures, locked output, stale temp, process-death tests | Network shares, cross-volume moves, permissions matrix, and storage-controller faults are delegated. |
| 3.6 persistence/transaction/recovery | partial | Same-directory temp staging, preservation tests, `DesiredFailureAtomicity`, crash-after-temp test | Fsync, parent-directory durability, and automatic stale-temp garbage collection are not verified. |
| 3.7 security/verification/authorization | partial | `ArchiveNamespaceNeverEscapes`, basename-only outside project behavior, path fuzz seeds | Explicit outside source inclusion is accepted for trusted local HHP compatibility; no signature, auth, tenant, owner, or sandbox enforcement exists in this compiler. |
| 3.8 resource/performance/degradation | partial | Oversized PMGL/PMGI tests, UC062/UC063, unsupported-feature warning behavior | Soak/load/resource exhaustion and memory pressure are delegated to stress/fault tests. |
| 3.9 UI/user ops | not applicable | CLI-only target; help/version/argument-error short-circuit behavior covered | No GUI operation order to verify. |
| 3.10 version/compat/config | partial | Unsupported HHW options warn, HHP duplicate/quoted/truthy options, encoding fallback seeds | Complete HTML Help Workshop compatibility remains differential-test work. |
| 3.11 business/integrity | yes | CHM structural/payload invariants, archive namespace invariants, generated contents escaping | Independent CHM reader compatibility is still delegated. |
| 3.12 idempotency/retry/dup | partial | Duplicate file/collision tests, `NoRetryPolicy`, no retry protocol modeled | Retry and idempotency-table behavior do not exist; product review needed if required. |
| 3.13 observability/log/alert | partial | Exit codes, warnings, stderr/stdout behavior in specs/tests | Structured logs, metrics, and alerting are not implemented. |
| 3.14 distributed/external services | not applicable | No service/API dependency in inspected compiler | Network/API exception taxonomy is not part of this target. |
| 3.15 update/install/deployment | not applicable to compiler runtime | Build/test harness covered by `dotnet run`; no installer/updater code inspected | Packaging and installer behavior require a separate target scope. |
| 3.16 Windows/desktop app | partial | Windows path/lock/process-death/current ANSI fallback cases covered on this workspace | COM, registry, device names, ACL matrix, and long-path platform matrix are delegated. |

## Mutation Family Coverage

| Augmented mutant family | Executed mutant or disposition | Classification |
| -- | -- | -- |
| `=` / `#` swap | `M001`, `M002` | killed |
| `<` / `<=` swap | `M003` | killed |
| `>` / `>=` swap | `M004` | equivalent because `retryCount` is always `0` in reachable states |
| `/\` / `\/` swap | `M005` | killed |
| `+` / `-` swap | `M017` | killed |
| Guard deletion | `M006` | killed |
| Guard relaxation | `M010` | killed |
| Important state update deletion | `M007`, `M013`, `M019` | killed |
| Reset deletion | `M008` | killed |
| Error transition change | `M009`, `M014`, `M020` | killed |
| Cancel/timeout transition deletion/change | `M011`, `M018` | equivalent because the implementation has no cancel/timeout protocol |
| Verification/safety disabled | `M016`, `M021` | killed |
| Persistence update deletion/order bug | `M015`, `M019` | killed |
| Fairness/liveness weakening | `M022` | killed |
| Action deletion/dead path | `M023` | killed |
| Signature/auth/tenant/owner/lease/idempotency mutants | not applicable | No such implementation state exists; tracked as unverified/non-feature, not verified behavior. |
| Retry increment/dedup/compensation mutants | not applicable | No retry, dedup table, or compensation workflow exists; absence is explicit in requirements. |

Current oracle result: 23 mutants, 20 killed, 3 equivalent, 0 true survivor.

## Implementation Test Wiring

| Check | Result |
| -- | -- |
| Correct implementation | `dotnet run --project tests\hhc.IntegrationTests\hhc.IntegrationTests.csproj -p:UseAppHost=false` passed all 59 tests. |
| Intentional break | Temporarily changed `ProjectCompiler.MakeArchiveRelative` to use `originalPath` for outside project files instead of `Path.GetFileName(sourcePath)`. |
| Expected red | `outside project paths stay inside archive namespace` failed because a sibling temp directory name appeared in CHM bytes; `outside project basename collisions warn and keep first` failed because collision detection no longer observed basename convergence. |
| Restore | Restored the basename-only implementation and reran the same command; all 59 tests passed again. |
| Caveat | MSBuild emitted stale apphost/cache delete warnings on this Windows workspace. The test harness passes `-p:UseAppHost=false` for nested target CLI runs so the assertions exercise the rebuilt DLL path. |

## Independent Exit Checks

| TLA+ abstraction | Independent check |
| -- | -- |
| Byte-level CHM format | Existing structural and payload tests; add differential validation with independent CHM readers. |
| Path/Unicode grammar | Existing bounded generated seeds; add platform-matrix fuzzing for long paths, UNC, device names, and normalization. |
| HTML/CSS parsing grammar | Existing syntax seeds; add coverage-guided or grammar fuzzing. |
| Encoding/codepages | Existing CP932/BOM/invalid UTF-8 seeds; add broader codepage corpus and invalid-byte fuzzing. |
| OS atomicity and crash | Existing temp-write/publish/process-death tests; add fsync/power-loss and cross-volume/network-share tests. |
| Same-output concurrency | Existing bounded races; add high-volume/platform stress or explicit product locking. |
| Unsupported HHW compatibility | Existing warnings; add compatibility/differential tests against HTML Help Workshop expectations. |

## Verification Claim Boundary

Verified only within the finite model and executable tests: pipeline ordering,
early failures, process-level output atomicity for injected failures, archive
namespace safety for selected/bounded path seeds, selected parser/link/encoding
boundaries, CHM structural/payload invariants, stale-temp non-interference, and
bounded same-output race final validity.

Not verified: power-loss durability, stale-temp garbage collection, exhaustive
path/link/encoding grammars, independent CHM reader compatibility, sandboxing of
untrusted HHP manifests, and retry/cancel/timeout semantics.
