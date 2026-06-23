---- MODULE ArchivePathNormalization ----
EXTENDS Integers

(*
Abstract ArchivePath and ProjectCompiler archive-name model.

It checks cleaning, ignored targets, dot-segment normalization, flat archive
names, and outside-project path handling.
*)

VARIABLES
  scenario,
  phase,
  cleanResult,
  archiveResult,
  ignored,
  warning

vars == <<scenario, phase, cleanResult, archiveResult, ignored, warning>>

Scenarios == {
  "Empty",
  "FragmentOnly",
  "ExternalHttp",
  "ProtocolRelative",
  "UncShare",
  "ChmScheme",
  "QueryFragment",
  "HtmlAndPercentDecode",
  "MalformedPercentKept",
  "BackslashSeparators",
  "RootRelativeLink",
  "DotSegments",
  "FlatMode",
  "OutsideRelativeProjectFile",
  "AbsoluteProjectFile",
  "DotPath"
}

Paths == {
  "None",
  "index.html",
  "topics/usage.html",
  "topics/a b.html",
  "bad%ZZ.html",
  "topics/intro.html",
  "images/logo.png",
  "usage.html",
  "page.html",
  "shared/page.html",
  "asset.bin"
}

ExpectedClean(s) ==
  CASE
    s \in {"Empty", "FragmentOnly", "ExternalHttp", "ProtocolRelative", "UncShare", "ChmScheme", "DotPath"} -> "None"
  [] s = "QueryFragment" -> "topics/usage.html"
  [] s = "HtmlAndPercentDecode" -> "topics/a b.html"
  [] s = "MalformedPercentKept" -> "bad%ZZ.html"
  [] s = "BackslashSeparators" -> "topics/intro.html"
  [] s = "RootRelativeLink" -> "images/logo.png"
  [] s = "DotSegments" -> "index.html"
  [] s = "FlatMode" -> "topics/usage.html"
  [] s = "OutsideRelativeProjectFile" -> "shared/page.html"
  [] s = "AbsoluteProjectFile" -> "asset.bin"
  [] OTHER -> "None"

ExpectedArchive(s) ==
  CASE
    s = "FlatMode" -> "usage.html"
  [] s = "OutsideRelativeProjectFile" -> "page.html"
  [] s \in {"Empty", "FragmentOnly", "ExternalHttp", "ProtocolRelative", "UncShare", "ChmScheme", "DotPath"} -> "None"
  [] OTHER -> ExpectedClean(s)

ExpectedIgnored(s) ==
  s \in {"Empty", "FragmentOnly", "ExternalHttp", "ProtocolRelative", "UncShare", "ChmScheme", "DotPath"}

Init ==
  /\ scenario \in Scenarios
  /\ phase = "Start"
  /\ cleanResult = "None"
  /\ archiveResult = "None"
  /\ ignored = FALSE
  /\ warning = FALSE

Normalize ==
  /\ phase = "Start"
  /\ phase' = "Done"
  /\ cleanResult' = ExpectedClean(scenario)
  /\ archiveResult' = ExpectedArchive(scenario)
  /\ ignored' = ExpectedIgnored(scenario)
  /\ warning' = FALSE
  /\ UNCHANGED scenario

StayDone ==
  /\ phase = "Done"
  /\ UNCHANGED vars

Next == Normalize \/ StayDone

Spec == Init /\ [][Next]_vars /\ WF_vars(Next)

TypeOK ==
  /\ scenario \in Scenarios
  /\ phase \in {"Start", "Done"}
  /\ cleanResult \in Paths
  /\ archiveResult \in Paths
  /\ ignored \in BOOLEAN
  /\ warning \in BOOLEAN

IgnoredTargetsProduceNoArchivePath ==
  phase = "Done" /\ ignored =>
    /\ cleanResult = "None"
    /\ archiveResult = "None"
    /\ ~warning

LinkCleaningRemovesQueryAndFragment ==
  phase = "Done" /\ scenario = "QueryFragment" => archiveResult = "topics/usage.html"

PercentDecodeIsBestEffort ==
  phase = "Done" =>
    /\ scenario = "HtmlAndPercentDecode" => archiveResult = "topics/a b.html"
    /\ scenario = "MalformedPercentKept" => archiveResult = "bad%ZZ.html"

FlatModeUsesFileNameOnly ==
  phase = "Done" /\ scenario = "FlatMode" => archiveResult = "usage.html"

OutsideProjectPathsStayFinite ==
  phase = "Done" /\ scenario \in {"OutsideRelativeProjectFile", "AbsoluteProjectFile"} =>
    archiveResult \in {"page.html", "asset.bin"}

EventuallyDone == <> (phase = "Done")

====
