"""Writes the evidence log: `Sessions/AgenticLoopSession{GUID}.md`.

The file is created when the session starts and flushed after every stage, so an
interrupted run still leaves a complete record of everything that happened.

Stage headings come from `core.stages`, the same source the console banners use,
so the PLAN / ACT / OBSERVE / AGENT / HUMAN REVIEW / ADAPT structure in the log
always matches what the user saw on screen.
"""

from __future__ import annotations

import re
from pathlib import Path

from core import stages
from core.redaction import scrub
from core.session import SessionState, format_timestamp


def _fence(text: str, language: str = "text") -> str:
    """Fence a block, widening the marker so nested fences cannot break out.

    Model evidence routinely contains ``` blocks; a fixed three-backtick fence
    would be closed early by them and the rest of the log would render as prose.
    """
    body = (text or "(empty)").rstrip()
    longest_run = max((len(run) for run in re.findall(r"`+", body)), default=0)
    marker = "`" * max(3, longest_run + 1)
    return f"{marker}{language}\n{body}\n{marker}"


class SessionWriter:
    """Append-only markdown evidence log for one CLI session."""

    def __init__(self, directory: Path, session: SessionState) -> None:
        self.session = session
        self.directory = Path(directory)
        self.directory.mkdir(parents=True, exist_ok=True)
        self.path = self.directory / f"AgenticLoopSession{session.guid}.md"
        self._closed = False
        self._stages_written: set[str] = set()

    def _append(self, text: str) -> None:
        with self.path.open("a", encoding="utf-8") as handle:
            handle.write(scrub(text))

    # -- session lifecycle ---------------------------------------------------

    def write_header(self, config_summary: dict[str, str]) -> Path:
        lines = [
            f"# Agentic Loop Session {self.session.guid}",
            "",
            f"- **Started:** {format_timestamp(self.session.started_at)}",
        ]
        lines.extend(f"- **{key}:** {value}" for key, value in config_summary.items())
        lines.extend(
            [
                "",
                "This is an evidence log. Each round below records the full loop: "
                + " → ".join(stages.STAGE_NAMES) + ".",
                "",
            ]
        )
        self._append("\n".join(lines))
        return self.path

    def write_footer(self, reason: str = "Session ended normally.") -> None:
        if self._closed:
            return
        self._closed = True
        plans = self.session.plans_created
        lines = [
            "",
            "---",
            "",
            "## Session Summary",
            "",
            f"- **Ended:** {format_timestamp()}",
            f"- **Rounds completed:** {self.session.round_count}",
            f"- **Findings presented:** {self.session.total_findings}",
            f"- **Findings accepted:** {self.session.total_accepted}",
            f"- **Tokens used:** {self.session.total_tokens}",
            f"- **Plans created:** {len(plans)}",
        ]
        lines.extend(f"  - `{path}`" for path in plans)
        lines.extend([f"- **Closing note:** {reason}", ""])
        self._append("\n".join(lines))

    # -- per-round stages ----------------------------------------------------

    def start_round(self, round_number: int) -> None:
        self._stages_written = set()
        self._append(f"\n---\n\n## Round {round_number}\n")

    def unwritten_stages(self) -> list[stages.Stage]:
        """Stages of the current round that have not been written yet, in order.

        The writer is the authority on what reached the log, so an interrupt part
        way through a stage cannot leave that stage's heading missing.
        """
        return [stage for stage in stages.STAGES if stage.name not in self._stages_written]

    def write_plan_stage(self, prompt: str, started_at: str) -> None:
        self._stages_written.add(stages.PLAN.name)
        self._append(
            "\n".join(
                [
                    "",
                    stages.PLAN.heading,
                    "",
                    f"_{stages.PLAN.description}_",
                    "",
                    "**Original prompt (verbatim):**",
                    "",
                    _fence(prompt),
                    "",
                    f"- Round started: {started_at}",
                    "",
                ]
            )
        )

    def write_act_stage(self, body: str) -> None:
        self._write_stage(stages.ACT, body)

    def write_observe_stage(self, observations: str) -> None:
        self._stages_written.add(stages.OBSERVE.name)
        self._append(
            "\n".join(
                [
                    "",
                    stages.OBSERVE.heading,
                    "",
                    f"_{stages.OBSERVE.description}_",
                    "",
                    _fence(observations),
                    "",
                ]
            )
        )

    def write_agent_stage(self, body: str) -> None:
        self._write_stage(stages.AGENT, body)

    def write_human_review_stage(self, body: str) -> None:
        self._write_stage(stages.HUMAN_REVIEW, body)

    def write_adapt_stage(self, body: str) -> None:
        self._write_stage(stages.ADAPT, body)

    def write_stage_not_reached(self, stage: stages.Stage, reason: str) -> None:
        self._write_stage(stage, f"_(not reached — {reason})_")

    def write_note(self, text: str) -> None:
        """Append a paragraph that belongs to the round but not to any one stage."""
        self._append(f"\n{text.rstrip()}\n")

    def _write_stage(self, stage: stages.Stage, body: str) -> None:
        self._stages_written.add(stage.name)
        self._append(
            "\n".join(
                [
                    "",
                    stage.heading,
                    "",
                    f"_{stage.description}_",
                    "",
                    body.rstrip(),
                    "",
                ]
            )
        )
