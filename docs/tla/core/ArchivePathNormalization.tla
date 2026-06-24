---- MODULE ArchivePathNormalization ----
EXTENDS Integers

(*
Abstract ArchivePath and ProjectCompiler archive-name model.

It checks cleaning, ignored targets, project-path literal preservation,
dot-segment normalization, flat archive names, decoded-NUL rejection, and
outside-project path handling.
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
  "ExternalCid",
  "ExternalUrn",
  "ExternalSmb",
  "ProtocolRelative",
  "UncShare",
  "ChmScheme",
  "QueryFragment",
  "HtmlAndPercentDecode",
  "MalformedPercentKept",
  "DecodedNulRejected",
  "ProjectEntityLiteral",
  "ProjectPercentLiteral",
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
  "docs/a&amp;b.html",
  "assets/a%20b.html",
  "topics/intro.html",
  "images/logo.png",
  "usage.html",
  "page.html",
  "shared/page.html",
  "asset.bin"
}

AbsoluteUriScenarios == {"ExternalHttp", "ExternalCid", "ExternalUrn", "ExternalSmb"}
IgnoredScenarios == {"Empty", "FragmentOnly", "ExternalHttp", "ExternalCid", "ExternalUrn", "ExternalSmb", "ProtocolRelative", "UncShare", "ChmScheme", "DecodedNulRejected", "DotPath"}
ProjectLiteralScenarios == {"ProjectEntityLiteral", "ProjectPercentLiteral"}

ExpectedClean(s) ==
  CASE
    s \in IgnoredScenarios -> "None"
  [] s = "QueryFragment" -> "topics/usage.html"
  [] s = "HtmlAndPercentDecode" -> "topics/a b.html"
  [] s = "MalformedPercentKept" -> "bad%ZZ.html"
  [] s = "ProjectEntityLiteral" -> "docs/a&amp;b.html"
  [] s = "ProjectPercentLiteral" -> "assets/a%20b.html"
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
  [] s \in IgnoredScenarios -> "None"
  [] OTHER -> ExpectedClean(s)

ExpectedIgnored(s) == s \in IgnoredScenarios

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

AllAbsoluteUriSchemesAreExternal ==
  phase = "Done" /\ scenario \in AbsoluteUriScenarios =>
    /\ ignored
    /\ archiveResult = "None"

ProjectPathsRemainLiteral ==
  phase = "Done" =>
    /\ scenario = "ProjectEntityLiteral" => archiveResult = "docs/a&amp;b.html"
    /\ scenario = "ProjectPercentLiteral" => archiveResult = "assets/a%20b.html"

DecodedNulNeverReachesPathResolution ==
  phase = "Done" /\ scenario = "DecodedNulRejected" =>
    /\ ignored
    /\ archiveResult = "None"

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
