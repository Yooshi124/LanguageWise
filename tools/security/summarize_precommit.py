"""Turn a pre-commit run log into a markdown summary for the GitHub step summary."""
import re
import sys
from pathlib import Path

ANSI = re.compile(r"\x1b\[[0-9;]*m")
# Skipped hooks print a reason in parentheses before the status, e.g. "(no files to check)Skipped".
RESULT = re.compile(r"^(?P<name>.+?)\.{3,}\s*(?:\([^)]*\))?\s*(?P<status>Passed|Failed|Skipped)$")
FIELD = re.compile(r"^\s+(?P<key>Branch|Commit|Time|Files checked):\s*(?P<value>.*)$")
CONTEXT_HOOK = "Run context"
ICONS = {"Passed": "✅ Passed", "Failed": "❌ Failed", "Skipped": "⚪ Skipped"}


def cell(text: str) -> str:
    return text.replace("|", "\\|")


def parse_context(lines: list[str]) -> tuple[dict[str, str], list[str]]:
    """Read the block written by precommit_context.py, which follows the "Run context" line."""
    fields: dict[str, str] = {}
    files: list[str] = []
    start = next((i for i, line in enumerate(lines) if line.strip() == CONTEXT_HOOK), None)
    if start is None:
        return fields, files
    for line in lines[start + 1 :]:
        match = FIELD.match(line)
        if match:
            fields[match["key"]] = match["value"]
        elif "Files checked" in fields and line.startswith("    ") and line.strip():
            files.append(line.strip())
        else:
            break
    return fields, files


def main() -> int:
    log = ANSI.sub("", Path(sys.argv[1]).read_text(encoding="utf-8", errors="replace"))
    lines = log.splitlines()
    fields, files = parse_context(lines)
    rows = [
        m
        for m in (RESULT.match(line.strip()) for line in lines)
        if m and m["name"].strip() != CONTEXT_HOOK
    ]

    print("## Security Check\n")
    if fields:
        print("### Run details\n")
        print("| Field | Value |")
        print("|---|---|")
        print(f"| Branch | {cell(fields.get('Branch', '(unknown)'))} |")
        print(f"| Commit | {cell(fields.get('Commit', '(unknown)'))} |")
        print(f"| Time | {cell(fields.get('Time', '(unknown)'))} |")
        print(f"| Files checked | {cell(fields.get('Files checked', '0'))} |")
        print()
        if files:
            print(f"<details><summary>Files checked ({len(files)})</summary>\n")
            print("```text")
            print("\n".join(files))
            print("```\n")
            print("</details>\n")

    print("### Hook results\n")
    if rows:
        print("| Hook | Result |")
        print("|---|---|")
        for row in rows:
            print(f"| {cell(row['name'])} | {ICONS[row['status']]} |")
        print()
    else:
        print("No hook results were found. See the full log below.\n")

    print("<details><summary>Full log</summary>\n")
    print("```text")
    print(log.rstrip())
    print("```\n")
    print("</details>")
    return 0


if __name__ == "__main__":
    sys.exit(main())
