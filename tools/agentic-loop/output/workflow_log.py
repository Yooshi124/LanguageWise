"""Append-only workflow evidence for multi-agent rounds.

- `workflow_history.jsonl`: one full record per workflow (planner, worker and
  reviewer results with their status, and the human decision).
- `coordination_audit.jsonl`: one compact record per workflow for governance
  audits (who participated, per-agent status, risk, decision).

A human decision made after the run (`--decide`) is appended as a separate
`human_decision` event to both files, so earlier lines are never rewritten.

Both live in the sessions directory and every line is secret-scrubbed.
"""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from core.redaction import scrub

HISTORY_FILENAME = "workflow_history.jsonl"
AUDIT_FILENAME = "coordination_audit.jsonl"

AGENT_KEYS = ("planner", "worker", "reviewer")
COMPLETED = "completed"
FAILED = "failed"
NOT_REACHED = "not_reached"
DECISION_EVENT = "human_decision"
PENDING_DECISION = "Pending"


class WorkflowLogError(RuntimeError):
    """Raised when a workflow cannot be found or updated in the history."""


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


def _read_history(directory: Path) -> list[dict[str, Any]]:
    path = Path(directory) / HISTORY_FILENAME
    if not path.is_file():
        raise WorkflowLogError(f"No workflow history found at {path}.")
    records = []
    with path.open("r", encoding="utf-8") as handle:
        for line in handle:
            if line.strip():
                records.append(json.loads(line))
    return records


def load_workflow(directory: Path, workflow_id: str) -> dict[str, Any]:
    """Return the workflow record (full ID or unique prefix) with its latest decision applied."""
    records = _read_history(directory)
    workflows = [record for record in records if record.get("event", "workflow") == "workflow"]
    matches = {
        record["workflow_id"]
        for record in workflows
        if str(record.get("workflow_id", "")).startswith(workflow_id.strip())
    }
    if not matches:
        raise WorkflowLogError(f"Workflow '{workflow_id}' is not in the workflow history.")
    if len(matches) > 1:
        raise WorkflowLogError(
            f"Workflow ID prefix '{workflow_id}' is ambiguous: {', '.join(sorted(matches))}."
        )
    full_id = matches.pop()
    record = dict(next(record for record in workflows if record["workflow_id"] == full_id))
    for event in records:
        if event.get("event") == DECISION_EVENT and event.get("workflow_id") == full_id:
            record["human_decision"] = event["human_decision"]
    return record


def agent_status(record: dict[str, Any], agent: str) -> str:
    """Status of one agent; older records without a status are inferred from their result."""
    result = record.get(f"{agent}_result")
    if isinstance(result, dict) and "status" in result and "agent" in result:
        return str(result["status"])
    return COMPLETED if result else NOT_REACHED


def evidence_count(record: dict[str, Any]) -> int:
    if "evidence_count" in record:
        return int(record["evidence_count"])
    return len(record.get("evidence_files") or [])


def decision_status(record: dict[str, Any]) -> str:
    decision = record.get("human_decision")
    if isinstance(decision, dict):
        return str(decision.get("status") or PENDING_DECISION)
    return str(decision or PENDING_DECISION)


def reviewer_is_independent(record: dict[str, Any]) -> bool:
    models = record.get("models") or {}
    return bool(models.get("reviewer_agent")) and models.get("reviewer_agent") != models.get(
        "worker_agent"
    )


def evidence_log_markdown(record: dict[str, Any], directory: Path) -> str:
    """The workflow evidence checklist: expected vs actual, pass or fail per item."""
    directory = Path(directory)
    models = record.get("models") or {}
    count = evidence_count(record)
    decision = decision_status(record)

    def row(item: str, expected: str, actual: str, passed: bool) -> str:
        return f"| {item} | {expected} | {actual} | {'Pass' if passed else 'Fail'} |"

    rows = [
        "| Evidence item | Expected | Actual result | Pass/Fail |",
        "| --- | --- | --- | --- |",
        row("Workflow ID recorded", "Present", f"`{record.get('workflow_id', '')}`",
            bool(record.get("workflow_id"))),
    ]
    for agent in AGENT_KEYS:
        status = agent_status(record, agent)
        rows.append(row(f"{agent.capitalize()} status", COMPLETED, status, status == COMPLETED))
        if agent == "worker":
            rows.append(row("Worker evidence count", "> 0", str(count), count > 0))
    rows.append(
        row(
            "Reviewer independent of worker",
            "Separate review model",
            f"worker `{models.get('worker_agent', '?')}`, reviewer `{models.get('reviewer_agent', '?')}`",
            reviewer_is_independent(record),
        )
    )
    rows.append(
        row("Human decision recorded", "Accept / Partially Accept / Reject", decision,
            decision != PENDING_DECISION)
    )
    for filename in (AUDIT_FILENAME, HISTORY_FILENAME):
        exists = (directory / filename).is_file()
        rows.append(row(f"`{filename}` exists", "Yes", "Yes" if exists else "No", exists))
    return "\n".join(rows)


def record_decision(
    directory: Path,
    record: dict[str, Any],
    decision: str,
    reason: str,
    evidence_used: str,
    decided_at: str,
    allowed_values: list[str],
) -> tuple[Path, Path]:
    """Append a human decision for an already-recorded workflow to both logs."""
    reviewer = record.get("reviewer_result") or {}
    review = (reviewer.get("output") if "agent" in reviewer else reviewer) or {}
    recommendation = review.get("recommendation")
    human_decision = {
        "required": True,
        "status": decision,
        "reason": reason,
        "evidence_used": evidence_used,
        "decided_at": decided_at,
        "allowed_values": allowed_values,
    }
    common = {
        "event": DECISION_EVENT,
        "workflow_id": record["workflow_id"],
        "timestamp": decided_at,
    }
    history = {
        **common,
        "human_decision": human_decision,
        "reviewer_recommendation": recommendation,
    }
    audit = {
        **common,
        "human_approval_required": True,
        "human_decision": decision,
        "reviewer_recommendation": recommendation,
    }
    return append_workflow(directory, history, audit)


def session_log_path(directory: Path, session_guid: str) -> Path:
    return Path(directory) / f"AgenticLoopSession{session_guid}.md"


def append_to_session_log(path: Path, markdown: str) -> None:
    with Path(path).open("a", encoding="utf-8") as handle:
        handle.write(scrub("\n" + markdown.rstrip() + "\n"))
