"""Append-only workflow evidence for multi-agent rounds.

- `workflow_history.jsonl`: one full record per workflow (plan, worker output,
  review, human decision).
- `coordination_audit.jsonl`: one compact record per workflow for governance
  audits (who participated, status, risk, decision).

Both live in the sessions directory and every line is secret-scrubbed.
"""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from core.redaction import scrub

HISTORY_FILENAME = "workflow_history.jsonl"
AUDIT_FILENAME = "coordination_audit.jsonl"


def _append(path: Path, record: dict[str, Any]) -> None:
    with path.open("a", encoding="utf-8") as handle:
        handle.write(scrub(json.dumps(record, ensure_ascii=False, default=str)) + "\n")


def append_workflow(
    directory: Path, history: dict[str, Any], audit: dict[str, Any]
) -> tuple[Path, Path]:
    directory = Path(directory)
    directory.mkdir(parents=True, exist_ok=True)
    history_path = directory / HISTORY_FILENAME
    audit_path = directory / AUDIT_FILENAME
    _append(history_path, history)
    _append(audit_path, audit)
    return history_path, audit_path
