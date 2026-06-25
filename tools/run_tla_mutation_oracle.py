from __future__ import annotations

import subprocess
import sys
import time
from dataclasses import dataclass
from pathlib import Path

sys.dont_write_bytecode = True

from tla_runner import ROOT


MODEL = ROOT / "docs" / "tla" / "inspection" / "ExistingCodeLoop.tla"
CFG = ROOT / "docs" / "tla" / "inspection" / "ExistingCodeLoop.cfg"
OUT = ROOT / "artifacts" / "tla-mutation-oracle"
SUMMARY = ROOT / "docs" / "tla" / "inspection" / "mutation-oracle-summary.md"
JAVA = ROOT / "artifacts" / "tools" / "jre21" / "jdk-21.0.11+10-jre" / "bin" / "java.exe"
TLA2TOOLS = ROOT / "artifacts" / "tools" / "tla2tools.jar"
RUN_OUT: Path | None = None


@dataclass(frozen=True)
class Mutant:
    ident: str
    description: str
    target: str
    replacement: str
    expected: str
    rationale: str


MUTANTS = [
    Mutant(
        "M001_eq_to_neq_final_exit",
        "Replace final exit-code equality with inequality.",
        '/\\ exitCode = ExpectedExit(scenario)',
        '/\\ exitCode # ExpectedExit(scenario)',
        "killed",
        "Final observable exit codes must match the current-code specification.",
    ),
    Mutant(
        "M002_neq_to_eq_error_chm",
        "Replace fatal no-valid-CHM inequality with equality.",
        "phase = \"Done\" /\\ scenario \\in {\"ArgError\", \"MissingProject\", \"OutputCreateFailure\", \"WriteFailureBeforePublish\"} => outputState # \"ValidChm\"",
        "phase = \"Done\" /\\ scenario \\in {\"ArgError\", \"MissingProject\", \"OutputCreateFailure\", \"WriteFailureBeforePublish\"} => outputState = \"ValidChm\"",
        "killed",
        "Fatal exits must not be classified as a valid CHM, while HHC-compatible partial-output cases remain valid CHMs.",
    ),
    Mutant(
        "M003_le_to_lt_retry_policy",
        "Tighten retryCount <= 0 to retryCount < 0.",
        '/\\ retryCount <= 0',
        '/\\ retryCount < 0',
        "killed",
        "The implementation has zero retries; making zero invalid should fail immediately.",
    ),
    Mutant(
        "M004_ge_to_gt_equivalent_retry_domain",
        "Replace retryCount >= 0 with retryCount > -1.",
        '/\\ retryCount >= 0',
        '/\\ retryCount > -1',
        "equivalent",
        "retryCount is always exactly 0 in all reachable states, so both predicates accept the same states.",
    ),
    Mutant(
        "M005_and_to_or_write_guard",
        "Change the WriteOutput phase guard conjunction marker to disjunction.",
        'WriteOutput ==\n  /\\ phase = "OutputOpened"',
        'WriteOutput ==\n  \\/ phase = "OutputOpened"',
        "killed",
        "The write transition must not be enabled outside the opened-output stage.",
    ),
    Mutant(
        "M006_guard_removed_missing_required",
        "Remove the MissingRequired guard by making it always true.",
        'IF scenario = "MissingRequired" THEN',
        'IF TRUE THEN',
        "killed",
        "Only the missing-required scenario may omit its archive payload and clear pending links at collection.",
    ),
    Mutant(
        "M007_package_update_deleted",
        "Delete the packageBuilt state update.",
        "/\\ packageBuilt' = TRUE",
        "/\\ packageBuilt' = packageBuilt",
        "killed",
        "A successful or write-failing output path must pass through package construction.",
    ),
    Mutant(
        "M008_pending_link_reset_deleted",
        "Delete the ScanLinks pending-link reset.",
        'ScanLinks ==\n  /\\ phase = "FilesCollected"\n  /\\ phase\' = "LinksScanned"\n  /\\ pendingLinks\' = {}',
        'ScanLinks ==\n  /\\ phase = "FilesCollected"\n  /\\ phase\' = "LinksScanned"\n  /\\ pendingLinks\' = pendingLinks',
        "killed",
        "Terminal states must not retain pending links.",
    ),
    Mutant(
        "M009_error_transition_changed_output_create",
        "Change output-create failure error kind to None.",
        '/\\ errorKind\' = "OutputCreateError"',
        '/\\ errorKind\' = "None"',
        "killed",
        "Output create failure must remain observable as an error.",
    ),
    Mutant(
        "M010_safety_guard_relaxed_create_output",
        "Allow CreateOutput from MetadataBuilt as well as PackageBuilt.",
        'CreateOutput ==\n  /\\ phase = "PackageBuilt"',
        'CreateOutput ==\n  /\\ phase \\in {"MetadataBuilt", "PackageBuilt"}',
        "killed",
        "A valid CHM must not be written before package construction.",
    ),
    Mutant(
        "M011_cancel_timeout_transition_changed",
        "Change the unreachable external abort exit code.",
        "/\\ exitCode' = 1\n  /\\ errorKind' = \"WriteError\"",
        "/\\ exitCode' = 0\n  /\\ errorKind' = \"WriteError\"",
        "equivalent",
        "External abort/cancel/timeout transitions are intentionally unreachable because the implementation exposes no such protocol.",
    ),
    Mutant(
        "M012_optional_link_warning_added",
        "Add HHC5003 warning accumulation to non-unsupported metadata build.",
        '/\\ warnings\' = IF scenario = \"UnsupportedWarning\" THEN warnings \\cup {\"unsupported feature\"} ELSE warnings',
        '/\\ warnings\' = IF scenario = \"UnsupportedWarning\" THEN warnings \\cup {\"unsupported feature\"} ELSE warnings \\cup {\"HHC5003\"}',
        "killed",
        "Optional link-read absorption and ordinary successful compiles must remain warning-free unless the current behavior explicitly emits HHC5003.",
    ),
    Mutant(
        "M013_link_absorb_update_deleted",
        "Delete the link-read absorbed marker update.",
        '/\\ linkReadAbsorbed\' = (scenario = "LinkReadFailure")',
        '/\\ linkReadAbsorbed\' = FALSE',
        "killed",
        "The absorbed link-read failure must be observable in the model state.",
    ),
    Mutant(
        "M014_output_create_condition_inverted",
        "Invert the output-create failure branch condition.",
        'IF scenario = "OutputCreateFailure" THEN',
        'IF scenario # "OutputCreateFailure" THEN',
        "killed",
        "Only output-create failure may avoid opening the output stream at that stage.",
    ),
    Mutant(
        "M015_temp_write_failure_leaves_partial",
        "Change temp-write failure to leave a partial final output.",
        'IF scenario = "WriteFailureBeforePublish" THEN\n       /\\ exitCode\' = 1\n       /\\ errorKind\' = "WriteError"\n       /\\ outputState\' = "Existing"',
        'IF scenario = "WriteFailureBeforePublish" THEN\n       /\\ exitCode\' = 1\n       /\\ errorKind\' = "WriteError"\n       /\\ outputState\' = "Partial"',
        "killed",
        "A staging failure must preserve the existing final output and must not publish partial bytes.",
    ),
    Mutant(
        "M016_outside_project_path_escapes_archive",
        "Change outside-project archive naming from basename-only to escaped.",
        "IF scenario = \"OutsideProjectPath\" THEN \"BasenameOnly\"\n       ELSE IF scenario = \"MissingRequired\" THEN \"None\"\n       ELSE \"InsideRelative\"",
        "IF scenario = \"OutsideProjectPath\" THEN \"Escaped\"\n       ELSE IF scenario = \"MissingRequired\" THEN \"None\"\n       ELSE \"InsideRelative\"",
        "killed",
        "Project paths outside the project directory must not leak parent/sibling path structure into the CHM archive namespace.",
    ),
    Mutant(
        "M017_plus_to_minus_transition_count",
        "Replace transition-count increment with decrement.",
        "/\\ transitionCount' = transitionCount + 1",
        "/\\ transitionCount' = transitionCount - 1",
        "killed",
        "Each non-stutter transition must advance the abstract path length by one.",
    ),
    Mutant(
        "M018_abort_action_removed_equivalent",
        "Delete the unreachable external abort action from Next.",
        "  \\/ ExternalAbort\n  \\/ StayDone",
        "  \\/ StayDone",
        "equivalent",
        "Cancel/timeout/abort behavior is intentionally unreachable because the implementation exposes no such protocol.",
    ),
    Mutant(
        "M019_success_publish_update_deleted",
        "Delete the success output-state update by retaining the previous output state.",
        "ELSE\n       /\\ exitCode' = IF scenario \\in {\"MissingRequired\", \"AllowMissing\"} THEN 0 ELSE 1\n       /\\ errorKind' = \"None\"\n       /\\ outputState' = \"ValidChm\"",
        "ELSE\n       /\\ exitCode' = IF scenario \\in {\"MissingRequired\", \"AllowMissing\"} THEN 0 ELSE 1\n       /\\ errorKind' = \"None\"\n       /\\ outputState' = outputState",
        "killed",
        "A successful compile must publish a valid CHM, not leave the output absent or unchanged.",
    ),
    Mutant(
        "M020_write_failure_returns_success",
        "Map the write-failure terminal transition to a success result.",
        'IF scenario = "WriteFailureBeforePublish" THEN\n       /\\ exitCode\' = 1\n       /\\ errorKind\' = "WriteError"\n       /\\ outputState\' = "Existing"',
        'IF scenario = "WriteFailureBeforePublish" THEN\n       /\\ exitCode\' = 0\n       /\\ errorKind\' = "None"\n       /\\ outputState\' = "Existing"',
        "killed",
        "A failed temp-stage write must remain observable as a failure even when the existing output is preserved.",
    ),
    Mutant(
        "M021_unchanged_allows_archive_namespace_drift",
        "Remove archiveNamespace from the WriteOutput UNCHANGED set and allow nondeterministic drift.",
        "/\\ UNCHANGED <<scenario, filesCollected, archiveNamespace, metadataBuilt, packageBuilt, outputOpened, warnings, pendingLinks, linkReadAbsorbed, retryCount>>",
        "/\\ archiveNamespace' \\in ArchiveNamespaces\n  /\\ UNCHANGED <<scenario, filesCollected, metadataBuilt, packageBuilt, outputOpened, warnings, pendingLinks, linkReadAbsorbed, retryCount>>",
        "killed",
        "Writing output must not mutate the already-reviewed archive namespace safety decision.",
    ),
    Mutant(
        "M022_fairness_removed",
        "Remove weak fairness from the specification.",
        "Spec == Init /\\ [][Next]_vars /\\ WF_vars(Next)",
        "Spec == Init /\\ [][Next]_vars",
        "killed",
        "The eventual terminal-state property relies on fairness to rule out infinite stuttering before completion.",
    ),
    Mutant(
        "M023_package_action_removed",
        "Delete the package-building action from Next.",
        "  \\/ BuildPackage\n  \\/ CreateOutput",
        "  \\/ CreateOutput",
        "killed",
        "Every successful or write-failing output path must be able to pass through package construction.",
    ),
]


def run_tlc(model: Path, cfg: Path, log: Path) -> tuple[bool, str, str]:
    stamp = time.time_ns()
    run_out = RUN_OUT or OUT
    metadir = run_out / "states" / f"{model.stem}-{stamp}"
    tmpdir = run_out / "tmp" / f"{model.stem}-{stamp}"
    metadir.mkdir(parents=True, exist_ok=True)
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
        "-coverage",
        "1",
        "-config",
        str(cfg),
        str(model),
    ]
    completed = subprocess.run(
        command,
        cwd=ROOT,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        timeout=120,
    )
    log.write_text(completed.stdout, encoding="utf-8", newline="\n")
    ok = completed.returncode == 0 and "No error has been found." in completed.stdout
    violation = next(
        (line for line in completed.stdout.splitlines() if line.startswith("Error:")),
        "",
    )
    return ok, violation, completed.stdout


def classify(mutant: Mutant, ok: bool) -> str:
    if not ok:
        return "killed"
    if mutant.expected == "equivalent":
        return "equivalent"
    return "true survivor"


def main() -> int:
    global RUN_OUT

    if not JAVA.exists():
        raise SystemExit(f"java.exe not found: {JAVA}")
    if not TLA2TOOLS.exists():
        raise SystemExit(f"tla2tools.jar not found: {TLA2TOOLS}")

    OUT.mkdir(parents=True, exist_ok=True)
    RUN_OUT = OUT / f"run-{time.strftime('%Y%m%d-%H%M%S')}-{time.time_ns()}"
    mutant_dir = RUN_OUT / "models"
    log_dir = RUN_OUT / "logs"
    mutant_dir.mkdir(parents=True, exist_ok=True)
    log_dir.mkdir(parents=True, exist_ok=True)

    base_text = MODEL.read_text(encoding="utf-8")
    cfg_text = CFG.read_text(encoding="utf-8")
    rows: list[tuple[Mutant, str, str, Path]] = []

    for mutant in MUTANTS:
        if mutant.target not in base_text:
            raise SystemExit(f"Target text not found for {mutant.ident}: {mutant.target!r}")
        module_name = "Mutant_" + "".join(ch if ch.isalnum() else "_" for ch in mutant.ident)
        text = base_text.replace("---- MODULE ExistingCodeLoop ----", f"---- MODULE {module_name} ----", 1)
        text = text.replace(mutant.target, mutant.replacement, 1)
        model_path = mutant_dir / f"{module_name}.tla"
        cfg_path = mutant_dir / f"{module_name}.cfg"
        log_path = log_dir / f"{mutant.ident}.log"
        model_path.write_text(text, encoding="utf-8", newline="\n")
        cfg_path.write_text(cfg_text, encoding="utf-8", newline="\n")
        ok, violation, _ = run_tlc(model_path, cfg_path, log_path)
        classification = classify(mutant, ok)
        rows.append((mutant, classification, violation, log_path))
        print(f"{classification.upper()} {mutant.ident}")

    killed = sum(1 for _, classification, _, _ in rows if classification == "killed")
    equivalent = sum(1 for _, classification, _, _ in rows if classification == "equivalent")
    survivors = [row for row in rows if row[1] == "true survivor"]

    summary = [
        "# TLA+ Mutation Oracle Summary",
        "",
        f"Mutants: {len(rows)}",
        f"Killed: {killed}",
        f"Equivalent: {equivalent}",
        f"True survivors: {len(survivors)}",
        "TLC coverage: enabled (1 minute interval)",
        "",
        "| Mutant | Expected | Classification | Description | Rationale | TLC signal | Log |",
        "| --- | --- | --- | --- | --- | --- | --- |",
    ]
    for mutant, classification, violation, log_path in rows:
        rel_log = log_path.relative_to(ROOT).as_posix()
        signal = violation.replace("|", "\\|") if violation else "No TLC violation"
        summary.append(
            f"| `{mutant.ident}` | {mutant.expected} | {classification} | "
            f"{mutant.description} | {mutant.rationale} | {signal} | `{rel_log}` |"
        )
    SUMMARY.write_text("\n".join(summary) + "\n", encoding="utf-8", newline="\n")
    print(f"Summary: {SUMMARY}")
    return 1 if survivors else 0


if __name__ == "__main__":
    raise SystemExit(main())
