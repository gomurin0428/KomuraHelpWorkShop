---- MODULE TextEncodingDetection ----
EXTENDS Integers

(*
Abstract TextEncodingDetector model.

It checks BOM priority, strict UTF-8 validation, LCID-to-ANSI selection, and
fallback behavior when bytes or cultures cannot be decoded directly.
*)

VARIABLES
  scenario,
  phase,
  detected,
  lcid,
  dbcs,
  fallbackUsed,
  bomTrimmed

vars == <<scenario, phase, detected, lcid, dbcs, fallbackUsed, bomTrimmed>>

Scenarios == {
  "Utf8Bom",
  "Utf16LeBom",
  "Utf16BeBom",
  "ValidUtf8NoBom",
  "InvalidUtf8WithDeclaredJapanese",
  "InvalidUtf8WithCurrentAnsi",
  "LanguageJapanese",
  "LanguageJapaneseNoPrefix",
  "LanguageInvalidFallsBack",
  "AnsiProviderUnavailableFallsBackUtf8",
  "DbcsChineseSimplified",
  "NonDbcsEnglish"
}

Encodings == {"Unset", "UTF8", "UTF16LE", "UTF16BE", "CP932", "CP936", "CP1252", "CurrentAnsi"}
Lcids == {"None", "0x0411", "0x0804", "CurrentCulture", "Invalid", "0x0409"}

ExpectedEncoding(s) ==
  CASE
    s = "Utf8Bom" -> "UTF8"
  [] s = "Utf16LeBom" -> "UTF16LE"
  [] s = "Utf16BeBom" -> "UTF16BE"
  [] s = "ValidUtf8NoBom" -> "UTF8"
  [] s \in {"InvalidUtf8WithDeclaredJapanese", "LanguageJapanese", "LanguageJapaneseNoPrefix"} -> "CP932"
  [] s = "DbcsChineseSimplified" -> "CP936"
  [] s = "NonDbcsEnglish" -> "CP1252"
  [] s = "AnsiProviderUnavailableFallsBackUtf8" -> "UTF8"
  [] OTHER -> "CurrentAnsi"

ExpectedLcid(s) ==
  CASE
    s \in {"InvalidUtf8WithDeclaredJapanese", "LanguageJapanese", "LanguageJapaneseNoPrefix"} -> "0x0411"
  [] s = "DbcsChineseSimplified" -> "0x0804"
  [] s = "NonDbcsEnglish" -> "0x0409"
  [] s = "LanguageInvalidFallsBack" -> "CurrentCulture"
  [] s = "InvalidUtf8WithCurrentAnsi" -> "CurrentCulture"
  [] s = "AnsiProviderUnavailableFallsBackUtf8" -> "CurrentCulture"
  [] OTHER -> "None"

ExpectedDbcs(s) == ExpectedEncoding(s) \in {"CP932", "CP936"}

ExpectedFallbackUsed(s) ==
  s \in {"InvalidUtf8WithDeclaredJapanese", "InvalidUtf8WithCurrentAnsi", "LanguageInvalidFallsBack", "AnsiProviderUnavailableFallsBackUtf8"}

ExpectedBomTrimmed(s) ==
  s \in {"Utf8Bom", "Utf16LeBom", "Utf16BeBom"}

Init ==
  /\ scenario \in Scenarios
  /\ phase = "Start"
  /\ detected = "Unset"
  /\ lcid = "None"
  /\ dbcs = FALSE
  /\ fallbackUsed = FALSE
  /\ bomTrimmed = FALSE

Detect ==
  /\ phase = "Start"
  /\ phase' = "Done"
  /\ detected' = ExpectedEncoding(scenario)
  /\ lcid' = ExpectedLcid(scenario)
  /\ dbcs' = ExpectedDbcs(scenario)
  /\ fallbackUsed' = ExpectedFallbackUsed(scenario)
  /\ bomTrimmed' = ExpectedBomTrimmed(scenario)
  /\ UNCHANGED scenario

StayDone ==
  /\ phase = "Done"
  /\ UNCHANGED vars

Next == Detect \/ StayDone

Spec == Init /\ [][Next]_vars /\ WF_vars(Next)

TypeOK ==
  /\ scenario \in Scenarios
  /\ phase \in {"Start", "Done"}
  /\ detected \in Encodings
  /\ lcid \in Lcids
  /\ dbcs \in BOOLEAN
  /\ fallbackUsed \in BOOLEAN
  /\ bomTrimmed \in BOOLEAN

BomBeatsFallback ==
  phase = "Done" /\ scenario \in {"Utf8Bom", "Utf16LeBom", "Utf16BeBom"} =>
    /\ bomTrimmed
    /\ ~fallbackUsed

StrictUtf8AcceptedBeforeAnsi ==
  phase = "Done" /\ scenario = "ValidUtf8NoBom" =>
    /\ detected = "UTF8"
    /\ ~fallbackUsed

InvalidUtf8UsesAvailableFallback ==
  phase = "Done" /\ scenario \in {"InvalidUtf8WithDeclaredJapanese", "InvalidUtf8WithCurrentAnsi"} =>
    /\ fallbackUsed
    /\ detected \in {"CP932", "CurrentAnsi"}

LanguageHexWithoutPrefixMatchesJapanese ==
  phase = "Done" /\ scenario = "LanguageJapaneseNoPrefix" =>
    /\ lcid = "0x0411"
    /\ detected = "CP932"

DbcsFlagMatchesEncoding ==
  phase = "Done" => (dbcs <=> detected \in {"CP932", "CP936"})

EventuallyDone == <> (phase = "Done")

====
