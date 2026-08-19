"""Walks the review scope and builds a manifest of candidate files.

The scanner is deliberately conservative: it skips anything that looks like a
secret, anything binary, and anything inside an ignored directory, so no
credential ever reaches the model.
"""

from __future__ import annotations

import fnmatch
import os
from dataclasses import dataclass
from pathlib import Path

from config.settings import EXTENSIONLESS_FILENAMES, Settings

BINARY_SNIFF_BYTES = 4096


@dataclass(frozen=True)
class ManifestEntry:
    """One candidate file discovered in the review scope."""

    path: Path
    relative_path: str
    size_bytes: int
    line_count: int

    def manifest_line(self) -> str:
        return f"{self.relative_path} | {self.size_bytes} bytes | {self.line_count} lines"


@dataclass
class ScanResult:
    scope: Path
    repo_root: Path
    entries: list[ManifestEntry]
    skipped_secrets: list[str]
    skipped_binary: list[str]
    skipped_unreadable: list[str]
    ignored_dir_count: int

    @property
    def total_bytes(self) -> int:
        return sum(entry.size_bytes for entry in self.entries)

    @property
    def total_lines(self) -> int:
        return sum(entry.line_count for entry in self.entries)

    def manifest_text(self, limit: int | None = None) -> str:
        entries = self.entries if limit is None else self.entries[:limit]
        if not entries:
            return "(no reviewable files found in scope)"
        lines = [entry.manifest_line() for entry in entries]
        if limit is not None and len(self.entries) > limit:
            lines.append(f"... and {len(self.entries) - limit} more files not listed")
        return "\n".join(lines)

    def by_relative_path(self) -> dict[str, ManifestEntry]:
        return {entry.relative_path: entry for entry in self.entries}


def is_secret_file(name: str, patterns: tuple[str, ...]) -> bool:
    lowered = name.lower()
    return any(fnmatch.fnmatch(lowered, pattern.lower()) for pattern in patterns)


def is_secret_path(name: str, relative_path: str, patterns: tuple[str, ...]) -> bool:
    """Match a secret pattern against the basename *and* the scope-relative path.

    Without the path check, `credentials/production.json` slips past a
    `credentials*` pattern because only `production.json` is ever tested.
    """
    if is_secret_file(name, patterns):
        return True
    lowered = relative_path.lower()
    for pattern in patterns:
        low_pattern = pattern.lower()
        if any(fnmatch.fnmatch(part, low_pattern) for part in lowered.split("/")):
            return True
    return False


def _is_traversable_dir(path: Path, root: Path) -> bool:
    """Reject symlinks, Windows junctions and anything resolving outside the root."""
    try:
        if path.is_symlink() or path.is_junction():
            return False
    except OSError:
        return False
    try:
        resolved = path.resolve()
    except (OSError, ValueError):
        return False
    if resolved.is_relative_to(root):
        return True
    # Windows paths can differ only by case; compare case-insensitively there.
    return os.name == "nt" and str(resolved).lower().startswith(str(root).lower())


def looks_binary(path: Path) -> bool:
    try:
        with path.open("rb") as handle:
            chunk = handle.read(BINARY_SNIFF_BYTES)
    except OSError:
        return True
    if b"\x00" in chunk:
        return True
    if not chunk:
        return False
    try:
        chunk.decode("utf-8")
    except UnicodeDecodeError as exc:
        # A multi-byte character split by the sniff boundary is not corruption:
        # only treat the failure as binary when it happens well inside the chunk.
        return exc.start < len(chunk) - 3
    return False


def _is_ignored_dir(name: str, ignore_dirs: frozenset[str]) -> bool:
    """Directory names are matched case-insensitively; Windows paths vary in case."""
    lowered = name.lower()
    return any(lowered == ignored.lower() for ignored in ignore_dirs)


def _is_included(path: Path, settings: Settings) -> bool:
    if path.suffix:
        return path.suffix.lower() in settings.include_extensions
    return path.name.lower() in EXTENSIONLESS_FILENAMES


def _count_lines(path: Path) -> int:
    try:
        with path.open("r", encoding="utf-8", errors="replace") as handle:
            return sum(1 for _ in handle)
    except OSError:
        return 0


def scan(settings: Settings, scope: Path | None = None) -> ScanResult:
    """Walk the scope and return every reviewable file."""
    root = (Path(scope) if scope else settings.scope).resolve()
    entries: list[ManifestEntry] = []
    skipped_secrets: list[str] = []
    skipped_binary: list[str] = []
    skipped_unreadable: list[str] = []
    ignored_dirs = 0

    def relative(path: Path) -> str:
        base = settings.repo_root
        try:
            return path.relative_to(base).as_posix()
        except ValueError:
            return path.as_posix()

    def walk(directory: Path) -> None:
        nonlocal ignored_dirs
        try:
            children = sorted(directory.iterdir(), key=lambda p: (p.is_file(), p.name.lower()))
        except OSError:
            skipped_unreadable.append(relative(directory))
            return

        for child in children:
            if child.is_symlink():
                continue
            if child.is_dir():
                if _is_ignored_dir(child.name, settings.ignore_dirs):
                    ignored_dirs += 1
                    continue
                if is_secret_path(child.name, relative(child), settings.secret_patterns):
                    skipped_secrets.append(f"{relative(child)}/ (directory)")
                    continue
                if not _is_traversable_dir(child, root):
                    ignored_dirs += 1
                    continue
                walk(child)
                continue

            if is_secret_path(child.name, relative(child), settings.secret_patterns):
                skipped_secrets.append(relative(child))
                continue
            if not _is_included(child, settings):
                continue
            try:
                size = child.stat().st_size
            except OSError:
                skipped_unreadable.append(relative(child))
                continue
            if size == 0:
                continue
            if looks_binary(child):
                skipped_binary.append(relative(child))
                continue

            entries.append(
                ManifestEntry(
                    path=child,
                    relative_path=relative(child),
                    size_bytes=size,
                    line_count=_count_lines(child),
                )
            )

    if root.is_dir():
        walk(root)

    entries.sort(key=lambda entry: entry.relative_path)
    return ScanResult(
        scope=root,
        repo_root=settings.repo_root,
        entries=entries,
        skipped_secrets=skipped_secrets,
        skipped_binary=skipped_binary,
        skipped_unreadable=skipped_unreadable,
        ignored_dir_count=ignored_dirs,
    )
