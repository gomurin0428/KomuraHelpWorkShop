from __future__ import annotations

from dataclasses import dataclass, field
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "docs" / "tla" / "usecases"


ALL_ARCHIVE_PATHS = {
    "index.html",
    "readme.txt",
    "topics/intro.html",
    "topics/start.html",
    "topics/usage.html",
    "topics/reference.html",
    "topics/a b.html",
    "images/logo.png",
    "images/bg.png",
    "asset.bin",
    "bad%ZZ.html",
    "styles/site.css",
    "styles/theme.css",
    "toc.hhc",
    "index.hhk",
    "Table of Contents.hhc",
    "downloads/manual.pdf",
    "shared/page.html",
    "page.html",
    "usage.html",
    "help.css",
    "site.css",
    "logo.png",
    "bg.png",
    "topic-&-one.html",
    "topics/chapter.html",
    "docs/a&amp;b.html",
    "C#Guide.html",
    "literal%23.html",
    "topic.html",
    "#SYSTEM",
}

ALL_COLLECTION_TAGS = {
    "Skipped",
    "ExplicitFiles",
    "RequiredFiles",
    "DefaultTopicFirstHtml",
    "NoGeneratedContents",
    "MissingRequired",
    "CollectFailed",
    "AllowMissing",
    "DuplicateSameFile",
    "DuplicateConflict",
    "VerboseLog",
    "LinkScan",
    "HtmlLinks",
    "CssLinks",
    "LocalParamLinks",
    "NoLinkScan",
    "ExternalLinksIgnored",
    "CleanQueryFragment",
    "DecodeHtmlPercent",
    "RelativeBase",
    "RootRelative",
    "NonScannable",
    "OptionalMissing",
    "EmptyLinkIgnored",
    "UncLinkIgnored",
    "MalformedPercentKept",
    "LinkReadFailed",
    "NormalizeArchivePath",
    "NormalizeDots",
    "ArchiveBaseDirectory",
    "AbsoluteProjectFile",
    "OutsideProjectFile",
    "DotPathIgnored",
    "Flat",
    "FlatArchive",
    "FlatRewrite",
    "BomDetected",
    "Utf8Strict",
    "InvalidUtf8Fallback",
    "HexLanguageWithoutPrefix",
    "DeclaredLanguageEncoding",
    "AnsiFallback",
    "LanguageParsed",
    "TruthyOption",

    "BaseFragment",

    "ProjectEntityLiteral",
    "ExternalBasePreserved",
    "AllAbsoluteSchemesExternal",
    "DecodedNulRejected",
    "ReservedInternalStreamCollision",
    "OutputPathCollision",
    "CaseOnlySourceCollision",
    "ReservedEscapePreserved",
    "NoDefaultTopic",
    "UnsupportedWarning",
    "SourceTocEmbedded",
    "SourceIndexEmbedded",
}

ALL_METADATA_TAGS = {
    "Output:project/help.chm",
    "Output:project/dist/output.chm",
    "Output:project/final.chm",
    "Output:project/created/out.chm",
    "Output:absolute/output.chm",
    "Output:project/manual.chm",
    "Title:Project Title",
    "Title:Quoted Title",
    "Title:New Title",
    "Title:'Open",
    "Title:Oversized",
    "Title:manual",
    "Title:Product Help",
    "DefaultTopic:index.html",
    "DefaultTopic:topics/start.html",
    "DefaultTopic:topic-&-one.html",
    "DefaultTopic:C#Guide.html",
    "Contents:toc.hhc",
    "Contents:Table of Contents.hhc",
    "Index:index.hhk",
    "Window:main",
    "Window:custom",
    "Font:MS UI Gothic, 9",
    "LCID:0x0411",
    "LCID:CurrentCulture",
    "TextEncoding:CP932",
    "TextEncoding:AnsiFallback",
    "TextEncoding:CurrentAnsi",
    "Encoding:BOM",
    "Encoding:UTF8",
    "Encoding:InvalidUtf8Fallback",
    "LCID:0x0411-no-prefix",
    "DBCS:true",
    "FullTextSearch:false",



    "OutputDirectoryCreated:true",
    "Hhp:CommentsIgnored",
    "Hhp:CaseInsensitive",
    "Hhp:QuotedValues",
    "Hhp:LastOptionWins",
    "Hhp:BlankOptionIgnored",
    "Hhp:UnbalancedQuoteLiteral",
    "Hhp:PreSectionIgnored",
    "KeptSource:b/index.html",

    "StringTable:Deduplicated",
    "ExistingOutput:Preserved",
    "MergeFiles:metadata-only",
    "Windows:generated-default",
}

ALL_WRITER_TAGS = {
    "Skipped",
    "Uncompressed",
    "PMGL",
    "PMGI",
    "NoPMGI",
    "InternalStreams",
    "NameList",
    "System",
    "Windows",
    "Strings",
    "Itbits",
    "WriteFailed",
    "OutputWriteError",
    "OutputCreateLocked",
    "InputReadError",
    "MetadataEntryTooLarge",
    "ExistingOutputPreserved",
    "StringTableDeduplicated",
    "DirectoryEntryTooLarge",
    "DirectoryTooLarge",
    "ReservedInternalStreamCollision",
}

ALL_STDOUT = {"Banner", "Usage", "Options", "Version", "Compiled", "Files"}

ALL_STDERR = {
    "Unable to open",
    "HHC5003",
    "error",
    "Directory entry is too large",
    "directory is too large for this compiler version",
    "#SYSTEM entry is too large",
    "input file read error",
    "output file locked",
    "internal stream collision",
    "output overwrite",
    "add log",
}

ALL_WARNINGS = {
    "HHC5003",


    "Full-text search index generation is not implemented",
    "Binary TOC is not implemented",
    "Binary Index is not implemented",
    "MERGE FILES is not generated",
    "WINDOWS custom settings are not parsed",
}


@dataclass(frozen=True)
class UseCase:
    num: int
    module: str
    title: str
    cli_mode: str = "Compile"
    project_state: str = "Loaded"
    collection_tags: frozenset[str] = field(default_factory=lambda: frozenset({"ExplicitFiles"}))
    archive: frozenset[str] = field(default_factory=lambda: frozenset({"index.html"}))
    metadata: frozenset[str] = field(default_factory=lambda: frozenset({"Output:project/help.chm", "Title:Project Title"}))
    writer_tags: frozenset[str] = field(default_factory=lambda: frozenset({"Uncompressed", "PMGL", "InternalStreams"}))
    stdout: frozenset[str] = field(default_factory=lambda: frozenset({"Banner", "Compiled", "Files"}))
    stderr: frozenset[str] = field(default_factory=frozenset)
    warnings: frozenset[str] = field(default_factory=frozenset)
    exit_code: int = 1
    chm_created: bool = True
    impl: tuple[str, ...] = ("CliOptions.Parse", "HhpProject.Load", "ProjectCompiler.CollectFiles", "ProjectCompiler.BuildMetadata", "ChmWriter.Write")
    specific: tuple[str, ...] = ()

    @property
    def name(self) -> str:
        return f"UC{self.num:03d}_{self.module}"

    @property
    def visited(self) -> frozenset[str]:
        if self.cli_mode in {"Help", "Version", "ArgError"}:
            return frozenset({"Start", "CliParsed", "Done"})
        if self.project_state == "Missing":
            return frozenset({"Start", "CliParsed", "ProjectMissing", "Done"})
        if "CollectFailed" in self.collection_tags:
            return frozenset({"Start", "CliParsed", "ProjectLoaded", "FilesCollected", "Done"})
        return frozenset({"Start", "CliParsed", "ProjectLoaded", "FilesCollected", "MetadataBuilt", "WriterRan", "Done"})


def tags(*items: str) -> frozenset[str]:
    return frozenset(items)


def cli(num: int, module: str, title: str, mode: str, stdout=(), stderr=(), exit_code=24) -> UseCase:
    return UseCase(
        num=num,
        module=module,
        title=title,
        cli_mode=mode,
        project_state="Skipped",
        collection_tags=tags("Skipped"),
        archive=frozenset(),
        metadata=frozenset(),
        writer_tags=tags("Skipped"),
        stdout=tags(*stdout),
        stderr=tags(*stderr),
        warnings=frozenset(),
        exit_code=exit_code,
        chm_created=False,
        impl=("CliOptions.Parse", "Program.Main"),
        specific=(f'cliMode = "{mode}"', "chmCreated = FALSE"),
    )


def project_missing(num: int, module: str, title: str) -> UseCase:
    return UseCase(
        num=num,
        module=module,
        title=title,
        project_state="Missing",
        collection_tags=tags("Skipped"),
        archive=frozenset(),
        metadata=frozenset(),
        writer_tags=tags("Skipped"),
        stdout=frozenset(),
        stderr=tags("Unable to open"),
        exit_code=0,
        chm_created=False,
        impl=("CliOptions.Parse", "HhpProject.Load", "Program.Main"),
        specific=('projectState = "Missing"', "exitCode = 0", "chmCreated = FALSE"),
    )


def collect_fail(num: int, module: str, title: str, stderr=(), warnings=()) -> UseCase:
    return UseCase(
        num=num,
        module=module,
        title=title,
        collection_tags=tags("CollectFailed", "MissingRequired"),
        archive=frozenset(),
        writer_tags=tags("Skipped"),
        stdout=frozenset(),
        stderr=tags(*stderr),
        warnings=tags(*warnings),
        exit_code=1,
        chm_created=False,
        impl=("CliOptions.Parse", "HhpProject.Load", "ProjectCompiler.CollectFiles", "Program.Main"),
        specific=('"CollectFailed" \\in collectionTags', "exitCode = 1", "chmCreated = FALSE"),
    )


def collect_exception(num: int, module: str, title: str, collection_tag: str, stderr=()) -> UseCase:
    return UseCase(
        num=num,
        module=module,
        title=title,
        collection_tags=tags("CollectFailed", collection_tag),
        archive=frozenset(),
        metadata=frozenset(),
        writer_tags=tags("Skipped"),
        stdout=frozenset(),
        stderr=tags(*stderr),
        warnings=frozenset(),
        exit_code=1,
        chm_created=False,
        impl=("CliOptions.Parse", "HhpProject.Load", "ProjectCompiler.CollectFiles", "Program.Main"),
        specific=('"CollectFailed" \\in collectionTags', f'"{collection_tag}" \\in collectionTags', "exitCode = 1", "chmCreated = FALSE"),
    )


def write_fail(num: int, module: str, title: str, writer_tags=(), stderr=()) -> UseCase:
    return UseCase(
        num=num,
        module=module,
        title=title,
        collection_tags=tags("ExplicitFiles"),
        archive=tags("index.html"),
        metadata=tags("Output:project/help.chm", "Title:Project Title"),
        writer_tags=tags("WriteFailed", *writer_tags),
        stdout=frozenset(),
        stderr=tags(*stderr),
        warnings=frozenset(),
        exit_code=1,
        chm_created=False,
        impl=("CliOptions.Parse", "HhpProject.Load", "ProjectCompiler.CollectFiles", "ProjectCompiler.BuildMetadata", "ChmWriter.Write", "Program.Main"),
        specific=('"WriteFailed" \\in writerTags', "exitCode = 1", "chmCreated = FALSE"),
    )


COMPILE_IMPL = ("CliOptions.Parse", "HhpProject.Load", "ProjectCompiler.CollectFiles", "ProjectCompiler.BuildMetadata", "ChmWriter.Write")
LINK_IMPL = ("CliOptions.Parse", "HhpProject.Load", "ProjectCompiler.CollectFiles", "LinkScanner.ExtractLinks", "ArchivePath.CleanLink", "ChmWriter.Write")
FLAT_IMPL = ("CliOptions.Parse", "HhpProject.Load", "ProjectCompiler.CollectFiles", "ArchivePath.NormalizeForArchive", "LinkScanner.ExtractLinks", "ChmWriter.Write")
ENCODING_IMPL = ("CliOptions.Parse", "HhpProject.Load", "TextEncodingDetector.Read", "TextEncodingDetector.ForLcid", "ProjectCompiler.BuildMetadata", "ChmWriter.Write")
WRITER_IMPL = ("CliOptions.Parse", "HhpProject.Load", "ProjectCompiler.CollectFiles", "ProjectCompiler.BuildMetadata", "ChmWriter.Write")


CASES: list[UseCase] = [
    cli(1, "Cli_NoArgsHelp", "no arguments prints help", "Help", stdout=("Banner", "Usage", "Options")),
    cli(2, "Cli_HelpOptions", "help option variants print help", "Help", stdout=("Banner", "Usage", "Options"),),
    cli(3, "Cli_Version", "version option prints version", "Version", stdout=("Banner", "Version")),
    cli(4, "Cli_UnknownOption", "unknown option is an argument error", "ArgError", stdout=("Banner", "Usage", "Options")),
    cli(5, "Cli_OutMissingValue", "missing --out value is an argument error", "ArgError", stdout=("Banner", "Usage", "Options")),
    cli(6, "Cli_MissingProjectArg", "missing project path is an argument error", "ArgError", stdout=("Banner", "Usage", "Options")),
    cli(7, "Cli_MultipleProjects", "multiple project paths are rejected", "ArgError", stdout=("Banner", "Usage", "Options")),

    UseCase(8, "Hhp_StandardCompile", "standard HHP compile", collection_tags=tags("RequiredFiles"), archive=tags("index.html", "topics/intro.html", "toc.hhc", "index.hhk"), metadata=tags("Output:project/help.chm", "Title:Project Title", "DefaultTopic:index.html", "Contents:toc.hhc", "Index:index.hhk"), impl=COMPILE_IMPL, specific=('"index.html" \\in archive', '"toc.hhc" \\in archive', '"DefaultTopic:index.html" \\in metadata')),
    UseCase(9, "Hhp_OutRelativeOverride", "relative --out overrides compiled file", collection_tags=tags("RequiredFiles"), archive=tags("index.html"), metadata=tags("Output:project/dist/output.chm", "Title:Project Title"), impl=COMPILE_IMPL, specific=('"Output:project/dist/output.chm" \\in metadata',)),
    UseCase(10, "Hhp_OutAbsoluteOverride", "absolute -o output is not rebased", collection_tags=tags("RequiredFiles"), archive=tags("index.html"), metadata=tags("Output:absolute/output.chm", "Title:Project Title"), impl=COMPILE_IMPL, specific=('"Output:absolute/output.chm" \\in metadata',)),
    UseCase(11, "Hhp_CompiledFileOmitted", "omitted compiled file uses project stem", collection_tags=tags("RequiredFiles"), archive=tags("index.html"), metadata=tags("Output:project/manual.chm", "Title:manual"), impl=COMPILE_IMPL, specific=('"Output:project/manual.chm" \\in metadata', '"Title:manual" \\in metadata')),
    project_missing(12, "Hhp_ProjectMissing", "missing HHP fails before collection"),
    UseCase(13, "Hhp_CommentsIgnored", "comments and blank lines are ignored", collection_tags=tags("ExplicitFiles"), archive=tags("index.html"), metadata=tags("Output:project/help.chm", "Title:Project Title", "Hhp:CommentsIgnored"), impl=COMPILE_IMPL, specific=('"Hhp:CommentsIgnored" \\in metadata',)),
    UseCase(14, "Hhp_CaseInsensitiveNames", "section and option names are case-insensitive", collection_tags=tags("ExplicitFiles"), archive=tags("index.html"), metadata=tags("Output:project/help.chm", "Title:Project Title", "Hhp:CaseInsensitive"), impl=COMPILE_IMPL, specific=('"Hhp:CaseInsensitive" \\in metadata',)),
    UseCase(15, "Hhp_QuotedValues", "quoted values are unquoted", collection_tags=tags("ExplicitFiles"), archive=tags("index.html"), metadata=tags("Output:project/help.chm", "Title:Quoted Title", "Hhp:QuotedValues"), impl=COMPILE_IMPL, specific=('"Title:Quoted Title" \\in metadata', '"index.html" \\in archive')),
    UseCase(16, "Hhp_DuplicateOptionLastWins", "last duplicate option value wins", collection_tags=tags("ExplicitFiles"), archive=tags("index.html"), metadata=tags("Output:project/help.chm", "Title:New Title", "Hhp:LastOptionWins"), impl=COMPILE_IMPL, specific=('"Title:New Title" \\in metadata', '"Hhp:LastOptionWins" \\in metadata')),
    UseCase(17, "Hhp_PreSectionLinesIgnored", "lines before first section are ignored", collection_tags=tags("ExplicitFiles"), archive=tags("index.html"), metadata=tags("Output:project/help.chm", "Title:Project Title", "Hhp:PreSectionIgnored"), impl=COMPILE_IMPL, specific=('"Hhp:PreSectionIgnored" \\in metadata',)),

    UseCase(18, "Files_RequiredFiles", "required HHP files are collected", collection_tags=tags("RequiredFiles"), archive=tags("index.html", "toc.hhc", "index.hhk", "topics/start.html"), metadata=tags("Output:project/help.chm", "DefaultTopic:topics/start.html"), impl=COMPILE_IMPL, specific=('"topics/start.html" \\in archive', '"DefaultTopic:topics/start.html" \\in metadata')),
    UseCase(19, "Files_DefaultTopicFirstHtml", "first HTML file becomes default topic", collection_tags=tags("DefaultTopicFirstHtml"), archive=tags("readme.txt", "index.html"), metadata=tags("Output:project/help.chm", "DefaultTopic:index.html"), impl=COMPILE_IMPL, specific=('"DefaultTopic:index.html" \\in metadata',)),
    UseCase(20, "Files_NoGeneratedContents", "omitted contents file does not synthesize a TOC", collection_tags=tags("NoGeneratedContents"), archive=tags("index.html", "topics/usage.html"), metadata=tags("Output:project/help.chm", "DefaultTopic:index.html"), impl=COMPILE_IMPL, specific=('"NoGeneratedContents" \\in collectionTags', '"Table of Contents.hhc" \\notin archive', '"Contents:Table of Contents.hhc" \\notin metadata')),
    UseCase(21, "Files_MissingRequiredPartial", "missing required file emits HHC5003 and a partial CHM", collection_tags=tags("MissingRequired"), archive=frozenset(), warnings=tags("HHC5003"), exit_code=0, impl=COMPILE_IMPL, specific=('"MissingRequired" \\in collectionTags', 'archive = {}', '"HHC5003" \\in warnings', "exitCode = 0", "chmCreated = TRUE")),
    UseCase(22, "Files_AllowMissing", "allow-missing retains HHC5003 partial-output behavior", collection_tags=tags("AllowMissing", "MissingRequired"), archive=tags("index.html"), warnings=tags("HHC5003"), exit_code=0, impl=COMPILE_IMPL, specific=('"AllowMissing" \\in collectionTags', '"index.html" \\in archive', '"HHC5003" \\in warnings', "exitCode = 0")),
    UseCase(23, "Files_DuplicateSameFile", "same archive path for same file is deduplicated", collection_tags=tags("DuplicateSameFile"), archive=tags("index.html"), impl=COMPILE_IMPL, specific=('archive = {"index.html"}',)),
    UseCase(24, "Files_DuplicateConflict", "conflicting duplicate archive path silently keeps last file", collection_tags=tags("DuplicateConflict", "Flat"), archive=tags("index.html"), metadata=tags("Output:project/help.chm", "KeptSource:b/index.html"), impl=COMPILE_IMPL, specific=('"DuplicateConflict" \\in collectionTags', '"KeptSource:b/index.html" \\in metadata', "warnings = {}")),
    UseCase(25, "Files_Verbose", "verbose logs collected files", collection_tags=tags("VerboseLog"), archive=tags("index.html"), stderr=tags("add log"), impl=COMPILE_IMPL, specific=('"VerboseLog" \\in collectionTags', '"add log" \\in stderr')),

    UseCase(26, "Links_HtmlHrefSrc", "HTML href and src links are recursively collected", collection_tags=tags("LinkScan", "HtmlLinks"), archive=tags("index.html", "topics/intro.html", "images/logo.png", "styles/site.css"), impl=LINK_IMPL, specific=('"HtmlLinks" \\in collectionTags', '"topics/intro.html" \\in archive', '"styles/site.css" \\in archive')),
    UseCase(27, "Links_CssImportUrl", "CSS import and url links are collected", collection_tags=tags("LinkScan", "CssLinks"), archive=tags("styles/site.css", "styles/theme.css", "images/bg.png"), impl=LINK_IMPL, specific=('"CssLinks" \\in collectionTags', '"styles/theme.css" \\in archive', '"images/bg.png" \\in archive')),
    UseCase(28, "Links_LocalParam", "HHC and HHK Local params are collected", collection_tags=tags("LinkScan", "LocalParamLinks"), archive=tags("toc.hhc", "index.hhk", "topics/usage.html", "topics/reference.html"), impl=LINK_IMPL, specific=('"LocalParamLinks" \\in collectionTags', '"topics/usage.html" \\in archive', '"topics/reference.html" \\in archive')),
    UseCase(29, "Links_NoLinkScan", "no-link-scan keeps only explicit files", collection_tags=tags("NoLinkScan", "ExplicitFiles"), archive=tags("index.html"), impl=LINK_IMPL, specific=('"NoLinkScan" \\in collectionTags', 'archive = {"index.html"}')),
    UseCase(30, "Links_ExternalIgnored", "external and CHM links are ignored", collection_tags=tags("ExternalLinksIgnored"), archive=tags("index.html"), impl=LINK_IMPL, specific=('"ExternalLinksIgnored" \\in collectionTags', 'archive = {"index.html"}', "warnings = {}")),
    UseCase(31, "Links_QueryFragment", "query and fragment are stripped for collection", collection_tags=tags("LinkScan", "CleanQueryFragment"), archive=tags("index.html", "topics/usage.html"), impl=LINK_IMPL, specific=('"CleanQueryFragment" \\in collectionTags', '"topics/usage.html" \\in archive')),
    UseCase(32, "Links_EntityPercent", "HTML entities and percent escapes are decoded", collection_tags=tags("LinkScan", "DecodeHtmlPercent"), archive=tags("index.html", "topics/a b.html"), impl=LINK_IMPL, specific=('"DecodeHtmlPercent" \\in collectionTags', '"topics/a b.html" \\in archive')),
    UseCase(33, "Links_RelativeBase", "relative links are resolved from source directory", collection_tags=tags("LinkScan", "RelativeBase"), archive=tags("topics/intro.html", "images/logo.png"), impl=LINK_IMPL, specific=('"RelativeBase" \\in collectionTags', '"images/logo.png" \\in archive')),
    UseCase(34, "Links_RootRelative", "root-relative links are resolved from project directory", collection_tags=tags("LinkScan", "RootRelative"), archive=tags("topics/intro.html", "images/logo.png"), impl=LINK_IMPL, specific=('"RootRelative" \\in collectionTags', '"images/logo.png" \\in archive')),
    UseCase(35, "Links_NonScannable", "non-scannable files do not produce links", collection_tags=tags("NonScannable"), archive=tags("downloads/manual.pdf"), impl=LINK_IMPL, specific=('"NonScannable" \\in collectionTags', 'archive = {"downloads/manual.pdf"}')),
    UseCase(36, "Links_OptionalMissing", "optional linked file absence is silently skipped", collection_tags=tags("LinkScan", "OptionalMissing"), archive=tags("index.html"), impl=LINK_IMPL, specific=('"OptionalMissing" \\in collectionTags', "warnings = {}", "exitCode = 1")),

    UseCase(37, "Paths_NormalArchivePath", "normal mode preserves project-relative archive path", collection_tags=tags("NormalizeArchivePath"), archive=tags("topics/usage.html"), impl=FLAT_IMPL, specific=('"topics/usage.html" \\in archive',)),
    UseCase(38, "Paths_NormalizeDots", "path separators and dot segments are normalized", collection_tags=tags("NormalizeDots"), archive=tags("index.html"), impl=FLAT_IMPL, specific=('"NormalizeDots" \\in collectionTags', 'archive = {"index.html"}')),
    UseCase(39, "Paths_OutsideRelativeLink", "outside relative links use archive base directory", collection_tags=tags("ArchiveBaseDirectory", "LinkScan"), archive=tags("topics/intro.html", "shared/page.html"), impl=FLAT_IMPL, specific=('"ArchiveBaseDirectory" \\in collectionTags', '"shared/page.html" \\in archive')),
    UseCase(40, "Paths_FlatNames", "Flat mode stores file names only", collection_tags=tags("Flat", "FlatArchive"), archive=tags("usage.html", "help.css"), impl=FLAT_IMPL, specific=('"FlatArchive" \\in collectionTags', '"usage.html" \\in archive', '"help.css" \\in archive')),
    UseCase(41, "Paths_FlatPayloadPreserved", "Flat mode flattens archive paths without rewriting payload links", collection_tags=tags("Flat", "FlatRewrite", "LinkScan"), archive=tags("index.html", "site.css", "toc.hhc", "usage.html", "logo.png", "bg.png"), metadata=tags("Output:project/help.chm"), impl=FLAT_IMPL, specific=('"FlatRewrite" \\in collectionTags', '"logo.png" \\in archive', '"LinksRewrittenForFlat:true" \\notin metadata')),

    UseCase(42, "Encoding_BomText", "BOM text files are read by BOM", collection_tags=tags("BomDetected"), archive=tags("index.html"), metadata=tags("Output:project/help.chm", "Encoding:BOM"), impl=ENCODING_IMPL, specific=('"BomDetected" \\in collectionTags', '"Encoding:BOM" \\in metadata')),
    UseCase(43, "Encoding_StrictUtf8", "BOM-less valid UTF-8 is read as UTF-8", collection_tags=tags("Utf8Strict"), archive=tags("index.html"), metadata=tags("Output:project/help.chm", "Encoding:UTF8"), impl=ENCODING_IMPL, specific=('"Utf8Strict" \\in collectionTags', '"Encoding:UTF8" \\in metadata')),
    UseCase(44, "Encoding_LcidAnsi", "Language LCID selects ANSI code page", collection_tags=tags("LanguageParsed"), archive=tags("toc.hhc"), metadata=tags("Output:project/help.chm", "LCID:0x0411", "TextEncoding:CP932", "DBCS:true"), impl=ENCODING_IMPL, specific=('"LCID:0x0411" \\in metadata', '"TextEncoding:CP932" \\in metadata')),
    UseCase(45, "Encoding_DeclaredLanguageRead", "Language is used for initial HHP read", collection_tags=tags("DeclaredLanguageEncoding"), archive=tags("index.html"), metadata=tags("Output:project/help.chm", "LCID:0x0411", "TextEncoding:CP932"), impl=ENCODING_IMPL, specific=('"DeclaredLanguageEncoding" \\in collectionTags', '"LCID:0x0411" \\in metadata')),
    UseCase(46, "Encoding_CurrentCultureFallback", "invalid or omitted Language uses current culture", collection_tags=tags("AnsiFallback"), archive=tags("index.html"), metadata=tags("Output:project/help.chm", "LCID:CurrentCulture", "TextEncoding:AnsiFallback"), impl=ENCODING_IMPL, specific=('"AnsiFallback" \\in collectionTags', '"LCID:CurrentCulture" \\in metadata')),
    UseCase(47, "Encoding_DbcsLanguages", "DBCS languages set the DBCS system flag", collection_tags=tags("LanguageParsed"), archive=tags("index.html"), metadata=tags("Output:project/help.chm", "DBCS:true"), impl=ENCODING_IMPL, specific=('"DBCS:true" \\in metadata',)),

    UseCase(48, "Metadata_CoreFields", "core metadata fields are reflected", collection_tags=tags("RequiredFiles"), archive=tags("index.html", "toc.hhc", "index.hhk"), metadata=tags("Output:project/help.chm", "Title:Product Help", "DefaultTopic:index.html", "Contents:toc.hhc", "Index:index.hhk"), impl=WRITER_IMPL, specific=('"Title:Product Help" \\in metadata', '"Contents:toc.hhc" \\in metadata', '"Index:index.hhk" \\in metadata')),
    UseCase(49, "Metadata_TitleOmitted", "omitted title uses HHP stem", collection_tags=tags("ExplicitFiles"), archive=tags("index.html"), metadata=tags("Output:project/help.chm", "Title:manual"), impl=WRITER_IMPL, specific=('"Title:manual" \\in metadata',)),
    UseCase(50, "Metadata_DefaultWindowMain", "omitted default window uses main", collection_tags=tags("ExplicitFiles"), archive=tags("index.html"), metadata=tags("Output:project/help.chm", "Window:main"), impl=WRITER_IMPL, specific=('"Window:main" \\in metadata',)),
    UseCase(51, "Metadata_WindowAndFont", "custom window and font are reflected", collection_tags=tags("ExplicitFiles"), archive=tags("index.html"), metadata=tags("Output:project/help.chm", "Window:custom", "Font:MS UI Gothic, 9"), impl=WRITER_IMPL, specific=('"Window:custom" \\in metadata', '"Font:MS UI Gothic, 9" \\in metadata')),
    UseCase(52, "Chm_InternalStreams", "CHM internal streams are present", collection_tags=tags("ExplicitFiles"), archive=tags("index.html"), writer_tags=tags("Uncompressed", "PMGL", "InternalStreams", "NameList", "System", "Windows", "Strings", "Itbits"), impl=WRITER_IMPL, specific=('"NameList" \\in writerTags', '"System" \\in writerTags', '"Windows" \\in writerTags', '"Strings" \\in writerTags', '"Itbits" \\in writerTags')),
    UseCase(53, "Chm_SmallPmglOnly", "small project uses PMGL without PMGI", collection_tags=tags("ExplicitFiles"), archive=tags("index.html"), writer_tags=tags("Uncompressed", "PMGL", "NoPMGI", "InternalStreams"), impl=WRITER_IMPL, specific=('"PMGL" \\in writerTags', '"NoPMGI" \\in writerTags')),
    UseCase(54, "Chm_LargePmgi", "large project generates PMGI index", collection_tags=tags("ExplicitFiles"), archive=tags("index.html"), writer_tags=tags("Uncompressed", "PMGL", "PMGI", "InternalStreams"), impl=WRITER_IMPL, specific=('"PMGI" \\in writerTags', '"PMGL" \\in writerTags')),
    UseCase(55, "Chm_Uncompressed", "user files are stored uncompressed", collection_tags=tags("ExplicitFiles"), archive=tags("index.html"), writer_tags=tags("Uncompressed", "PMGL", "InternalStreams"), impl=WRITER_IMPL, specific=('"Uncompressed" \\in writerTags',)),

    UseCase(56, "Unsupported_FullTextSearch", "full text search warns and disables index", collection_tags=tags("UnsupportedWarning"), archive=tags("index.html"), metadata=tags("Output:project/help.chm", "FullTextSearch:false"), warnings=tags("Full-text search index generation is not implemented"), impl=WRITER_IMPL, specific=('"FullTextSearch:false" \\in metadata', '"Full-text search index generation is not implemented" \\in warnings')),
    UseCase(57, "Unsupported_BinaryToc", "Binary TOC warns and embeds HHC source", collection_tags=tags("UnsupportedWarning", "SourceTocEmbedded"), archive=tags("toc.hhc"), warnings=tags("Binary TOC is not implemented"), impl=WRITER_IMPL, specific=('"SourceTocEmbedded" \\in collectionTags', '"toc.hhc" \\in archive', '"Binary TOC is not implemented" \\in warnings')),
    UseCase(58, "Unsupported_BinaryIndex", "Binary Index warns and embeds HHK source", collection_tags=tags("UnsupportedWarning", "SourceIndexEmbedded"), archive=tags("index.hhk"), warnings=tags("Binary Index is not implemented"), impl=WRITER_IMPL, specific=('"SourceIndexEmbedded" \\in collectionTags', '"index.hhk" \\in archive', '"Binary Index is not implemented" \\in warnings')),
    UseCase(59, "Unsupported_MergeFiles", "merge files warns and does not generate collection", collection_tags=tags("UnsupportedWarning"), archive=tags("index.html"), metadata=tags("Output:project/help.chm", "MergeFiles:metadata-only"), warnings=tags("MERGE FILES is not generated"), impl=WRITER_IMPL, specific=('"MergeFiles:metadata-only" \\in metadata', '"MERGE FILES is not generated" \\in warnings')),
    UseCase(60, "Unsupported_WindowsSection", "WINDOWS section warns and generated default is used", collection_tags=tags("UnsupportedWarning"), archive=tags("index.html"), metadata=tags("Output:project/help.chm", "Windows:generated-default"), writer_tags=tags("Uncompressed", "PMGL", "InternalStreams", "Windows"), warnings=tags("WINDOWS custom settings are not parsed"), impl=WRITER_IMPL, specific=('"Windows:generated-default" \\in metadata', '"Windows" \\in writerTags', '"WINDOWS custom settings are not parsed" \\in warnings')),

    write_fail(61, "Error_OutputUnwritable", "unwritable output path fails", writer_tags=("OutputWriteError",), stderr=("error",)),
    write_fail(62, "Error_DirectoryEntryTooLarge", "single directory entry too large fails", writer_tags=("DirectoryEntryTooLarge",), stderr=("Directory entry is too large",)),
    write_fail(63, "Error_DirectoryTooLarge", "directory too large for compiler version fails", writer_tags=("DirectoryTooLarge",), stderr=("directory is too large for this compiler version",)),

    cli(64, "Cli_HelpShortCircuits", "help option short-circuits later arguments", "Help", stdout=("Banner", "Usage", "Options")),
    cli(65, "Cli_VersionShortCircuits", "version option short-circuits project loading", "Version", stdout=("Banner", "Version")),
    UseCase(66, "Cli_RepeatedOutLastWins", "repeated output option uses the last value", collection_tags=tags("RequiredFiles"), archive=tags("index.html"), metadata=tags("Output:project/final.chm", "Title:Project Title", "Hhp:LastOptionWins"), impl=COMPILE_IMPL, specific=('"Output:project/final.chm" \\in metadata', '"Hhp:LastOptionWins" \\in metadata')),
    UseCase(67, "Cli_OutBeforeProject", "output option before the project path is accepted", collection_tags=tags("RequiredFiles"), archive=tags("index.html"), metadata=tags("Output:project/dist/output.chm", "Title:Project Title"), impl=COMPILE_IMPL, specific=('"Output:project/dist/output.chm" \\in metadata',)),
    UseCase(68, "Hhp_BlankCompiledFileIgnored", "blank compiled file option falls back to project stem", collection_tags=tags("RequiredFiles"), archive=tags("index.html"), metadata=tags("Output:project/manual.chm", "Title:manual", "Hhp:BlankOptionIgnored"), impl=COMPILE_IMPL, specific=('"Hhp:BlankOptionIgnored" \\in metadata', '"Output:project/manual.chm" \\in metadata')),
    UseCase(69, "Hhp_UnbalancedQuoteLiteral", "unbalanced quoted option value is kept literally", collection_tags=tags("ExplicitFiles"), archive=tags("index.html"), metadata=tags("Output:project/help.chm", "Title:'Open", "Hhp:UnbalancedQuoteLiteral"), impl=COMPILE_IMPL, specific=('"Title:\'Open" \\in metadata', '"Hhp:UnbalancedQuoteLiteral" \\in metadata')),
    UseCase(70, "Hhp_TruthyFlatOn", "truthy Flat option values enable flat archive mode", collection_tags=tags("Flat", "FlatArchive", "TruthyOption"), archive=tags("usage.html", "help.css"), metadata=tags("Output:project/help.chm", "Title:Project Title"), impl=FLAT_IMPL, specific=('"TruthyOption" \\in collectionTags', '"usage.html" \\in archive', '"help.css" \\in archive')),
    UseCase(71, "Files_DotPathIgnored", "project paths that normalize to an empty archive path are skipped", collection_tags=tags("DotPathIgnored"), archive=tags("index.html"), metadata=tags("Output:project/help.chm", "DefaultTopic:index.html"), impl=COMPILE_IMPL, specific=('"DotPathIgnored" \\in collectionTags', 'archive = {"index.html"}', "warnings = {}")),
    UseCase(72, "Files_AbsoluteProjectFileNameOnly", "rooted project file outside the project stores the file name", collection_tags=tags("AbsoluteProjectFile"), archive=tags("asset.bin"), metadata=tags("Output:project/help.chm", "Title:Project Title"), impl=COMPILE_IMPL, specific=('"AbsoluteProjectFile" \\in collectionTags', 'archive = {"asset.bin"}')),
    UseCase(73, "Files_OutsideRelativeProjectPath", "relative project file outside the project is stored by basename", collection_tags=tags("OutsideProjectFile"), archive=tags("page.html"), metadata=tags("Output:project/help.chm", "Title:Project Title"), impl=COMPILE_IMPL, specific=('"OutsideProjectFile" \\in collectionTags', 'archive = {"page.html"}')),
    UseCase(74, "Links_EmptyAndFragmentIgnored", "empty and fragment-only links are ignored without warnings", collection_tags=tags("LinkScan", "EmptyLinkIgnored"), archive=tags("index.html"), impl=LINK_IMPL, specific=('"EmptyLinkIgnored" \\in collectionTags', 'archive = {"index.html"}', "warnings = {}")),
    UseCase(75, "Links_UncIgnored", "network-share style links are ignored as non-local targets", collection_tags=tags("UncLinkIgnored"), archive=tags("index.html"), impl=LINK_IMPL, specific=('"UncLinkIgnored" \\in collectionTags', 'archive = {"index.html"}', "warnings = {}")),
    UseCase(76, "Links_MalformedPercentKept", "malformed percent escapes keep their original spelling", collection_tags=tags("LinkScan", "MalformedPercentKept"), archive=tags("index.html", "bad%ZZ.html"), impl=LINK_IMPL, specific=('"MalformedPercentKept" \\in collectionTags', '"bad%ZZ.html" \\in archive')),
    UseCase(77, "Links_ReadErrorAbsorbed", "link scanner read failures are absorbed as no outgoing links", collection_tags=tags("LinkScan", "LinkReadFailed"), archive=tags("index.html"), impl=LINK_IMPL, specific=('"LinkReadFailed" \\in collectionTags', 'archive = {"index.html"}', "exitCode = 1")),
    UseCase(78, "Encoding_InvalidUtf8Fallback", "invalid UTF-8 without BOM falls back to the selected ANSI encoding", collection_tags=tags("InvalidUtf8Fallback", "AnsiFallback"), archive=tags("index.html"), metadata=tags("Output:project/help.chm", "Encoding:InvalidUtf8Fallback", "TextEncoding:AnsiFallback"), impl=ENCODING_IMPL, specific=('"InvalidUtf8Fallback" \\in collectionTags', '"Encoding:InvalidUtf8Fallback" \\in metadata')),
    UseCase(79, "Encoding_LanguageHexWithoutPrefix", "Language LCID can be parsed without the 0x prefix", collection_tags=tags("LanguageParsed", "HexLanguageWithoutPrefix"), archive=tags("toc.hhc"), metadata=tags("Output:project/help.chm", "LCID:0x0411-no-prefix", "TextEncoding:CP932", "DBCS:true"), impl=ENCODING_IMPL, specific=('"HexLanguageWithoutPrefix" \\in collectionTags', '"LCID:0x0411-no-prefix" \\in metadata')),
    UseCase(80, "Metadata_NoDefaultTopicWhenNoHtml", "projects with no HTML file omit the default topic and generated TOC", collection_tags=tags("NoGeneratedContents", "NoDefaultTopic"), archive=tags("readme.txt"), metadata=tags("Output:project/help.chm"), impl=WRITER_IMPL, specific=('"NoDefaultTopic" \\in collectionTags', '"DefaultTopic:index.html" \\notin metadata', '"Table of Contents.hhc" \\notin archive')),
    UseCase(81, "Metadata_FlatOptionArchivePaths", "flat mode normalizes contents and index option archive paths", collection_tags=tags("Flat", "FlatArchive"), archive=tags("toc.hhc", "index.hhk", "index.html"), metadata=tags("Output:project/help.chm", "Contents:toc.hhc", "Index:index.hhk"), impl=WRITER_IMPL, specific=('"FlatArchive" \\in collectionTags', '"Contents:toc.hhc" \\in metadata', '"Index:index.hhk" \\in metadata')),
    UseCase(82, "Chm_StringTableDeduplicates", "CHM string table reuses duplicate metadata strings", collection_tags=tags("ExplicitFiles"), archive=tags("index.html"), metadata=tags("Output:project/help.chm", "Title:Project Title", "Window:main", "StringTable:Deduplicated"), writer_tags=tags("Uncompressed", "PMGL", "InternalStreams", "StringTableDeduplicated"), impl=WRITER_IMPL, specific=('"StringTable:Deduplicated" \\in metadata', '"StringTableDeduplicated" \\in writerTags')),
    write_fail(84, "Error_WriterInputReadFailure", "writer input file read failure exits without creating CHM", writer_tags=("InputReadError",), stderr=("input file read error", "error")),
    UseCase(85, "Error_LockedOutputPreserved", "locked existing output fails without overwriting it", collection_tags=tags("ExplicitFiles"), archive=tags("index.html"), metadata=tags("Output:project/help.chm", "Title:Project Title", "ExistingOutput:Preserved"), writer_tags=tags("WriteFailed", "OutputCreateLocked", "ExistingOutputPreserved"), stdout=frozenset(), stderr=tags("output file locked", "error"), warnings=frozenset(), exit_code=1, chm_created=False, impl=("CliOptions.Parse", "HhpProject.Load", "ProjectCompiler.CollectFiles", "ProjectCompiler.BuildMetadata", "ChmWriter.Write", "Program.Main"), specific=('"OutputCreateLocked" \\in writerTags', '"ExistingOutputPreserved" \\in writerTags', '"ExistingOutput:Preserved" \\in metadata', "chmCreated = FALSE")),
    UseCase(86, "Error_MetadataEntryTooLarge", "oversized metadata entry fails before CHM creation", collection_tags=tags("ExplicitFiles"), archive=tags("index.html"), metadata=tags("Output:project/help.chm", "Title:Oversized"), writer_tags=tags("WriteFailed", "MetadataEntryTooLarge"), stdout=frozenset(), stderr=tags("#SYSTEM entry is too large", "error"), warnings=frozenset(), exit_code=1, chm_created=False, impl=("CliOptions.Parse", "HhpProject.Load", "ProjectCompiler.CollectFiles", "ProjectCompiler.BuildMetadata", "ChmWriter.Write", "Program.Main"), specific=('"MetadataEntryTooLarge" \\in writerTags', '"#SYSTEM entry is too large" \\in stderr', "chmCreated = FALSE")),
    UseCase(87, "Files_OmittedContentsKeepsHtmlTopic", "omitted contents does not synthesize an escaped TOC", collection_tags=tags("NoGeneratedContents"), archive=tags("topic-&-one.html"), metadata=tags("Output:project/help.chm", "DefaultTopic:topic-&-one.html"), impl=COMPILE_IMPL, specific=('"NoGeneratedContents" \\in collectionTags', '"topic-&-one.html" \\in archive', '"Table of Contents.hhc" \\notin archive')),
    UseCase(88, "Links_BaseHrefFragmentTarget", "fragment-only links resolve against local base href", collection_tags=tags("LinkScan", "RelativeBase", "BaseFragment"), archive=tags("index.html", "topics/chapter.html"), impl=LINK_IMPL, specific=('"BaseFragment" \\in collectionTags', '"topics/chapter.html" \\in archive')),
    UseCase(89, "Files_OmittedContentsNoReservedToc", "omitted contents does not synthesize a reserved-name TOC", collection_tags=tags("NoGeneratedContents"), archive=tags("C#Guide.html", "literal%23.html"), metadata=tags("Output:project/help.chm", "DefaultTopic:C#Guide.html"), impl=COMPILE_IMPL, specific=('"NoGeneratedContents" \\in collectionTags', '"C#Guide.html" \\in archive', '"literal%23.html" \\in archive', '"Table of Contents.hhc" \\notin archive')),
    UseCase(90, "Paths_ProjectEntityLiteral", "project paths keep HTML entities literal", collection_tags=tags("ExplicitFiles", "ProjectEntityLiteral"), archive=tags("docs/a&amp;b.html"), impl=COMPILE_IMPL, specific=('"ProjectEntityLiteral" \\in collectionTags', '"docs/a&amp;b.html" \\in archive')),
    UseCase(91, "Links_ExternalBaseFlatRewriteSkipped", "flat rewrite leaves references under external base href unchanged", collection_tags=tags("Flat", "FlatRewrite", "ExternalBasePreserved"), archive=tags("index.html"), impl=FLAT_IMPL, specific=('"ExternalBasePreserved" \\in collectionTags', 'archive = {"index.html"}')),
    UseCase(92, "Links_AnyAbsoluteUriSchemeExternal", "all absolute URI schemes are treated as external links", collection_tags=tags("ExternalLinksIgnored", "AllAbsoluteSchemesExternal"), archive=tags("index.html"), impl=LINK_IMPL, specific=('"AllAbsoluteSchemesExternal" \\in collectionTags', 'archive = {"index.html"}', 'warnings = {}')),
    UseCase(93, "Links_DecodedNulRejected", "decoded NUL links are rejected before path resolution", collection_tags=tags("LinkScan", "DecodedNulRejected"), archive=tags("index.html"), impl=LINK_IMPL, specific=('"DecodedNulRejected" \\in collectionTags', 'archive = {"index.html"}', 'warnings = {}')),
    UseCase(94, "Chm_InternalStreamCollision", "user archive names cannot collide with CHM internal streams", collection_tags=tags("ExplicitFiles", "ReservedInternalStreamCollision"), archive=tags("#SYSTEM"), metadata=tags("Output:project/help.chm", "Title:Project Title"), writer_tags=tags("WriteFailed", "ReservedInternalStreamCollision"), stdout=frozenset(), stderr=tags("internal stream collision", "error"), warnings=frozenset(), exit_code=1, chm_created=False, impl=("CliOptions.Parse", "HhpProject.Load", "ProjectCompiler.CollectFiles", "ProjectCompiler.BuildMetadata", "ChmWriter.Write", "Program.Main"), specific=('"ReservedInternalStreamCollision" \\in collectionTags', '"ReservedInternalStreamCollision" \\in writerTags', '"internal stream collision" \\in stderr', 'chmCreated = FALSE')),
    collect_exception(95, "Error_OutputOverwriteRejected", "output paths that overwrite project or input files are rejected", "OutputPathCollision", stderr=("output overwrite", "error")),
    UseCase(96, "Files_CaseOnlySourceCollision", "case-only source archive collisions are quiet and keep the active source", collection_tags=tags("DuplicateConflict", "CaseOnlySourceCollision"), archive=tags("topic.html"), impl=COMPILE_IMPL, specific=('"CaseOnlySourceCollision" \\in collectionTags', "warnings = {}", '"topic.html" \\in archive')),
    UseCase(97, "Links_FlatReservedEscapesPreserved", "flat archives preserve payload links with reserved filename escapes", collection_tags=tags("Flat", "FlatRewrite", "ReservedEscapePreserved"), archive=tags("index.html", "C#Guide.html"), metadata=tags("Output:project/help.chm"), impl=FLAT_IMPL, specific=('"ReservedEscapePreserved" \\in collectionTags', '"C#Guide.html" \\in archive', '"LinksRewrittenForFlat:true" \\notin metadata')),
]


def tla_set(values: frozenset[str] | set[str] | tuple[str, ...] | list[str]) -> str:
    vals = sorted(values)
    if not vals:
        return "{}"
    return "{" + ", ".join(f'"{v}"' for v in vals) + "}"


def tla_bool(value: bool) -> str:
    return "TRUE" if value else "FALSE"


def module_text(uc: UseCase) -> str:
    specific = "\n".join(f"    /\\ {line}" for line in (uc.specific or ("TRUE",)))
    return f'''---- MODULE {uc.name} ----
EXTENDS Integers

(*
Scenario: {uc.title}
Implementation slices reflected: {", ".join(uc.impl)}
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

ExpectedCliMode == "{uc.cli_mode}"
ExpectedProjectState == "{uc.project_state}"
ExpectedCollectionTags == {tla_set(uc.collection_tags)}
ExpectedArchive == {tla_set(uc.archive)}
ExpectedMetadata == {tla_set(uc.metadata)}
ExpectedWriterTags == {tla_set(uc.writer_tags)}
ExpectedStdout == {tla_set(uc.stdout)}
ExpectedStderr == {tla_set(uc.stderr)}
ExpectedWarnings == {tla_set(uc.warnings)}
ExpectedExit == {uc.exit_code}
ExpectedChmCreated == {tla_bool(uc.chm_created)}
ExpectedVisited == {tla_set(uc.visited)}

TerminalCliModes == {{"Help", "Version", "ArgError"}}

AllPhases == {{"Start", "CliParsed", "ProjectLoaded", "FilesCollected", "MetadataBuilt", "Done"}}
AllCliModes == {{"Unset", "Help", "Version", "ArgError", "Compile"}}
AllProjectStates == {{"NotLoaded", "Loaded", "Missing", "Skipped"}}
AllCollectionTags == {tla_set(ALL_COLLECTION_TAGS)}
AllArchivePaths == {tla_set(ALL_ARCHIVE_PATHS)}
AllMetadataTags == {tla_set(ALL_METADATA_TAGS)}
AllWriterTags == {tla_set(ALL_WRITER_TAGS)}
AllStdout == {tla_set(ALL_STDOUT)}
AllStderr == {tla_set(ALL_STDERR)}
AllWarnings == {tla_set(ALL_WARNINGS)}
AllVisited == {{"Start", "CliParsed", "ProjectLoaded", "ProjectMissing", "FilesCollected", "MetadataBuilt", "WriterRan", "Done"}}

Init ==
  /\\ phase = "Start"
  /\\ cliMode = "Unset"
  /\\ projectState = "NotLoaded"
  /\\ collectionTags = {{}}
  /\\ archive = {{}}
  /\\ metadata = {{}}
  /\\ writerTags = {{}}
  /\\ stdout = {{}}
  /\\ stderr = {{}}
  /\\ warnings = {{}}
  /\\ exitCode = -1
  /\\ chmCreated = FALSE
  /\\ visited = {{"Start"}}

FinalizeData ==
  /\\ cliMode' = ExpectedCliMode
  /\\ projectState' = ExpectedProjectState
  /\\ collectionTags' = ExpectedCollectionTags
  /\\ archive' = ExpectedArchive
  /\\ metadata' = ExpectedMetadata
  /\\ writerTags' = ExpectedWriterTags
  /\\ stdout' = ExpectedStdout
  /\\ stderr' = ExpectedStderr
  /\\ warnings' = ExpectedWarnings
  /\\ exitCode' = ExpectedExit
  /\\ chmCreated' = ExpectedChmCreated

ParseCli ==
  /\\ phase = "Start"
  /\\ IF ExpectedCliMode \\in TerminalCliModes THEN
       /\\ phase' = "Done"
       /\\ FinalizeData
       /\\ visited' = ExpectedVisited
     ELSE
       /\\ phase' = "CliParsed"
       /\\ cliMode' = ExpectedCliMode
       /\\ visited' = visited \\cup {{"CliParsed"}}
       /\\ UNCHANGED <<projectState, collectionTags, archive, metadata, writerTags, stdout, stderr, warnings, exitCode, chmCreated>>

LoadProject ==
  /\\ phase = "CliParsed"
  /\\ IF ExpectedProjectState = "Missing" THEN
       /\\ phase' = "Done"
       /\\ FinalizeData
       /\\ visited' = ExpectedVisited
     ELSE
       /\\ phase' = "ProjectLoaded"
       /\\ projectState' = "Loaded"
       /\\ visited' = visited \\cup {{"ProjectLoaded"}}
       /\\ UNCHANGED <<cliMode, collectionTags, archive, metadata, writerTags, stdout, stderr, warnings, exitCode, chmCreated>>

CollectFiles ==
  /\\ phase = "ProjectLoaded"
  /\\ IF "CollectFailed" \\in ExpectedCollectionTags THEN
       /\\ phase' = "Done"
       /\\ FinalizeData
       /\\ visited' = ExpectedVisited
     ELSE
       /\\ phase' = "FilesCollected"
       /\\ collectionTags' = ExpectedCollectionTags
       /\\ archive' = ExpectedArchive
       /\\ warnings' = ExpectedWarnings
       /\\ visited' = visited \\cup {{"FilesCollected"}}
       /\\ UNCHANGED <<cliMode, projectState, metadata, writerTags, stdout, stderr, exitCode, chmCreated>>

BuildMetadata ==
  /\\ phase = "FilesCollected"
  /\\ phase' = "MetadataBuilt"
  /\\ metadata' = ExpectedMetadata
  /\\ visited' = visited \\cup {{"MetadataBuilt"}}
  /\\ UNCHANGED <<cliMode, projectState, collectionTags, archive, writerTags, stdout, stderr, warnings, exitCode, chmCreated>>

WriteChm ==
  /\\ phase = "MetadataBuilt"
  /\\ phase' = "Done"
  /\\ FinalizeData
  /\\ visited' = ExpectedVisited

StayDone ==
  /\\ phase = "Done"
  /\\ UNCHANGED vars

Next == ParseCli \\/ LoadProject \\/ CollectFiles \\/ BuildMetadata \\/ WriteChm \\/ StayDone

Spec == Init /\\ [][Next]_vars

TypeOK ==
  /\\ phase \\in AllPhases
  /\\ cliMode \\in AllCliModes
  /\\ projectState \\in AllProjectStates
  /\\ collectionTags \\in SUBSET AllCollectionTags
  /\\ archive \\in SUBSET AllArchivePaths
  /\\ metadata \\in SUBSET AllMetadataTags
  /\\ writerTags \\in SUBSET AllWriterTags
  /\\ stdout \\in SUBSET AllStdout
  /\\ stderr \\in SUBSET AllStderr
  /\\ warnings \\in SUBSET AllWarnings
  /\\ exitCode \\in {{-1, 0, 1, 24}}
  /\\ chmCreated \\in BOOLEAN
  /\\ visited \\in SUBSET AllVisited

FinalOutcome ==
  phase = "Done" =>
    /\\ cliMode = ExpectedCliMode
    /\\ projectState = ExpectedProjectState
    /\\ collectionTags = ExpectedCollectionTags
    /\\ archive = ExpectedArchive
    /\\ metadata = ExpectedMetadata
    /\\ writerTags = ExpectedWriterTags
    /\\ stdout = ExpectedStdout
    /\\ stderr = ExpectedStderr
    /\\ warnings = ExpectedWarnings
    /\\ exitCode = ExpectedExit
    /\\ chmCreated = ExpectedChmCreated
    /\\ visited = ExpectedVisited

StageDiscipline ==
  /\\ phase = "Start" => visited = {{"Start"}}
  /\\ phase = "CliParsed" => visited = {{"Start", "CliParsed"}}
  /\\ phase = "ProjectLoaded" => visited = {{"Start", "CliParsed", "ProjectLoaded"}}
  /\\ phase = "FilesCollected" => visited = {{"Start", "CliParsed", "ProjectLoaded", "FilesCollected"}}
  /\\ phase = "MetadataBuilt" => visited = {{"Start", "CliParsed", "ProjectLoaded", "FilesCollected", "MetadataBuilt"}}

UseCaseSpecificContract ==
  phase = "Done" =>
{specific}

====
'''


def cfg_text() -> str:
    return """SPECIFICATION Spec

INVARIANTS
  TypeOK
  FinalOutcome
  StageDiscipline
  UseCaseSpecificContract
"""


def write_models() -> None:
    OUT.mkdir(parents=True, exist_ok=True)

    for uc in CASES:
        (OUT / f"{uc.name}.tla").write_text(module_text(uc), encoding="utf-8", newline="\n")
        (OUT / f"{uc.name}.cfg").write_text(cfg_text(), encoding="utf-8", newline="\n")

    readme = [
        "# TLA+ Use Case Models",
        "",
        "Generated by `tools/generate_tla_usecase_models.py`.",
        "",
        "Each `UC###_*.tla` module models one Gherkin use case and includes only the implementation stages relevant to that use case.",
        "The staged pipeline is: CLI parsing, HHP loading, file collection/link/path/encoding handling, metadata construction, and CHM writing or the relevant early failure.",
        "",
        "| ID | Module | Use case | Implementation slices |",
        "| --- | --- | --- | --- |",
    ]
    for uc in CASES:
        readme.append(f"| {uc.num:03d} | `{uc.name}` | {uc.title} | {', '.join(uc.impl)} |")
    (OUT / "README.md").write_text("\n".join(readme) + "\n", encoding="utf-8", newline="\n")

    runner = """$ErrorActionPreference = 'Stop'
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Root = (Resolve-Path (Join-Path $ScriptDir '..\\..\\..')).Path
python (Join-Path $Root 'tools\\run_tla_usecase_models.py')
"""
    (OUT / "run_all.ps1").write_text(runner, encoding="utf-8", newline="\n")


if __name__ == "__main__":
    write_models()
    print(f"Wrote {len(CASES)} TLA+ use case models to {OUT}")
