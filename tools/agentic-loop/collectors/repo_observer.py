"""OBSERVE stage: deterministic, locally computed evidence about the code.

Nothing here involves a model. These are verifiable facts that both the human
and the agent can rely on, and they are recorded verbatim in the evidence log.
"""

from __future__ import annotations

import re
import subprocess
from collections import Counter
from dataclasses import dataclass, field
from pathlib import Path, PurePosixPath

from collectors.file_reader import CodeBundle
from collectors.repo_scanner import ScanResult

# A path segment or filename part that means "test", e.g. tests/, test_x.py, x.spec.ts
TEST_SEGMENT = re.compile(r"(?:^|[/_.\-])(?:tests?|specs?|__tests__)(?:[/_.\-]|$)", re.IGNORECASE)
# A camel-case suffix, e.g. UserServiceTests.cs, but not "latest.py"
TEST_SUFFIX = re.compile(r"(?:(?:^|[^A-Za-z])[Tt]ests?|Tests?|Specs?)$")


@dataclass
class Observation:
    scope: str
    file_count: int
    total_lines: int
    total_bytes: int
    extension_breakdown: list[tuple[str, int]]
    test_file_count: int
    test_file_examples: list[str]
    largest_files: list[tuple[str, int]]
    git_branch: str | None
    git_commit: str | None
    warnings: list[str] = field(default_factory=list)

    def as_text(self) -> str:
        lines = [
            f"Scope: {self.scope}",
            f"Reviewable files: {self.file_count}",
            f"Total lines: {self.total_lines}",
            f"Total bytes: {self.total_bytes}",
        ]

        if self.extension_breakdown:
            breakdown = ", ".join(f"{ext} x{count}" for ext, count in self.extension_breakdown)
            lines.append(f"File types: {breakdown}")

        lines.append(f"Test files detected: {self.test_file_count}")
        if self.test_file_examples:
            lines.append("Test file examples: " + ", ".join(self.test_file_examples))

        if self.largest_files:
            largest = ", ".join(f"{path} ({size} bytes)" for path, size in self.largest_files)
            lines.append(f"Largest files: {largest}")

        if self.git_branch or self.git_commit:
            lines.append(f"Git: branch={self.git_branch or 'unknown'} commit={self.git_commit or 'unknown'}")

        if self.warnings:
            lines.append("Warnings:")
            lines.extend(f"  - {warning}" for warning in self.warnings)

        return "\n".join(lines)


def _git(repo_root: Path, *args: str) -> str | None:
    try:
        result = subprocess.run(
            ["git", "-C", str(repo_root), *args],
            capture_output=True,
            text=True,
            timeout=5,
            check=False,
        )
    except (OSError, subprocess.SubprocessError):
        return None
    output = result.stdout.strip()
    return output if result.returncode == 0 and output else None


def _is_test_file(relative_path: str) -> bool:
    """True for conventional test paths, without matching words that merely contain 'test'."""
    if TEST_SEGMENT.search(relative_path):
        return True
    return bool(TEST_SUFFIX.search(PurePosixPath(relative_path).stem))


def observe(
    scan: ScanResult,
    bundle: CodeBundle | None = None,
    max_examples: int = 5,
) -> Observation:
    """Summarise a scan (and optionally the loaded bundle) into hard evidence."""
    extension_counter: Counter[str] = Counter()
    test_files: list[str] = []

    for entry in scan.entries:
        suffix = entry.path.suffix.lower() or f"({entry.path.name.lower()})"
        extension_counter[suffix] += 1
        if _is_test_file(entry.relative_path):
            test_files.append(entry.relative_path)

    largest = sorted(scan.entries, key=lambda e: e.size_bytes, reverse=True)[:max_examples]

    warnings: list[str] = []
    if not scan.entries:
        warnings.append(
            "No reviewable files were found. Check TARGETED_DIRECTORY and INCLUDE_EXTENSIONS."
        )
    if scan.skipped_secrets:
        warnings.append(
            f"{len(scan.skipped_secrets)} secret-like file(s) skipped and never sent to the model: "
            + ", ".join(scan.skipped_secrets[:max_examples])
        )
    if scan.skipped_binary:
        warnings.append(f"{len(scan.skipped_binary)} binary file(s) skipped.")
    if scan.skipped_unreadable:
        warnings.append(f"{len(scan.skipped_unreadable)} unreadable path(s) skipped.")
    if not test_files:
        warnings.append("No files matching test naming conventions were found in scope.")

    if bundle is not None:
        if bundle.truncated_files:
            warnings.append(
                f"{len(bundle.truncated_files)} file(s) truncated to fit MAX_FILE_BYTES: "
                + ", ".join(bundle.truncated_files[:max_examples])
            )
        if bundle.skipped_over_budget:
            warnings.append(
                f"{len(bundle.skipped_over_budget)} selected file(s) dropped after "
                "MAX_TOTAL_CONTEXT_BYTES was reached: "
                + ", ".join(bundle.skipped_over_budget[:max_examples])
            )
        if bundle.unreadable:
            warnings.append(
                f"{len(bundle.unreadable)} selected file(s) could not be read: "
                + ", ".join(bundle.unreadable[:max_examples])
            )

    return Observation(
        scope=str(scan.scope),
        file_count=len(scan.entries),
        total_lines=scan.total_lines,
        total_bytes=scan.total_bytes,
        extension_breakdown=extension_counter.most_common(10),
        test_file_count=len(test_files),
        test_file_examples=test_files[:max_examples],
        largest_files=[(entry.relative_path, entry.size_bytes) for entry in largest],
        git_branch=_git(scan.repo_root, "rev-parse", "--abbrev-ref", "HEAD"),
        git_commit=_git(scan.repo_root, "rev-parse", "--short", "HEAD"),
        warnings=warnings,
    )
