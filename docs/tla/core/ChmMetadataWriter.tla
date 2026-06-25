---- MODULE ChmMetadataWriter ----
EXTENDS Integers

(*
Abstract CHM metadata writer model.

ChmDirectory covers PMGL/PMGI structure. This model focuses on metadata stream
selection, #SYSTEM optional entries, DBCS flags, string-table reuse, and writer
failures that must not create or overwrite a CHM.
*)

VARIABLES
  scenario,
  phase,
  systemCodes,
  stringTags,
  writerTags,
  error,
  chmCreated,
  existingOutputPreserved

vars == <<scenario, phase, systemCodes, stringTags, writerTags, error, chmCreated, existingOutputPreserved>>

Scenarios == {
  "FullMetadata",
  "OmitOptionalTopicContentsIndexFont",
  "OmittedContentsNoGeneratedToc",
  "DbcsLanguage",
  "NonDbcsLanguage",
  "StringTableDeduplicates",
  "OversizedSystemEntry",
  "InputReadFailure",
  "LockedOutputPreserved"
}

SystemCodeTags == {
  "Contents",
  "Index",
  "DefaultTopic",
  "Title",
  "DefaultWindow",
  "CompiledStem",
  "Generator",
  "Timestamp",
  "SystemFlags",
  "DefaultFont",
  "Unknown12"
}

StringTags == {"main", "custom", "title", "contents", "index", "topic", "dedup"}
WriterTagSet == {"InternalStreams", "System", "Windows", "Strings", "Dbcs", "NonDbcs", "WriteFailed", "MetadataEntryTooLarge", "InputReadError", "OutputCreateLocked", "ExistingOutputPreserved", "StringTableDeduplicated"}
Errors == {"None", "#SYSTEM entry is too large", "input file read error", "output file locked"}

BaseCodes == {"Title", "DefaultWindow", "CompiledStem", "Generator", "Timestamp", "SystemFlags", "Unknown12"}

ExpectedSystemCodes(s) ==
  CASE
    s = "FullMetadata" -> BaseCodes \cup {"Contents", "Index", "DefaultTopic", "DefaultFont"}
  [] s = "OmittedContentsNoGeneratedToc" -> BaseCodes
  [] s = "OversizedSystemEntry" -> BaseCodes
  [] OTHER -> BaseCodes

ExpectedStrings(s) ==
  CASE
    s = "FullMetadata" -> {"custom", "title", "contents", "index", "topic"}
  [] s = "OmittedContentsNoGeneratedToc" -> {"main", "title"}
  [] s = "StringTableDeduplicates" -> {"main", "title", "dedup"}
  [] OTHER -> {"main", "title"}

ExpectedWriterTags(s) ==
  CASE
    s = "DbcsLanguage" -> {"InternalStreams", "System", "Windows", "Strings", "Dbcs"}
  [] s = "NonDbcsLanguage" -> {"InternalStreams", "System", "Windows", "Strings", "NonDbcs"}
  [] s = "StringTableDeduplicates" -> {"InternalStreams", "System", "Windows", "Strings", "StringTableDeduplicated"}
  [] s = "OversizedSystemEntry" -> {"WriteFailed", "MetadataEntryTooLarge"}
  [] s = "InputReadFailure" -> {"WriteFailed", "InputReadError"}
  [] s = "LockedOutputPreserved" -> {"WriteFailed", "OutputCreateLocked", "ExistingOutputPreserved"}
  [] OTHER -> {"InternalStreams", "System", "Windows", "Strings"}

ExpectedError(s) ==
  CASE
    s = "OversizedSystemEntry" -> "#SYSTEM entry is too large"
  [] s = "InputReadFailure" -> "input file read error"
  [] s = "LockedOutputPreserved" -> "output file locked"
  [] OTHER -> "None"

ExpectedChmCreated(s) ==
  s \notin {"OversizedSystemEntry", "InputReadFailure", "LockedOutputPreserved"}

Init ==
  /\ scenario \in Scenarios
  /\ phase = "Start"
  /\ systemCodes = {}
  /\ stringTags = {}
  /\ writerTags = {}
  /\ error = "None"
  /\ chmCreated = FALSE
  /\ existingOutputPreserved = FALSE

WriteMetadata ==
  /\ phase = "Start"
  /\ phase' = "Done"
  /\ systemCodes' = ExpectedSystemCodes(scenario)
  /\ stringTags' = ExpectedStrings(scenario)
  /\ writerTags' = ExpectedWriterTags(scenario)
  /\ error' = ExpectedError(scenario)
  /\ chmCreated' = ExpectedChmCreated(scenario)
  /\ existingOutputPreserved' = (scenario = "LockedOutputPreserved")
  /\ UNCHANGED scenario

StayDone ==
  /\ phase = "Done"
  /\ UNCHANGED vars

Next == WriteMetadata \/ StayDone

Spec == Init /\ [][Next]_vars /\ WF_vars(Next)

TypeOK ==
  /\ scenario \in Scenarios
  /\ phase \in {"Start", "Done"}
  /\ systemCodes \in SUBSET SystemCodeTags
  /\ stringTags \in SUBSET StringTags
  /\ writerTags \in SUBSET WriterTagSet
  /\ error \in Errors
  /\ chmCreated \in BOOLEAN
  /\ existingOutputPreserved \in BOOLEAN

OptionalSystemEntriesAreConditional ==
  phase = "Done" /\ scenario = "OmitOptionalTopicContentsIndexFont" =>
    /\ "Contents" \notin systemCodes
    /\ "Index" \notin systemCodes
    /\ "DefaultTopic" \notin systemCodes
    /\ "DefaultFont" \notin systemCodes

OmittedContentsDoesNotSupplyContentsEntry ==
  phase = "Done" /\ scenario = "OmittedContentsNoGeneratedToc" =>
    /\ "Contents" \notin systemCodes
    /\ "contents" \notin stringTags

DbcsFlagMatchesMetadataEncoding ==
  phase = "Done" =>
    /\ scenario = "DbcsLanguage" => "Dbcs" \in writerTags
    /\ scenario = "NonDbcsLanguage" => "NonDbcs" \in writerTags

StringTableReusesDuplicates ==
  phase = "Done" /\ scenario = "StringTableDeduplicates" =>
    /\ "StringTableDeduplicated" \in writerTags
    /\ "dedup" \in stringTags

WriterFailuresDoNotCreateChm ==
  phase = "Done" /\ "WriteFailed" \in writerTags =>
    /\ ~chmCreated
    /\ error # "None"

LockedOutputPreservesExistingBytes ==
  phase = "Done" /\ scenario = "LockedOutputPreserved" =>
    /\ existingOutputPreserved
    /\ "ExistingOutputPreserved" \in writerTags
    /\ ~chmCreated

EventuallyDone == <> (phase = "Done")

====
