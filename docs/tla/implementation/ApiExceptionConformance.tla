---- MODULE ApiExceptionConformance ----
EXTENDS Integers

(*
Exception-injection model for implementation API boundaries.

The main implementation conformance model checks scenario outcomes. This model
checks a different question: when an implementation API throws at each modeled
boundary, is the exception policy explicit and preserved?

Policy:
- LinkScanner.ExtractLinks absorbs read/parse failures and returns no links.
- All other modeled API exceptions become compile errors, return exit code 1,
  and must not report a successfully created CHM.
*)

VARIABLES
  phase,
  injectedApi,
  archive,
  warnings,
  stderr,
  exceptionObserved,
  exceptionAbsorbed,
  exitCode,
  chmCreated,
  visited

vars == <<
  phase,
  injectedApi,
  archive,
  warnings,
  stderr,
  exceptionObserved,
  exceptionAbsorbed,
  exitCode,
  chmCreated,
  visited
>>

ApiExceptionPoints == {
  "None",
  "HhpProject.Load",
  "TextEncodingDetector.ReadProject",
  "ProjectCompiler.ResolveSourcePath",
  "ProjectCompiler.BuildInputData",
  "ProjectCompiler.ResolveOutputPath",
  "LinkScanner.ExtractLinks",
  "ChmWriter.BuildSystemFile",
  "ChmWriter.BuildDirectory",
  "ChmWriter.ReadInputFile",
  "ChmWriter.CreateOutput"
}

FailingApiExceptionPoints ==
  ApiExceptionPoints \ {"None", "LinkScanner.ExtractLinks"}

AbsorbedApiExceptionPoints == {"LinkScanner.ExtractLinks"}

ArchivePaths == {"index.html", "linked.html", "Table of Contents.hhc"}
WarningTags == {"generated toc"}
StderrTags == {"error"}
VisitedTags == {
  "Start",
  "ProjectLoaded",
  "ExplicitFilesCollected",
  "LinksScanned",
  "MetadataBuilt",
  "OutputResolved",
  "InternalEntriesBuilt",
  "DirectoryBuilt",
  "Done"
}

Init ==
  /\ phase = "Start"
  /\ injectedApi \in ApiExceptionPoints
  /\ archive = {}
  /\ warnings = {}
  /\ stderr = {}
  /\ exceptionObserved = FALSE
  /\ exceptionAbsorbed = FALSE
  /\ exitCode = -1
  /\ chmCreated = FALSE
  /\ visited = {"Start"}

FailAt(api) ==
  /\ injectedApi = api
  /\ phase' = "Done"
  /\ exceptionObserved' = TRUE
  /\ exceptionAbsorbed' = FALSE
  /\ stderr' = stderr \cup {"error"}
  /\ exitCode' = 1
  /\ chmCreated' = FALSE
  /\ visited' = visited \cup {"Done"}
  /\ UNCHANGED <<injectedApi, archive, warnings>>

LoadProject ==
  /\ phase = "Start"
  /\ IF injectedApi \in {"HhpProject.Load", "TextEncodingDetector.ReadProject"} THEN
       FailAt(injectedApi)
     ELSE
       /\ phase' = "ProjectLoaded"
       /\ visited' = visited \cup {"ProjectLoaded"}
       /\ UNCHANGED <<injectedApi, archive, warnings, stderr, exceptionObserved, exceptionAbsorbed, exitCode, chmCreated>>

CollectExplicitFiles ==
  /\ phase = "ProjectLoaded"
  /\ IF injectedApi \in {"ProjectCompiler.ResolveSourcePath", "ProjectCompiler.BuildInputData"} THEN
       FailAt(injectedApi)
     ELSE
       /\ phase' = "ExplicitFilesCollected"
       /\ archive' = archive \cup {"index.html"}
       /\ visited' = visited \cup {"ExplicitFilesCollected"}
       /\ UNCHANGED <<injectedApi, warnings, stderr, exceptionObserved, exceptionAbsorbed, exitCode, chmCreated>>

ScanLinks ==
  /\ phase = "ExplicitFilesCollected"
  /\ IF injectedApi = "LinkScanner.ExtractLinks" THEN
       /\ phase' = "LinksScanned"
       /\ exceptionObserved' = TRUE
       /\ exceptionAbsorbed' = TRUE
       /\ visited' = visited \cup {"LinksScanned"}
       /\ UNCHANGED <<injectedApi, archive, warnings, stderr, exitCode, chmCreated>>
     ELSE
       /\ phase' = "LinksScanned"
       /\ archive' = archive \cup {"linked.html"}
       /\ visited' = visited \cup {"LinksScanned"}
       /\ UNCHANGED <<injectedApi, warnings, stderr, exceptionObserved, exceptionAbsorbed, exitCode, chmCreated>>

BuildMetadata ==
  /\ phase = "LinksScanned"
  /\ phase' = "MetadataBuilt"
  /\ archive' = archive \cup {"Table of Contents.hhc"}
  /\ warnings' = warnings \cup {"generated toc"}
  /\ visited' = visited \cup {"MetadataBuilt"}
  /\ UNCHANGED <<injectedApi, stderr, exceptionObserved, exceptionAbsorbed, exitCode, chmCreated>>

ResolveOutput ==
  /\ phase = "MetadataBuilt"
  /\ IF injectedApi = "ProjectCompiler.ResolveOutputPath" THEN
       FailAt(injectedApi)
     ELSE
       /\ phase' = "OutputResolved"
       /\ visited' = visited \cup {"OutputResolved"}
       /\ UNCHANGED <<injectedApi, archive, warnings, stderr, exceptionObserved, exceptionAbsorbed, exitCode, chmCreated>>

BuildInternalEntries ==
  /\ phase = "OutputResolved"
  /\ IF injectedApi = "ChmWriter.BuildSystemFile" THEN
       FailAt(injectedApi)
     ELSE
       /\ phase' = "InternalEntriesBuilt"
       /\ visited' = visited \cup {"InternalEntriesBuilt"}
       /\ UNCHANGED <<injectedApi, archive, warnings, stderr, exceptionObserved, exceptionAbsorbed, exitCode, chmCreated>>

BuildDirectory ==
  /\ phase = "InternalEntriesBuilt"
  /\ IF injectedApi = "ChmWriter.BuildDirectory" THEN
       FailAt(injectedApi)
     ELSE
       /\ phase' = "DirectoryBuilt"
       /\ visited' = visited \cup {"DirectoryBuilt"}
       /\ UNCHANGED <<injectedApi, archive, warnings, stderr, exceptionObserved, exceptionAbsorbed, exitCode, chmCreated>>

WriteOutput ==
  /\ phase = "DirectoryBuilt"
  /\ IF injectedApi \in {"ChmWriter.ReadInputFile", "ChmWriter.CreateOutput"} THEN
       FailAt(injectedApi)
     ELSE
       /\ phase' = "Done"
       /\ exitCode' = 0
       /\ chmCreated' = TRUE
       /\ visited' = visited \cup {"Done"}
       /\ UNCHANGED <<injectedApi, archive, warnings, stderr, exceptionObserved, exceptionAbsorbed>>

StayDone ==
  /\ phase = "Done"
  /\ UNCHANGED vars

Next ==
  LoadProject
  \/ CollectExplicitFiles
  \/ ScanLinks
  \/ BuildMetadata
  \/ ResolveOutput
  \/ BuildInternalEntries
  \/ BuildDirectory
  \/ WriteOutput
  \/ StayDone

Spec == Init /\ [][Next]_vars /\ WF_vars(Next)

TypeOK ==
  /\ phase \in {"Start", "ProjectLoaded", "ExplicitFilesCollected", "LinksScanned", "MetadataBuilt", "OutputResolved", "InternalEntriesBuilt", "DirectoryBuilt", "Done"}
  /\ injectedApi \in ApiExceptionPoints
  /\ archive \in SUBSET ArchivePaths
  /\ warnings \in SUBSET WarningTags
  /\ stderr \in SUBSET StderrTags
  /\ exceptionObserved \in BOOLEAN
  /\ exceptionAbsorbed \in BOOLEAN
  /\ exitCode \in {-1, 0, 1}
  /\ chmCreated \in BOOLEAN
  /\ visited \in SUBSET VisitedTags

EveryApiBoundaryModeled ==
  ApiExceptionPoints =
    {"None",
     "HhpProject.Load",
     "TextEncodingDetector.ReadProject",
     "ProjectCompiler.ResolveSourcePath",
     "ProjectCompiler.BuildInputData",
     "ProjectCompiler.ResolveOutputPath",
     "LinkScanner.ExtractLinks",
     "ChmWriter.BuildSystemFile",
     "ChmWriter.BuildDirectory",
     "ChmWriter.ReadInputFile",
     "ChmWriter.CreateOutput"}

FailingApiExceptionsBecomeCompileErrors ==
  phase = "Done" /\ injectedApi \in FailingApiExceptionPoints =>
    /\ exceptionObserved
    /\ ~exceptionAbsorbed
    /\ exitCode = 1
    /\ chmCreated = FALSE
    /\ "error" \in stderr

LinkScannerExceptionsAreAbsorbed ==
  phase = "Done" /\ injectedApi = "LinkScanner.ExtractLinks" =>
    /\ exceptionObserved
    /\ exceptionAbsorbed
    /\ exitCode = 0
    /\ chmCreated = TRUE
    /\ "index.html" \in archive
    /\ "linked.html" \notin archive

NoInjectedExceptionSucceeds ==
  phase = "Done" /\ injectedApi = "None" =>
    /\ ~exceptionObserved
    /\ exitCode = 0
    /\ chmCreated = TRUE
    /\ "index.html" \in archive
    /\ "linked.html" \in archive

ErrorDoesNotCreateChm ==
  phase = "Done" /\ exitCode # 0 => chmCreated = FALSE

EventuallyDone == <> (phase = "Done")

====
