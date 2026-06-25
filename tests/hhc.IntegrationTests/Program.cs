using System.Diagnostics;
using System.Globalization;
using System.Text;
using Komura.Hhc;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var root = FindRepositoryRoot();
var hhcProject = new FileInfo(System.IO.Path.Combine(root.FullName, "src", "hhc", "hhc.csproj"));

if (args is ["--crash-after-temp", var crashOutputPath])
{
    return CrashAfterTempHelper(crashOutputPath);
}

var tests = new (string Name, Action Body)[]
{
    ("small CHM has ITSF/ITSP, internal streams, and PMGL without PMGI", SmallProjectHasHeaderInternalStreamsAndPmglOnly),
    ("large CHM uses PMGI for multiple PMGL blocks", LargeProjectUsesPmgi),
    ("CHM structural header invariants hold", ChmStructuralHeaderInvariantsHold),
    ("CHM directory entries resolve exact user content", ChmDirectoryEntriesResolveExactUserContent),
    ("Japanese Language metadata is stored with CP932 bytes", JapaneseLanguageStoresCp932Metadata),
    ("invalid Language falls back before metadata storage", InvalidLanguageFallsBackBeforeMetadataStorage),
    ("help exits 24 before project loading", HelpExitsUsageBeforeProjectLoading),
    ("version prints usage and exits 24 before project loading", VersionPrintsUsageBeforeProjectLoading),
    ("unknown CLI option exits 24 without compiling", UnknownCliOptionExitsUsage),
    ("missing out value exits 24 without compiling", MissingOutValueExitsUsage),
    ("missing HHP project exits 0 without compiling", MissingProjectExitsZero),
    ("missing required file emits partial CHM", MissingRequiredFileEmitsPartialChm),
    ("allow-missing downgrades required absence to warning", AllowMissingDowngradesRequiredAbsence),
    ("Flat duplicate archive conflict keeps last file", FlatDuplicateConflictKeepsLast),
    ("Flat replacement prunes losing source links", FlatReplacementPrunesLosingSourceLinks),
    ("outside project paths stay inside archive namespace", OutsideProjectPathsStayInsideArchiveNamespace),
    ("outside project basename collisions keep last", OutsideProjectBasenameCollisionsKeepLast),
    ("archive path normalization property seeds never escape", ArchivePathNormalizationPropertySeedsNeverEscape),
    ("generated archive path fuzz seeds never escape", GeneratedArchivePathFuzzSeedsNeverEscape),
    ("dot project path entries are ignored", DotProjectPathEntriesAreIgnored),
    ("link cleaning covers boundary targets", LinkCleaningCoversBoundaryTargets),
    ("project file percent escapes remain literal", ProjectFilePercentEscapesRemainLiteral),
    ("project path entities remain literal", ProjectPathEntitiesRemainLiteral),
    ("HTML base href resolves scanned links", HtmlBaseHrefResolvesScannedLinks),
    ("HTML base href resolves fragment-only links", HtmlBaseHrefResolvesFragmentOnlyLinks),
    ("absolute URI schemes are external links", AbsoluteUriSchemesAreExternalLinks),
    ("decoded NUL links do not abort compile", DecodedNulLinksDoNotAbortCompile),
    ("link scanner extraction seeds cover syntax", LinkScannerExtractionSeedsCoverSyntax),
    ("flat link rewrite seed properties are stable", FlatLinkRewriteSeedPropertiesAreStable),
    ("flat base href scanner flattens archive without rewriting payload", FlatBaseHrefScannerFlattensArchiveWithoutRewritingPayload),
    ("flat rewrite preserves external base references", FlatRewritePreservesExternalBaseReferences),
    ("flat link rewrite decodes encoded separators", FlatLinkRewriteDecodesEncodedSeparators),
    ("flat link rewrite preserves reserved escapes", FlatLinkRewritePreservesReservedEscapes),
    ("omitted Contents file has no generated reserved TOC", OmittedContentsFileHasNoGeneratedReservedToc),
    ("generated flat link rewrite fuzz seeds are idempotent", GeneratedFlatLinkRewriteFuzzSeedsAreIdempotent),
    ("link scanner read failure is absorbed", LinkScannerReadFailureIsAbsorbed),
    ("HHP parser boundary options are stable", HhpParserBoundaryOptionsAreStable),
    ("HHP parser encoding and line ending seeds are stable", HhpParserEncodingAndLineEndingSeedsAreStable),
    ("encoding detector fallback seeds are stable", EncodingDetectorFallbackSeedsAreStable),
    ("generated invalid UTF-8 fallback seeds select fallback", GeneratedInvalidUtf8FallbackSeedsSelectFallback),
    ("UTF-16 HHP project compiles", Utf16ProjectCompiles),
    ("flat UTF-16 rewrite preserves BOM", FlatUtf16RewritePreservesBom),
    ("no-link-scan skips optional linked missing files", NoLinkScanSkipsOptionalLinkedMissingFiles),
    ("unsupported HHW features warn but succeed", UnsupportedHhwFeaturesWarnButSucceed),
    ("omitted Contents file does not generate TOC", OmittedContentsFileDoesNotGenerateToc),
    ("internal stream archive path collision exits 1", InternalStreamArchivePathCollisionExitsOne),
    ("output path cannot overwrite project or input files", OutputPathCannotOverwriteProjectOrInputFiles),
    ("output path cannot overwrite replaced flat collision source", OutputPathCannotOverwriteReplacedFlatCollisionSource),
    ("case-only archive source collision is quiet", CaseOnlyArchiveSourceCollisionIsQuiet),
    ("unwritable output target exits 1 without CHM creation", UnwritableOutputTargetExitsOne),
    ("locked input file read exits 1 before creating CHM", LockedInputFileReadExitsOne),
    ("locked output file create exits 1 without overwriting existing file", LockedOutputFileCreateExitsOne),
    ("stale temp output does not block next compile", StaleTempOutputDoesNotBlockNextCompile),
    ("process death after temp creation preserves existing output", ProcessDeathAfterTempCreationPreservesExistingOutput),
    ("concurrent writers leave a valid final output", ConcurrentWritersLeaveValidFinalOutput),
    ("cross-process same-output compiles leave a valid final output", CrossProcessSameOutputCompilesLeaveValidFinalOutput),
    ("temp write failure preserves existing CHM output", TempWriteFailurePreservesExistingOutput),
    ("publish failure after temp staging cleans temp", PublishFailureAfterTempStagingCleansTemp),
    ("oversized metadata entry exits 1 before creating CHM", OversizedMetadataEntryExitsOne),
    ("oversized directory entry fails before publishing output", OversizedDirectoryEntryFailsBeforePublishingOutput),
    ("aggregate directory index too large fails before publishing output", AggregateDirectoryIndexTooLargeFailsBeforePublishingOutput),
};

var failures = new List<string>();
foreach (var test in tests)
{
    try
    {
        test.Body();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{test.Name}: {ex.Message}");
        Console.Error.WriteLine($"FAIL {test.Name}");
        Console.Error.WriteLine(ex);
    }
}

if (failures.Count > 0)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine($"{failures.Count} integration test(s) failed.");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"- {failure}");
    }

    return 1;
}

Console.WriteLine($"All {tests.Length} integration tests passed.");
return 0;

void SmallProjectHasHeaderInternalStreamsAndPmglOnly()
{
    using var project = TempProject.Create();
    WriteStandardProject(project, "help.chm");

    var result = RunHhc(project.File("help.hhp").FullName);
    AssertEqual(1, result.ExitCode, result.ToString());

    var chmPath = project.File("help.chm");
    AssertFileExists(chmPath);
    var bytes = File.ReadAllBytes(chmPath.FullName);

    AssertAsciiAt(bytes, 0, "ITSF");
    AssertContainsAscii(bytes, "ITSP");
    AssertContainsAscii(bytes, "PMGL");
    AssertNotContainsAscii(bytes, "PMGI");
    AssertChmStructure(bytes, expectPmgi: false);
    AssertContainsAscii(bytes, "::DataSpace/NameList");
    AssertContainsAscii(bytes, "/#SYSTEM");
    AssertContainsAscii(bytes, "/#WINDOWS");
    AssertContainsAscii(bytes, "/#STRINGS");
    AssertContainsAscii(bytes, "/#ITBITS");
    AssertContainsAscii(bytes, "/index.html");
    AssertContainsAscii(bytes, "/toc.hhc");
    AssertContainsAscii(bytes, "/index.hhk");
}

void LargeProjectUsesPmgi()
{
    using var project = TempProject.Create();
    var files = Enumerable.Range(0, 220)
        .Select(i => $"topics/topic-{i:000}.html")
        .ToList();

    foreach (var file in files)
    {
        project.WriteText(file, $"<html><body>Topic {file}</body></html>");
    }

    var hhp = new StringBuilder();
    hhp.AppendLine("[OPTIONS]");
    hhp.AppendLine("Compiled file=large.chm");
    hhp.AppendLine("Title=Large Project");
    hhp.AppendLine("[FILES]");
    foreach (var file in files)
    {
        hhp.AppendLine(file);
    }

    project.WriteText("help.hhp", hhp.ToString());

    var result = RunHhc(project.File("help.hhp").FullName);
    AssertEqual(1, result.ExitCode, result.ToString());

    var bytes = File.ReadAllBytes(project.File("large.chm").FullName);
    AssertAsciiAt(bytes, 0, "ITSF");
    AssertContainsAscii(bytes, "PMGL");
    AssertContainsAscii(bytes, "PMGI");
    AssertChmStructure(bytes, expectPmgi: true);
}

void ChmStructuralHeaderInvariantsHold()
{
    using var project = TempProject.Create();
    var files = Enumerable.Range(0, 220)
        .Select(i => $"topics/struct-{i:000}.html")
        .ToList();

    foreach (var file in files)
    {
        project.WriteText(file, $"<html><body>Structure {file}</body></html>");
    }

    var hhp = new StringBuilder();
    hhp.AppendLine("[OPTIONS]");
    hhp.AppendLine("Compiled file=structure.chm");
    hhp.AppendLine("Title=Structure Project");
    hhp.AppendLine("[FILES]");
    foreach (var file in files)
    {
        hhp.AppendLine(file);
    }

    project.WriteText("help.hhp", hhp.ToString());

    var result = RunHhc(project.File("help.hhp").FullName);
    AssertEqual(1, result.ExitCode, result.ToString());

    var bytes = File.ReadAllBytes(project.File("structure.chm").FullName);
    AssertChmStructure(bytes, expectPmgi: true);
    AssertContainsAscii(bytes, "/topics/struct-000.html");
    AssertContainsAscii(bytes, "/topics/struct-219.html");
    var entries = ReadChmUncompressedEntries(bytes);
    AssertBytesEqual(
        Encoding.UTF8.GetBytes("<html><body>Structure topics/struct-000.html</body></html>"),
        entries["/topics/struct-000.html"],
        "Decoded first PMGI-backed file payload mismatch.");
    AssertBytesEqual(
        Encoding.UTF8.GetBytes("<html><body>Structure topics/struct-219.html</body></html>"),
        entries["/topics/struct-219.html"],
        "Decoded last PMGI-backed file payload mismatch.");
}

void ChmDirectoryEntriesResolveExactUserContent()
{
    using var project = TempProject.Create();
    var index = Encoding.UTF8.GetBytes("<html><body>Exact index payload</body></html>");
    var binary = new byte[] { 0, 1, 2, 3, 0x7F, 0x80, 0xFE, 0xFF, 10, 13 };

    project.WriteBytes("index.html", index);
    project.WriteBytes("assets/data.bin", binary);
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=exact-content.chm",
            "Title=Exact Content",
            "[FILES]",
            "index.html",
            "assets/data.bin",
            string.Empty));

    var result = RunHhc(project.File("help.hhp").FullName);
    AssertEqual(1, result.ExitCode, result.ToString());

    var entries = ReadChmUncompressedEntries(File.ReadAllBytes(project.File("exact-content.chm").FullName));
    AssertBytesEqual(index, entries["/index.html"], "Decoded /index.html payload mismatch.");
    AssertBytesEqual(binary, entries["/assets/data.bin"], "Decoded /assets/data.bin payload mismatch.");
    AssertEqual(true, entries.ContainsKey("/#SYSTEM"), "Decoded CHM entries should include /#SYSTEM.");
    AssertEqual(true, entries.ContainsKey("::DataSpace/NameList"), "Decoded CHM entries should include ::DataSpace/NameList.");
}

void JapaneseLanguageStoresCp932Metadata()
{
    using var project = TempProject.Create();
    var cp932 = Encoding.GetEncoding(932);
    var title = "\u65E5\u672C\u8A9E\u30BF\u30A4\u30C8\u30EB";

    project.WriteText("index.html", "<html><body>Japanese metadata</body></html>");
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=jp.chm",
            "Language=0x0411 Japanese",
            $"Title={title}",
            "Default topic=index.html",
            "[FILES]",
            "index.html",
            string.Empty),
        cp932);

    var result = RunHhc(project.File("help.hhp").FullName);
    AssertEqual(1, result.ExitCode, result.ToString());

    var bytes = File.ReadAllBytes(project.File("jp.chm").FullName);
    AssertAsciiAt(bytes, 0, "ITSF");
    AssertContainsBytes(bytes, cp932.GetBytes(title), "CP932 title bytes");
}

void HelpExitsUsageBeforeProjectLoading()
{
    using var project = TempProject.Create();
    WriteStandardProject(project, "help.chm");

    var result = RunHhc("--help", project.File("missing.hhp").FullName);

    AssertEqual(24, result.ExitCode, result.ToString());
    AssertContainsText(result.Stdout, "Usage:");
    AssertFileDoesNotExist(project.File("help.chm"));
}

void VersionPrintsUsageBeforeProjectLoading()
{
    using var project = TempProject.Create();
    WriteStandardProject(project, "help.chm");

    var result = RunHhc("--version", project.File("missing.hhp").FullName);

    AssertEqual(24, result.ExitCode, result.ToString());
    AssertContainsText(result.Stdout, VersionInfo.Version);
    AssertFileDoesNotExist(project.File("help.chm"));
}

void UnknownCliOptionExitsUsage()
{
    using var project = TempProject.Create();
    WriteStandardProject(project, "help.chm");

    var result = RunHhc("--unknown", project.File("help.hhp").FullName);

    AssertEqual(24, result.ExitCode, result.ToString());
    AssertContainsText(result.Stdout, "Usage:");
    AssertNotContainsText(result.Stderr, "unknown option");
    AssertFileDoesNotExist(project.File("help.chm"));
}

void MissingOutValueExitsUsage()
{
    using var project = TempProject.Create();
    WriteStandardProject(project, "help.chm");

    var result = RunHhc("--out");

    AssertEqual(24, result.ExitCode, result.ToString());
    AssertContainsText(result.Stdout, "Usage:");
    AssertNotContainsText(result.Stderr, "--out requires a path");
    AssertFileDoesNotExist(project.File("help.chm"));
}

void MissingProjectExitsZero()
{
    using var project = TempProject.Create();
    var result = RunHhc(project.File("missing.hhp").FullName);

    AssertEqual(0, result.ExitCode, result.ToString());
    AssertContainsText(result.Stderr, "Unable to open");
    AssertNotContainsText(result.Stderr, "Project file not found");
    AssertFileDoesNotExist(project.File("missing.chm"));
}

void MissingRequiredFileEmitsPartialChm()
{
    using var project = TempProject.Create();
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=missing-required.chm",
            "[FILES]",
            "missing.html",
            string.Empty));

    var result = RunHhc(project.File("help.hhp").FullName);

    AssertEqual(0, result.ExitCode, result.ToString());
    AssertContainsText(result.Stderr, "HHC5003");
    AssertContainsText(result.Stderr, "missing.html");
    AssertFileExists(project.File("missing-required.chm"));
    var bytes = File.ReadAllBytes(project.File("missing-required.chm").FullName);
    AssertNotContainsAscii(bytes, "/missing.html");
}

void AllowMissingDowngradesRequiredAbsence()
{
    using var project = TempProject.Create();
    project.WriteText("index.html", "<html><body>Present</body></html>");
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=allow-missing.chm",
            "[FILES]",
            "index.html",
            "missing.html",
            string.Empty));

    var result = RunHhc(project.File("help.hhp").FullName, "--allow-missing");

    AssertEqual(0, result.ExitCode, result.ToString());
    AssertContainsText(result.Stderr, "HHC5003");
    var bytes = File.ReadAllBytes(project.File("allow-missing.chm").FullName);
    AssertContainsAscii(bytes, "/index.html");
    AssertNotContainsAscii(bytes, "/missing.html");
}

void FlatDuplicateConflictKeepsLast()
{
    using var project = TempProject.Create();
    project.WriteText("a/index.html", "<html><body>FIRST</body></html>");
    project.WriteText("b/index.html", "<html><body>SECOND</body></html>");
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=flat-duplicate.chm",
            "Flat=Yes",
            "[FILES]",
            "a/index.html",
            "b/index.html",
            string.Empty));

    var result = RunHhc(project.File("help.hhp").FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    AssertNotContainsText(result.Stderr, "duplicate archive path");
    var bytes = File.ReadAllBytes(project.File("flat-duplicate.chm").FullName);
    AssertContainsAscii(bytes, "/index.html");
    AssertContainsAscii(bytes, "SECOND");
    AssertNotContainsAscii(bytes, "FIRST");
}

void FlatReplacementPrunesLosingSourceLinks()
{
    using var project = TempProject.Create();
    project.WriteText("a/page.html", "<html><body>LOSING<img src=\"old-only.png\"><a href=\"../b/page.html\">replacement</a></body></html>");
    project.WriteText("a/old-only.png", "OLD ONLY");
    project.WriteText("b/page.html", "<html><body>WINNING<img src=\"new-only.png\"></body></html>");
    project.WriteText("b/new-only.png", "NEW ONLY");
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=flat-replacement.chm",
            "Flat=Yes",
            "[FILES]",
            "a/page.html",
            string.Empty));

    var result = RunHhc(project.File("help.hhp").FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    var entries = ReadChmUncompressedEntries(File.ReadAllBytes(project.File("flat-replacement.chm").FullName));
    AssertEqual(true, entries.ContainsKey("/page.html"), "Replacing flat page should remain under the collided archive name.");
    AssertContainsBytes(entries["/page.html"], Encoding.UTF8.GetBytes("WINNING"), "winning replacement page");
    AssertEqual(true, entries.ContainsKey("/new-only.png"), "Replacement page links should be collected.");
    AssertEqual(false, entries.ContainsKey("/old-only.png"), "Links reachable only from the replaced page should be pruned.");
}

void OutsideProjectPathsStayInsideArchiveNamespace()
{
    using var project = TempProject.Create();
    var outsideDir = Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hhc-outside-" + Guid.NewGuid().ToString("N")));

    try
    {
        var relativeOutside = System.IO.Path.Combine(outsideDir.FullName, "outside-relative.html");
        var absoluteOutside = System.IO.Path.Combine(outsideDir.FullName, "outside-absolute.html");
        System.IO.File.WriteAllText(relativeOutside, "<html><body>RELATIVE OUTSIDE</body></html>");
        System.IO.File.WriteAllText(absoluteOutside, "<html><body>ABSOLUTE OUTSIDE</body></html>");

        project.WriteText("index.html", "<html><body>Inside</body></html>");
        project.WriteText(
            "help.hhp",
            string.Join(
                "\r\n",
                "[OPTIONS]",
                "Compiled file=outside-paths.chm",
                "[FILES]",
                "index.html",
                System.IO.Path.GetRelativePath(project.Path.FullName, relativeOutside),
                absoluteOutside,
                string.Empty));

        var result = RunHhc(project.File("help.hhp").FullName);

        AssertEqual(1, result.ExitCode, result.ToString());
        var bytes = System.IO.File.ReadAllBytes(project.File("outside-paths.chm").FullName);
        AssertContainsAscii(bytes, "/outside-relative.html");
        AssertContainsAscii(bytes, "/outside-absolute.html");
        AssertContainsAscii(bytes, "RELATIVE OUTSIDE");
        AssertContainsAscii(bytes, "ABSOLUTE OUTSIDE");
        AssertNotContainsAscii(bytes, outsideDir.Name);
        AssertNotContainsAscii(bytes, "../");
    }
    finally
    {
        outsideDir.Delete(recursive: true);
    }
}

void OutsideProjectBasenameCollisionsKeepLast()
{
    using var project = TempProject.Create();
    var outsideOne = Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hhc-outside-one-" + Guid.NewGuid().ToString("N")));
    var outsideTwo = Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hhc-outside-two-" + Guid.NewGuid().ToString("N")));

    try
    {
        var first = System.IO.Path.Combine(outsideOne.FullName, "shared.html");
        var second = System.IO.Path.Combine(outsideTwo.FullName, "shared.html");
        System.IO.File.WriteAllText(first, "<html><body>FIRST OUTSIDE</body></html>");
        System.IO.File.WriteAllText(second, "<html><body>SECOND OUTSIDE</body></html>");

        project.WriteText(
            "help.hhp",
            string.Join(
                "\r\n",
                "[OPTIONS]",
                "Compiled file=outside-collision.chm",
                "[FILES]",
                first,
                second,
                string.Empty));

        var result = RunHhc(project.File("help.hhp").FullName);

        AssertEqual(1, result.ExitCode, result.ToString());
        AssertNotContainsText(result.Stderr, "duplicate archive path");
        var bytes = System.IO.File.ReadAllBytes(project.File("outside-collision.chm").FullName);
        AssertContainsAscii(bytes, "/shared.html");
        AssertContainsAscii(bytes, "SECOND OUTSIDE");
        AssertNotContainsAscii(bytes, "FIRST OUTSIDE");
        AssertNotContainsAscii(bytes, outsideOne.Name);
        AssertNotContainsAscii(bytes, outsideTwo.Name);
    }
    finally
    {
        outsideOne.Delete(recursive: true);
        outsideTwo.Delete(recursive: true);
    }
}

void ArchivePathNormalizationPropertySeedsNeverEscape()
{
    var seeds = new[]
    {
        "",
        ".",
        "./",
        "../",
        "../safe/topic.html",
        "../../safe/topic.html",
        "topics/../index.html",
        "topics/./intro.html",
        "/rooted/topic.html",
        @"..\sibling\topic.html",
        @"C:\outside\topic.html",
        "a//b///c.html",
    };

    foreach (var seed in seeds)
    {
        var normalized = ArchivePath.NormalizeForArchive(seed, flat: false);
        AssertNoEscapingArchivePath(normalized, seed);
        AssertEqual(
            normalized,
            ArchivePath.NormalizeForArchive(normalized, flat: false),
            $"Archive path normalization should be idempotent for seed '{seed}'.");

        var flat = ArchivePath.NormalizeForArchive(seed, flat: true);
        AssertNoEscapingArchivePath(flat, seed + " (flat)");
        AssertEqual(
            flat,
            ArchivePath.NormalizeForArchive(flat, flat: true),
            $"Flat archive path normalization should be idempotent for seed '{seed}'.");
        if (flat.Contains('/'))
        {
            throw new InvalidOperationException($"Flat archive path retained a directory separator for seed '{seed}': {flat}");
        }
    }
}

void GeneratedArchivePathFuzzSeedsNeverEscape()
{
    var atoms = new[]
    {
        "", ".", "..", "alpha", "beta", "space name", "C:", "drive", "aux", "trailing.", "multi.part", "%2e%2e"
    };
    var separators = new[] { "/", "\\", "//", "\\\\" };

    for (var i = 0; i < 512; i++)
    {
        var partCount = 1 + i % 6;
        var raw = new StringBuilder();
        if (i % 5 == 0)
        {
            raw.Append('/');
        }
        else if (i % 7 == 0)
        {
            raw.Append("./");
        }
        else if (i % 11 == 0)
        {
            raw.Append("../");
        }

        for (var part = 0; part < partCount; part++)
        {
            if (part > 0)
            {
                raw.Append(separators[(i + part) % separators.Length]);
            }

            raw.Append(atoms[(i * 17 + part * 7) % atoms.Length]);
        }

        var seed = raw.ToString();
        var normalized = ArchivePath.NormalizeForArchive(seed, flat: false);
        AssertNoEscapingArchivePath(normalized, seed);
        AssertEqual(
            normalized,
            ArchivePath.NormalizeForArchive(normalized, flat: false),
            $"Archive path normalization should be idempotent for generated seed '{seed}'.");

        var flat = ArchivePath.NormalizeForArchive(seed, flat: true);
        AssertNoEscapingArchivePath(flat, seed + " (flat)");
        AssertEqual(
            flat,
            ArchivePath.NormalizeForArchive(flat, flat: true),
            $"Flat archive path normalization should be idempotent for generated seed '{seed}'.");
        if (flat.Contains('/'))
        {
            throw new InvalidOperationException($"Flat archive path retained a directory separator for generated seed '{seed}': {flat}");
        }
    }
}

void LinkCleaningCoversBoundaryTargets()
{
    AssertNull(ArchivePath.CleanLink("#fragment-only"), "Fragment-only links should be ignored.");
    AssertNull(ArchivePath.CleanLink("https://example.test/help.html"), "HTTP links should be ignored.");
    AssertNull(ArchivePath.CleanLink("//cdn.example.test/site.css"), "Protocol-relative links should be ignored.");
    AssertNull(ArchivePath.CleanLink(@"\\server\share\topic.html"), "UNC links should be ignored.");
    AssertEqual(
        System.IO.Path.Combine("topics", "file name.html"),
        ArchivePath.CleanLink("topics/file%20name.html?query=1#fragment"),
        "Local links should be decoded after query/fragment removal.");
    AssertEqual("bad%zz.html", ArchivePath.CleanLink("bad%zz.html"), "Malformed percent escapes should keep original spelling.");
    AssertEqual("safe/topic.html", ArchivePath.NormalizeForArchive("../safe/./topic.html", flat: false), "Leading parent segments must not escape the archive namespace.");
}

void LinkScannerExtractionSeedsCoverSyntax()
{
    using var project = TempProject.Create();
    project.WriteText(
        "links.html",
        """
        <html><head>
        <LINK HREF='styles/site.css'>
        </head><body>
        <img SRC=images/logo.png>
        <a href = "topics/intro.html?x=1#frag">Intro</a>
        <object type="text/sitemap"><param value='toc/topic.html' name='Local'></object>
        <object type="text/sitemap"><param name='Name' value='Not local'></object>
        </body></html>
        """);
    project.WriteText(
        "styles/site.css",
        """
        @import "base/reset.css";
        body { background: url('../images/bg.png#hash'); }
        .icon { background: url(https://example.test/icon.png); }
        """);

    var htmlLinks = LinkScanner.ExtractLinks(project.File("links.html").FullName, Encoding.UTF8);
    AssertSequenceContains(htmlLinks, "styles/site.css", "HTML link extraction should include single-quoted href.");
    AssertSequenceContains(htmlLinks, "images/logo.png", "HTML link extraction should include unquoted uppercase src.");
    AssertSequenceContains(htmlLinks, "topics/intro.html?x=1#frag", "HTML link extraction should preserve raw query and fragment before cleanup.");
    AssertSequenceContains(htmlLinks, "toc/topic.html", "HHC/HHK Local param extraction should be attribute-order independent.");
    AssertEqual(false, htmlLinks.Contains("Not local", StringComparer.Ordinal), "Non-Local param values should not be extracted.");

    var cssLinks = LinkScanner.ExtractLinks(project.File("styles/site.css").FullName, Encoding.UTF8);
    AssertSequenceContains(cssLinks, "base/reset.css", "CSS @import extraction should include quoted imports.");
    AssertSequenceContains(cssLinks, "../images/bg.png#hash", "CSS url extraction should include local urls.");
    AssertSequenceContains(cssLinks, "https://example.test/icon.png", "Extraction keeps external urls for later cleanup.");
    AssertNull(ArchivePath.CleanLink("https://example.test/icon.png"), "External CSS links should be ignored at cleanup.");
}

void FlatLinkRewriteSeedPropertiesAreStable()
{
    var text =
        """
        <a href="topics/intro.html?x=1#frag">Intro</a>
        <img src='images/logo.png'>
        <object type="text/sitemap"><param name="Local" value="toc/topics/topic.html#anchor"></object>
        @import "styles/base.css";
        body { background: url('../images/bg.png'); }
        a { background: url(https://example.test/remote.png); }
        """;

    var rewritten = LinkScanner.RewriteLinksForFlatArchive(text);

    AssertContainsText(rewritten, "href=\"intro.html?x=1#frag\"");
    AssertContainsText(rewritten, "src='logo.png'");
    AssertContainsText(rewritten, "value=\"topic.html#anchor\"");
    AssertContainsText(rewritten, "@import \"base.css\"");
    AssertContainsText(rewritten, "url('bg.png')");
    AssertContainsText(rewritten, "url(https://example.test/remote.png)");
    AssertEqual(rewritten, LinkScanner.RewriteLinksForFlatArchive(rewritten), "Flat link rewriting should be idempotent.");
}

void GeneratedFlatLinkRewriteFuzzSeedsAreIdempotent()
{
    var suffixes = new[] { "", "?x=1", "#frag", "?x=1#frag" };
    for (var i = 0; i < 128; i++)
    {
        var fileName = $"file-{i}.html";
        var target = i % 2 == 0
            ? $"dir{i % 9}/sub{(i * 3) % 7}/{fileName}"
            : $@"dir{i % 9}\sub{(i * 3) % 7}\{fileName}";
        var suffix = suffixes[i % suffixes.Length];
        var fullTarget = target + suffix;
        var text = string.Join(
            "\n",
            $"<a href=\"{fullTarget}\">generated</a>",
            $"<img src='{fullTarget}'>",
            $"<object type=\"text/sitemap\"><param name=\"Local\" value=\"{fullTarget}\"></object>",
            $"@import \"{fullTarget}\";",
            $"body {{ background: url('{fullTarget}'); }}");

        var rewritten = LinkScanner.RewriteLinksForFlatArchive(text);
        AssertEqual(rewritten, LinkScanner.RewriteLinksForFlatArchive(rewritten), $"Flat rewrite should be idempotent for generated target '{fullTarget}'.");
        AssertContainsText(rewritten, fileName + suffix);
        AssertNotContainsText(rewritten, $"dir{i % 9}/sub{(i * 3) % 7}/");
        AssertNotContainsText(rewritten, $@"dir{i % 9}\sub{(i * 3) % 7}\");
    }
}

void LinkScannerReadFailureIsAbsorbed()
{
    using var project = TempProject.Create();
    project.WriteText("locked.html", "<html><body><a href=\"later.html\">later</a></body></html>");

    using var locked = new FileStream(
        project.File("locked.html").FullName,
        FileMode.Open,
        FileAccess.ReadWrite,
        FileShare.None);

    var links = LinkScanner.ExtractLinks(project.File("locked.html").FullName, Encoding.UTF8);

    AssertEqual(0, links.Count, "Link scanner read failures should be absorbed as no outgoing links.");
}

void HhpParserBoundaryOptionsAreStable()
{
    using var project = TempProject.Create();
    project.WriteText(
        "parser.hhp",
        string.Join(
            "\r\n",
            "ignored before section",
            "[OPTIONS]",
            "Title=First Title",
            "Title=\"Second Title\"",
            "Compiled file=",
            "Flat=on",
            "Default Window=\"unterminated",
            "[UNKNOWN]",
            "kept line",
            "[FILES]",
            "\"index.html\"",
            string.Empty));

    var parsed = HhpProject.Load(project.File("parser.hhp").FullName);

    AssertEqual("Second Title", parsed.Option("Title"), "Duplicate HHP options should keep the last value.");
    AssertNull(parsed.Option("Compiled file"), "Blank option values should behave as omitted.");
    AssertEqual(true, parsed.OptionIsYes("Flat"), "Flat=on should be truthy.");
    AssertEqual("\"unterminated", parsed.Option("Default Window"), "Unbalanced quotes should remain literal.");
    AssertEqual("index.html", parsed.Files.Single(), "Quoted section lines should be unquoted.");
    AssertEqual(true, parsed.Sections.ContainsKey("UNKNOWN"), "Unknown sections should be retained for warning decisions.");
}

void HhpParserEncodingAndLineEndingSeedsAreStable()
{
    using var project = TempProject.Create();
    var cp932 = Encoding.GetEncoding(932);
    var title = "\u65E5\u672C\u8A9E\u30BF\u30A4\u30C8\u30EB";
    project.WriteText(
        "cp932.hhp",
        "[OPTIONS]\rLanguage=0411 Japanese\rTitle=" + title + "\r[FILES]\r\"index.html\"\r",
        cp932);

    var cp932Project = HhpProject.Load(project.File("cp932.hhp").FullName);
    AssertEqual(932, cp932Project.TextEncoding.CodePage, "Language=0411 should select CP932 for invalid UTF-8 bytes.");
    AssertEqual(title, cp932Project.Option("Title"), "Declared language decoding should preserve CP932 title text.");
    AssertEqual("index.html", cp932Project.Files.Single(), "CR-only line endings should be parsed.");

    project.WriteText(
        "utf16-language.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Language=0x0411 Japanese",
            "Title=UTF16 Wins",
            "[FILES]",
            "index.html",
            string.Empty),
        Encoding.Unicode);

    var utf16Project = HhpProject.Load(project.File("utf16-language.hhp").FullName);
    AssertEqual(Encoding.Unicode.CodePage, utf16Project.TextEncoding.CodePage, "UTF-16 BOM should win over declared language.");
    AssertEqual("UTF16 Wins", utf16Project.Option("Title"), "UTF-16 title should parse after BOM detection.");
}

void EncodingDetectorFallbackSeedsAreStable()
{
    using var project = TempProject.Create();
    var cp932 = Encoding.GetEncoding(932);
    var title = "\u65E5\u672C\u8A9E\u30BF\u30A4\u30C8\u30EB";

    project.WriteBytes("invalid-utf8-cp932.txt", cp932.GetBytes(title));
    var cp932Text = TextEncodingDetector.Read(project.File("invalid-utf8-cp932.txt").FullName, cp932);
    AssertEqual(932, cp932Text.Encoding.CodePage, "Invalid UTF-8 with CP932 fallback should select CP932.");
    AssertEqual(title, cp932Text.Text, "Invalid UTF-8 with CP932 fallback should decode using CP932.");

    project.WriteBytes("utf8-bom.txt", new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes("BOM text")).ToArray());
    var utf8Bom = TextEncodingDetector.Read(project.File("utf8-bom.txt").FullName, cp932);
    AssertEqual(Encoding.UTF8.CodePage, utf8Bom.Encoding.CodePage, "UTF-8 BOM should win over fallback encoding.");
    AssertEqual("BOM text", utf8Bom.Text, "UTF-8 BOM should be trimmed from text.");

    project.WriteBytes("invalid-utf8-ansi.txt", new byte[] { 0x80, 0x81, 0x82 });
    var ansiText = TextEncodingDetector.Read(project.File("invalid-utf8-ansi.txt").FullName);
    AssertEqual(TextEncodingDetector.AnsiEncoding.CodePage, ansiText.Encoding.CodePage, "Invalid UTF-8 without fallback should use current ANSI encoding.");
}

void GeneratedInvalidUtf8FallbackSeedsSelectFallback()
{
    using var project = TempProject.Create();
    var cp932 = Encoding.GetEncoding(932);
    for (var i = 0; i < 64; i++)
    {
        var bytes = new byte[]
        {
            0x80,
            (byte)i,
            0xC0,
            0xAF,
            (byte)(0x81 + i % 32),
            (byte)(0x40 + i % 64)
        };
        var relative = $"invalid-{i:00}.bin";
        project.WriteBytes(relative, bytes);

        var decoded = TextEncodingDetector.Read(project.File(relative).FullName, cp932);
        AssertEqual(932, decoded.Encoding.CodePage, $"Invalid UTF-8 seed {i} should select CP932 fallback.");
    }
}

void Utf16ProjectCompiles()
{
    using var project = TempProject.Create();
    project.WriteText("index.html", "<html><body>UTF-16 project</body></html>");
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=utf16.chm",
            "Title=UTF16 Title",
            "[FILES]",
            "index.html",
            string.Empty),
        Encoding.Unicode);

    var result = RunHhc(project.File("help.hhp").FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    var bytes = System.IO.File.ReadAllBytes(project.File("utf16.chm").FullName);
    AssertContainsAscii(bytes, "/index.html");
    AssertContainsAscii(bytes, "UTF16 Title");
}

void NoLinkScanSkipsOptionalLinkedMissingFiles()
{
    using var project = TempProject.Create();
    project.WriteText("index.html", "<html><body><a href=\"missing-linked.html\">missing</a></body></html>");
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=no-link-scan.chm",
            "[FILES]",
            "index.html",
            string.Empty));

    var result = RunHhc(project.File("help.hhp").FullName, "--no-link-scan");

    AssertEqual(1, result.ExitCode, result.ToString());
    AssertNotContainsText(result.Stderr, "missing-linked.html");
    var bytes = System.IO.File.ReadAllBytes(project.File("no-link-scan.chm").FullName);
    AssertNotContainsAscii(bytes, "/missing-linked.html");
}

void UnsupportedHhwFeaturesWarnButSucceed()
{
    using var project = TempProject.Create();
    project.WriteText("index.html", "<html><body>Unsupported feature warnings</body></html>");
    project.WriteText("toc.hhc", "<html><body>TOC</body></html>");
    project.WriteText("index.hhk", "<html><body>Index</body></html>");
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=unsupported.chm",
            "Contents file=toc.hhc",
            "Index file=index.hhk",
            "Full-text search=Yes",
            "Binary TOC=Yes",
            "Binary Index=Yes",
            "[FILES]",
            "index.html",
            "[MERGE FILES]",
            "other.chm",
            "[WINDOWS]",
            "main=\"Main\"",
            string.Empty));

    var result = RunHhc(project.File("help.hhp").FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    AssertContainsText(result.Stderr, "Full-text search index generation is not implemented");
    AssertContainsText(result.Stderr, "Binary TOC is not implemented");
    AssertContainsText(result.Stderr, "Binary Index is not implemented");
    AssertContainsText(result.Stderr, "[MERGE FILES] is preserved only as project metadata");
    AssertContainsText(result.Stderr, "[WINDOWS] custom settings are not parsed");
    AssertFileExists(project.File("unsupported.chm"));
}

void UnwritableOutputTargetExitsOne()
{
    using var project = TempProject.Create();
    WriteStandardProject(project, "should-not-be-created.chm");

    var result = RunHhc(project.File("help.hhp").FullName, "--out", project.Path.FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    AssertContainsText(result.Stderr, "error:");
    AssertFileDoesNotExist(project.File("should-not-be-created.chm"));
}

void LockedInputFileReadExitsOne()
{
    using var project = TempProject.Create();
    project.WriteText("asset.bin", "locked input");
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=locked-input.chm",
            "[FILES]",
            "asset.bin",
            string.Empty));

    using var locked = new FileStream(
        project.File("asset.bin").FullName,
        FileMode.Open,
        FileAccess.ReadWrite,
        FileShare.None);

    var result = RunHhc(project.File("help.hhp").FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    AssertContainsText(result.Stderr, "error:");
    AssertFileDoesNotExist(project.File("locked-input.chm"));
}

void LockedOutputFileCreateExitsOne()
{
    using var project = TempProject.Create();
    WriteStandardProject(project, "locked-output.chm");
    var original = Encoding.ASCII.GetBytes("existing output");
    project.WriteBytes("locked-output.chm", original);

    using var locked = new FileStream(
        project.File("locked-output.chm").FullName,
        FileMode.Open,
        FileAccess.ReadWrite,
        FileShare.None);

    var result = RunHhc(project.File("help.hhp").FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    AssertContainsText(result.Stderr, "error:");
    locked.Position = 0;
    var current = new byte[original.Length];
    var read = locked.Read(current, 0, current.Length);
    AssertEqual(original.Length, read, "Locked output file length changed.");
    AssertBytesEqual(original, current, "Locked output file contents changed.");
}

void StaleTempOutputDoesNotBlockNextCompile()
{
    using var project = TempProject.Create();
    WriteStandardProject(project, "stale-temp.chm");
    var staleTemp = project.File(".stale-temp.chm.00000000000000000000000000000000.tmp");
    project.WriteBytes(staleTemp.Name, Encoding.ASCII.GetBytes("stale temp"));

    var result = RunHhc(project.File("help.hhp").FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    AssertFileExists(project.File("stale-temp.chm"));
    AssertFileExists(staleTemp);
    AssertChmStructure(File.ReadAllBytes(project.File("stale-temp.chm").FullName), expectPmgi: false);
    AssertBytesEqual(Encoding.ASCII.GetBytes("stale temp"), File.ReadAllBytes(staleTemp.FullName), "Existing stale temp file changed.");
}

void ProcessDeathAfterTempCreationPreservesExistingOutput()
{
    using var project = TempProject.Create();
    var outputPath = project.File("crash-after-temp.chm");
    var original = Encoding.ASCII.GetBytes("existing output");
    project.WriteBytes(outputPath.Name, original);

    var testDll = System.IO.Path.Combine(AppContext.BaseDirectory, "hhc.IntegrationTests.dll");
    AssertFileExists(new FileInfo(testDll));

    var startInfo = new ProcessStartInfo("dotnet")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };
    startInfo.ArgumentList.Add(testDll);
    startInfo.ArgumentList.Add("--crash-after-temp");
    startInfo.ArgumentList.Add(outputPath.FullName);

    using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start crash helper process.");
    var stdout = process.StandardOutput.ReadToEnd();
    var stderr = process.StandardError.ReadToEnd();
    if (!process.WaitForExit(120_000))
    {
        process.Kill(entireProcessTree: true);
        throw new TimeoutException("crash helper process did not finish within 120 seconds.");
    }

    if (process.ExitCode == 0)
    {
        throw new InvalidOperationException($"Crash helper unexpectedly exited 0.\nstdout:\n{stdout}\nstderr:\n{stderr}");
    }

    AssertBytesEqual(original, File.ReadAllBytes(outputPath.FullName), "Existing output changed after process death during temp staging.");
    AssertEqual(
        1,
        Directory.GetFiles(project.Path.FullName, ".crash-after-temp.chm.*.tmp").Length,
        "Process death after temp creation should leave exactly one stale temp file in this controlled helper scenario.");
}

void ConcurrentWritersLeaveValidFinalOutput()
{
    using var project = TempProject.Create();

    for (var i = 0; i < 20; i++)
    {
        var outputPath = project.File($"race-{i:00}.chm");
        var ready = new ManualResetEventSlim(false);
        var writerA = new ChmWriter();
        var writerB = new ChmWriter();

        var taskA = Task.Run(() =>
        {
            ready.Wait();
            writerA.Write(
                outputPath.FullName,
                new[] { new InputFile(string.Empty, "a.html", "[FILES]", Encoding.UTF8.GetBytes($"PAYLOAD-A-{i:00}")) },
                MinimalMetadata($"race-a-{i:00}"));
        });
        var taskB = Task.Run(() =>
        {
            ready.Wait();
            writerB.Write(
                outputPath.FullName,
                new[] { new InputFile(string.Empty, "b.html", "[FILES]", Encoding.UTF8.GetBytes($"PAYLOAD-B-{i:00}")) },
                MinimalMetadata($"race-b-{i:00}"));
        });

        ready.Set();
        var exceptionA = CaptureTaskException(taskA);
        var exceptionB = CaptureTaskException(taskB);

        if (exceptionA is not null && exceptionB is not null)
        {
            throw new InvalidOperationException($"Both concurrent writers failed on iteration {i}: {exceptionA.Message}; {exceptionB.Message}");
        }

        AssertFileExists(outputPath);
        var bytes = File.ReadAllBytes(outputPath.FullName);
        AssertChmStructure(bytes, expectPmgi: false);
        var hasA = ContainsBytes(bytes, Encoding.UTF8.GetBytes($"PAYLOAD-A-{i:00}"));
        var hasB = ContainsBytes(bytes, Encoding.UTF8.GetBytes($"PAYLOAD-B-{i:00}"));
        if (hasA == hasB)
        {
            throw new InvalidOperationException($"Race output should contain exactly one winning payload on iteration {i}.");
        }
    }
}

void CrossProcessSameOutputCompilesLeaveValidFinalOutput()
{
    using var project = TempProject.Create();
    var outputPath = project.File("cross-process-race.chm");
    var projectA = Directory.CreateDirectory(System.IO.Path.Combine(project.Path.FullName, "a"));
    var projectB = Directory.CreateDirectory(System.IO.Path.Combine(project.Path.FullName, "b"));

    WriteRaceProject(projectA.FullName, "PROCESS-A");
    WriteRaceProject(projectB.FullName, "PROCESS-B");

    var hhcDll = System.IO.Path.Combine(root.FullName, "src", "hhc", "bin", "Debug", "net8.0", "hhc.dll");
    AssertFileExists(new FileInfo(hhcDll));

    var startGate = new ManualResetEventSlim(false);
    var taskA = Task.Run(() => RunHhcDllAfterGate(hhcDll, System.IO.Path.Combine(projectA.FullName, "help.hhp"), outputPath.FullName, startGate));
    var taskB = Task.Run(() => RunHhcDllAfterGate(hhcDll, System.IO.Path.Combine(projectB.FullName, "help.hhp"), outputPath.FullName, startGate));

    startGate.Set();
    var resultA = taskA.Result;
    var resultB = taskB.Result;

    if (resultA.ExitCode != 1 && resultB.ExitCode != 1)
    {
        throw new InvalidOperationException($"Both cross-process compiles failed.\nA: {resultA}\nB: {resultB}");
    }

    AssertFileExists(outputPath);
    var bytes = File.ReadAllBytes(outputPath.FullName);
    AssertChmStructure(bytes, expectPmgi: false);
    var hasA = ContainsBytes(bytes, Encoding.UTF8.GetBytes("PROCESS-A"));
    var hasB = ContainsBytes(bytes, Encoding.UTF8.GetBytes("PROCESS-B"));
    if (hasA == hasB)
    {
        throw new InvalidOperationException($"Cross-process race output should contain exactly one winning payload.\nA: {resultA}\nB: {resultB}");
    }
}

void TempWriteFailurePreservesExistingOutput()
{
    using var project = TempProject.Create();
    var outputPath = project.File("atomic-output.chm");
    var inputPath = project.File("index.html");
    var original = Encoding.ASCII.GetBytes("existing output");

    project.WriteBytes("atomic-output.chm", original);
    project.WriteText("index.html", "<html><body>Atomic output</body></html>");

    var writer = new ChmWriter((tempPath, writeArchive) =>
    {
        _ = writeArchive;
        System.IO.File.WriteAllBytes(tempPath, new byte[16]);
        throw new IOException("simulated temp write failure");
    });

    var metadata = MinimalMetadata("atomic-output");
    var inputFiles = new[]
    {
        new InputFile(inputPath.FullName, "index.html", "[FILES]")
    };

    var exception = AssertThrows<IOException>(() => writer.Write(outputPath.FullName, inputFiles, metadata));

    AssertContainsText(exception.Message, "simulated temp write failure");
    AssertBytesEqual(original, System.IO.File.ReadAllBytes(outputPath.FullName), "Existing output file contents changed.");
    AssertEqual(
        0,
        Directory.GetFiles(project.Path.FullName, ".atomic-output.chm.*.tmp").Length,
        "Temporary output file was left behind.");
}

void PublishFailureAfterTempStagingCleansTemp()
{
    using var project = TempProject.Create();
    var outputPath = project.File("publish-failure.chm");
    var inputPath = project.File("index.html");
    var original = Encoding.ASCII.GetBytes("existing output");

    project.WriteBytes("publish-failure.chm", original);
    project.WriteText("index.html", "<html><body>Publish failure</body></html>");

    using var locked = new FileStream(outputPath.FullName, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

    var writer = new ChmWriter();
    _ = AssertThrows<IOException>(() => writer.Write(
        outputPath.FullName,
        new[] { new InputFile(inputPath.FullName, "index.html", "[FILES]") },
        MinimalMetadata("publish-failure")));

    locked.Position = 0;
    var current = new byte[original.Length];
    var read = locked.Read(current, 0, current.Length);
    AssertEqual(original.Length, read, "Locked output file length changed.");
    AssertBytesEqual(original, current, "Locked output file contents changed.");
    AssertEqual(
        0,
        Directory.GetFiles(project.Path.FullName, ".publish-failure.chm.*.tmp").Length,
        "Temporary output file was left behind after publish failure.");
}

void OversizedMetadataEntryExitsOne()
{
    using var project = TempProject.Create();
    project.WriteText("index.html", "<html><body>Oversized metadata</body></html>");
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=oversized-metadata.chm",
            "Title=" + new string('A', 70_000),
            "[FILES]",
            "index.html",
            string.Empty));

    var result = RunHhc(project.File("help.hhp").FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    AssertContainsText(result.Stderr, "#SYSTEM entry");
    AssertFileDoesNotExist(project.File("oversized-metadata.chm"));
}

void OversizedDirectoryEntryFailsBeforePublishingOutput()
{
    using var project = TempProject.Create();
    var outputPath = project.File("oversized-entry.chm");
    var original = Encoding.ASCII.GetBytes("existing output");
    project.WriteBytes("oversized-entry.chm", original);

    var writer = new ChmWriter();
    var longArchivePath = new string('a', 5_000) + ".html";

    var exception = AssertThrows<CompilationException>(() => writer.Write(
        outputPath.FullName,
        new[] { new InputFile(string.Empty, longArchivePath, "[FILES]", Encoding.UTF8.GetBytes("x")) },
        MinimalMetadata("oversized-entry")));

    AssertContainsText(exception.Message, "Directory entry is too large");
    AssertBytesEqual(original, System.IO.File.ReadAllBytes(outputPath.FullName), "Existing output file contents changed.");
    AssertEqual(
        0,
        Directory.GetFiles(project.Path.FullName, ".oversized-entry.chm.*.tmp").Length,
        "Temporary output file was created before directory entry validation failed.");
}

void AggregateDirectoryIndexTooLargeFailsBeforePublishingOutput()
{
    using var project = TempProject.Create();
    var outputPath = project.File("oversized-directory.chm");
    var original = Encoding.ASCII.GetBytes("existing output");
    project.WriteBytes("oversized-directory.chm", original);

    var writer = new ChmWriter();
    var inputFiles = Enumerable.Range(0, 3)
        .Select(i => new InputFile(
            string.Empty,
            $"{new string((char)('a' + i), 3_000)}-{i}.html",
            "[FILES]",
            Encoding.UTF8.GetBytes("x")))
        .ToArray();

    var exception = AssertThrows<CompilationException>(() => writer.Write(
        outputPath.FullName,
        inputFiles,
        MinimalMetadata("oversized-directory")));

    AssertContainsText(exception.Message, "directory is too large for this compiler version");
    AssertBytesEqual(original, System.IO.File.ReadAllBytes(outputPath.FullName), "Existing output file contents changed.");
    AssertEqual(
        0,
        Directory.GetFiles(project.Path.FullName, ".oversized-directory.chm.*.tmp").Length,
        "Temporary output file was created before aggregate directory validation failed.");
}

void InvalidLanguageFallsBackBeforeMetadataStorage()
{
    using var project = TempProject.Create();
    project.WriteText("index.html", "<html><body>Invalid LCID fallback</body></html>");
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=invalid-language.chm",
            "Language=0xFFFF Broken",
            "[FILES]",
            "index.html",
            string.Empty));

    var result = RunHhc(project.File("help.hhp").FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    var entries = ReadChmUncompressedEntries(File.ReadAllBytes(project.File("invalid-language.chm").FullName));
    AssertEqual(CultureInfo.CurrentCulture.LCID, ReadSystemLcid(entries["/#SYSTEM"]), "Invalid LCID should fall back before #SYSTEM metadata is written.");
}

void ProjectFilePercentEscapesRemainLiteral()
{
    AssertEqual(
        System.IO.Path.Combine("assets", "a%20b.html"),
        ArchivePath.CleanProjectPath("assets/a%20b.html"),
        "Project paths should preserve literal percent escapes.");

    using var project = TempProject.Create();
    project.WriteText("assets/a%20b.html", "<html><body>PERCENT LITERAL</body></html>");
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=percent-literal.chm",
            "[FILES]",
            "assets/a%20b.html",
            string.Empty));

    var result = RunHhc(project.File("help.hhp").FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    var entries = ReadChmUncompressedEntries(File.ReadAllBytes(project.File("percent-literal.chm").FullName));
    AssertEqual(true, entries.ContainsKey("/assets/a%20b.html"), "Literal percent path should be embedded under its exact archive name.");
    AssertContainsBytes(entries["/assets/a%20b.html"], Encoding.UTF8.GetBytes("PERCENT LITERAL"), "literal percent file payload");
}

void ProjectPathEntitiesRemainLiteral()
{
    AssertEqual(
        System.IO.Path.Combine("docs", "a&amp;b.html"),
        ArchivePath.CleanProjectPath("docs/a&amp;b.html"),
        "Project paths should not HTML-decode entity text.");

    using var project = TempProject.Create();
    project.WriteText("docs/a&amp;b.html", "<html><body>ENTITY LITERAL</body></html>");
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=entity-literal.chm",
            "[FILES]",
            "docs/a&amp;b.html",
            string.Empty));

    var result = RunHhc(project.File("help.hhp").FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    AssertNotContainsText(result.Stderr, "file not found");
    var entries = ReadChmUncompressedEntries(File.ReadAllBytes(project.File("entity-literal.chm").FullName));
    AssertEqual(true, entries.ContainsKey("/docs/a&amp;b.html"), "Literal entity path should be embedded under its exact archive name.");
    AssertContainsBytes(entries["/docs/a&amp;b.html"], Encoding.UTF8.GetBytes("ENTITY LITERAL"), "literal entity file payload");
}

void HtmlBaseHrefResolvesScannedLinks()
{
    using var project = TempProject.Create();
    project.WriteText(
        "topics/page.html",
        """
        <html><head><base href="../assets/"></head>
        <body><img src="logo.png"></body></html>
        """);
    project.WriteBytes("assets/logo.png", new byte[] { 1, 2, 3, 4, 5 });
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=base-href.chm",
            "[FILES]",
            "topics/page.html",
            string.Empty));

    var result = RunHhc(project.File("help.hhp").FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    var entries = ReadChmUncompressedEntries(File.ReadAllBytes(project.File("base-href.chm").FullName));
    AssertEqual(true, entries.ContainsKey("/assets/logo.png"), "Link scanner should resolve relative href/src values through local base href.");
}

void HtmlBaseHrefResolvesFragmentOnlyLinks()
{
    using var project = TempProject.Create();
    project.WriteText(
        "index.html",
        """
        <html><head><base href="topics/chapter.html"></head>
        <body><a href="#intro">Intro</a></body></html>
        """);
    project.WriteText("topics/chapter.html", "<html><body>Chapter target</body></html>");

    var links = LinkScanner.ExtractLinks(project.File("index.html").FullName, Encoding.UTF8);
    AssertSequenceContains(links, "topics/chapter.html#intro", "Fragment-only href should resolve against a local file base href.");

    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=base-fragment.chm",
            "[FILES]",
            "index.html",
            string.Empty));

    var result = RunHhc(project.File("help.hhp").FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    var entries = ReadChmUncompressedEntries(File.ReadAllBytes(project.File("base-fragment.chm").FullName));
    AssertEqual(true, entries.ContainsKey("/topics/chapter.html"), "Base-resolved fragment target should be embedded.");
}

void AbsoluteUriSchemesAreExternalLinks()
{
    AssertNull(ArchivePath.CleanLink("cid:part1@example.test"), "cid links should be external.");
    AssertNull(ArchivePath.CleanLink("urn:isbn:9780000000000"), "urn links should be external.");
    AssertNull(ArchivePath.CleanLink("irc://irc.example.test/channel"), "irc links should be external.");
    AssertNull(ArchivePath.CleanLink("smb://server/share/a.css"), "smb links should be external.");

    var rewritten = LinkScanner.RewriteLinksForFlatArchive("<link href=\"smb://server/share/a.css\"><img src=\"cid:part1@example.test\">");
    AssertContainsText(rewritten, "href=\"smb://server/share/a.css\"");
    AssertContainsText(rewritten, "src=\"cid:part1@example.test\"");
    AssertNotContainsText(rewritten, "href=\"a.css\"");

    using var project = TempProject.Create();
    project.WriteText("index.html", "<html><body><a href=\"urn:topic:usage\">URN</a><link href=\"smb://server/share/a.css\"></body></html>");
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=absolute-schemes.chm",
            "Flat=Yes",
            "[FILES]",
            "index.html",
            string.Empty));

    var result = RunHhc(project.File("help.hhp").FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    AssertNotContainsText(result.Stderr, "urn:topic:usage");
    AssertNotContainsText(result.Stderr, "smb://server/share/a.css");
    var entries = ReadChmUncompressedEntries(File.ReadAllBytes(project.File("absolute-schemes.chm").FullName));
    AssertEqual(false, entries.ContainsKey("/a.css"), "External SMB URL should not be flattened into an archive entry.");
}

void DecodedNulLinksDoNotAbortCompile()
{
    AssertNull(ArchivePath.CleanLink("bad%00.html"), "Decoded NUL links should be rejected before path resolution.");

    using var project = TempProject.Create();
    project.WriteText("index.html", "<html><body><a href=\"bad%00.html\">bad</a></body></html>");
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=nul-link.chm",
            "[FILES]",
            "index.html",
            string.Empty));

    var result = RunHhc(project.File("help.hhp").FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    AssertNotContainsText(result.Stderr, "Null");
    AssertNotContainsText(result.Stderr, "illegal characters");
    var entries = ReadChmUncompressedEntries(File.ReadAllBytes(project.File("nul-link.chm").FullName));
    AssertEqual(true, entries.ContainsKey("/index.html"), "Malformed optional link should not prevent normal input from compiling.");
}

void FlatLinkRewriteDecodesEncodedSeparators()
{
    var text = """
        <a href="topics%2Fusage.html#top">Usage</a>
        <img src="topics&#47;cover.png?size=small">
        """;

    var rewritten = LinkScanner.RewriteLinksForFlatArchive(text);

    AssertContainsText(rewritten, "href=\"usage.html#top\"");
    AssertContainsText(rewritten, "src=\"cover.png?size=small\"");
}

void FlatUtf16RewritePreservesBom()
{
    using var project = TempProject.Create();
    project.WriteText("index.html", "<html><body><a href=\"topics/usage.html\">Usage</a></body></html>", Encoding.Unicode);
    project.WriteText("topics/usage.html", "<html><body>Usage topic</body></html>");
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=utf16-flat.chm",
            "Flat=Yes",
            "[FILES]",
            "index.html",
            "topics/usage.html",
            string.Empty));

    var result = RunHhc(project.File("help.hhp").FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    var entries = ReadChmUncompressedEntries(File.ReadAllBytes(project.File("utf16-flat.chm").FullName));
    var index = entries["/index.html"];
    AssertEqual(0xFF, index[0], "UTF-16 LE BOM first byte should be preserved.");
    AssertEqual(0xFE, index[1], "UTF-16 LE BOM second byte should be preserved.");
    AssertContainsText(Encoding.Unicode.GetString(index), "href=\"topics/usage.html\"");
}

void OmittedContentsFileDoesNotGenerateToc()
{
    using var project = TempProject.Create();
    project.WriteText("index.html", "<html><body>Topic</body></html>");
    project.WriteText("Table of Contents.hhc", "<html><body>USER TOC PAYLOAD</body></html>");
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=toc-collision.chm",
            "[FILES]",
            "index.html",
            "Table of Contents.hhc",
            string.Empty));

    var result = RunHhc(project.File("help.hhp").FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    AssertNotContainsText(result.Stderr, "Generated contents");
    var entries = ReadChmUncompressedEntries(File.ReadAllBytes(project.File("toc-collision.chm").FullName));
    AssertContainsBytes(entries["/Table of Contents.hhc"], Encoding.UTF8.GetBytes("USER TOC PAYLOAD"), "user TOC payload");
    AssertEqual(false, entries.ContainsKey("/Table of Contents 2.hhc"), "Contents omission should not generate a synthetic TOC.");
    if (ContainsBytes(entries["/#SYSTEM"], Encoding.UTF8.GetBytes("Table of Contents 2.hhc")))
    {
        throw new InvalidOperationException("#SYSTEM should not point at a generated TOC path.");
    }
}

void InternalStreamArchivePathCollisionExitsOne()
{
    using var project = TempProject.Create();
    project.WriteText("#SYSTEM", "user system payload");
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=internal-collision.chm",
            "[FILES]",
            "./#SYSTEM",
            string.Empty));

    var result = RunHhc(project.File("help.hhp").FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    AssertContainsText(result.Stderr, "collides with internal CHM stream");
    AssertFileDoesNotExist(project.File("internal-collision.chm"));
}

void OutputPathCannotOverwriteProjectOrInputFiles()
{
    using var inputProject = TempProject.Create();
    inputProject.WriteText("index.html", "<html><body>ORIGINAL INPUT</body></html>");
    inputProject.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=unused.chm",
            "[FILES]",
            "index.html",
            string.Empty));

    var inputResult = RunHhc(inputProject.File("help.hhp").FullName, "--out", inputProject.File("index.html").FullName);

    AssertEqual(1, inputResult.ExitCode, inputResult.ToString());
    AssertContainsText(inputResult.Stderr, "overwrite an input file");
    AssertContainsText(File.ReadAllText(inputProject.File("index.html").FullName), "ORIGINAL INPUT");

    using var hhpProject = TempProject.Create();
    hhpProject.WriteText("index.html", "<html><body>Project overwrite guard</body></html>");
    var hhpText = string.Join(
        "\r\n",
        "[OPTIONS]",
        "Compiled file=unused.chm",
        "[FILES]",
        "index.html",
        string.Empty);
    hhpProject.WriteText("help.hhp", hhpText);

    var hhpResult = RunHhc(hhpProject.File("help.hhp").FullName, "--out", hhpProject.File("help.hhp").FullName);

    AssertEqual(1, hhpResult.ExitCode, hhpResult.ToString());
    AssertContainsText(hhpResult.Stderr, "overwrite the project file");
    AssertEqual(hhpText, File.ReadAllText(hhpProject.File("help.hhp").FullName), "Project file contents should be preserved.");
}

void OutputPathCannotOverwriteReplacedFlatCollisionSource()
{
    using var project = TempProject.Create();
    const string losingText = "<html><body>ORIGINAL LOSING INPUT</body></html>";
    project.WriteText("a/index.html", losingText);
    project.WriteText("b/index.html", "<html><body>WINNING INPUT</body></html>");
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=unused.chm",
            "Flat=Yes",
            "[FILES]",
            "a/index.html",
            "b/index.html",
            string.Empty));

    var result = RunHhc(project.File("help.hhp").FullName, "--out", project.File("a/index.html").FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    AssertContainsText(result.Stderr, "overwrite an input file");
    AssertEqual(losingText, File.ReadAllText(project.File("a/index.html").FullName), "Replaced input source should be preserved.");
}

void CaseOnlyArchiveSourceCollisionIsQuiet()
{
    using var project = TempProject.Create();
    project.WriteText("topic.html", "<html><body>LOWER TOPIC</body></html>");
    if (!OperatingSystem.IsWindows())
    {
        project.WriteText("TOPIC.html", "<html><body>UPPER TOPIC</body></html>");
    }

    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=case-collision.chm",
            "[FILES]",
            "topic.html",
            "TOPIC.html",
            string.Empty));

    var result = RunHhc(project.File("help.hhp").FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    AssertNotContainsText(result.Stderr, "duplicate archive path");
    var bytes = File.ReadAllBytes(project.File("case-collision.chm").FullName);
    if (OperatingSystem.IsWindows())
    {
        AssertContainsAscii(bytes, "LOWER TOPIC");
        AssertNotContainsAscii(bytes, "UPPER TOPIC");
    }
    else
    {
        AssertContainsAscii(bytes, "UPPER TOPIC");
        AssertNotContainsAscii(bytes, "LOWER TOPIC");
    }
}

void DotProjectPathEntriesAreIgnored()
{
    using var project = TempProject.Create();
    project.WriteText("index.html", "<html><body>Dot paths are no-op entries</body></html>");
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=dot-path.chm",
            "[FILES]",
            ".",
            "./",
            "index.html",
            string.Empty));

    var result = RunHhc(project.File("help.hhp").FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    AssertNotContainsText(result.Stderr, "file not found: .");
    var entries = ReadChmUncompressedEntries(File.ReadAllBytes(project.File("dot-path.chm").FullName));
    AssertEqual(true, entries.ContainsKey("/index.html"), "Dot path entries should not prevent normal files from compiling.");
}

void FlatBaseHrefScannerFlattensArchiveWithoutRewritingPayload()
{
    var text = """
        <html><head><base href="../assets/"></head>
        <body><img src="logo.png"></body></html>
        """;
    var rewritten = LinkScanner.RewriteLinksForFlatArchive(text);

    AssertNotContainsText(rewritten, "<base");
    AssertContainsText(rewritten, "src=\"logo.png\"");
    AssertEqual(rewritten, LinkScanner.RewriteLinksForFlatArchive(rewritten), "Removing a local base tag during flat rewrite should be idempotent.");

    using var project = TempProject.Create();
    project.WriteText("topics/page.html", text);
    project.WriteBytes("assets/logo.png", new byte[] { 9, 8, 7, 6 });
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=flat-base.chm",
            "Flat=Yes",
            "[FILES]",
            "topics/page.html",
            string.Empty));

    var result = RunHhc(project.File("help.hhp").FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    var entries = ReadChmUncompressedEntries(File.ReadAllBytes(project.File("flat-base.chm").FullName));
    AssertEqual(true, entries.ContainsKey("/page.html"), "Flat page should be stored by basename.");
    AssertEqual(true, entries.ContainsKey("/logo.png"), "Base-resolved asset should be stored by flattened basename.");
    AssertEqual(false, entries.ContainsKey("/assets/logo.png"), "Flat archive should not keep the asset directory path.");
    var pageText = Encoding.UTF8.GetString(entries["/page.html"]);
    AssertContainsText(pageText, "<base href=\"../assets/\"");
    AssertContainsText(pageText, "src=\"logo.png\"");
}

void FlatRewritePreservesExternalBaseReferences()
{
    var text = """
        <html><head><base href="https://cdn.example.test/assets/"></head>
        <body><img src="images/logo.png"><a href="topics/usage.html#top">Usage</a></body></html>
        """;
    var rewritten = LinkScanner.RewriteLinksForFlatArchive(text);

    AssertContainsText(rewritten, "<base href=\"https://cdn.example.test/assets/\"");
    AssertContainsText(rewritten, "src=\"images/logo.png\"");
    AssertContainsText(rewritten, "href=\"topics/usage.html#top\"");
    AssertNotContainsText(rewritten, "src=\"logo.png\"");
    AssertNotContainsText(rewritten, "href=\"usage.html#top\"");

    using var project = TempProject.Create();
    project.WriteText("index.html", text);
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=external-base.chm",
            "Flat=Yes",
            "[FILES]",
            "index.html",
            string.Empty));

    var result = RunHhc(project.File("help.hhp").FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    AssertNotContainsText(result.Stderr, "images/logo.png");
    AssertNotContainsText(result.Stderr, "topics/usage.html");
    var entries = ReadChmUncompressedEntries(File.ReadAllBytes(project.File("external-base.chm").FullName));
    AssertEqual(false, entries.ContainsKey("/logo.png"), "External-base relative image should not be embedded as a flat local file.");
    var indexText = Encoding.UTF8.GetString(entries["/index.html"]);
    AssertContainsText(indexText, "<base href=\"https://cdn.example.test/assets/\"");
    AssertContainsText(indexText, "src=\"images/logo.png\"");
    AssertContainsText(indexText, "href=\"topics/usage.html#top\"");
}

void FlatLinkRewritePreservesReservedEscapes()
{
    var text = "<a href=\"topics/C%23Guide.html\">C#</a>";
    var rewritten = LinkScanner.RewriteLinksForFlatArchive(text);

    AssertContainsText(rewritten, "href=\"C%23Guide.html\"");
    AssertNotContainsText(rewritten, "href=\"C#Guide.html\"");

    using var project = TempProject.Create();
    project.WriteText("index.html", "<html><body><a href=\"topics/C%23Guide.html\">C# guide</a></body></html>");
    project.WriteText("topics/C#Guide.html", "<html><body>C# Guide</body></html>");
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=reserved-escape.chm",
            "Flat=Yes",
            "[FILES]",
            "index.html",
            "topics/C#Guide.html",
            string.Empty));

    var result = RunHhc(project.File("help.hhp").FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    var entries = ReadChmUncompressedEntries(File.ReadAllBytes(project.File("reserved-escape.chm").FullName));
    AssertEqual(true, entries.ContainsKey("/C#Guide.html"), "Decoded archive filename should still be embedded.");
    var indexText = Encoding.UTF8.GetString(entries["/index.html"]);
    AssertContainsText(indexText, "href=\"topics/C%23Guide.html\"");
    AssertNotContainsText(indexText, "href=\"C#Guide.html\"");
}

static int ReadSystemLcid(byte[] systemFile)
{
    var offset = 4;
    while (offset + 4 <= systemFile.Length)
    {
        var code = BitConverter.ToUInt16(systemFile, offset);
        var length = BitConverter.ToUInt16(systemFile, offset + 2);
        offset += 4;
        if (offset + length > systemFile.Length)
        {
            throw new InvalidOperationException("#SYSTEM entry exceeds file bounds.");
        }

        if (code == 4)
        {
            return BitConverter.ToInt32(systemFile, offset);
        }

        offset += length;
    }

    throw new InvalidOperationException("#SYSTEM code 4 entry was not found.");
}

void OmittedContentsFileHasNoGeneratedReservedToc()
{
    using var project = TempProject.Create();
    project.WriteText("C#Guide.html", "<html><body>C# guide</body></html>");
    project.WriteText("literal%23.html", "<html><body>Literal percent 23</body></html>");
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=toc-reserved.chm",
            "[FILES]",
            "C#Guide.html",
            "literal%23.html",
            string.Empty));

    var result = RunHhc(project.File("help.hhp").FullName);

    AssertEqual(1, result.ExitCode, result.ToString());
    var entries = ReadChmUncompressedEntries(File.ReadAllBytes(project.File("toc-reserved.chm").FullName));
    AssertEqual(false, entries.ContainsKey("/Table of Contents.hhc"), "Contents omission should not synthesize a TOC.");
    AssertEqual(true, entries.ContainsKey("/C#Guide.html"), "Reserved-character topic should still be embedded under decoded archive name.");
    AssertEqual(true, entries.ContainsKey("/literal%23.html"), "Literal-percent topic should still be embedded under literal archive name.");
}

void WriteStandardProject(TempProject project, string outputName)
{
    project.WriteText("index.html", "<html><body><a href=\"topics/intro.html\">Intro</a></body></html>");
    project.WriteText("topics/intro.html", "<html><body>Intro</body></html>");
    project.WriteText(
        "toc.hhc",
        """
        <html><body>
        <object type="text/sitemap">
        <param name="Name" value="Home">
        <param name="Local" value="index.html">
        </object>
        </body></html>
        """);
    project.WriteText(
        "index.hhk",
        """
        <html><body>
        <object type="text/sitemap">
        <param name="Name" value="Home">
        <param name="Local" value="index.html">
        </object>
        </body></html>
        """);
    project.WriteText(
        "help.hhp",
        string.Join(
            "\r\n",
            "[OPTIONS]",
            $"Compiled file={outputName}",
            "Contents file=toc.hhc",
            "Index file=index.hhk",
            "Default topic=index.html",
            "Title=Product Help",
            "[FILES]",
            "index.html",
            "topics/intro.html",
            string.Empty));
}

void WriteRaceProject(string directory, string marker)
{
    Directory.CreateDirectory(directory);
    System.IO.File.WriteAllText(
        System.IO.Path.Combine(directory, "index.html"),
        $"<html><body>{marker}</body></html>",
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    System.IO.File.WriteAllText(
        System.IO.Path.Combine(directory, "help.hhp"),
        string.Join(
            "\r\n",
            "[OPTIONS]",
            "Compiled file=unused.chm",
            $"Title={marker}",
            "[FILES]",
            "index.html",
            string.Empty),
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
}

ProcessResult RunHhc(params string[] arguments)
{
    var startInfo = new ProcessStartInfo("dotnet")
    {
        WorkingDirectory = root.FullName,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };
    startInfo.ArgumentList.Add("run");
    startInfo.ArgumentList.Add("--project");
    startInfo.ArgumentList.Add(hhcProject.FullName);
    startInfo.ArgumentList.Add("-p:UseAppHost=false");
    startInfo.ArgumentList.Add("--");
    foreach (var argument in arguments)
    {
        startInfo.ArgumentList.Add(argument);
    }

    using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet.");
    var stdout = process.StandardOutput.ReadToEnd();
    var stderr = process.StandardError.ReadToEnd();
    if (!process.WaitForExit(120_000))
    {
        process.Kill(entireProcessTree: true);
        throw new TimeoutException("hhc process did not finish within 120 seconds.");
    }

    return new ProcessResult(process.ExitCode, stdout, stderr);
}

ProcessResult RunHhcDllAfterGate(string hhcDll, string projectPath, string outputPath, ManualResetEventSlim gate)
{
    var startInfo = new ProcessStartInfo("dotnet")
    {
        WorkingDirectory = root.FullName,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };
    startInfo.ArgumentList.Add(hhcDll);
    startInfo.ArgumentList.Add(projectPath);
    startInfo.ArgumentList.Add("--out");
    startInfo.ArgumentList.Add(outputPath);

    gate.Wait();
    using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet.");
    var stdout = process.StandardOutput.ReadToEnd();
    var stderr = process.StandardError.ReadToEnd();
    if (!process.WaitForExit(120_000))
    {
        process.Kill(entireProcessTree: true);
        throw new TimeoutException("hhc process did not finish within 120 seconds.");
    }

    return new ProcessResult(process.ExitCode, stdout, stderr);
}

int CrashAfterTempHelper(string outputPath)
{
    var writer = new ChmWriter((tempPath, writeArchive) =>
    {
        _ = writeArchive;
        File.WriteAllBytes(tempPath, Encoding.ASCII.GetBytes("staged temp before process death"));
        Process.GetCurrentProcess().Kill(entireProcessTree: false);
        throw new InvalidOperationException("Process.Kill returned unexpectedly.");
    });

    writer.Write(
        outputPath,
        new[] { new InputFile(string.Empty, "index.html", "[FILES]", Encoding.UTF8.GetBytes("new output that must not publish")) },
        MinimalMetadata("crash-after-temp"));
    return 99;
}

static DirectoryInfo FindRepositoryRoot()
{
    var current = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (current is not null)
    {
        if (File.Exists(System.IO.Path.Combine(current.FullName, "src", "hhc", "hhc.csproj")))
        {
            return current;
        }

        current = current.Parent;
    }

    throw new InvalidOperationException("Could not locate repository root.");
}

static void AssertEqual<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected {expected}, got {actual}. {message}");
    }
}

static void AssertFileExists(FileInfo path)
{
    if (!File.Exists(path.FullName))
    {
        throw new FileNotFoundException($"Expected file was not created: {path.FullName}");
    }
}

static void AssertFileDoesNotExist(FileInfo path)
{
    if (File.Exists(path.FullName))
    {
        throw new InvalidOperationException($"Expected file not to exist: {path.FullName}");
    }
}

static void AssertContainsText(string haystack, string needle)
{
    if (!haystack.Contains(needle, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"Expected text to contain '{needle}'. Actual text:\n{haystack}");
    }
}

static void AssertSequenceContains(IEnumerable<string> haystack, string needle, string message)
{
    if (!haystack.Contains(needle, StringComparer.Ordinal))
    {
        throw new InvalidOperationException($"{message} Expected sequence to contain '{needle}'. Actual values: {string.Join(", ", haystack)}");
    }
}

static void AssertNotContainsText(string haystack, string needle)
{
    if (haystack.Contains(needle, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"Did not expect text to contain '{needle}'. Actual text:\n{haystack}");
    }
}

static void AssertNull(object? value, string message)
{
    if (value is not null)
    {
        throw new InvalidOperationException($"{message} Actual value: {value}");
    }
}

static void AssertNoEscapingArchivePath(string normalized, string seed)
{
    if (normalized.StartsWith("../", StringComparison.Ordinal)
        || normalized.Equals("..", StringComparison.Ordinal)
        || normalized.Contains("/../", StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"Archive path escaped for seed '{seed}': {normalized}");
    }
}

static TException AssertThrows<TException>(Action action)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException ex)
    {
        return ex;
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"Expected {typeof(TException).Name}, got {ex.GetType().Name}.", ex);
    }

    throw new InvalidOperationException($"Expected {typeof(TException).Name}, but no exception was thrown.");
}

static Exception? CaptureTaskException(Task task)
{
    try
    {
        task.Wait();
        return null;
    }
    catch (AggregateException ex)
    {
        return ex.InnerExceptions.Count == 1 ? ex.InnerExceptions[0] : ex;
    }
}

static void AssertAsciiAt(byte[] haystack, int offset, string expected)
{
    var bytes = Encoding.ASCII.GetBytes(expected);
    if (offset < 0 || offset + bytes.Length > haystack.Length)
    {
        throw new InvalidOperationException($"Cannot read {expected} at offset {offset}.");
    }

    for (var i = 0; i < bytes.Length; i++)
    {
        if (haystack[offset + i] != bytes[i])
        {
            throw new InvalidOperationException($"Expected ASCII {expected} at offset {offset}.");
        }
    }
}

static void AssertChmStructure(byte[] bytes, bool expectPmgi)
{
    const int headerLength = 0x60;
    const int section0Length = 0x18;
    const int directoryHeaderLength = 0x54;
    const int directoryBlockLength = 0x1000;

    AssertAsciiAt(bytes, 0, "ITSF");
    AssertEqual(3, ReadInt32LittleEndian(bytes, 4), "ITSF version should be 3.");
    AssertEqual(headerLength, ReadInt32LittleEndian(bytes, 8), "ITSF header length mismatch.");

    var section0Offset = checked((int)ReadInt64LittleEndian(bytes, 56));
    var actualSection0Length = checked((int)ReadInt64LittleEndian(bytes, 64));
    var directoryOffset = checked((int)ReadInt64LittleEndian(bytes, 72));
    var directoryLength = checked((int)ReadInt64LittleEndian(bytes, 80));
    var dataOffset = checked((int)ReadInt64LittleEndian(bytes, 88));

    AssertEqual(headerLength, section0Offset, "Section 0 should follow the ITSF header.");
    AssertEqual(section0Length, actualSection0Length, "Section 0 length mismatch.");
    AssertEqual(headerLength + section0Length, directoryOffset, "Directory should follow section 0.");
    AssertEqual(directoryOffset + directoryLength, dataOffset, "Data offset should follow the directory section.");
    if (dataOffset > bytes.Length)
    {
        throw new InvalidOperationException($"CHM data offset {dataOffset} exceeds file length {bytes.Length}.");
    }

    AssertEqual(0x01FE, ReadInt32LittleEndian(bytes, section0Offset), "Header section 0 marker mismatch.");
    AssertEqual(bytes.Length, checked((int)ReadInt64LittleEndian(bytes, section0Offset + 8)), "Header section file size mismatch.");

    AssertAsciiAt(bytes, directoryOffset, "ITSP");
    AssertEqual(1, ReadInt32LittleEndian(bytes, directoryOffset + 4), "ITSP version should be 1.");
    AssertEqual(directoryHeaderLength, ReadInt32LittleEndian(bytes, directoryOffset + 8), "ITSP header length mismatch.");
    AssertEqual(directoryBlockLength, ReadInt32LittleEndian(bytes, directoryOffset + 16), "Directory block length mismatch.");

    var depth = ReadInt32LittleEndian(bytes, directoryOffset + 24);
    var rootChunk = ReadInt32LittleEndian(bytes, directoryOffset + 28);
    var firstPmgl = ReadInt32LittleEndian(bytes, directoryOffset + 32);
    var lastPmgl = ReadInt32LittleEndian(bytes, directoryOffset + 36);
    var chunkCount = ReadInt32LittleEndian(bytes, directoryOffset + 44);

    if (chunkCount <= 0)
    {
        throw new InvalidOperationException("CHM directory has no chunks.");
    }

    AssertEqual(directoryHeaderLength + chunkCount * directoryBlockLength, directoryLength, "Directory length should match header plus chunks.");
    AssertEqual(expectPmgi ? 2 : 1, depth, "Directory index depth mismatch.");
    AssertEqual(expectPmgi ? 0 : -1, rootChunk, "Root chunk number mismatch.");
    AssertEqual(expectPmgi ? 1 : 0, firstPmgl, "First PMGL chunk number mismatch.");
    AssertEqual(firstPmgl + chunkCount - (expectPmgi ? 2 : 1), lastPmgl, "Last PMGL chunk number mismatch.");

    var chunkOffset = directoryOffset + directoryHeaderLength;
    if (expectPmgi)
    {
        AssertAsciiAt(bytes, chunkOffset, "PMGI");
        AssertDirectoryChunkBounds(bytes, chunkOffset, "PMGI");
        chunkOffset += directoryBlockLength;
    }

    var pmglChunks = chunkCount - (expectPmgi ? 1 : 0);
    if (pmglChunks <= 0)
    {
        throw new InvalidOperationException("CHM directory has no PMGL chunks.");
    }

    for (var i = 0; i < pmglChunks; i++)
    {
        AssertAsciiAt(bytes, chunkOffset, "PMGL");
        AssertDirectoryChunkBounds(bytes, chunkOffset, "PMGL");

        var previous = ReadInt32LittleEndian(bytes, chunkOffset + 12);
        var next = ReadInt32LittleEndian(bytes, chunkOffset + 16);
        var expectedChunkNumber = firstPmgl + i;
        AssertEqual(i == 0 ? -1 : expectedChunkNumber - 1, previous, "PMGL previous chunk link mismatch.");
        AssertEqual(i == pmglChunks - 1 ? -1 : expectedChunkNumber + 1, next, "PMGL next chunk link mismatch.");

        chunkOffset += directoryBlockLength;
    }
}

static void AssertDirectoryChunkBounds(byte[] bytes, int offset, string chunkType)
{
    const int directoryBlockLength = 0x1000;

    if (offset < 0 || offset + directoryBlockLength > bytes.Length)
    {
        throw new InvalidOperationException($"{chunkType} chunk exceeds file bounds at {offset}.");
    }

    var freeSpace = ReadInt32LittleEndian(bytes, offset + 4);
    if (freeSpace < 0 || freeSpace >= directoryBlockLength)
    {
        throw new InvalidOperationException($"{chunkType} free-space value is outside the directory block: {freeSpace}.");
    }
}

static Dictionary<string, byte[]> ReadChmUncompressedEntries(byte[] bytes)
{
    const int directoryHeaderLength = 0x54;
    const int directoryBlockLength = 0x1000;

    AssertChmStructure(bytes, expectPmgi: ContainsBytes(bytes, Encoding.ASCII.GetBytes("PMGI")));

    var directoryOffset = checked((int)ReadInt64LittleEndian(bytes, 72));
    var dataOffset = checked((int)ReadInt64LittleEndian(bytes, 88));
    var chunkCount = ReadInt32LittleEndian(bytes, directoryOffset + 44);
    var entries = new Dictionary<string, byte[]>(StringComparer.Ordinal);

    var chunkOffset = directoryOffset + directoryHeaderLength;
    for (var chunk = 0; chunk < chunkCount; chunk++, chunkOffset += directoryBlockLength)
    {
        if (AsciiAt(bytes, chunkOffset, "PMGI"))
        {
            continue;
        }

        AssertAsciiAt(bytes, chunkOffset, "PMGL");
        var entryCount = ReadUInt16LittleEndian(bytes, chunkOffset + directoryBlockLength - 2);
        var pos = chunkOffset + 0x14;
        var chunkEnd = chunkOffset + directoryBlockLength;

        for (var entryIndex = 0; entryIndex < entryCount; entryIndex++)
        {
            var nameLength = checked((int)ReadEncInt(bytes, ref pos, chunkEnd));
            if (nameLength < 0 || pos + nameLength > chunkEnd)
            {
                throw new InvalidOperationException($"Directory entry name exceeds PMGL bounds at chunk {chunk} entry {entryIndex}.");
            }

            var name = Encoding.UTF8.GetString(bytes, pos, nameLength);
            pos += nameLength;
            var section = ReadEncInt(bytes, ref pos, chunkEnd);
            var offset = checked((int)ReadEncInt(bytes, ref pos, chunkEnd));
            var length = checked((int)ReadEncInt(bytes, ref pos, chunkEnd));

            AssertEqual(0L, section, $"Directory entry {name} should be in uncompressed section 0.");
            if (offset < 0 || length < 0 || dataOffset + offset + length > bytes.Length)
            {
                throw new InvalidOperationException($"Directory entry {name} points outside CHM content bounds.");
            }

            entries[name] = bytes.AsSpan(dataOffset + offset, length).ToArray();
        }
    }

    return entries;
}

static long ReadEncInt(byte[] bytes, ref int offset, int limit)
{
    long value = 0;
    var bytesRead = 0;
    while (true)
    {
        if (offset >= limit || offset >= bytes.Length)
        {
            throw new InvalidOperationException("Encoded integer exceeds input bounds.");
        }

        var b = bytes[offset++];
        bytesRead++;
        if (bytesRead > 10)
        {
            throw new InvalidOperationException("Encoded integer is too long.");
        }

        value = checked((value << 7) | (long)(b & 0x7F));
        if ((b & 0x80) == 0)
        {
            return value;
        }
    }
}

static int ReadInt32LittleEndian(byte[] bytes, int offset)
{
    if (offset < 0 || offset + sizeof(int) > bytes.Length)
    {
        throw new InvalidOperationException($"Cannot read Int32 at offset {offset}.");
    }

    return BitConverter.ToInt32(bytes, offset);
}

static int ReadUInt16LittleEndian(byte[] bytes, int offset)
{
    if (offset < 0 || offset + sizeof(ushort) > bytes.Length)
    {
        throw new InvalidOperationException($"Cannot read UInt16 at offset {offset}.");
    }

    return BitConverter.ToUInt16(bytes, offset);
}

static long ReadInt64LittleEndian(byte[] bytes, int offset)
{
    if (offset < 0 || offset + sizeof(long) > bytes.Length)
    {
        throw new InvalidOperationException($"Cannot read Int64 at offset {offset}.");
    }

    return BitConverter.ToInt64(bytes, offset);
}

static bool AsciiAt(byte[] haystack, int offset, string expected)
{
    var bytes = Encoding.ASCII.GetBytes(expected);
    if (offset < 0 || offset + bytes.Length > haystack.Length)
    {
        return false;
    }

    for (var i = 0; i < bytes.Length; i++)
    {
        if (haystack[offset + i] != bytes[i])
        {
            return false;
        }
    }

    return true;
}

static void AssertContainsAscii(byte[] haystack, string needle)
{
    AssertContainsBytes(haystack, Encoding.ASCII.GetBytes(needle), $"ASCII {needle}");
}

static void AssertNotContainsAscii(byte[] haystack, string needle)
{
    if (ContainsBytes(haystack, Encoding.ASCII.GetBytes(needle)))
    {
        throw new InvalidOperationException($"Did not expect ASCII {needle}.");
    }
}

static void AssertContainsBytes(byte[] haystack, byte[] needle, string label)
{
    if (!ContainsBytes(haystack, needle))
    {
        throw new InvalidOperationException($"Expected to find {label}.");
    }
}

static void AssertBytesEqual(byte[] expected, byte[] actual, string label)
{
    if (!expected.SequenceEqual(actual))
    {
        throw new InvalidOperationException(label);
    }
}

static bool ContainsBytes(byte[] haystack, byte[] needle)
{
    if (needle.Length == 0)
    {
        return true;
    }

    for (var i = 0; i <= haystack.Length - needle.Length; i++)
    {
        var found = true;
        for (var j = 0; j < needle.Length; j++)
        {
            if (haystack[i + j] != needle[j])
            {
                found = false;
                break;
            }
        }

        if (found)
        {
            return true;
        }
    }

    return false;
}

static ChmMetadata MinimalMetadata(string stem) =>
    new(
        Title: "Test Project",
        DefaultTopic: "index.html",
        ContentsFile: null,
        IndexFile: null,
        ContentsFileGenerated: true,
        DefaultWindow: "main",
        DefaultFont: null,
        CompiledFileStem: stem,
        Lcid: 1033,
        TextEncoding: Encoding.UTF8,
        FullTextSearch: false);

readonly record struct ProcessResult(int ExitCode, string Stdout, string Stderr)
{
    public override string ToString() =>
        $"exit={ExitCode}\nstdout:\n{Stdout}\nstderr:\n{Stderr}";
}

sealed class TempProject : IDisposable
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private TempProject(DirectoryInfo path)
    {
        Path = path;
    }

    public DirectoryInfo Path { get; }

    public FileInfo File(string relativePath) =>
        new(System.IO.Path.Combine(Path.FullName, relativePath));

    public static TempProject Create()
    {
        var path = new DirectoryInfo(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hhc-it-" + Guid.NewGuid().ToString("N")));
        path.Create();
        return new TempProject(path);
    }

    public void WriteText(string relativePath, string text, Encoding? encoding = null)
    {
        var fullPath = File(relativePath);
        Directory.CreateDirectory(fullPath.DirectoryName!);
        System.IO.File.WriteAllText(fullPath.FullName, text, encoding ?? Utf8NoBom);
    }

    public void WriteBytes(string relativePath, byte[] bytes)
    {
        var fullPath = File(relativePath);
        Directory.CreateDirectory(fullPath.DirectoryName!);
        System.IO.File.WriteAllBytes(fullPath.FullName, bytes);
    }

    public void Dispose()
    {
        try
        {
            Path.Delete(recursive: true);
        }
        catch
        {
            // Leaving a temp directory is less harmful than hiding the test result.
        }
    }
}
