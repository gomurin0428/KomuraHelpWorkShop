---- MODULE ExistingCodeLoop ----
EXTENDS Integers, FiniteSets

(*
Existing-code formal inspection model for Komura HHC.

The model is intentionally smaller than the implementation. It keeps only the
state transitions that matter for bug finding: CLI terminal exits, project
loading, required/optional collection failures, absorbed link-read failures,
metadata/package construction, output creation, write failure before publish, and
the absence of retries.

It models remediated implementation behavior: CHM bytes are staged in a temporary
file and the final output path is replaced only after staging succeeds.
*)

VARIABLES
  scenario,
  phase,
  filesCollected,
  archiveNamespace,
  metadataBuilt,
  packageBuilt,
  outputOpened,
  outputState,
  exitCode,
  errorKind,
  warnings,
  pendingLinks,
  linkReadAbsorbed,
  retryCount,
  transitionCount,
  visited

vars == <<
  scenario,
  phase,
  filesCollected,
  archiveNamespace,
  metadataBuilt,
  packageBuilt,
  outputOpened,
  outputState,
  exitCode,
  errorKind,
  warnings,
  pendingLinks,
  linkReadAbsorbed,
  retryCount,
  transitionCount,
  visited
>>

Scenarios == {
  "Help",
  "ArgError",
  "Success",
  "MissingProject",
  "MissingRequired",
  "AllowMissing",
  "LinkReadFailure",
  "UnsupportedWarning",
  "OutsideProjectPath",
  "OutputCreateFailure",
  "WriteFailureBeforePublish"
}

Phases == {"Start", "ProjectLoaded", "FilesCollected", "LinksScanned", "MetadataBuilt", "PackageBuilt", "OutputOpened", "Done"}
ArchiveNamespaces == {"None", "InsideRelative", "BasenameOnly", "Escaped"}
OutputStates == {"Absent", "Existing", "ValidChm", "Partial"}
ErrorKinds == {"None", "ArgError", "MissingProject", "MissingRequired", "OutputCreateError", "WriteError"}
WarningTags == {"HHC5003", "unsupported feature"}
LinkTags == {"explicit-root", "linked-html", "unreadable-linked-html"}
VisitedTags == {"Start", "CliTerminal", "ProjectLoaded", "FilesCollected", "LinksScanned", "MetadataBuilt", "PackageBuilt", "OutputOpened", "Done"}

CliTerminalScenarios == {"Help", "ArgError"}
CompileScenarios == Scenarios \ CliTerminalScenarios
ErrorScenarios == {"ArgError", "MissingProject", "OutputCreateFailure", "WriteFailureBeforePublish"}
SuccessScenarios == Scenarios \ ErrorScenarios
ExternalAbortScenarios == {}

InitialOutput(s) ==
  IF s \in {"OutputCreateFailure", "WriteFailureBeforePublish"}
  THEN "Existing"
  ELSE "Absent"

ExpectedExit(s) ==
  CASE
    s \in {"Help", "ArgError"} -> 24
  [] s \in {"MissingProject", "MissingRequired", "AllowMissing"} -> 0
  [] s \in {"OutputCreateFailure", "WriteFailureBeforePublish"} -> 1
  [] OTHER -> 1

ExpectedError(s) ==
  CASE
    s = "ArgError" -> "ArgError"
  [] s = "MissingProject" -> "MissingProject"
  [] s = "MissingRequired" -> "None"
  [] s = "OutputCreateFailure" -> "OutputCreateError"
  [] s = "WriteFailureBeforePublish" -> "WriteError"
  [] OTHER -> "None"

ExpectedWarnings(s) ==
  CASE
    s \in {"MissingRequired", "AllowMissing"} -> {"HHC5003"}
  [] s = "UnsupportedWarning" -> {"unsupported feature"}
  [] OTHER -> {}

ExpectedOutput(s) ==
  CASE
    s \in {"Success", "MissingRequired", "AllowMissing", "LinkReadFailure", "UnsupportedWarning", "OutsideProjectPath"} -> "ValidChm"
  [] s = "WriteFailureBeforePublish" -> "Existing"
  [] s = "OutputCreateFailure" -> "Existing"
  [] OTHER -> InitialOutput(s)

ExpectedArchiveNamespace(s) ==
  CASE
    s = "OutsideProjectPath" -> "BasenameOnly"
  [] s \in {"Success", "AllowMissing", "LinkReadFailure", "UnsupportedWarning", "OutputCreateFailure", "WriteFailureBeforePublish"} -> "InsideRelative"
  [] OTHER -> "None"

ExpectedVisited(s) ==
  CASE
    s \in CliTerminalScenarios -> {"Start", "CliTerminal", "Done"}
  [] s = "MissingProject" -> {"Start", "ProjectLoaded", "Done"}
  [] s \in {"OutputCreateFailure"} -> {"Start", "ProjectLoaded", "FilesCollected", "LinksScanned", "MetadataBuilt", "PackageBuilt", "Done"}
  [] s \in {"WriteFailureBeforePublish"} -> {"Start", "ProjectLoaded", "FilesCollected", "LinksScanned", "MetadataBuilt", "PackageBuilt", "OutputOpened", "Done"}
  [] OTHER -> {"Start", "ProjectLoaded", "FilesCollected", "LinksScanned", "MetadataBuilt", "PackageBuilt", "OutputOpened", "Done"}

ExpectedTransitionCount(s) ==
  CASE
    s \in CliTerminalScenarios -> 1
  [] s = "MissingProject" -> 1
  [] s = "OutputCreateFailure" -> 6
  [] OTHER -> 7

Init ==
  /\ scenario \in Scenarios
  /\ phase = "Start"
  /\ filesCollected = FALSE
  /\ archiveNamespace = "None"
  /\ metadataBuilt = FALSE
  /\ packageBuilt = FALSE
  /\ outputOpened = FALSE
  /\ outputState = InitialOutput(scenario)
  /\ exitCode = -1
  /\ errorKind = "None"
  /\ warnings = {}
  /\ pendingLinks = {"explicit-root"}
  /\ linkReadAbsorbed = FALSE
  /\ retryCount = 0
  /\ transitionCount = 0
  /\ visited = {"Start"}

CliTerminal ==
  /\ phase = "Start"
  /\ scenario \in CliTerminalScenarios
  /\ phase' = "Done"
  /\ exitCode' = ExpectedExit(scenario)
  /\ errorKind' = ExpectedError(scenario)
  /\ warnings' = {}
  /\ pendingLinks' = {}
  /\ transitionCount' = transitionCount + 1
  /\ visited' = visited \cup {"CliTerminal", "Done"}
  /\ UNCHANGED <<scenario, filesCollected, archiveNamespace, metadataBuilt, packageBuilt, outputOpened, outputState, linkReadAbsorbed, retryCount>>

LoadProject ==
  /\ phase = "Start"
  /\ scenario \in CompileScenarios
  /\ IF scenario = "MissingProject" THEN
       /\ phase' = "Done"
       /\ exitCode' = 0
       /\ errorKind' = "MissingProject"
       /\ warnings' = {}
       /\ pendingLinks' = {}
       /\ transitionCount' = transitionCount + 1
       /\ visited' = visited \cup {"ProjectLoaded", "Done"}
       /\ UNCHANGED <<filesCollected, archiveNamespace, metadataBuilt, packageBuilt, outputOpened, outputState, linkReadAbsorbed, retryCount>>
     ELSE
       /\ phase' = "ProjectLoaded"
       /\ transitionCount' = transitionCount + 1
       /\ visited' = visited \cup {"ProjectLoaded"}
       /\ UNCHANGED <<filesCollected, archiveNamespace, metadataBuilt, packageBuilt, outputOpened, outputState, exitCode, errorKind, warnings, pendingLinks, linkReadAbsorbed, retryCount>>
  /\ UNCHANGED scenario

CollectFiles ==
  /\ phase = "ProjectLoaded"
  /\ phase' = "FilesCollected"
  /\ filesCollected' = TRUE
  /\ archiveNamespace' =
       IF scenario = "OutsideProjectPath" THEN "BasenameOnly"
       ELSE IF scenario = "MissingRequired" THEN "None"
       ELSE "InsideRelative"
  /\ warnings' = IF scenario \in {"MissingRequired", "AllowMissing"} THEN {"HHC5003"} ELSE {}
  /\ pendingLinks' =
       IF scenario = "MissingRequired" THEN {}
       ELSE IF scenario = "LinkReadFailure" THEN {"unreadable-linked-html"}
       ELSE {"linked-html"}
  /\ transitionCount' = transitionCount + 1
  /\ visited' = visited \cup {"FilesCollected"}
  /\ UNCHANGED <<scenario, metadataBuilt, packageBuilt, outputOpened, outputState, exitCode, errorKind, linkReadAbsorbed, retryCount>>

ScanLinks ==
  /\ phase = "FilesCollected"
  /\ phase' = "LinksScanned"
  /\ pendingLinks' = {}
  /\ linkReadAbsorbed' = (scenario = "LinkReadFailure")
  /\ transitionCount' = transitionCount + 1
  /\ visited' = visited \cup {"LinksScanned"}
  /\ UNCHANGED <<scenario, filesCollected, archiveNamespace, metadataBuilt, packageBuilt, outputOpened, outputState, exitCode, errorKind, warnings, retryCount>>

BuildMetadata ==
  /\ phase = "LinksScanned"
  /\ phase' = "MetadataBuilt"
  /\ metadataBuilt' = TRUE
  /\ warnings' = IF scenario = "UnsupportedWarning" THEN warnings \cup {"unsupported feature"} ELSE warnings
  /\ transitionCount' = transitionCount + 1
  /\ visited' = visited \cup {"MetadataBuilt"}
  /\ UNCHANGED <<scenario, filesCollected, archiveNamespace, packageBuilt, outputOpened, outputState, exitCode, errorKind, pendingLinks, linkReadAbsorbed, retryCount>>

BuildPackage ==
  /\ phase = "MetadataBuilt"
  /\ phase' = "PackageBuilt"
  /\ packageBuilt' = TRUE
  /\ transitionCount' = transitionCount + 1
  /\ visited' = visited \cup {"PackageBuilt"}
  /\ UNCHANGED <<scenario, filesCollected, archiveNamespace, metadataBuilt, outputOpened, outputState, exitCode, errorKind, warnings, pendingLinks, linkReadAbsorbed, retryCount>>

CreateOutput ==
  /\ phase = "PackageBuilt"
  /\ IF scenario = "OutputCreateFailure" THEN
       /\ phase' = "Done"
       /\ exitCode' = 1
       /\ errorKind' = "OutputCreateError"
       /\ outputState' = "Existing"
       /\ transitionCount' = transitionCount + 1
       /\ visited' = visited \cup {"Done"}
       /\ UNCHANGED <<outputOpened, retryCount>>
     ELSE
       /\ phase' = "OutputOpened"
       /\ outputOpened' = TRUE
       /\ transitionCount' = transitionCount + 1
       /\ visited' = visited \cup {"OutputOpened"}
       /\ UNCHANGED <<outputState, exitCode, errorKind, retryCount>>
  /\ UNCHANGED <<scenario, filesCollected, archiveNamespace, metadataBuilt, packageBuilt, warnings, pendingLinks, linkReadAbsorbed>>

WriteOutput ==
  /\ phase = "OutputOpened"
  /\ phase' = "Done"
  /\ IF scenario = "WriteFailureBeforePublish" THEN
       /\ exitCode' = 1
       /\ errorKind' = "WriteError"
       /\ outputState' = "Existing"
     ELSE
       /\ exitCode' = IF scenario \in {"MissingRequired", "AllowMissing"} THEN 0 ELSE 1
       /\ errorKind' = "None"
       /\ outputState' = "ValidChm"
  /\ transitionCount' = transitionCount + 1
  /\ visited' = visited \cup {"Done"}
  /\ UNCHANGED <<scenario, filesCollected, archiveNamespace, metadataBuilt, packageBuilt, outputOpened, warnings, pendingLinks, linkReadAbsorbed, retryCount>>

ExternalAbort ==
  /\ phase # "Done"
  /\ scenario \in ExternalAbortScenarios
  /\ phase' = "Done"
  /\ exitCode' = 1
  /\ errorKind' = "WriteError"
  /\ outputState' = IF outputOpened THEN "Partial" ELSE outputState
  /\ pendingLinks' = {}
  /\ transitionCount' = transitionCount + 1
  /\ visited' = visited \cup {"Done"}
  /\ UNCHANGED <<scenario, filesCollected, archiveNamespace, metadataBuilt, packageBuilt, outputOpened, warnings, linkReadAbsorbed, retryCount>>

StayDone ==
  /\ phase = "Done"
  /\ UNCHANGED vars

Next ==
  CliTerminal
  \/ LoadProject
  \/ CollectFiles
  \/ ScanLinks
  \/ BuildMetadata
  \/ BuildPackage
  \/ CreateOutput
  \/ WriteOutput
  \/ ExternalAbort
  \/ StayDone

Spec == Init /\ [][Next]_vars /\ WF_vars(Next)

TypeOK ==
  /\ scenario \in Scenarios
  /\ phase \in Phases
  /\ filesCollected \in BOOLEAN
  /\ archiveNamespace \in ArchiveNamespaces
  /\ metadataBuilt \in BOOLEAN
  /\ packageBuilt \in BOOLEAN
  /\ outputOpened \in BOOLEAN
  /\ outputState \in OutputStates
  /\ exitCode \in {-1, 0, 1, 24}
  /\ errorKind \in ErrorKinds
  /\ warnings \in SUBSET WarningTags
  /\ pendingLinks \in SUBSET LinkTags
  /\ linkReadAbsorbed \in BOOLEAN
  /\ retryCount >= 0
  /\ retryCount <= 0
  /\ transitionCount \in 0..8
  /\ visited \in SUBSET VisitedTags

StageDiscipline ==
  /\ metadataBuilt => filesCollected
  /\ packageBuilt => metadataBuilt
  /\ outputOpened => packageBuilt

FinalOutcomeMatchesCurrentCode ==
  phase = "Done" =>
    /\ exitCode = ExpectedExit(scenario)
    /\ errorKind = ExpectedError(scenario)
    /\ warnings = ExpectedWarnings(scenario)
    /\ outputState = ExpectedOutput(scenario)
    /\ archiveNamespace = ExpectedArchiveNamespace(scenario)
    /\ visited = ExpectedVisited(scenario)

TerminalTransitionCountMatchesPath ==
  phase = "Done" => transitionCount = ExpectedTransitionCount(scenario)

FatalErrorDoesNotCreateChm ==
  phase = "Done" /\ scenario \in {"ArgError", "MissingProject", "OutputCreateFailure", "WriteFailureBeforePublish"} => outputState # "ValidChm"

SuccessRequiresPackageAndOutputOpen ==
  phase = "Done" /\ outputState = "ValidChm" =>
    /\ exitCode \in {0, 1}
    /\ packageBuilt
    /\ outputOpened

EarlyFailuresStopBeforeMetadata ==
  phase = "Done" /\ scenario \in {"Help", "ArgError", "MissingProject"} =>
    /\ ~metadataBuilt
    /\ ~packageBuilt
    /\ ~outputOpened

OutputCreateFailurePreservesExistingOutput ==
  phase = "Done" /\ scenario = "OutputCreateFailure" =>
    /\ outputState = "Existing"
    /\ ~outputOpened
    /\ packageBuilt

OutsideProjectPathUsesBasenameArchiveName ==
  phase = "Done" /\ scenario = "OutsideProjectPath" =>
    /\ archiveNamespace = "BasenameOnly"
    /\ outputState = "ValidChm"

ArchiveNamespaceNeverEscapes ==
  phase = "Done" => archiveNamespace # "Escaped"

WriteFailureBeforePublishPreservesExistingOutput ==
  phase = "Done" /\ scenario = "WriteFailureBeforePublish" =>
    /\ outputOpened
    /\ packageBuilt
    /\ outputState = "Existing"
    /\ exitCode = 1

LinkReadFailureIsAbsorbedWithoutWarning ==
  phase = "Done" /\ scenario = "LinkReadFailure" =>
    /\ linkReadAbsorbed
    /\ "HHC5003" \notin warnings
    /\ outputState = "ValidChm"

NoPendingLinksAtTerminalState ==
  phase = "Done" => pendingLinks = {}

UnsupportedFeaturesWarnButSucceed ==
  phase = "Done" /\ scenario = "UnsupportedWarning" =>
    /\ exitCode = 1
    /\ "unsupported feature" \in warnings
    /\ outputState = "ValidChm"

NoExplicitCancellationOrTimeoutPath ==
  ExternalAbortScenarios = {}

NoRetryPolicy ==
  retryCount <= 0

DesiredFailureAtomicity ==
  phase = "Done" /\ scenario \in {"OutputCreateFailure", "WriteFailureBeforePublish"} => outputState # "Partial"

EventuallyDone == <> (phase = "Done")

====
