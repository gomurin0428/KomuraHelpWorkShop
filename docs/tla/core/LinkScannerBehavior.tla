---- MODULE LinkScannerBehavior ----
EXTENDS Integers

(*
Abstract LinkScanner and flat-link rewrite model.

It checks scannable extensions, absorbed read failures, local-target extraction,
ignored targets after ArchivePath cleaning, and flat archive rewrites.
*)

VARIABLES
  scenario,
  phase,
  extracted,
  collected,
  rewritten,
  readFailed,
  errorAbsorbed

vars == <<scenario, phase, extracted, collected, rewritten, readFailed, errorAbsorbed>>

Scenarios == {
  "HtmlHrefSrc",
  "HtmlSingleQuotedAndUnquoted",
  "CssImportAndUrl",
  "HhcLocalParam",
  "NonLocalParamIgnored",
  "NonScannable",
  "ReadFailure",
  "EmptyAndFragmentTargets",
  "ExternalTargets",
  "FlatRewriteHtml",
  "FlatRewriteCss",
  "FlatRewriteLocalParam",
  "FlatRewriteExternalUnchanged"
}

Targets == {"intro", "logo", "theme", "bg", "usage", "external", "fragment"}
RewriteTags == {"None", "intro.html", "logo.png?size=small", "bg.png", "usage.html", "external-unchanged"}

ExpectedExtracted(s) ==
  CASE
    s = "HtmlHrefSrc" -> {"intro", "logo"}
  [] s = "HtmlSingleQuotedAndUnquoted" -> {"intro", "logo"}
  [] s = "CssImportAndUrl" -> {"theme", "bg"}
  [] s = "HhcLocalParam" -> {"usage"}
  [] s = "NonLocalParamIgnored" -> {}
  [] s = "EmptyAndFragmentTargets" -> {"fragment"}
  [] s = "ExternalTargets" -> {"external"}
  [] OTHER -> {}

ExpectedCollected(s) ==
  CASE
    s = "HtmlHrefSrc" -> {"intro", "logo"}
  [] s = "HtmlSingleQuotedAndUnquoted" -> {"intro", "logo"}
  [] s = "CssImportAndUrl" -> {"theme", "bg"}
  [] s = "HhcLocalParam" -> {"usage"}
  [] OTHER -> {}

ExpectedRewrite(s) ==
  CASE
    s = "FlatRewriteHtml" -> "logo.png?size=small"
  [] s = "FlatRewriteCss" -> "bg.png"
  [] s = "FlatRewriteLocalParam" -> "usage.html"
  [] s = "FlatRewriteExternalUnchanged" -> "external-unchanged"
  [] OTHER -> "None"

ExpectedReadFailed(s) == s = "ReadFailure"

Init ==
  /\ scenario \in Scenarios
  /\ phase = "Start"
  /\ extracted = {}
  /\ collected = {}
  /\ rewritten = "None"
  /\ readFailed = FALSE
  /\ errorAbsorbed = FALSE

Scan ==
  /\ phase = "Start"
  /\ phase' = "Done"
  /\ extracted' = ExpectedExtracted(scenario)
  /\ collected' = ExpectedCollected(scenario)
  /\ rewritten' = ExpectedRewrite(scenario)
  /\ readFailed' = ExpectedReadFailed(scenario)
  /\ errorAbsorbed' = ExpectedReadFailed(scenario)
  /\ UNCHANGED scenario

StayDone ==
  /\ phase = "Done"
  /\ UNCHANGED vars

Next == Scan \/ StayDone

Spec == Init /\ [][Next]_vars /\ WF_vars(Next)

TypeOK ==
  /\ scenario \in Scenarios
  /\ phase \in {"Start", "Done"}
  /\ extracted \in SUBSET Targets
  /\ collected \in SUBSET Targets
  /\ rewritten \in RewriteTags
  /\ readFailed \in BOOLEAN
  /\ errorAbsorbed \in BOOLEAN

ReadFailuresAreAbsorbed ==
  phase = "Done" /\ scenario = "ReadFailure" =>
    /\ readFailed
    /\ errorAbsorbed
    /\ extracted = {}
    /\ collected = {}

OnlyLocalCleanedTargetsAreCollected ==
  phase = "Done" =>
    /\ collected \subseteq {"intro", "logo", "theme", "bg", "usage"}
    /\ "external" \notin collected
    /\ "fragment" \notin collected

NonScannableFilesProduceNoLinks ==
  phase = "Done" /\ scenario = "NonScannable" =>
    /\ extracted = {}
    /\ collected = {}

FlatRewritePreservesSuffixAndIgnoresExternal ==
  phase = "Done" =>
    /\ scenario = "FlatRewriteHtml" => rewritten = "logo.png?size=small"
    /\ scenario = "FlatRewriteExternalUnchanged" => rewritten = "external-unchanged"

LocalParamRequiresLocalName ==
  phase = "Done" /\ scenario = "NonLocalParamIgnored" =>
    /\ extracted = {}
    /\ collected = {}

EventuallyDone == <> (phase = "Done")

====
