from __future__ import annotations

import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SUMMARY_FILES = [
    ROOT / "docs" / "tla" / "inspection" / "verification-summary.md",
    ROOT / "docs" / "tla" / "implementation" / "verification-summary.md",
    ROOT / "docs" / "tla" / "core" / "verification-summary.md",
    ROOT / "docs" / "tla" / "usecases" / "verification-summary.md",
]
OUT = ROOT / "docs" / "tla" / "coverage-audit.md"

LOG_RE = re.compile(r"`([^`]+\.log)`")
COVERAGE_RE = re.compile(r"The coverage statistics at ")
TOP_LEVEL_RE = re.compile(r"^<([^ >]+)[^>]*>: (\d+)(?::(\d+))?")
ALLOWED_ZERO = {"ExternalAbort", "StayDone"}
USE_CASE_SHARED_ACTIONS = {
    "LoadProject",
    "CollectFiles",
    "BuildMetadata",
    "WriteChm",
}


def collect_logs() -> list[Path]:
    logs: list[Path] = []
    for summary in SUMMARY_FILES:
        if not summary.exists():
            raise SystemExit(f"missing summary: {summary}")
        for match in LOG_RE.finditer(summary.read_text(encoding="utf-8")):
            path = ROOT / match.group(1).replace("/", "\\")
            if path not in logs:
                logs.append(path)
    return logs


def is_use_case_log(path: Path) -> bool:
    try:
        rel_parts = path.relative_to(ROOT).parts
    except ValueError:
        rel_parts = path.parts
    return "Use Case" in rel_parts


def zero_action_disposition(path: Path, action: str) -> str | None:
    if action in ALLOWED_ZERO:
        return "documented unreachable/stutter action"
    if is_use_case_log(path) and action in USE_CASE_SHARED_ACTIONS:
        return "use-case model stops before this shared pipeline action"
    return None


def inspect_log(path: Path) -> tuple[bool, list[str], list[str], dict[str, str]]:
    if not path.exists():
        return False, ["missing log"], ["missing log"], {}

    text = path.read_text(encoding="utf-8", errors="replace")
    has_coverage = COVERAGE_RE.search(text) is not None
    zero_ops: list[str] = []
    unexpected: list[str] = []
    dispositions: dict[str, str] = {}

    for line in text.splitlines():
        match = TOP_LEVEL_RE.match(line)
        if not match:
            continue
        name, first_count, second_count = match.groups()
        if second_count is None:
            continue
        if int(first_count) == 0:
            zero_ops.append(name)
            disposition = zero_action_disposition(path, name)
            if disposition is None:
                unexpected.append(name)
            else:
                dispositions[name] = disposition

    if not has_coverage:
        unexpected.append("coverage missing")

    return has_coverage, sorted(set(zero_ops)), sorted(set(unexpected)), dispositions


def main() -> int:
    rows: list[str] = []
    unexpected_total: list[str] = []
    logs = collect_logs()

    rows.extend(
        [
            "# TLA+ Coverage Audit",
            "",
            "This audit is generated from the TLC logs referenced by the current verification summaries.",
            "Coverage is expected to be present for every checked model. Zero-hit top-level actions are allowed for documented unreachable/stutter actions: `ExternalAbort` and `StayDone`.",
            "Use-case models also reuse a shared pipeline action skeleton; when a scenario exits early, later shared actions are recorded as intentionally zero-hit rather than as missing coverage.",
            "",
            f"Referenced logs: {len(logs)}",
            "",
            "| Log | Coverage present | Zero-hit top-level actions | Zero-hit disposition | Unexpected zero/missing coverage |",
            "| -- | -- | -- | -- | -- |",
        ]
    )

    for log in logs:
        has_coverage, zero_ops, unexpected, dispositions = inspect_log(log)
        rel = log.relative_to(ROOT).as_posix()
        zero_text = ", ".join(f"`{name}`" for name in zero_ops) if zero_ops else "-"
        disposition_text = (
            "; ".join(f"`{name}`: {dispositions[name]}" for name in zero_ops if name in dispositions)
            if dispositions
            else "-"
        )
        unexpected_text = ", ".join(f"`{name}`" for name in unexpected) if unexpected else "-"
        rows.append(
            f"| `{rel}` | {'yes' if has_coverage else 'no'} | {zero_text} | {disposition_text} | {unexpected_text} |"
        )
        unexpected_total.extend(f"{rel}: {item}" for item in unexpected)

    rows.extend(
        [
            "",
            f"Unexpected coverage issues: {len(unexpected_total)}",
        ]
    )

    if unexpected_total:
        rows.extend(f"- {item}" for item in unexpected_total)

    OUT.write_text("\n".join(rows) + "\n", encoding="utf-8", newline="\n")
    print(f"Coverage audit: {OUT}")
    if unexpected_total:
        print("Unexpected coverage issues found:")
        for item in unexpected_total:
            print(f"- {item}")
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
