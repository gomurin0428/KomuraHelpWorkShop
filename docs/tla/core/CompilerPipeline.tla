---- MODULE CompilerPipeline ----
EXTENDS Integers

(*
Abstract compiler pipeline for Komura HHC.

This model deliberately avoids CHM bytes and filesystem details. It checks the
public state contract across CLI parsing, HHP loading, file collection,
output/input collision validation, metadata construction, CHM writing, and
terminal failures.
*)

VARIABLES
  phase,
  request,
  projectExists,
  allowMissing,
  requiredMissing,
  outputCollision,
  writeOutcome,
  filesCollected,
  metadataBuilt,
  writerRan,
  warnings,
  exitCode,
  chmCreated

vars == <<
  phase,
  request,
  projectExists,
  allowMissing,
  requiredMissing,
  outputCollision,
  writeOutcome,
  filesCollected,
  metadataBuilt,
  writerRan,
  warnings,
  exitCode,
  chmCreated
>>

ConfigVars == <<request, projectExists, allowMissing, requiredMissing, outputCollision, writeOutcome>>

Phases == {"Start", "CliParsed", "ProjectLoaded", "FilesCollected", "MetadataBuilt", "Done"}
Requests == {"Help", "Version", "ArgError", "Compile"}
WriteOutcomes == {"Ok", "OutputUnwritable", "DirectoryEntryTooLarge", "DirectoryTooLarge", "ReservedInternalStreamCollision"}
WarningTags == {"file not found", "Missing required files", "output collision", "write failed"}

Init ==
  /\ phase = "Start"
  /\ request \in Requests
  /\ projectExists \in BOOLEAN
  /\ allowMissing \in BOOLEAN
  /\ requiredMissing \in BOOLEAN
  /\ outputCollision \in BOOLEAN
  /\ writeOutcome \in WriteOutcomes
  /\ filesCollected = FALSE
  /\ metadataBuilt = FALSE
  /\ writerRan = FALSE
  /\ warnings = {}
  /\ exitCode = -1
  /\ chmCreated = FALSE

ParseCli ==
  /\ phase = "Start"
  /\ phase' = "CliParsed"
  /\ UNCHANGED <<ConfigVars, filesCollected, metadataBuilt, writerRan, warnings, exitCode, chmCreated>>

FinishCliTerminal ==
  /\ phase = "CliParsed"
  /\ request \in {"Help", "Version", "ArgError"}
  /\ phase' = "Done"
  /\ exitCode' = IF request = "ArgError" THEN 2 ELSE 0
  /\ chmCreated' = FALSE
  /\ UNCHANGED <<ConfigVars, filesCollected, metadataBuilt, writerRan, warnings>>

LoadProject ==
  /\ phase = "CliParsed"
  /\ request = "Compile"
  /\ projectExists
  /\ phase' = "ProjectLoaded"
  /\ UNCHANGED <<ConfigVars, filesCollected, metadataBuilt, writerRan, warnings, exitCode, chmCreated>>

ProjectMissing ==
  /\ phase = "CliParsed"
  /\ request = "Compile"
  /\ ~projectExists
  /\ phase' = "Done"
  /\ exitCode' = 1
  /\ chmCreated' = FALSE
  /\ UNCHANGED <<ConfigVars, filesCollected, metadataBuilt, writerRan, warnings>>

CollectFiles ==
  /\ phase = "ProjectLoaded"
  /\ requiredMissing => allowMissing
  /\ phase' = "FilesCollected"
  /\ filesCollected' = TRUE
  /\ warnings' = IF requiredMissing THEN warnings \cup {"file not found"} ELSE warnings
  /\ UNCHANGED <<ConfigVars, metadataBuilt, writerRan, exitCode, chmCreated>>

CollectRequiredMissingFails ==
  /\ phase = "ProjectLoaded"
  /\ requiredMissing
  /\ ~allowMissing
  /\ phase' = "Done"
  /\ warnings' = warnings \cup {"file not found", "Missing required files"}
  /\ exitCode' = 1
  /\ chmCreated' = FALSE
  /\ UNCHANGED <<ConfigVars, filesCollected, metadataBuilt, writerRan>>

OutputCollisionFails ==
  /\ phase = "FilesCollected"
  /\ outputCollision
  /\ phase' = "Done"
  /\ warnings' = warnings \cup {"output collision"}
  /\ exitCode' = 1
  /\ chmCreated' = FALSE
  /\ UNCHANGED <<ConfigVars, filesCollected, metadataBuilt, writerRan>>

BuildMetadata ==
  /\ phase = "FilesCollected"
  /\ ~outputCollision
  /\ phase' = "MetadataBuilt"
  /\ metadataBuilt' = TRUE
  /\ UNCHANGED <<ConfigVars, filesCollected, writerRan, warnings, exitCode, chmCreated>>

WriteChm ==
  /\ phase = "MetadataBuilt"
  /\ writeOutcome = "Ok"
  /\ phase' = "Done"
  /\ writerRan' = TRUE
  /\ exitCode' = 0
  /\ chmCreated' = TRUE
  /\ UNCHANGED <<ConfigVars, filesCollected, metadataBuilt, warnings>>

WriteFails ==
  /\ phase = "MetadataBuilt"
  /\ writeOutcome # "Ok"
  /\ phase' = "Done"
  /\ writerRan' = TRUE
  /\ warnings' = warnings \cup {"write failed"}
  /\ exitCode' = 1
  /\ chmCreated' = FALSE
  /\ UNCHANGED <<ConfigVars, filesCollected, metadataBuilt>>

StayDone ==
  /\ phase = "Done"
  /\ UNCHANGED vars

Next ==
  ParseCli
  \/ FinishCliTerminal
  \/ LoadProject
  \/ ProjectMissing
  \/ CollectFiles
  \/ CollectRequiredMissingFails
  \/ OutputCollisionFails
  \/ BuildMetadata
  \/ WriteChm
  \/ WriteFails
  \/ StayDone

Spec == Init /\ [][Next]_vars /\ WF_vars(Next)

TypeOK ==
  /\ phase \in Phases
  /\ request \in Requests
  /\ projectExists \in BOOLEAN
  /\ allowMissing \in BOOLEAN
  /\ requiredMissing \in BOOLEAN
  /\ outputCollision \in BOOLEAN
  /\ writeOutcome \in WriteOutcomes
  /\ filesCollected \in BOOLEAN
  /\ metadataBuilt \in BOOLEAN
  /\ writerRan \in BOOLEAN
  /\ warnings \in SUBSET WarningTags
  /\ exitCode \in {-1, 0, 1, 2}
  /\ chmCreated \in BOOLEAN

RequiredMissingBlocksChm ==
  phase = "Done" /\ request = "Compile" /\ projectExists /\ requiredMissing /\ ~allowMissing =>
    /\ exitCode = 1
    /\ chmCreated = FALSE

OutputCollisionBlocksMetadataAndWrite ==
  phase = "Done" /\ request = "Compile" /\ projectExists /\ outputCollision /\ ~(requiredMissing /\ ~allowMissing) =>
    /\ filesCollected
    /\ ~metadataBuilt
    /\ ~writerRan
    /\ exitCode = 1
    /\ chmCreated = FALSE

SuccessfulCompileContract ==
  chmCreated =>
    /\ request = "Compile"
    /\ projectExists
    /\ ~outputCollision
    /\ filesCollected
    /\ metadataBuilt
    /\ writerRan
    /\ exitCode = 0

ErrorDoesNotCreateChm ==
  phase = "Done" /\ exitCode # 0 => chmCreated = FALSE

CliTerminalDoesNotCompile ==
  phase = "Done" /\ request \in {"Help", "Version", "ArgError"} =>
    /\ filesCollected = FALSE
    /\ metadataBuilt = FALSE
    /\ writerRan = FALSE
    /\ chmCreated = FALSE

StageDiscipline ==
  /\ phase \in {"Start", "CliParsed", "ProjectLoaded"} => ~filesCollected
  /\ phase \in {"Start", "CliParsed", "ProjectLoaded", "FilesCollected"} => ~metadataBuilt
  /\ phase # "Done" => ~writerRan

EventuallyDone == <> (phase = "Done")

====
