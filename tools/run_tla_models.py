from __future__ import annotations

import sys

sys.dont_write_bytecode = True

from tla_runner import ROOT, run_suite


SUITES = (
    ("Implementation", ROOT / "docs" / "tla" / "implementation", "*Conformance.tla"),
    ("Core", ROOT / "docs" / "tla" / "core", "*.tla"),
    ("Use Case", ROOT / "docs" / "tla" / "usecases", "UC*.tla"),
)


def main() -> int:
    total = 0
    failures = 0
    for suite_name, models_dir, pattern in SUITES:
        results = run_suite(
            suite_name=suite_name,
            models_dir=models_dir,
            summary_path=models_dir / "verification-summary.md",
            pattern=pattern,
        )
        total += len(results)
        failures += sum(1 for result in results if not result.ok)

    if failures:
        print(f"{failures} of {total} TLA+ model(s) failed.")
        return 1

    print(f"All {total} TLA+ models passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
