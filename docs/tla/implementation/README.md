# TLA+ Implementation Conformance

This suite expresses the workflow the user asked for:

1. `docs/usecases.feature` and `docs/usecases.additional.feature` are the
   reader-facing Gherkin specifications inferred from observable command
   behavior.
2. `GherkinSpec.tla` is the TLC-friendly projection of those scenarios into
   observable obligations.
3. `ImplementationConformance.tla` walks the implementation stages represented
   by `Program.Main`, `CliOptions.Parse`, `HhpProject.Load`,
   `ProjectCompiler.CollectFiles`, `ProjectCompiler.BuildMetadata`, and
   `ChmWriter.Write`.
4. TLC checks that every modeled implementation terminal state satisfies the
   Gherkin obligations.

The model also checks abnormal-case coverage explicitly: argument errors,
argument errors, compile errors, missing-project exits, partial-output success
cases, warning-bearing CHM outputs, and failure-without-CHM cases must all be
present in the Gherkin-derived obligations and must keep the expected
exit-code/CHM-creation behavior in the implementation model.

`ApiExceptionConformance.tla` injects exceptions at modeled implementation API
boundaries. Most API exceptions must become compile errors with exit code 1 and
no successful CHM. `LinkScanner.ExtractLinks` is the intentional exception: the
implementation catches read/parse failures there and continues as if the file
had no links.

The C# integration harness complements this with real OS-level cases: locked
input files, locked output files, unwritable output targets, and oversized CHM
metadata entries.

Run this suite:

```powershell
python .\tools\run_tla_implementation_model.py
```

Regenerate it after editing the Gherkin use cases:

```powershell
python .\tools\generate_tla_implementation_model.py
```
