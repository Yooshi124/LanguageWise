"""Turn a pre-commit run log into a markdown summary for the GitHub step summary."""
import re
import sys
from pathlib import Path

ANSI = re.compile(r"\x1b\[[0-9;]*m")
RESULT = re.compile(r"^(?P<name>.+?)\.{3,}\s*(?P<status>Passed|Failed|Skipped)$")
ICONS = {"Passed": "✅ Passed", "Failed": "❌ Failed", "Skipped": "⚪ Skipped"}


def main() -> int:
    log = ANSI.sub("", Path(sys.argv[1]).read_text(encoding="utf-8", errors="replace"))
    rows = [m for m in (RESULT.match(line.strip()) for line in log.splitlines()) if m]

    print("## Security Check\n")
    if rows:
        print("| Hook | Result |")
        print("|---|---|")
        for row in rows:
            print(f"| {row['name']} | {ICONS[row['status']]} |")
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
