from __future__ import annotations

import sys

sys.dont_write_bytecode = True

from tla_runner import ROOT, run_suite


MODELS = ROOT / "docs" / "tla" / "core"


def main() -> int:
    results = run_suite(
        suite_name="Core",
        models_dir=MODELS,
        summary_path=MODELS / "verification-summary.md",
    )
    failures = [result for result in results if not result.ok]
    if failures:
        print(f"{len(failures)} core model(s) failed. See {MODELS / 'verification-summary.md'}")
        return 1

    print(f"All {len(results)} core models passed. Summary: {MODELS / 'verification-summary.md'}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
