"""Build per-service dotnet test filters and per-student endpoint reports."""

import argparse
import json
import os
import sys
from datetime import datetime, timezone
from pathlib import Path
from xml.etree import ElementTree  # noqa: S405

REPO_ROOT = Path(__file__).resolve().parents[2]
CONFIG_PATH = Path(__file__).resolve().parent / "student_endpoints.json"
DEFAULT_OUT_DIR = REPO_ROOT / "reports" / "post-commit" / "endpoints"


def load_students(service):
    students = json.loads(CONFIG_PATH.read_text(encoding="utf-8"))["students"]
    return [student for student in students if student["service"] == service]


def build_filter(students):
    names = [endpoint["test"] for student in students for endpoint in student["endpoints"]]
    return "|".join(f"FullyQualifiedName~{name}" for name in names)


def read_outcomes(results_dir):
    outcomes = {}
    for trx_path in results_dir.rglob("*.trx"):
        # TRX files come from a local dotnet test run, so parsing them is safe here.
        root = ElementTree.parse(trx_path).getroot()  # noqa: S314
        for element in root.iter():
            if element.tag.endswith("UnitTestResult"):
                outcomes[element.get("testName", "")] = element.get("outcome", "Unknown")
    return outcomes


def find_outcome(test_name, outcomes):
    for full_name, outcome in outcomes.items():
        if test_name in full_name:
            return outcome
    return "NotRun"


def write_reports(service, results_dir, out_dir):
    students = load_students(service)
    outcomes = read_outcomes(results_dir)
    generated = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M:%S UTC")
    commit = os.environ.get("GITHUB_SHA", "local")
    out_dir.mkdir(parents=True, exist_ok=True)

    for student in students:
        lines = [
            f"# Post-commit endpoint tests: {student['id']} ({student['owner']})",
            "",
            f"- Service: `{service}`",
            f"- Commit: `{commit}`",
            f"- Generated: {generated}",
            "",
            "| Method | Route | Test | Result |",
            "|---|---|---|---|",
        ]
        for endpoint in student["endpoints"]:
            result = find_outcome(endpoint["test"], outcomes)
            lines.append(
                f"| {endpoint['method']} | `{endpoint['route']}` | `{endpoint['test']}` | {result} |"
            )
        (out_dir / f"{student['id']}.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
        print(f"wrote {out_dir / (student['id'] + '.md')}")


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    subparsers = parser.add_subparsers(dest="command", required=True)

    filter_parser = subparsers.add_parser("filter", help="print the dotnet test filter for a service")
    filter_parser.add_argument("--service", required=True)

    report_parser = subparsers.add_parser("report", help="write per-student markdown reports")
    report_parser.add_argument("--service", required=True)
    report_parser.add_argument("--results-dir", required=True, type=Path)
    report_parser.add_argument("--out-dir", type=Path, default=DEFAULT_OUT_DIR)

    args = parser.parse_args(argv)
    students = load_students(args.service)
    if not students:
        print(f"no students configured for service: {args.service}", file=sys.stderr)
        return 1

    if args.command == "filter":
        print(build_filter(students))
    else:
        write_reports(args.service, args.results_dir, args.out_dir)
    return 0


if __name__ == "__main__":
    sys.exit(main())
