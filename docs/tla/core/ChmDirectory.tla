---- MODULE ChmDirectory ----
EXTENDS Naturals, FiniteSets

(*
Abstract CHM directory writer model for Komura HHC.

This model treats CHM bytes as opaque entries. It checks the directory-level
contract: internal streams are present, reserved internal stream collisions are
reported, all user files are reachable on success, PMGI appears exactly when
multiple PMGL blocks are needed, and size-limit failures do not create a CHM.
*)

VARIABLES
  phase,
  shape,
  userFiles,
  entries,
  offsetsAssigned,
  pmglBlocks,
  pmgi,
  reachable,
  error,
  chmCreated

vars == <<
  phase,
  shape,
  userFiles,
  entries,
  offsetsAssigned,
  pmglBlocks,
  pmgi,
  reachable,
  error,
  chmCreated
>>

Shapes == {"Small", "Large", "ReservedCollision", "EntryTooLarge", "DirectoryTooLarge"}
InternalStreams == {"::DataSpace/NameList", "/#SYSTEM", "/#WINDOWS", "/#STRINGS", "/#ITBITS"}
UserAtoms == {"index.html", "toc.hhc", "index.hhk", "topic001.html", "topic002.html", "oversized-name.html", "/#SYSTEM"}
AllEntries == InternalStreams \cup UserAtoms
Errors == {"None", "ReservedInternalStreamCollision", "DirectoryEntryTooLarge", "DirectoryTooLarge"}
MaxPmgiFanout == 4

UserFilesFor(s) ==
  CASE
    s = "Small" -> {"index.html", "toc.hhc", "index.hhk"}
  [] s = "Large" -> {"index.html", "toc.hhc", "index.hhk", "topic001.html", "topic002.html"}
  [] s = "ReservedCollision" -> {"/#SYSTEM"}
  [] s = "EntryTooLarge" -> {"oversized-name.html"}
  [] s = "DirectoryTooLarge" -> {"index.html", "toc.hhc", "index.hhk", "topic001.html", "topic002.html"}

PmglCountFor(s) ==
  CASE
    s = "Small" -> 1
  [] s = "Large" -> 2
  [] s = "ReservedCollision" -> 0
  [] s = "EntryTooLarge" -> 0
  [] s = "DirectoryTooLarge" -> MaxPmgiFanout + 1

Init ==
  /\ phase = "Start"
  /\ shape \in Shapes
  /\ userFiles = UserFilesFor(shape)
  /\ entries = {}
  /\ offsetsAssigned = FALSE
  /\ pmglBlocks = 0
  /\ pmgi = FALSE
  /\ reachable = {}
  /\ error = "None"
  /\ chmCreated = FALSE

ReservedCollision ==
  /\ phase = "Start"
  /\ shape = "ReservedCollision"
  /\ phase' = "Done"
  /\ error' = "ReservedInternalStreamCollision"
  /\ chmCreated' = FALSE
  /\ UNCHANGED <<shape, userFiles, entries, offsetsAssigned, pmglBlocks, pmgi, reachable>>

BuildEntries ==
  /\ phase = "Start"
  /\ shape # "ReservedCollision"
  /\ phase' = "EntriesBuilt"
  /\ entries' = InternalStreams \cup userFiles
  /\ UNCHANGED <<shape, userFiles, offsetsAssigned, pmglBlocks, pmgi, reachable, error, chmCreated>>

AssignOffsets ==
  /\ phase = "EntriesBuilt"
  /\ phase' = "OffsetsAssigned"
  /\ offsetsAssigned' = TRUE
  /\ UNCHANGED <<shape, userFiles, entries, pmglBlocks, pmgi, reachable, error, chmCreated>>

DirectoryEntryTooLarge ==
  /\ phase = "OffsetsAssigned"
  /\ shape = "EntryTooLarge"
  /\ phase' = "Done"
  /\ error' = "DirectoryEntryTooLarge"
  /\ chmCreated' = FALSE
  /\ UNCHANGED <<shape, userFiles, entries, offsetsAssigned, pmglBlocks, pmgi, reachable>>

DirectoryTooLarge ==
  /\ phase = "OffsetsAssigned"
  /\ shape = "DirectoryTooLarge"
  /\ PmglCountFor(shape) > MaxPmgiFanout
  /\ phase' = "Done"
  /\ error' = "DirectoryTooLarge"
  /\ chmCreated' = FALSE
  /\ UNCHANGED <<shape, userFiles, entries, offsetsAssigned, pmglBlocks, pmgi, reachable>>

BuildDirectory ==
  /\ phase = "OffsetsAssigned"
  /\ shape \in {"Small", "Large"}
  /\ phase' = "DirectoryBuilt"
  /\ pmglBlocks' = PmglCountFor(shape)
  /\ pmgi' = (PmglCountFor(shape) > 1)
  /\ UNCHANGED <<shape, userFiles, entries, offsetsAssigned, reachable, error, chmCreated>>

WriteContent ==
  /\ phase = "DirectoryBuilt"
  /\ phase' = "Done"
  /\ reachable' = entries
  /\ chmCreated' = TRUE
  /\ UNCHANGED <<shape, userFiles, entries, offsetsAssigned, pmglBlocks, pmgi, error>>

StayDone ==
  /\ phase = "Done"
  /\ UNCHANGED vars

Next ==
  ReservedCollision
  \/ BuildEntries
  \/ AssignOffsets
  \/ DirectoryEntryTooLarge
  \/ DirectoryTooLarge
  \/ BuildDirectory
  \/ WriteContent
  \/ StayDone

Spec == Init /\ [][Next]_vars /\ WF_vars(Next)

TypeOK ==
  /\ phase \in {"Start", "EntriesBuilt", "OffsetsAssigned", "DirectoryBuilt", "Done"}
  /\ shape \in Shapes
  /\ userFiles \in SUBSET UserAtoms
  /\ entries \in SUBSET AllEntries
  /\ offsetsAssigned \in BOOLEAN
  /\ pmglBlocks \in 0..(MaxPmgiFanout + 1)
  /\ pmgi \in BOOLEAN
  /\ reachable \in SUBSET AllEntries
  /\ error \in Errors
  /\ chmCreated \in BOOLEAN

SuccessHasInternalStreams ==
  chmCreated => InternalStreams \subseteq reachable

SuccessHasAllUserFiles ==
  chmCreated => userFiles \subseteq reachable

ReservedInternalStreamsCannotBeUserFiles ==
  phase = "Done" /\ shape = "ReservedCollision" =>
    /\ error = "ReservedInternalStreamCollision"
    /\ chmCreated = FALSE
    /\ reachable = {}

PmgiIffMultiplePmgl ==
  phase = "Done" /\ chmCreated => (pmgi <=> pmglBlocks > 1)

DirectoryErrorsDoNotCreateChm ==
  phase = "Done" /\ error # "None" => chmCreated = FALSE

EntryLimitEnforced ==
  phase = "Done" /\ shape = "EntryTooLarge" =>
    /\ error = "DirectoryEntryTooLarge"
    /\ chmCreated = FALSE

DirectoryLimitEnforced ==
  phase = "Done" /\ shape = "DirectoryTooLarge" =>
    /\ error = "DirectoryTooLarge"
    /\ chmCreated = FALSE

OffsetsBeforeDirectory ==
  phase \in {"DirectoryBuilt", "Done"} /\ (pmglBlocks > 0 \/ error \in {"DirectoryEntryTooLarge", "DirectoryTooLarge"} \/ chmCreated) =>
    offsetsAssigned

EventuallyDone == <> (phase = "Done")

====
