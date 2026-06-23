# EARS Requirements From Existing Code

Each statement is derived from the current implementation, then tagged as either accepted current behavior or human-review-needed behavior.

## Normal And Event Requirements

| ID | EARS requirement | Status |
| -- | -- | -- |
| N-001 | The system SHALL return exit code 0 and avoid project loading when invoked in help or version mode. | accepted current behavior |
| N-002 | WHEN a valid HHP project is compiled, the system SHALL load the project, collect files, build metadata, build a CHM package, publish output, and print a success summary. | accepted current behavior |
| N-003 | WHEN required project-declared files are present, the system SHALL include them in the CHM under normalized archive paths. | accepted current behavior |
| N-004 | WHERE link scanning is enabled, the system SHALL collect local links from HTML, CSS, HHC, and HHK files as optional inputs. | human review: silent read failures |
| N-005 | The system SHALL emit required internal CHM streams for uncompressed CHM output. | accepted current behavior |
| N-006 | WHEN CHM bytes are written, the system SHALL stage bytes in a same-directory temporary file before publishing the final output path. | accepted current behavior after remediation |
| N-007 | WHEN unsupported HHW-compatible features are present, the system SHALL warn and continue if the project is otherwise compilable. | human review: warning-only policy |
| N-008 | WHERE `--no-link-scan` is set, the system SHALL skip optional link discovery and compile only explicitly collected files. | accepted current behavior |
| E-001 | IF CLI arguments are invalid, THEN the system SHALL exit with code 2 before project loading and output writing. | accepted current behavior |
| E-002 | IF the HHP project file is missing, THEN the system SHALL exit with code 1 before file collection. | accepted current behavior |
| E-003 | IF a required file is missing and `--allow-missing` is not set, THEN the system SHALL exit with code 1 before metadata construction and output writing. | accepted current behavior |
| E-004 | IF a required file is missing and `--allow-missing` is set, THEN the system SHALL warn, omit that file, and continue compilation. | human review: metadata may still reference omitted files |
| E-005 | IF two source files map to the same archive path, THEN the system SHALL keep the first and warn when the sources differ. | human review: first-wins policy |
| E-006 | IF output publication fails, THEN the system SHALL exit with code 1 and preserve any existing final output. | accepted current behavior after remediation |
| E-007 | IF a required input file cannot be read outside link scanning, THEN the system SHALL fail compilation before final output publication. | accepted current behavior |
| E-008 | IF CHM metadata, a single directory entry, or the aggregate directory index exceeds supported limits, THEN the system SHALL fail with a compilation error before final output publication. | accepted current behavior |

## Systematically Generated Unwanted Requirements

| ID | Parent | EARS requirement | Status |
| -- | -- | -- | -- |
| U-001 | N-001/E-001 | IF CLI input is empty, malformed, duplicated, or has a missing option value, THEN the system SHALL not load a project or create a CHM. | tested |
| U-002 | N-002/E-002 | IF project loading fails, THEN the system SHALL not collect files, build metadata, or publish output. | TLA + tested |
| U-003 | N-003/E-003 | IF required file collection fails without `--allow-missing`, THEN the system SHALL not publish a CHM. | TLA + tested |
| U-004 | N-004 | IF link scanning cannot read a scannable optional file, THEN the system SHALL treat it as having no outgoing links and continue. | TLA; human review for warning |
| U-005 | N-006/E-006 | IF temp-file writing, final move, or final replace fails, THEN the system SHALL not leave a partial final CHM. | TLA + tested |
| U-006 | N-003/B-002 | IF a project-declared source path resolves outside the HHP directory, THEN the system SHALL use only the source basename as the CHM archive path. | TLA + tested after remediation |
| U-007 | N-004/B-004 | IF a discovered link is external, UNC, fragment-only, or empty, THEN the system SHALL not treat it as a local input file. | tested |
| U-008 | B-005 | IF HHP options are duplicated, blank, balanced-quoted, or unbalanced-quoted, THEN the system SHALL parse them deterministically. | tested |
| U-009 | B-006 | IF text bytes are invalid UTF-8 but a language encoding is declared, THEN the system SHALL decode with that declared encoding. | tested for CP932 seed; broader fuzz/property recommended |
| U-010 | N-007 | IF unsupported HHW feature declarations are present, THEN the system SHALL emit explicit warnings before succeeding. | tested |
| U-011 | C-001 | IF two compiler processes publish to the same final output concurrently, THEN the final output SHALL be a structurally valid CHM from one winning writer in the bounded local-filesystem stress case. | bounded stress tested; locking policy still human review |
| U-012 | T-001 | IF a compile is externally cancelled or times out, THEN the system has no application-level cancellation/timeout transition. | not verified; human review |
| U-013 | P-002 | IF the process dies after temp-file creation but before final publication, THEN any existing final output SHALL remain unchanged in the controlled process-death case; stale temp garbage collection is not implemented. | fault-injection tested for final-output preservation; cleanup policy human review |

## Gherkin Scenarios Added Or Backed By Tests

```gherkin
Feature: Existing-code verification scenarios

  Scenario: Temp-file write failure preserves the existing output
    Given an existing final CHM output
    And CHM bytes are being staged in a temporary file
    When temporary-file writing fails
    Then the compiler exits with an error
    And the existing final CHM output remains byte-for-byte unchanged
    And no partial final CHM is published

  Scenario: Outside project paths do not escape the archive namespace
    Given an HHP project lists a parent-relative file outside the project directory
    And the same project lists an absolute file outside the project directory
    When the project is compiled
    Then both files are included by basename
    And the sibling or parent directory name does not appear in the CHM archive path

  Scenario: Outside project basename collisions warn and keep first
    Given two outside project files have the same basename
    When the project is compiled
    Then a duplicate archive path warning is printed
    And only the first file's bytes are included under that basename

  Scenario: Flat link rewriting is idempotent
    Given local HTML, HHC, CSS import, and CSS url targets include nested directories
    When flat-mode link rewriting runs
    Then local targets become basenames and retain suffixes
    And external targets are unchanged
    And a second rewrite leaves the text unchanged

  Scenario: Invalid UTF-8 uses declared CP932 fallback
    Given project bytes are invalid UTF-8
    And the project declares Language=0411 Japanese
    When the project is loaded
    Then CP932 is selected
    And Japanese metadata text is preserved

  Scenario: Unsupported HHW features are warning-only
    Given a project declares Full-text search, Binary TOC, Binary Index, MERGE FILES, and WINDOWS settings
    When the project is otherwise compilable
    Then compilation succeeds
    And one warning is printed for each unsupported feature family

  Scenario: Oversized directory entries fail before final publication
    Given a CHM archive path is too large for one PMGL directory block
    When CHM directory construction runs
    Then compilation fails with a compilation error
    And any existing final CHM remains unchanged

  Scenario: Aggregate directory index overflow fails before final publication
    Given multiple CHM archive paths individually fit in PMGL blocks
    But their generated PMGI index no longer fits in one supported directory block
    When CHM directory construction runs
    Then compilation fails with a compilation error
    And any existing final CHM remains unchanged

  Scenario: Same-output concurrent publication leaves a valid winner
    Given two compiler instances publish different CHM contents to the same final output
    When both instances run concurrently on the tested local filesystem
    Then at least one compile succeeds
    And the final output is a structurally valid CHM
    And the final output contains exactly one writer's payload

  Scenario: Stale temporary output does not block a later compile
    Given a same-directory temporary output file from an earlier run is still present
    When the project is compiled again
    Then the final CHM output is published successfully
    And the stale temporary file is left untouched

  Scenario: Process death after temp creation preserves existing final output
    Given an existing final CHM output
    And a compiler process creates a staged temporary output
    When the compiler process dies before final publication and cleanup
    Then the existing final output remains byte-for-byte unchanged
    And a stale temporary file remains for later policy handling
```
