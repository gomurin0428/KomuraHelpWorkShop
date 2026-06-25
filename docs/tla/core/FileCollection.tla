---- MODULE FileCollection ----
EXTENDS Naturals, FiniteSets

(*
Abstract file collection model for Komura HHC.

The model checks the recursive collection contract over a small filesystem:
explicit roots, optional links, missing files, silent duplicate replacement, ignored
external targets, link cycles, and Flat=Yes archive naming.
*)

VARIABLES
  phase,
  roots,
  exists,
  allowMissing,
  scanLinks,
  flat,
  pending,
  stored,
  storedSource,
  attempted,
  missingRequired,
  warnings,
  collectionOk

vars == <<
  phase,
  roots,
  exists,
  allowMissing,
  scanLinks,
  flat,
  pending,
  stored,
  storedSource,
  attempted,
  missingRequired,
  warnings,
  collectionOk
>>

ConfigVars == <<roots, exists, allowMissing, scanLinks, flat>>

Files == {"index", "intro", "logo", "missing", "aIndex", "bIndex", "cycleA", "cycleB", "external", "fragment"}
IgnoredFiles == {"external", "fragment"}
RealFiles == Files \ IgnoredFiles

ArchivePaths == {
  "index.html",
  "topics/intro.html",
  "images/logo.png",
  "missing.html",
  "a/index.html",
  "b/index.html",
  "cycle/a.html",
  "cycle/b.html",
  "intro.html",
  "logo.png",
  "a.html",
  "b.html"
}

FlatArchivePaths == {"index.html", "intro.html", "logo.png", "missing.html", "a.html", "b.html"}
WarningTags == {"HHC5003"}
SourceOrNone == Files \cup {"None"}
ItemSet == {[file |-> f, required |-> r] : f \in Files, r \in BOOLEAN}

RootSets == {
  {"index"},
  {"aIndex", "bIndex"},
  {"cycleA"},
  {"missing"},
  {"external"},
  {"fragment"}
}

ExistsSets == {
  RealFiles,
  RealFiles \ {"missing"},
  {"index", "intro", "logo", "aIndex", "bIndex", "cycleA", "cycleB"}
}

ItemsFor(fileSet, isRequired) ==
  {[file |-> f, required |-> isRequired] : f \in fileSet}

Cleaned(file) ==
  IF file \in IgnoredFiles THEN "None" ELSE file

ArchiveOf(file, isFlat) ==
  CASE
    file = "index" -> "index.html"
  [] file = "intro" -> IF isFlat THEN "intro.html" ELSE "topics/intro.html"
  [] file = "logo" -> IF isFlat THEN "logo.png" ELSE "images/logo.png"
  [] file = "missing" -> "missing.html"
  [] file = "aIndex" -> IF isFlat THEN "index.html" ELSE "a/index.html"
  [] file = "bIndex" -> IF isFlat THEN "index.html" ELSE "b/index.html"
  [] file = "cycleA" -> IF isFlat THEN "a.html" ELSE "cycle/a.html"
  [] file = "cycleB" -> IF isFlat THEN "b.html" ELSE "cycle/b.html"
  [] OTHER -> "index.html"

IsScannable(file) ==
  file \in {"index", "intro", "aIndex", "bIndex", "cycleA", "cycleB"}

LinkSet(file) ==
  CASE
    file = "index" -> {"intro", "logo", "external", "fragment"}
  [] file = "intro" -> {"logo"}
  [] file = "cycleA" -> {"cycleB"}
  [] file = "cycleB" -> {"cycleA"}
  [] OTHER -> {}

LinkItems(file) ==
  IF scanLinks /\ IsScannable(file)
  THEN ItemsFor(LinkSet(file), FALSE)
  ELSE {}

Init ==
  /\ phase = "Collecting"
  /\ roots \in RootSets
  /\ exists \in ExistsSets
  /\ allowMissing \in BOOLEAN
  /\ scanLinks \in BOOLEAN
  /\ flat \in BOOLEAN
  /\ pending = ItemsFor(roots, TRUE)
  /\ stored = {}
  /\ storedSource = [a \in ArchivePaths |-> "None"]
  /\ attempted = [a \in ArchivePaths |-> {}]
  /\ missingRequired = {}
  /\ warnings = {}
  /\ collectionOk = FALSE

ProcessIgnored(item) ==
  /\ phase = "Collecting"
  /\ item \in pending
  /\ Cleaned(item.file) = "None"
  /\ pending' = pending \ {item}
  /\ UNCHANGED <<ConfigVars, stored, storedSource, attempted, missingRequired, warnings, collectionOk>>
  /\ phase' = phase

ProcessMissing(item) ==
  /\ phase = "Collecting"
  /\ item \in pending
  /\ Cleaned(item.file) # "None"
  /\ item.file \notin exists
  /\ pending' = pending \ {item}
  /\ warnings' = IF item.required THEN warnings \cup {"HHC5003"} ELSE warnings
  /\ missingRequired' = IF item.required THEN missingRequired \cup {item.file} ELSE missingRequired
  /\ UNCHANGED <<ConfigVars, stored, storedSource, attempted, collectionOk>>
  /\ phase' = phase

ProcessNew(item) ==
  /\ phase = "Collecting"
  /\ item \in pending
  /\ Cleaned(item.file) # "None"
  /\ item.file \in exists
  /\ LET a == ArchiveOf(item.file, flat) IN
     /\ a \notin stored
     /\ pending' = (pending \ {item}) \cup LinkItems(item.file)
     /\ stored' = stored \cup {a}
     /\ storedSource' = [storedSource EXCEPT ![a] = item.file]
     /\ attempted' = [attempted EXCEPT ![a] = @ \cup {item.file}]
     /\ UNCHANGED <<ConfigVars, missingRequired, warnings, collectionOk>>
     /\ phase' = phase

ProcessDuplicate(item) ==
  /\ phase = "Collecting"
  /\ item \in pending
  /\ Cleaned(item.file) # "None"
  /\ item.file \in exists
  /\ LET a == ArchiveOf(item.file, flat) IN
     /\ a \in stored
     /\ pending' = pending \ {item}
     /\ attempted' = [attempted EXCEPT ![a] = @ \cup {item.file}]
     /\ storedSource' = [storedSource EXCEPT ![a] = item.file]
     /\ warnings' = warnings
     /\ UNCHANGED <<ConfigVars, stored, missingRequired, collectionOk>>
     /\ phase' = phase

FinishOk ==
  /\ phase = "Collecting"
  /\ pending = {}
  /\ phase' = "Done"
  /\ collectionOk' = TRUE
  /\ UNCHANGED <<ConfigVars, pending, stored, storedSource, attempted, missingRequired, warnings>>

FinishFail == FALSE

StayDone ==
  /\ phase = "Done"
  /\ UNCHANGED vars

Next ==
  (\E item \in pending:
    ProcessIgnored(item)
    \/ ProcessMissing(item)
    \/ ProcessNew(item)
    \/ ProcessDuplicate(item))
  \/ FinishOk
  \/ StayDone

Spec == Init /\ [][Next]_vars /\ WF_vars(Next)

TypeOK ==
  /\ phase \in {"Collecting", "Done"}
  /\ roots \in RootSets
  /\ exists \in ExistsSets
  /\ allowMissing \in BOOLEAN
  /\ scanLinks \in BOOLEAN
  /\ flat \in BOOLEAN
  /\ pending \in SUBSET ItemSet
  /\ stored \in SUBSET ArchivePaths
  /\ storedSource \in [ArchivePaths -> SourceOrNone]
  /\ attempted \in [ArchivePaths -> SUBSET Files]
  /\ missingRequired \in SUBSET Files
  /\ warnings \in SUBSET WarningTags
  /\ collectionOk \in BOOLEAN

StoredSourceMatchesStored ==
  /\ \A a \in stored: storedSource[a] \in exists
  /\ \A a \in ArchivePaths \ stored: storedSource[a] = "None"

IgnoredTargetsAreNeverStored ==
  \A a \in stored: storedSource[a] \notin IgnoredFiles

MissingRequiredDoesNotBlockCollection ==
  phase = "Done" /\ missingRequired # {} => collectionOk = TRUE

AllowMissingKeepsCollectionPossible ==
  phase = "Done" /\ missingRequired # {} /\ allowMissing => collectionOk = TRUE

DuplicateConflictIsSilentAndReplaced ==
  \A a \in ArchivePaths:
    Cardinality(attempted[a]) > 1 =>
      /\ warnings = {}
      /\ storedSource[a] \in attempted[a]

FlatStorageConsistent ==
  flat => stored \subseteq FlatArchivePaths

StoredFilesExist ==
  \A a \in stored: storedSource[a] \in exists

EventuallyDone == <> (phase = "Done")

====
