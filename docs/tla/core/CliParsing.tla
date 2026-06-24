---- MODULE CliParsing ----
EXTENDS Integers

(*
Abstract model of CliOptions.Parse.

It checks parser precedence and option state that the broader compiler models
only observe after parsing has finished.
*)

VARIABLES
  scenario,
  phase,
  mode,
  projectSelected,
  outputPath,
  flags,
  error,
  exitCode,
  compileStarts

vars == <<
  scenario,
  phase,
  mode,
  projectSelected,
  outputPath,
  flags,
  error,
  exitCode,
  compileStarts
>>

Scenarios == {
  "NoArgs",
  "HelpFirst",
  "HelpAfterProject",
  "SlashQuestion",
  "VersionFirst",
  "VersionBeforeMissingProject",
  "UnknownDashOption",
  "OutMissingValue",
  "MissingProject",
  "MultipleProjects",
  "OutBeforeProject",
  "RepeatedOutLastWins",
  "FlagsAnyOrder"
}

Modes == {"Unset", "Help", "Version", "ArgError", "Compile"}
OutputPaths == {"None", "dist/output.chm", "first.chm", "final.chm"}
FlagTags == {"AllowMissing", "NoLinkScan", "Verbose"}
Errors == {"None", "unknown option", "--out requires a path", "missing .hhp project path", "only one .hhp project"}

ExpectedMode(s) ==
  CASE
    s \in {"NoArgs", "HelpFirst", "HelpAfterProject", "SlashQuestion"} -> "Help"
  [] s \in {"VersionFirst", "VersionBeforeMissingProject"} -> "Version"
  [] s \in {"UnknownDashOption", "OutMissingValue", "MissingProject", "MultipleProjects"} -> "ArgError"
  [] OTHER -> "Compile"

ExpectedProjectSelected(s) ==
  s \in {"OutBeforeProject", "RepeatedOutLastWins", "FlagsAnyOrder"}

ExpectedOutputPath(s) ==
  CASE
    s = "OutBeforeProject" -> "dist/output.chm"
  [] s = "RepeatedOutLastWins" -> "final.chm"
  [] OTHER -> "None"

ExpectedFlags(s) ==
  CASE
    s = "FlagsAnyOrder" -> {"AllowMissing", "NoLinkScan", "Verbose"}
  [] OTHER -> {}

ExpectedError(s) ==
  CASE
    s = "UnknownDashOption" -> "unknown option"
  [] s = "OutMissingValue" -> "--out requires a path"
  [] s = "MissingProject" -> "missing .hhp project path"
  [] s = "MultipleProjects" -> "only one .hhp project"
  [] OTHER -> "None"

ExpectedExitCode(s) ==
  IF ExpectedMode(s) = "ArgError" THEN 2 ELSE 0

Init ==
  /\ scenario \in Scenarios
  /\ phase = "Start"
  /\ mode = "Unset"
  /\ projectSelected = FALSE
  /\ outputPath = "None"
  /\ flags = {}
  /\ error = "None"
  /\ exitCode = -1
  /\ compileStarts = FALSE

Parse ==
  /\ phase = "Start"
  /\ phase' = "Done"
  /\ mode' = ExpectedMode(scenario)
  /\ projectSelected' = ExpectedProjectSelected(scenario)
  /\ outputPath' = ExpectedOutputPath(scenario)
  /\ flags' = ExpectedFlags(scenario)
  /\ error' = ExpectedError(scenario)
  /\ exitCode' = ExpectedExitCode(scenario)
  /\ compileStarts' = (ExpectedMode(scenario) = "Compile")
  /\ UNCHANGED scenario

StayDone ==
  /\ phase = "Done"
  /\ UNCHANGED vars

Next == Parse \/ StayDone

Spec == Init /\ [][Next]_vars /\ WF_vars(Next)

TypeOK ==
  /\ scenario \in Scenarios
  /\ phase \in {"Start", "Done"}
  /\ mode \in Modes
  /\ projectSelected \in BOOLEAN
  /\ outputPath \in OutputPaths
  /\ flags \in SUBSET FlagTags
  /\ error \in Errors
  /\ exitCode \in {-1, 0, 2}
  /\ compileStarts \in BOOLEAN

TerminalModesDoNotCompile ==
  phase = "Done" /\ mode \in {"Help", "Version", "ArgError"} => ~compileStarts

HelpAndVersionShortCircuit ==
  phase = "Done" /\ scenario \in {"HelpFirst", "HelpAfterProject", "SlashQuestion", "VersionFirst", "VersionBeforeMissingProject"} =>
    /\ ~projectSelected
    /\ outputPath = "None"
    /\ flags = {}
    /\ error = "None"
    /\ exitCode = 0

ArgumentErrorsStopBeforeCompile ==
  phase = "Done" /\ mode = "ArgError" =>
    /\ exitCode = 2
    /\ error # "None"
    /\ ~compileStarts

OutputOptionOrderAndOverride ==
  phase = "Done" =>
    /\ scenario = "OutBeforeProject" => outputPath = "dist/output.chm"
    /\ scenario = "RepeatedOutLastWins" => outputPath = "final.chm"

ProjectIsRequiredForCompile ==
  phase = "Done" /\ mode = "Compile" => projectSelected

EventuallyDone == <> (phase = "Done")

====
