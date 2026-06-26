# Completion Audit Against The Formal-Methods Loop Request

This audit maps the user's requested verification loop to current repository
evidence. It does not claim whole-product correctness. It states what is
proved by the checked artifacts and what remains outside the verified scope.

| Request item | Current evidence | Status |
| -- | -- | -- |
| 1. Extract state, events, external I/O, abnormal paths, boundaries, concurrency, persistence, and security conditions with IDs. | `extraction-ledger.md` records `N-*`, `E-*`, `S-*`, `B-*`, `IO-*`, `C-*`, `P-*`, `SEC-*`, `T-*`, and `U-*` IDs. | Satisfied within inspected `src/hhc` scope. |
| 2. Separate current behavior from human-review specification decisions. | `extraction-ledger.md`, `ears-requirements.md`, and `inspection-report.md` list human-review items such as warning-only unsupported features, retry/cancel/timeout absence, locking policy, crash durability, and stale-temp cleanup. Silent link read absorption and explicit outside source inclusion are now accepted compatibility decisions. | Satisfied. |
| 3. Generate unwanted requirements for invalid input, failure, cancel, timeout, duplicate execution, external I/O failure, and crash restart. | `ears-requirements.md` includes `U-001` through `U-013`; `current-spec.feature` contains corresponding scenarios, including no retry/cancel/timeout and process-death/stale-temp cases. | Satisfied; unsupported retry/cancel/timeout are documented as absence/product decisions. |
| 4. Drop requirements into EARS or Gherkin. | `ears-requirements.md` and `current-spec.feature`. | Satisfied. |
| 5. Build state/domain model and state TLA+ modeling scope plus abstractions. | `domain-model.md` and `docs/tla/inspection/ExistingCodeLoop.tla`. | Satisfied. |
| 6. Provide traceability from spec ID to EARS/Gherkin, TLA Action/Inv/Temporal, Lean/Dafny target, and implementation tests. | `traceability.md`. | Satisfied; Lean/Dafny targets are marked as future candidates where applicable, not implemented. |
| 7. Write TLA+ model/cfg and run normal TLC. | `docs/tla/inspection/ExistingCodeLoop.tla`, `ExistingCodeLoop.cfg`, `ExistingCodeLoop.AtomicityCandidate.cfg`; `python tools/run_tla_inspection_model.py` passes. Broader `python tools/run_tla_models.py` passes 106 models. `docs/tla/coverage-audit.md` confirms coverage is present for all 107 referenced logs and has 0 unexpected zero-hit or missing-coverage issues. | Satisfied. |
| 8. Run mutation oracle for comparison/logical/guard/update/reset/error/cancel/timeout/verification mutations. | `tools/run_tla_mutation_oracle.py`; `mutation-oracle-summary.md` records 23 mutants. `augmented-loop-audit.md` maps required mutant families to executed mutants or non-applicable implementation features. | Satisfied for the inspection TLA model. |
| 9. Classify survivors and strengthen invariants/modeling for true survivors. | `mutation-oracle-summary.md` and `inspection-report.md`: 20 killed, 3 equivalent, 0 true survivor. Strengthened invariants include `DesiredFailureAtomicity`, `OutsideProjectPathUsesBasenameArchiveName`, `ArchiveNamespaceNeverEscapes`, `TerminalTransitionCountMatchesPath`, `NoPendingLinksAtTerminalState`, `LinkReadFailureIsAbsorbedWithoutWarning`, and liveness/fairness coverage. | Satisfied; no true survivor remains in the inspection model. |
| 10. Do not call true survivors verified. | `inspection-report.md` reports 0 true survivors and separates verified vs not-verified scope. | Satisfied. |
| 11. Drop TLC counterexamples or true survivors to Gherkin/unit/property/fuzz seeds. | No true survivor remains. Discovered holes/bugs are represented in `current-spec.feature`, implementation tests, and `fuzzing-seeds.md`. | Satisfied. |
| 12. Verify correct implementation green and intentionally broken implementation red. | `inspection-report.md` records source mutation red/green checks for outside-project archive naming, aggregate PMGI size guard, output staging path, invalid UTF-8 fallback, and CHM content-offset advancement. Current `dotnet run --project tests\hhc.IntegrationTests\hhc.IntegrationTests.csproj -p:UseAppHost=false` passes 61 tests; a temporary outside-path mutation failed the two expected archive-namespace tests and passed again after restore. | Satisfied. |
| 13. Propose complements for TLA abstractions using fuzzing, property tests, differential tests, Lean/Dafny, or unit tests. | `domain-model.md`, `traceability.md`, `recommended-tests.md`, and `fuzzing-seeds.md`. | Satisfied. |

## Current Verification Results

| Gate | Command | Result |
| -- | -- | -- |
| Implementation/direct tests | `dotnet run --project tests\hhc.IntegrationTests\hhc.IntegrationTests.csproj -p:UseAppHost=false` | 61 passed |
| Inspection TLA model | `python tools\run_tla_inspection_model.py` | pass |
| Broader TLA suite | `python tools\run_tla_models.py` | 106 passed |
| TLA coverage audit | `python tools\audit_tla_coverage.py` | 107 logs checked, 0 unexpected issues |
| TLA mutation oracle | `python tools\run_tla_mutation_oracle.py` | 20 killed, 3 equivalent, 0 true survivor |
| Whitespace check | `git diff --check` | no whitespace errors; CRLF warnings only |

## Verified Scope

Verified within the finite TLA abstraction and executable tests:

- CLI/project/file/output stage ordering and early failure boundaries.
- Process-level final output atomicity for temp-write, publish exceptions, locked output, and controlled process death after temp creation.
- CHM ITSF/section0/ITSP/PMGL/PMGI structural invariants and PMGL directory-entry-to-payload resolution for generated small and indexed outputs.
- Archive namespace safety for selected and bounded generated path seeds, including outside-project basename-only storage and basename collision behavior.
- Link cleanup, extraction, and flat rewrite behavior for selected and bounded generated seeds.
- HHP parser boundary behavior for duplicate, blank, quoted, unbalanced quoted, CR-only line ending, unknown section, and truthy option seeds.
- Encoding behavior for declared CP932, invalid UTF-8 fallback, ANSI fallback selection, UTF-8 BOM, UTF-16 BOM, and bounded generated invalid UTF-8 seeds.
- Stale temp non-interference and bounded same-output writer/process race final-output validity on the tested filesystem.
- TLA mutation oracle has no true survivor in the inspection model after the augmented 23-mutant run.

## Not Verified

Still outside the verified claim:

- Power-loss, storage-controller failure, and fsync/parent-directory durability.
- Automatic stale-temp garbage collection after process death.
- Platform/high-volume same-output race behavior beyond the bounded local-filesystem tests.
- Full Windows/UNC/path grammar beyond selected and bounded generated seeds.
- Full HTML/CSS parsing compatibility beyond selected and bounded generated extraction/cleanup/rewrite seeds.
- Exhaustive encoding behavior across all invalid byte/codepage combinations beyond bounded generated seeds.
- Independent CHM-reader compatibility beyond structural header/chunk and PMGL exact-payload invariants.
- Sandboxing or blocking behavior for untrusted HHP projects; compatibility mode treats HHP files as trusted local project manifests and allows explicit outside source inclusion.
- Cancellation, timeout, and retry semantics; no such protocol exists.
