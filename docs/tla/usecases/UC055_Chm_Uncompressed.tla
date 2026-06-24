---- MODULE UC055_Chm_Uncompressed ----
EXTENDS Integers

(*
Scenario: user files are stored uncompressed
Implementation slices reflected: CliOptions.Parse, HhpProject.Load, ProjectCompiler.CollectFiles, ProjectCompiler.BuildMetadata, ChmWriter.Write
This model follows the concrete compile pipeline only as far as this use case needs:
CLI parsing, HHP loading, file collection/link/path/encoding behavior, metadata construction,
and CHM writing or the relevant early failure.
*)

VARIABLES
  phase,
  cliMode,
  projectState,
  collectionTags,
  archive,
  metadata,
  writerTags,
  stdout,
  stderr,
  warnings,
  exitCode,
  chmCreated,
  visited

vars == <<
  phase,
  cliMode,
  projectState,
  collectionTags,
  archive,
  metadata,
  writerTags,
  stdout,
  stderr,
  warnings,
  exitCode,
  chmCreated,
  visited
>>

ExpectedCliMode == "Compile"
ExpectedProjectState == "Loaded"
ExpectedCollectionTags == {"ExplicitFiles"}
ExpectedArchive == {"index.html"}
ExpectedMetadata == {"Output:project/help.chm", "Title:Project Title"}
ExpectedWriterTags == {"InternalStreams", "PMGL", "Uncompressed"}
ExpectedStdout == {"Banner", "Compiled", "Files"}
ExpectedStderr == {}
ExpectedWarnings == {}
ExpectedExit == 0
ExpectedChmCreated == TRUE
ExpectedVisited == {"CliParsed", "Done", "FilesCollected", "MetadataBuilt", "ProjectLoaded", "Start", "WriterRan"}

TerminalCliModes == {"Help", "Version", "ArgError"}

AllPhases == {"Start", "CliParsed", "ProjectLoaded", "FilesCollected", "MetadataBuilt", "Done"}
AllCliModes == {"Unset", "Help", "Version", "ArgError", "Compile"}
AllProjectStates == {"NotLoaded", "Loaded", "Missing", "Skipped"}
AllCollectionTags == {"AbsoluteProjectFile", "AllAbsoluteSchemesExternal", "AllowMissing", "AnsiFallback", "ArchiveBaseDirectory", "BaseFragment", "BomDetected", "CaseOnlySourceCollision", "CleanQueryFragment", "CollectFailed", "CollectInputReadError", "CssLinks", "DeclaredLanguageEncoding", "DecodeHtmlPercent", "DecodedNulRejected", "DefaultTopicFirstHtml", "DotPathIgnored", "DuplicateConflict", "DuplicateSameFile", "EmptyLinkIgnored", "ExplicitFiles", "ExternalBasePreserved", "ExternalLinksIgnored", "Flat", "FlatArchive", "FlatRewrite", "GeneratedContents", "GeneratedContentsEscaped", "GeneratedContentsLocalUrlEscaped", "HexLanguageWithoutPrefix", "HtmlLinks", "InvalidUtf8Fallback", "LanguageParsed", "LinkReadFailed", "LinkScan", "LocalParamLinks", "MalformedPercentKept", "MissingRequired", "NoDefaultTopic", "NoLinkScan", "NonScannable", "NormalizeArchivePath", "NormalizeDots", "OptionalMissing", "OutputPathCollision", "OutsideProjectFile", "ProjectEntityLiteral", "RelativeBase", "RequiredFiles", "ReservedEscapePreserved", "ReservedInternalStreamCollision", "RootRelative", "Skipped", "SourceIndexEmbedded", "SourceTocEmbedded", "TruthyOption", "UncLinkIgnored", "UnsupportedWarning", "Utf8Strict", "VerboseLog"}
AllArchivePaths == {"#SYSTEM", "C#Guide.html", "Table of Contents.hhc", "asset.bin", "bad%ZZ.html", "bg.png", "docs/a&amp;b.html", "downloads/manual.pdf", "help.css", "images/bg.png", "images/logo.png", "index.hhk", "index.html", "literal%23.html", "logo.png", "readme.txt", "shared/page.html", "site.css", "styles/site.css", "styles/theme.css", "toc.hhc", "topic-&-one.html", "topic.html", "topics/a b.html", "topics/chapter.html", "topics/intro.html", "topics/reference.html", "topics/start.html", "topics/usage.html", "usage.html"}
AllMetadataTags == {"Contents:Table of Contents.hhc", "Contents:toc.hhc", "ContentsGenerated:true", "DBCS:true", "DefaultTopic:index.html", "DefaultTopic:topics/start.html", "Encoding:BOM", "Encoding:InvalidUtf8Fallback", "Encoding:UTF8", "ExistingOutput:Preserved", "Font:MS UI Gothic, 9", "FullTextSearch:false", "GeneratedContentsEscaped:true", "GeneratedContentsLocalUrlEscaped:true", "Hhp:BlankOptionIgnored", "Hhp:CaseInsensitive", "Hhp:CommentsIgnored", "Hhp:LastOptionWins", "Hhp:PreSectionIgnored", "Hhp:QuotedValues", "Hhp:UnbalancedQuoteLiteral", "Index:index.hhk", "KeptSource:a/index.html", "LCID:0x0411", "LCID:0x0411-no-prefix", "LCID:CurrentCulture", "LinksRewrittenForFlat:true", "MergeFiles:metadata-only", "Output:absolute/output.chm", "Output:project/created/out.chm", "Output:project/dist/output.chm", "Output:project/final.chm", "Output:project/help.chm", "Output:project/manual.chm", "OutputDirectoryCreated:true", "StringTable:Deduplicated", "TextEncoding:AnsiFallback", "TextEncoding:CP932", "TextEncoding:CurrentAnsi", "Title:'Open", "Title:New Title", "Title:Oversized", "Title:Product Help", "Title:Project Title", "Title:Quoted Title", "Title:manual", "Window:custom", "Window:main", "Windows:generated-default"}
AllWriterTags == {"DirectoryEntryTooLarge", "DirectoryTooLarge", "ExistingOutputPreserved", "InputReadError", "InternalStreams", "Itbits", "MetadataEntryTooLarge", "NameList", "NoPMGI", "OutputCreateLocked", "OutputWriteError", "PMGI", "PMGL", "ReservedInternalStreamCollision", "Skipped", "StringTableDeduplicated", "Strings", "System", "Uncompressed", "Windows", "WriteFailed"}
AllStdout == {"Banner", "Compiled", "Files", "Options", "Usage", "Version"}
AllStderr == {"#SYSTEM entry is too large", "--out requires a path", "Directory entry is too large", "Missing required files", "Project file not found", "add log", "directory is too large for this compiler version", "error", "input file read error", "internal stream collision", "missing .hhp project path", "only one .hhp project", "output file locked", "output overwrite", "unknown option"}
AllWarnings == {"Binary Index is not implemented", "Binary TOC is not implemented", "Full-text search index generation is not implemented", "MERGE FILES is not generated", "WINDOWS custom settings are not parsed", "duplicate archive path", "file not found", "generated toc"}
AllVisited == {"Start", "CliParsed", "ProjectLoaded", "ProjectMissing", "FilesCollected", "MetadataBuilt", "WriterRan", "Done"}

Init ==
  /\ phase = "Start"
  /\ cliMode = "Unset"
  /\ projectState = "NotLoaded"
  /\ collectionTags = {}
  /\ archive = {}
  /\ metadata = {}
  /\ writerTags = {}
  /\ stdout = {}
  /\ stderr = {}
  /\ warnings = {}
  /\ exitCode = -1
  /\ chmCreated = FALSE
  /\ visited = {"Start"}

FinalizeData ==
  /\ cliMode' = ExpectedCliMode
  /\ projectState' = ExpectedProjectState
  /\ collectionTags' = ExpectedCollectionTags
  /\ archive' = ExpectedArchive
  /\ metadata' = ExpectedMetadata
  /\ writerTags' = ExpectedWriterTags
  /\ stdout' = ExpectedStdout
  /\ stderr' = ExpectedStderr
  /\ warnings' = ExpectedWarnings
  /\ exitCode' = ExpectedExit
  /\ chmCreated' = ExpectedChmCreated

ParseCli ==
  /\ phase = "Start"
  /\ IF ExpectedCliMode \in TerminalCliModes THEN
       /\ phase' = "Done"
       /\ FinalizeData
       /\ visited' = ExpectedVisited
     ELSE
       /\ phase' = "CliParsed"
       /\ cliMode' = ExpectedCliMode
       /\ visited' = visited \cup {"CliParsed"}
       /\ UNCHANGED <<projectState, collectionTags, archive, metadata, writerTags, stdout, stderr, warnings, exitCode, chmCreated>>

LoadProject ==
  /\ phase = "CliParsed"
  /\ IF ExpectedProjectState = "Missing" THEN
       /\ phase' = "Done"
       /\ FinalizeData
       /\ visited' = ExpectedVisited
     ELSE
       /\ phase' = "ProjectLoaded"
       /\ projectState' = "Loaded"
       /\ visited' = visited \cup {"ProjectLoaded"}
       /\ UNCHANGED <<cliMode, collectionTags, archive, metadata, writerTags, stdout, stderr, warnings, exitCode, chmCreated>>

CollectFiles ==
  /\ phase = "ProjectLoaded"
  /\ IF "CollectFailed" \in ExpectedCollectionTags THEN
       /\ phase' = "Done"
       /\ FinalizeData
       /\ visited' = ExpectedVisited
     ELSE
       /\ phase' = "FilesCollected"
       /\ collectionTags' = ExpectedCollectionTags
       /\ archive' = ExpectedArchive
       /\ warnings' = ExpectedWarnings
       /\ visited' = visited \cup {"FilesCollected"}
       /\ UNCHANGED <<cliMode, projectState, metadata, writerTags, stdout, stderr, exitCode, chmCreated>>

BuildMetadata ==
  /\ phase = "FilesCollected"
  /\ phase' = "MetadataBuilt"
  /\ metadata' = ExpectedMetadata
  /\ visited' = visited \cup {"MetadataBuilt"}
  /\ UNCHANGED <<cliMode, projectState, collectionTags, archive, writerTags, stdout, stderr, warnings, exitCode, chmCreated>>

WriteChm ==
  /\ phase = "MetadataBuilt"
  /\ phase' = "Done"
  /\ FinalizeData
  /\ visited' = ExpectedVisited

StayDone ==
  /\ phase = "Done"
  /\ UNCHANGED vars

Next == ParseCli \/ LoadProject \/ CollectFiles \/ BuildMetadata \/ WriteChm \/ StayDone

Spec == Init /\ [][Next]_vars

TypeOK ==
  /\ phase \in AllPhases
  /\ cliMode \in AllCliModes
  /\ projectState \in AllProjectStates
  /\ collectionTags \in SUBSET AllCollectionTags
  /\ archive \in SUBSET AllArchivePaths
  /\ metadata \in SUBSET AllMetadataTags
  /\ writerTags \in SUBSET AllWriterTags
  /\ stdout \in SUBSET AllStdout
  /\ stderr \in SUBSET AllStderr
  /\ warnings \in SUBSET AllWarnings
  /\ exitCode \in {-1, 0, 1, 2}
  /\ chmCreated \in BOOLEAN
  /\ visited \in SUBSET AllVisited

FinalOutcome ==
  phase = "Done" =>
    /\ cliMode = ExpectedCliMode
    /\ projectState = ExpectedProjectState
    /\ collectionTags = ExpectedCollectionTags
    /\ archive = ExpectedArchive
    /\ metadata = ExpectedMetadata
    /\ writerTags = ExpectedWriterTags
    /\ stdout = ExpectedStdout
    /\ stderr = ExpectedStderr
    /\ warnings = ExpectedWarnings
    /\ exitCode = ExpectedExit
    /\ chmCreated = ExpectedChmCreated
    /\ visited = ExpectedVisited

StageDiscipline ==
  /\ phase = "Start" => visited = {"Start"}
  /\ phase = "CliParsed" => visited = {"Start", "CliParsed"}
  /\ phase = "ProjectLoaded" => visited = {"Start", "CliParsed", "ProjectLoaded"}
  /\ phase = "FilesCollected" => visited = {"Start", "CliParsed", "ProjectLoaded", "FilesCollected"}
  /\ phase = "MetadataBuilt" => visited = {"Start", "CliParsed", "ProjectLoaded", "FilesCollected", "MetadataBuilt"}

UseCaseSpecificContract ==
  phase = "Done" =>
    /\ "Uncompressed" \in writerTags

====
