# TLA+ Verification

Komura HHC uses three complementary TLA+ suites:

- `implementation`: the main conformance check. `docs/usecases.feature`
  and `docs/usecases.additional.feature` describe the observable Gherkin
  specification, `GherkinSpec.tla` projects it into TLC obligations, and
  `ImplementationConformance.tla` walks the modeled implementation stages to
  verify those obligations.
- `core`: hand-written abstract models for CLI parsing, path/link cleaning,
  text encoding, link scanning and rewriting, file collection, CHM metadata,
  and CHM directory contracts.
- `usecases`: generated smoke models derived from the Gherkin use cases.

Run everything with:

```powershell
python .\tools\run_tla_models.py
```

Run only the Gherkin-vs-implementation conformance check with:

```powershell
python .\tools\run_tla_implementation_model.py
```

The TLA+ models intentionally verify state transitions and public contracts, not
byte-for-byte CHM output. Concrete CHM header, PMGL/PMGI, internal stream, and
encoding checks are covered by the C# integration tests.

The local runner enables TLC coverage by default and records the coverage
setting plus each model log in the suite summary.
