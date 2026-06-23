from __future__ import annotations

import concurrent.futures
import subprocess
import time
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
RESULTS = ROOT / "artifacts" / "tla-results"
JAVA = ROOT / "artifacts" / "tools" / "jre21" / "jdk-21.0.11+10-jre" / "bin" / "java.exe"
TLA2TOOLS = ROOT / "artifacts" / "tools" / "tla2tools.jar"


@dataclass(frozen=True)
class Result:
    model: str
    ok: bool
    seconds: float
    states_line: str
    depth_line: str
    log_path: Path
    coverage: str


def run_one(model: Path, suite_name: str, timeout_seconds: int = 120, coverage_minutes: int = 1) -> Result:
    cfg = model.with_suffix(".cfg")
    log_path = RESULTS / suite_name / f"{model.stem}.log"
    metadir = RESULTS / suite_name / "states" / f"{model.stem}-{time.time_ns()}"
    tmpdir = RESULTS / suite_name / "tmp" / f"{model.stem}-{time.time_ns()}"
    log_path.parent.mkdir(parents=True, exist_ok=True)
    metadir.parent.mkdir(parents=True, exist_ok=True)
    tmpdir.mkdir(parents=True, exist_ok=True)

    command = [
        str(JAVA),
        "-Xmx256m",
        "-XX:+UseParallelGC",
        f"-Djava.io.tmpdir={tmpdir}",
        "-cp",
        str(TLA2TOOLS),
        "tlc2.TLC",
        "-cleanup",
        "-workers",
        "1",
        "-metadir",
        str(metadir),
    ]
    if coverage_minutes > 0:
        command.extend(["-coverage", str(coverage_minutes)])
    command.extend([
        "-config",
        str(cfg),
        str(model),
    ])
    start = time.perf_counter()
    completed = subprocess.run(
        command,
        cwd=ROOT,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        timeout=timeout_seconds,
    )
    seconds = time.perf_counter() - start
    log_path.write_text(completed.stdout, encoding="utf-8", newline="\n")
    lines = completed.stdout.splitlines()
    states_line = next((line for line in lines if "states generated" in line and "distinct states" in line), "")
    depth_line = next((line for line in lines if "The depth of the complete state graph search" in line), "")
    ok = completed.returncode == 0 and "No error has been found." in completed.stdout
    coverage = f"enabled ({coverage_minutes}m)" if coverage_minutes > 0 else "disabled"
    return Result(model.name, ok, seconds, states_line, depth_line, log_path, coverage)


def run_suite(
    suite_name: str,
    models_dir: Path,
    summary_path: Path,
    pattern: str = "*.tla",
    max_workers: int = 6,
    timeout_seconds: int = 120,
    coverage_minutes: int = 1,
) -> list[Result]:
    if not JAVA.exists():
        raise SystemExit(f"java.exe not found: {JAVA}")
    if not TLA2TOOLS.exists():
        raise SystemExit(f"tla2tools.jar not found: {TLA2TOOLS}")

    RESULTS.mkdir(parents=True, exist_ok=True)
    models = sorted(models_dir.glob(pattern))
    if not models:
        raise SystemExit(f"No TLA+ models matching {pattern} found in {models_dir}")

    started = time.perf_counter()
    results: list[Result] = []
    with concurrent.futures.ThreadPoolExecutor(max_workers=max_workers) as executor:
        futures = {executor.submit(run_one, model, suite_name, timeout_seconds, coverage_minutes): model for model in models}
        for future in concurrent.futures.as_completed(futures):
            result = future.result()
            results.append(result)
            print(f"{'PASS' if result.ok else 'FAIL'} {suite_name}/{result.model} ({result.seconds:.1f}s)")

    results.sort(key=lambda item: item.model)
    failures = [result for result in results if not result.ok]
    elapsed = time.perf_counter() - started

    summary = [
        f"# TLA+ {suite_name} Verification Summary",
        "",
        f"Verified models: {len(results)}",
        f"Passed: {len(results) - len(failures)}",
        f"Failed: {len(failures)}",
        f"Elapsed seconds: {elapsed:.1f}",
        f"TLC coverage: {'enabled' if coverage_minutes > 0 else 'disabled'}" + (f" ({coverage_minutes} minute interval)" if coverage_minutes > 0 else ""),
        "",
        "| Model | Status | Seconds | Coverage | States | Depth | Log |",
        "| --- | --- | ---: | --- | --- | --- | --- |",
    ]
    for result in results:
        status = "PASS" if result.ok else "FAIL"
        states = result.states_line.replace("|", "\\|") or "-"
        depth = result.depth_line.replace("|", "\\|") or "-"
        rel_log = result.log_path.relative_to(ROOT).as_posix()
        summary.append(f"| `{result.model}` | {status} | {result.seconds:.1f} | {result.coverage} | {states} | {depth} | `{rel_log}` |")

    summary_path.write_text("\n".join(summary) + "\n", encoding="utf-8", newline="\n")
    return results
