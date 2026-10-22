"""Print the run context (branch, commit, time, files checked) at the top of a pre-commit run.

pre-commit passes the files this hook is run against as arguments, so the list
matches what the other hooks check: staged files locally, all tracked files in CI.
"""
import os
import subprocess
import sys
from datetime import datetime


def git(*args: str) -> str:
    try:
        result = subprocess.run(["git", *args], capture_output=True, text=True, check=True)  # noqa: S603, S607 -- fixed git argv; no shell.
    except (OSError, subprocess.CalledProcessError):
        return ""
    return result.stdout.strip()


def main() -> int:
    branch = (
        os.environ.get("GITHUB_HEAD_REF")
        or os.environ.get("GITHUB_REF_NAME")
        or git("rev-parse", "--abbrev-ref", "HEAD")
    )
    if branch == "HEAD":
        branch = "(detached)"
    commit = git("log", "-1", "--format=%h %s") or "(no commits yet)"
    now = datetime.now().astimezone().isoformat(timespec="seconds")
    files = sys.argv[1:]

    print("Run context")
    print(f"  Branch: {branch or '(unknown)'}")
    print(f"  Commit: {commit}")
    print(f"  Time: {now}")
    print(f"  Files checked: {len(files)}")
    for name in files:
        print(f"    {name}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
