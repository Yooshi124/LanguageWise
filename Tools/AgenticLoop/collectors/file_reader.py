"""Reads selected files into a size-capped bundle for the model prompt.

Truncation is always explicit: the model is told when it is looking at a partial
file, so it never reasons about code it cannot see.
"""

from __future__ import annotations

from dataclasses import dataclass, field

from collectors.repo_scanner import ManifestEntry
from config.settings import Settings

TRUNCATION_NOTE = "... [TRUNCATED — file exceeds MAX_FILE_BYTES; {shown} of {total} bytes shown]"

# Room reserved for the truncation marker so the total budget is never exceeded.
NOTE_RESERVE_BYTES = 160

# Below this, a fragment of a file is more misleading than useful.
MIN_USEFUL_BYTES = 512


@dataclass
class LoadedFile:
    relative_path: str
    content: str
    size_bytes: int
    truncated: bool = False

    def as_prompt_block(self) -> str:
        header = f"=== FILE: {self.relative_path} ==="
        return f"{header}\n{self.content}\n=== END FILE: {self.relative_path} ===\n"


@dataclass
class CodeBundle:
    files: list[LoadedFile] = field(default_factory=list)
    truncated_files: list[str] = field(default_factory=list)
    skipped_over_budget: list[str] = field(default_factory=list)
    unreadable: list[str] = field(default_factory=list)

    @property
    def total_bytes(self) -> int:
        return sum(len(file.content.encode("utf-8")) for file in self.files)

    def as_prompt_text(self) -> str:
        if not self.files:
            return "(no files were loaded)"
        return "\n".join(file.as_prompt_block() for file in self.files)

    def file_list_text(self) -> str:
        if not self.files:
            return "(none)"
        return "\n".join(
            f"- {file.relative_path}"
            + (" (truncated)" if file.truncated else "")
            for file in self.files
        )


def _block_overhead_bytes(relative_path: str) -> int:
    """Bytes `as_prompt_block` adds around a file's content, charged to the budget."""
    wrapper = f"=== FILE: {relative_path} ===\n\n=== END FILE: {relative_path} ===\n\n"
    return len(wrapper.encode("utf-8"))


def load_files(entries: list[ManifestEntry], settings: Settings) -> CodeBundle:
    """Read `entries` in order, honouring per-file and total context budgets."""
    bundle = CodeBundle()
    used = 0

    for entry in entries:
        overhead = _block_overhead_bytes(entry.relative_path)
        remaining = settings.max_total_context_bytes - used - overhead
        budget = min(settings.max_file_bytes, remaining)
        # Always reserve marker space so appending it can never breach the budget.
        content_cap = budget - NOTE_RESERVE_BYTES
        if content_cap < MIN_USEFUL_BYTES:
            bundle.skipped_over_budget.append(entry.relative_path)
            continue

        try:
            with entry.path.open("rb") as handle:
                # One byte past the cap is enough to know the file was truncated.
                raw = handle.read(content_cap + 1)
        except OSError:
            bundle.unreadable.append(entry.relative_path)
            continue

        total_size = max(entry.size_bytes, len(raw))
        truncated = total_size > content_cap

        text = raw[:content_cap].decode("utf-8", errors="replace")
        # Replacement characters are wider than the bytes they stand in for, so a
        # file with invalid encoding can grow past its allowance on decode.
        encoded = text.encode("utf-8")
        if len(encoded) > content_cap:
            text = encoded[:content_cap].decode("utf-8", errors="ignore")
            truncated = True

        if truncated:
            shown = len(text.encode("utf-8"))
            text += "\n" + TRUNCATION_NOTE.format(shown=shown, total=total_size)
            bundle.truncated_files.append(entry.relative_path)

        bundle.files.append(
            LoadedFile(
                relative_path=entry.relative_path,
                content=text,
                size_bytes=total_size,
                truncated=truncated,
            )
        )
        used += len(text.encode("utf-8")) + overhead

    return bundle
