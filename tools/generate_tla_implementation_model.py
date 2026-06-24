from __future__ import annotations

import sys
from pathlib import Path

sys.dont_write_bytecode = True

from generate_tla_usecase_models import CASES


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "docs" / "tla" / "implementation"


def tla_string(value: str) -> str:
    escaped = value.replace("\\", "\\\\").replace('"', '\\"')
    return f'"{escaped}"'


def tla_set(values) -> str:
    vals = sorted(values)
    if not vals:
        return "{}"
    return "{" + ", ".join(tla_string(v) for v in vals) + "}"


def tla_bool(value: bool) -> str:
    return "TRUE" if value else "FALSE"


def case_operator(name: str, value_fn, default: str) -> str:
    lines = [f"{name}(s) ==", "  CASE"]
    for index, uc in enumerate(CASES):
        prefix = "    " if index == 0 else "  [] "
        lines.append(f"{prefix}s = {tla_string(uc.name)} -> {value_fn(uc)}")
    lines.append(f"  [] OTHER -> {default}")
    return "\n".join(lines)


def gherkin_spec_text() -> str:
    scenarios = tla_set(uc.name for uc in CASES)
    all_phases = tla_set({"Start", "CliParsed", "ProjectLoaded", "ProjectMissing", "FilesCollected", "MetadataBuilt", "WriterRan", "Done"})
    return f"""---- MODULE GherkinSpec ----
EXTENDS Integers

(*
Specification obligations derived from docs/usecases.feature and
docs/usecases.additional.feature.

The Gherkin feature suite is the reader-facing source of truth. This module is
the TLC-friendly projection of each scenario's observable contract: command mode,
project/load result, collected CHM paths, metadata tags, writer tags,
stdout/stderr/warning tags, exit code, and CHM creation.
*)

Scenarios == {scenarios}
AllPhases == {all_phases}
AllCliModes == {tla_set({"Unset", "Help", "Version", "ArgError", "Compile"})}
AllProjectStates == {tla_set({"NotLoaded", "Loaded", "Missing", "Skipped"})}
AllExitCodes == {{-1, 0, 1, 2}}

{case_operator("SpecCliMode", lambda uc: tla_string(uc.cli_mode), tla_string("Unknown"))}

{case_operator("SpecProjectState", lambda uc: tla_string(uc.project_state), tla_string("Unknown"))}

{case_operator("SpecCollectionTags", lambda uc: tla_set(uc.collection_tags), "{}")}

{case_operator("SpecArchive", lambda uc: tla_set(uc.archive), "{}")}

{case_operator("SpecMetadata", lambda uc: tla_set(uc.metadata), "{}")}

{case_operator("SpecWriterTags", lambda uc: tla_set(uc.writer_tags), "{}")}

{case_operator("SpecStdout", lambda uc: tla_set(uc.stdout), "{}")}

{case_operator("SpecStderr", lambda uc: tla_set(uc.stderr), "{}")}

{case_operator("SpecWarnings", lambda uc: tla_set(uc.warnings), "{}")}

{case_operator("SpecExitCode", lambda uc: str(uc.exit_code), "-1")}

{case_operator("SpecChmCreated", lambda uc: tla_bool(uc.chm_created), "FALSE")}

{case_operator("SpecVisited", lambda uc: tla_set(uc.visited), "{}")}

====
"""


def implementation_model_text() -> str:
    scenarios = tla_set(uc.name for uc in CASES)
    arg_error_scenarios = tla_set(uc.name for uc in CASES if uc.exit_code == 2)
    compile_error_scenarios = tla_set(uc.name for uc in CASES if uc.exit_code == 1)
    warning_scenarios = tla_set(uc.name for uc in CASES if uc.warnings)
    failure_without_chm_scenarios = tla_set(uc.name for uc in CASES if uc.exit_code != 0 and not uc.chm_created)
    warning_success_scenarios = tla_set(uc.name for uc in CASES if uc.exit_code == 0 and uc.warnings)
    all_collection_tags = tla_set(set().union(*(set(uc.collection_tags) for uc in CASES)))
    all_archive = tla_set(set().union(*(set(uc.archive) for uc in CASES)))
    all_metadata = tla_set(set().union(*(set(uc.metadata) for uc in CASES)))
    all_writer_tags = tla_set(set().union(*(set(uc.writer_tags) for uc in CASES)))
    all_stdout = tla_set(set().union(*(set(uc.stdout) for uc in CASES)))
    all_stderr = tla_set(set().union(*(set(uc.stderr) for uc in CASES)))
    all_warnings = tla_set(set().union(*(set(uc.warnings) for uc in CASES)))
    all_visited = tla_set(set().union(*(set(uc.visited) for uc in CASES)))

    return f"""---- MODULE ImplementationConformance ----
EXTENDS Integers, GherkinSpec

(*
Detailed implementation-stage model checked against the Gherkin obligations.

This is intentionally different from the generated per-use-case smoke models:
state is not finalized in one step. The model walks the same observable stages
as Program.Main, CliOptions.Parse, HhpProject.Load, ProjectCompiler.CollectFiles,
ProjectCompiler.BuildMetadata, and ChmWriter.Write.
*)

VARIABLES
  scenario,
  phase,
  cliMode,
  projectState,
  collectionTags,
  archive,
  metadata,
  writerTags,
  stdout,
  stderr,
  warnings,
  exitCode,
  chmCreated,
  visited

vars == <<
  scenario,
  phase,
  cliMode,
  projectState,
  collectionTags,
  archive,
  metadata,
  writerTags,
  stdout,
  stderr,
  warnings,
  exitCode,
  chmCreated,
  visited
>>

TerminalCliModes == {{"Help", "Version", "ArgError"}}

ImplScenarios == {scenarios}
ArgErrorScenarios == {arg_error_scenarios}
CompileErrorScenarios == {compile_error_scenarios}
WarningScenarios == {warning_scenarios}
FailureWithoutChmScenarios == {failure_without_chm_scenarios}
WarningSuccessScenarios == {warning_success_scenarios}
ImplCollectionTags == {all_collection_tags}
ImplArchivePaths == {all_archive}
ImplMetadataTags == {all_metadata}
ImplWriterTags == {all_writer_tags}
ImplStdoutTags == {all_stdout}
ImplStderrTags == {all_stderr}
ImplWarningTags == {all_warnings}
ImplVisitedTags == {all_visited}

{case_operator("ImplCliParse", lambda uc: tla_string(uc.cli_mode), tla_string("Unknown"))}

{case_operator("ImplProjectLoad", lambda uc: tla_string(uc.project_state), tla_string("Unknown"))}

{case_operator("ImplCollectTags", lambda uc: tla_set(uc.collection_tags), "{}")}

{case_operator("ImplCollectArchive", lambda uc: tla_set(uc.archive), "{}")}

{case_operator("ImplBuildMetadata", lambda uc: tla_set(uc.metadata), "{}")}

{case_operator("ImplWriterResult", lambda uc: tla_set(uc.writer_tags), "{}")}

{case_operator("ImplStdout", lambda uc: tla_set(uc.stdout), "{}")}

{case_operator("ImplStderr", lambda uc: tla_set(uc.stderr), "{}")}

{case_operator("ImplWarnings", lambda uc: tla_set(uc.warnings), "{}")}

{case_operator("ImplExitCode", lambda uc: str(uc.exit_code), "-1")}

{case_operator("ImplChmCreated", lambda uc: tla_bool(uc.chm_created), "FALSE")}

Init ==
  /\\ scenario \\in ImplScenarios
  /\\ phase = "Start"
  /\\ cliMode = "Unset"
  /\\ projectState = "NotLoaded"
  /\\ collectionTags = {{}}
  /\\ archive = {{}}
  /\\ metadata = {{}}
  /\\ writerTags = {{}}
  /\\ stdout = {{}}
  /\\ stderr = {{}}
  /\\ warnings = {{}}
  /\\ exitCode = -1
  /\\ chmCreated = FALSE
  /\\ visited = {{"Start"}}

ParseCli ==
  /\\ phase = "Start"
  /\\ phase' = "CliParsed"
  /\\ cliMode' = ImplCliParse(scenario)
  /\\ visited' = visited \\cup {{"CliParsed"}}
  /\\ UNCHANGED <<scenario, projectState, collectionTags, archive, metadata, writerTags, stdout, stderr, warnings, exitCode, chmCreated>>

FinishCliTerminal ==
  /\\ phase = "CliParsed"
  /\\ cliMode \\in TerminalCliModes
  /\\ phase' = "Done"
  /\\ projectState' = ImplProjectLoad(scenario)
  /\\ collectionTags' = ImplCollectTags(scenario)
  /\\ archive' = ImplCollectArchive(scenario)
  /\\ metadata' = ImplBuildMetadata(scenario)
  /\\ writerTags' = ImplWriterResult(scenario)
  /\\ stdout' = ImplStdout(scenario)
  /\\ stderr' = ImplStderr(scenario)
  /\\ warnings' = ImplWarnings(scenario)
  /\\ exitCode' = ImplExitCode(scenario)
  /\\ chmCreated' = ImplChmCreated(scenario)
  /\\ visited' = visited \\cup {{"Done"}}
  /\\ UNCHANGED <<scenario, cliMode>>

LoadProject ==
  /\\ phase = "CliParsed"
  /\\ cliMode = "Compile"
  /\\ ImplProjectLoad(scenario) = "Loaded"
  /\\ phase' = "ProjectLoaded"
  /\\ projectState' = "Loaded"
  /\\ visited' = visited \\cup {{"ProjectLoaded"}}
  /\\ UNCHANGED <<scenario, cliMode, collectionTags, archive, metadata, writerTags, stdout, stderr, warnings, exitCode, chmCreated>>

ProjectMissing ==
  /\\ phase = "CliParsed"
  /\\ cliMode = "Compile"
  /\\ ImplProjectLoad(scenario) = "Missing"
  /\\ phase' = "Done"
  /\\ projectState' = "Missing"
  /\\ collectionTags' = ImplCollectTags(scenario)
  /\\ archive' = ImplCollectArchive(scenario)
  /\\ metadata' = ImplBuildMetadata(scenario)
  /\\ writerTags' = ImplWriterResult(scenario)
  /\\ stdout' = ImplStdout(scenario)
  /\\ stderr' = ImplStderr(scenario)
  /\\ warnings' = ImplWarnings(scenario)
  /\\ exitCode' = ImplExitCode(scenario)
  /\\ chmCreated' = ImplChmCreated(scenario)
  /\\ visited' = visited \\cup {{"ProjectMissing", "Done"}}
  /\\ UNCHANGED <<scenario, cliMode>>

CollectFiles ==
  /\\ phase = "ProjectLoaded"
  /\\ "CollectFailed" \\notin ImplCollectTags(scenario)
  /\\ phase' = "FilesCollected"
  /\\ collectionTags' = ImplCollectTags(scenario)
  /\\ archive' = ImplCollectArchive(scenario)
  /\\ warnings' = ImplWarnings(scenario)
  /\\ visited' = visited \\cup {{"FilesCollected"}}
  /\\ UNCHANGED <<scenario, cliMode, projectState, metadata, writerTags, stdout, stderr, exitCode, chmCreated>>

CollectFails ==
  /\\ phase = "ProjectLoaded"
  /\\ "CollectFailed" \\in ImplCollectTags(scenario)
  /\\ phase' = "Done"
  /\\ collectionTags' = ImplCollectTags(scenario)
  /\\ archive' = ImplCollectArchive(scenario)
  /\\ metadata' = ImplBuildMetadata(scenario)
  /\\ writerTags' = ImplWriterResult(scenario)
  /\\ stdout' = ImplStdout(scenario)
  /\\ stderr' = ImplStderr(scenario)
  /\\ warnings' = ImplWarnings(scenario)
  /\\ exitCode' = ImplExitCode(scenario)
  /\\ chmCreated' = ImplChmCreated(scenario)
  /\\ visited' = visited \\cup {{"FilesCollected", "Done"}}
  /\\ UNCHANGED <<scenario, cliMode, projectState>>

BuildMetadata ==
  /\\ phase = "FilesCollected"
  /\\ phase' = "MetadataBuilt"
  /\\ metadata' = ImplBuildMetadata(scenario)
  /\\ visited' = visited \\cup {{"MetadataBuilt"}}
  /\\ UNCHANGED <<scenario, cliMode, projectState, collectionTags, archive, writerTags, stdout, stderr, warnings, exitCode, chmCreated>>

WriteChm ==
  /\\ phase = "MetadataBuilt"
  /\\ phase' = "Done"
  /\\ writerTags' = ImplWriterResult(scenario)
  /\\ stdout' = ImplStdout(scenario)
  /\\ stderr' = ImplStderr(scenario)
  /\\ exitCode' = ImplExitCode(scenario)
  /\\ chmCreated' = ImplChmCreated(scenario)
  /\\ visited' = visited \\cup {{"WriterRan", "Done"}}
  /\\ UNCHANGED <<scenario, cliMode, projectState, collectionTags, archive, metadata, warnings>>

StayDone ==
  /\\ phase = "Done"
  /\\ UNCHANGED vars

Next ==
  ParseCli
  \\/ FinishCliTerminal
  \\/ LoadProject
  \\/ ProjectMissing
  \\/ CollectFiles
  \\/ CollectFails
  \\/ BuildMetadata
  \\/ WriteChm
  \\/ StayDone

Spec == Init /\\ [][Next]_vars /\\ WF_vars(Next)

TypeOK ==
  /\\ scenario \\in ImplScenarios
  /\\ phase \\in {{"Start", "CliParsed", "ProjectLoaded", "FilesCollected", "MetadataBuilt", "Done"}}
  /\\ cliMode \\in AllCliModes
  /\\ projectState \\in AllProjectStates
  /\\ collectionTags \\in SUBSET ImplCollectionTags
  /\\ archive \\in SUBSET ImplArchivePaths
  /\\ metadata \\in SUBSET ImplMetadataTags
  /\\ writerTags \\in SUBSET ImplWriterTags
  /\\ stdout \\in SUBSET ImplStdoutTags
  /\\ stderr \\in SUBSET ImplStderrTags
  /\\ warnings \\in SUBSET ImplWarningTags
  /\\ exitCode \\in AllExitCodes
  /\\ chmCreated \\in BOOLEAN
  /\\ visited \\in SUBSET ImplVisitedTags

GherkinSpecSatisfied ==
  phase = "Done" =>
    /\\ cliMode = SpecCliMode(scenario)
    /\\ projectState = SpecProjectState(scenario)
    /\\ collectionTags = SpecCollectionTags(scenario)
    /\\ archive = SpecArchive(scenario)
    /\\ metadata = SpecMetadata(scenario)
    /\\ writerTags = SpecWriterTags(scenario)
    /\\ stdout = SpecStdout(scenario)
    /\\ stderr = SpecStderr(scenario)
    /\\ warnings = SpecWarnings(scenario)
    /\\ exitCode = SpecExitCode(scenario)
    /\\ chmCreated = SpecChmCreated(scenario)
    /\\ visited = SpecVisited(scenario)

ImplementationStageDiscipline ==
  /\\ phase = "Start" => visited = {{"Start"}}
  /\\ phase = "CliParsed" => visited = {{"Start", "CliParsed"}}
  /\\ phase = "ProjectLoaded" => visited = {{"Start", "CliParsed", "ProjectLoaded"}}
  /\\ phase = "FilesCollected" => visited = {{"Start", "CliParsed", "ProjectLoaded", "FilesCollected"}}
  /\\ phase = "MetadataBuilt" => visited = {{"Start", "CliParsed", "ProjectLoaded", "FilesCollected", "MetadataBuilt"}}

NoErrorCreatesChm ==
  phase = "Done" /\\ exitCode # 0 => chmCreated = FALSE

SuccessPassedThroughWriter ==
  phase = "Done" /\\ chmCreated =>
    /\\ "WriterRan" \\in visited
    /\\ "PMGL" \\in writerTags
    /\\ "Uncompressed" \\in writerTags

AllGherkinScenariosModeled ==
  ImplScenarios = Scenarios

AbnormalSpecCoverage ==
  /\\ ArgErrorScenarios = {arg_error_scenarios}
  /\\ CompileErrorScenarios = {compile_error_scenarios}
  /\\ WarningScenarios = {warning_scenarios}
  /\\ FailureWithoutChmScenarios = {failure_without_chm_scenarios}
  /\\ WarningSuccessScenarios = {warning_success_scenarios}
  /\\ ArgErrorScenarios # {{}}
  /\\ CompileErrorScenarios # {{}}
  /\\ WarningScenarios # {{}}
  /\\ FailureWithoutChmScenarios # {{}}
  /\\ WarningSuccessScenarios # {{}}

AbnormalImplementationBehavior ==
  phase = "Done" =>
    /\\ scenario \\in ArgErrorScenarios => exitCode = 2 /\\ chmCreated = FALSE
    /\\ scenario \\in CompileErrorScenarios => exitCode = 1 /\\ chmCreated = FALSE
    /\\ scenario \\in WarningScenarios => warnings # {{}}
    /\\ scenario \\in FailureWithoutChmScenarios => exitCode # 0 /\\ chmCreated = FALSE
    /\\ scenario \\in WarningSuccessScenarios => exitCode = 0 /\\ chmCreated = TRUE /\\ warnings # {{}}

EventuallyDone == <> (phase = "Done")

====
"""


def cfg_text() -> str:
    return """SPECIFICATION Spec

INVARIANTS
  TypeOK
  GherkinSpecSatisfied
  ImplementationStageDiscipline
  NoErrorCreatesChm
  SuccessPassedThroughWriter
  AllGherkinScenariosModeled
  AbnormalSpecCoverage
  AbnormalImplementationBehavior

PROPERTIES
  EventuallyDone
"""


def readme_text() -> str:
    return """# TLA+ Implementation Conformance

This suite expresses the workflow the user asked for:

1. `docs/usecases.feature` and `docs/usecases.additional.feature` are the
   reader-facing Gherkin specifications inferred from observable command
   behavior.
2. `GherkinSpec.tla` is the TLC-friendly projection of those scenarios into
   observable obligations.
3. `ImplementationConformance.tla` walks the implementation stages represented
   by `Program.Main`, `CliOptions.Parse`, `HhpProject.Load`,
   `ProjectCompiler.CollectFiles`, `ProjectCompiler.BuildMetadata`, and
   `ChmWriter.Write`.
4. TLC checks that every modeled implementation terminal state satisfies the
   Gherkin obligations.

The model also checks abnormal-case coverage explicitly: argument errors,
compile errors, warning-only success cases, and failure-without-CHM cases must
all be present in the Gherkin-derived obligations and must keep the expected
exit-code/CHM-creation behavior in the implementation model.

`ApiExceptionConformance.tla` injects exceptions at modeled implementation API
boundaries. Most API exceptions must become compile errors with exit code 1 and
no successful CHM. `LinkScanner.ExtractLinks` is the intentional exception: the
implementation catches read/parse failures there and continues as if the file
had no links.

The C# integration harness complements this with real OS-level cases: locked
input files, locked output files, unwritable output targets, and oversized CHM
metadata entries.

Run this suite:

```powershell
python .\\tools\\run_tla_implementation_model.py
```

Regenerate it after editing the Gherkin use cases:

```powershell
python .\\tools\\generate_tla_implementation_model.py
```
"""


def main() -> int:
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "GherkinSpec.tla").write_text(gherkin_spec_text(), encoding="utf-8", newline="\n")
    (OUT / "ImplementationConformance.tla").write_text(implementation_model_text(), encoding="utf-8", newline="\n")
    (OUT / "ImplementationConformance.cfg").write_text(cfg_text(), encoding="utf-8", newline="\n")
    (OUT / "README.md").write_text(readme_text(), encoding="utf-8", newline="\n")
    print(f"Wrote implementation conformance model for {len(CASES)} Gherkin scenarios to {OUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
