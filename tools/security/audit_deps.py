"""Fail the commit when a frontend or .NET backend depends on a known-vulnerable package."""

import json
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
FAIL_SEVERITIES = {"high", "critical"}


def require(tool: str) -> str:
    path = shutil.which(tool)
    if path is None:
        sys.exit(f"{tool} is not on PATH; install it to run this security check.")
    return path


def run(cmd: list[str], cwd: Path) -> subprocess.CompletedProcess[str]:
    # Commands are fixed argument lists built from resolved tool paths and tracked files.
    return subprocess.run(cmd, cwd=cwd, capture_output=True, text=True, check=False)  # noqa: S603


def tracked(pattern: str) -> list[Path]:
    result = run([require("git"), "ls-files", pattern], ROOT)
    return [ROOT / line for line in result.stdout.splitlines() if line]


SHARED_CONFIG = {"nuget.config", "directory.packages.props", "directory.build.props", "directory.build.targets"}


def affected(candidates: list[Path], changed: list[Path]) -> list[Path]:
    # No changed files means a manual full audit. Shared NuGet config in a parent folder also applies below it.
    if not changed:
        return candidates
    return [
        candidate
        for candidate in candidates
        if any(
            candidate.parent in path.parents
            or (path.name.lower() in SHARED_CONFIG and path.parent in candidate.parent.parents)
            for path in changed
        )
    ]


def audit_npm(changed: list[Path]) -> list[str]:
    npm = require("npm")
    failed = []
    for lockfile in affected(tracked("*package-lock.json"), changed):
        project = lockfile.parent
        name = project.relative_to(ROOT).as_posix()
        # Dev-only tooling (vitest, build plugins) never ships in the nginx image.
        result = run([npm, "audit", "--omit=dev", "--audit-level=high"], project)
        if result.returncode != 0:
            print(f"--- {name} ---\n{result.stdout}{result.stderr}")
            failed.append(name)
        else:
            print(f"ok    {name}")
    return failed


def vulnerable_packages(report: dict) -> list[str]:
    found = []
    for project in report.get("projects", []):
        for framework in project.get("frameworks", []):
            packages = framework.get("topLevelPackages", []) + framework.get("transitivePackages", [])
            for package in packages:
                for vuln in package.get("vulnerabilities", []):
                    if vuln.get("severity", "").lower() in FAIL_SEVERITIES:
                        found.append(
                            f"{Path(project['path']).name}: {package['id']} {package['resolvedVersion']}"
                            f" ({vuln['severity']}) {vuln.get('advisoryurl', '')}"
                        )
    return found


def audit_dotnet(changed: list[Path]) -> list[str]:
    dotnet = require("dotnet")
    failed = []
    for solution in affected(tracked("*.slnx"), changed):
        name = solution.relative_to(ROOT).as_posix()
        restore = run([dotnet, "restore", str(solution), "--verbosity", "quiet"], ROOT)
        if restore.returncode != 0:
            print(f"--- {name} (restore failed) ---\n{restore.stdout}{restore.stderr}")
            failed.append(name)
            continue
        result = run(
            [dotnet, "list", str(solution), "package", "--vulnerable", "--include-transitive", "--format", "json"],
            ROOT,
        )
        try:
            report = json.loads(result.stdout)
        except json.JSONDecodeError:
            print(f"--- {name} (unreadable output) ---\n{result.stdout}{result.stderr}")
            failed.append(name)
            continue
        findings = vulnerable_packages(report)
        if findings:
            print(f"--- {name} ---\n" + "\n".join(findings))
            failed.append(name)
        else:
            print(f"ok    {name}")
    return failed


def main() -> int:
    audits = {"npm": audit_npm, "dotnet": audit_dotnet}
    if len(sys.argv) < 2 or sys.argv[1] not in audits:
        sys.exit(f"usage: {Path(__file__).name} {{{'|'.join(audits)}}} [changed files...]")
    changed = [ROOT / arg for arg in sys.argv[2:]]
    failed = audits[sys.argv[1]](changed)
    if failed:
        print(f"\nHigh/critical vulnerabilities in: {', '.join(failed)}")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
