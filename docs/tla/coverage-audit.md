# TLA+ Coverage Audit

This audit is generated from the TLC logs referenced by the current verification summaries.
Coverage is expected to be present for every checked model. Zero-hit top-level actions are allowed for documented unreachable/stutter actions: `ExternalAbort` and `StayDone`.
Use-case models also reuse a shared pipeline action skeleton; when a scenario exits early, later shared actions are recorded as intentionally zero-hit rather than as missing coverage.

Referenced logs: 98

| Log | Coverage present | Zero-hit top-level actions | Zero-hit disposition | Unexpected zero/missing coverage |
| -- | -- | -- | -- | -- |
| `artifacts/tla-results/Inspection/ExistingCodeLoop.log` | yes | `ExternalAbort`, `StayDone` | `ExternalAbort`: documented unreachable/stutter action; `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Implementation/ApiExceptionConformance.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Implementation/ImplementationConformance.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Core/ArchivePathNormalization.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Core/ChmDirectory.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Core/ChmMetadataWriter.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Core/CliParsing.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Core/CompilerPipeline.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Core/FileCollection.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Core/LinkScannerBehavior.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Core/TextEncodingDetection.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC001_Cli_NoArgsHelp.log` | yes | `BuildMetadata`, `CollectFiles`, `LoadProject`, `StayDone`, `WriteChm` | `BuildMetadata`: use-case model stops before this shared pipeline action; `CollectFiles`: use-case model stops before this shared pipeline action; `LoadProject`: use-case model stops before this shared pipeline action; `StayDone`: documented unreachable/stutter action; `WriteChm`: use-case model stops before this shared pipeline action | - |
| `artifacts/tla-results/Use Case/UC002_Cli_HelpOptions.log` | yes | `BuildMetadata`, `CollectFiles`, `LoadProject`, `StayDone`, `WriteChm` | `BuildMetadata`: use-case model stops before this shared pipeline action; `CollectFiles`: use-case model stops before this shared pipeline action; `LoadProject`: use-case model stops before this shared pipeline action; `StayDone`: documented unreachable/stutter action; `WriteChm`: use-case model stops before this shared pipeline action | - |
| `artifacts/tla-results/Use Case/UC003_Cli_Version.log` | yes | `BuildMetadata`, `CollectFiles`, `LoadProject`, `StayDone`, `WriteChm` | `BuildMetadata`: use-case model stops before this shared pipeline action; `CollectFiles`: use-case model stops before this shared pipeline action; `LoadProject`: use-case model stops before this shared pipeline action; `StayDone`: documented unreachable/stutter action; `WriteChm`: use-case model stops before this shared pipeline action | - |
| `artifacts/tla-results/Use Case/UC004_Cli_UnknownOption.log` | yes | `BuildMetadata`, `CollectFiles`, `LoadProject`, `StayDone`, `WriteChm` | `BuildMetadata`: use-case model stops before this shared pipeline action; `CollectFiles`: use-case model stops before this shared pipeline action; `LoadProject`: use-case model stops before this shared pipeline action; `StayDone`: documented unreachable/stutter action; `WriteChm`: use-case model stops before this shared pipeline action | - |
| `artifacts/tla-results/Use Case/UC005_Cli_OutMissingValue.log` | yes | `BuildMetadata`, `CollectFiles`, `LoadProject`, `StayDone`, `WriteChm` | `BuildMetadata`: use-case model stops before this shared pipeline action; `CollectFiles`: use-case model stops before this shared pipeline action; `LoadProject`: use-case model stops before this shared pipeline action; `StayDone`: documented unreachable/stutter action; `WriteChm`: use-case model stops before this shared pipeline action | - |
| `artifacts/tla-results/Use Case/UC006_Cli_MissingProjectArg.log` | yes | `BuildMetadata`, `CollectFiles`, `LoadProject`, `StayDone`, `WriteChm` | `BuildMetadata`: use-case model stops before this shared pipeline action; `CollectFiles`: use-case model stops before this shared pipeline action; `LoadProject`: use-case model stops before this shared pipeline action; `StayDone`: documented unreachable/stutter action; `WriteChm`: use-case model stops before this shared pipeline action | - |
| `artifacts/tla-results/Use Case/UC007_Cli_MultipleProjects.log` | yes | `BuildMetadata`, `CollectFiles`, `LoadProject`, `StayDone`, `WriteChm` | `BuildMetadata`: use-case model stops before this shared pipeline action; `CollectFiles`: use-case model stops before this shared pipeline action; `LoadProject`: use-case model stops before this shared pipeline action; `StayDone`: documented unreachable/stutter action; `WriteChm`: use-case model stops before this shared pipeline action | - |
| `artifacts/tla-results/Use Case/UC008_Hhp_StandardCompile.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC009_Hhp_OutRelativeOverride.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC010_Hhp_OutAbsoluteOverride.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC011_Hhp_CompiledFileOmitted.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC012_Hhp_ProjectMissing.log` | yes | `BuildMetadata`, `CollectFiles`, `StayDone`, `WriteChm` | `BuildMetadata`: use-case model stops before this shared pipeline action; `CollectFiles`: use-case model stops before this shared pipeline action; `StayDone`: documented unreachable/stutter action; `WriteChm`: use-case model stops before this shared pipeline action | - |
| `artifacts/tla-results/Use Case/UC013_Hhp_CommentsIgnored.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC014_Hhp_CaseInsensitiveNames.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC015_Hhp_QuotedValues.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC016_Hhp_DuplicateOptionLastWins.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC017_Hhp_PreSectionLinesIgnored.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC018_Files_RequiredFiles.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC019_Files_DefaultTopicFirstHtml.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC020_Files_GenerateContents.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC021_Files_MissingRequiredFails.log` | yes | `BuildMetadata`, `StayDone`, `WriteChm` | `BuildMetadata`: use-case model stops before this shared pipeline action; `StayDone`: documented unreachable/stutter action; `WriteChm`: use-case model stops before this shared pipeline action | - |
| `artifacts/tla-results/Use Case/UC022_Files_AllowMissing.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC023_Files_DuplicateSameFile.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC024_Files_DuplicateConflict.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC025_Files_Verbose.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC026_Links_HtmlHrefSrc.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC027_Links_CssImportUrl.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC028_Links_LocalParam.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC029_Links_NoLinkScan.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC030_Links_ExternalIgnored.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC031_Links_QueryFragment.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC032_Links_EntityPercent.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC033_Links_RelativeBase.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC034_Links_RootRelative.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC035_Links_NonScannable.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC036_Links_OptionalMissing.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC037_Paths_NormalArchivePath.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC038_Paths_NormalizeDots.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC039_Paths_OutsideRelativeLink.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC040_Paths_FlatNames.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC041_Paths_FlatRewrite.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC042_Encoding_BomText.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC043_Encoding_StrictUtf8.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC044_Encoding_LcidAnsi.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC045_Encoding_DeclaredLanguageRead.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC046_Encoding_CurrentCultureFallback.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC047_Encoding_DbcsLanguages.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC048_Metadata_CoreFields.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC049_Metadata_TitleOmitted.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC050_Metadata_DefaultWindowMain.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC051_Metadata_WindowAndFont.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC052_Chm_InternalStreams.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC053_Chm_SmallPmglOnly.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC054_Chm_LargePmgi.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC055_Chm_Uncompressed.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC056_Unsupported_FullTextSearch.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC057_Unsupported_BinaryToc.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC058_Unsupported_BinaryIndex.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC059_Unsupported_MergeFiles.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC060_Unsupported_WindowsSection.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC061_Error_OutputUnwritable.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC062_Error_DirectoryEntryTooLarge.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC063_Error_DirectoryTooLarge.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC064_Cli_HelpShortCircuits.log` | yes | `BuildMetadata`, `CollectFiles`, `LoadProject`, `StayDone`, `WriteChm` | `BuildMetadata`: use-case model stops before this shared pipeline action; `CollectFiles`: use-case model stops before this shared pipeline action; `LoadProject`: use-case model stops before this shared pipeline action; `StayDone`: documented unreachable/stutter action; `WriteChm`: use-case model stops before this shared pipeline action | - |
| `artifacts/tla-results/Use Case/UC065_Cli_VersionShortCircuits.log` | yes | `BuildMetadata`, `CollectFiles`, `LoadProject`, `StayDone`, `WriteChm` | `BuildMetadata`: use-case model stops before this shared pipeline action; `CollectFiles`: use-case model stops before this shared pipeline action; `LoadProject`: use-case model stops before this shared pipeline action; `StayDone`: documented unreachable/stutter action; `WriteChm`: use-case model stops before this shared pipeline action | - |
| `artifacts/tla-results/Use Case/UC066_Cli_RepeatedOutLastWins.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC067_Cli_OutBeforeProject.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC068_Hhp_BlankCompiledFileIgnored.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC069_Hhp_UnbalancedQuoteLiteral.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC070_Hhp_TruthyFlatOn.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC071_Files_DotPathIgnored.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC072_Files_AbsoluteProjectFileNameOnly.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC073_Files_OutsideRelativeProjectPath.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC074_Links_EmptyAndFragmentIgnored.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC075_Links_UncIgnored.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC076_Links_MalformedPercentKept.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC077_Links_ReadErrorAbsorbed.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC078_Encoding_InvalidUtf8Fallback.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC079_Encoding_LanguageHexWithoutPrefix.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC080_Metadata_NoDefaultTopicWhenNoHtml.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC081_Metadata_FlatOptionArchivePaths.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC082_Chm_StringTableDeduplicates.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC083_Error_CollectInputReadFailure.log` | yes | `BuildMetadata`, `StayDone`, `WriteChm` | `BuildMetadata`: use-case model stops before this shared pipeline action; `StayDone`: documented unreachable/stutter action; `WriteChm`: use-case model stops before this shared pipeline action | - |
| `artifacts/tla-results/Use Case/UC084_Error_WriterInputReadFailure.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC085_Error_LockedOutputPreserved.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC086_Error_MetadataEntryTooLarge.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |
| `artifacts/tla-results/Use Case/UC087_Files_GeneratedContentsEscapesHtml.log` | yes | `StayDone` | `StayDone`: documented unreachable/stutter action | - |

Unexpected coverage issues: 0
