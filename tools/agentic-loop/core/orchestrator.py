"""The agentic loop itself.

One round walks the six canonical stages in order:

    PLAN -> ACT -> OBSERVE -> AGENT -> HUMAN REVIEW -> ADAPT

Every stage prints a banner and writes a matching heading into the evidence log,
including stages that could not be reached, so the structure of the loop is always
visible in the record.
"""

from __future__ import annotations

import logging
from dataclasses import dataclass
from pathlib import Path
from typing import Callable

from agents import analyst, critic, decision_parser, file_selector, planner
from collectors import file_reader, repo_observer, repo_scanner
from config.settings import Settings
from core import console, stages
from core.gemini_client import GeminiClient, GeminiError
from core.models import Finding
from core.prompt_registry import PromptRegistry
from core.session import RoundState, SessionState, format_timestamp
from output.plan_writer import write_plan
from output.session_writer import SessionWriter

logger = logging.getLogger(__name__)

InputFn = Callable[[str], str]

CONSOLE_FILE_PREVIEW = 12


class RoundAborted(RuntimeError):
    """Raised when a round cannot continue; the reason is recorded in the log."""

    def __init__(self, stage: stages.Stage, reason: str) -> None:
        super().__init__(reason)
        self.stage = stage
        self.reason = reason


@dataclass
class RoundOutcome:
    round_state: RoundState
    findings: list[Finding]
    accepted: list[Finding]
    plan_path: Path | None
    aborted_reason: str | None = None
    end_session: bool = False
    follow_up_prompt: str | None = None


class Orchestrator:
    def __init__(
        self,
        settings: Settings,
        prompts: PromptRegistry,
        client: GeminiClient,
        session: SessionState,
        writer: SessionWriter,
        input_fn: InputFn | None = None,
        interactive: bool = True,
    ) -> None:
        self.settings = settings
        self._initial_settings = settings
        self.prompts = prompts
        self.client = client
        self.session = session
        self.writer = writer
        self.input_fn = input_fn or console.ask
        self.interactive = interactive
        self._end_session = False
        self._follow_up_prompt: str | None = None

    def set_scope(self, directory: Path) -> None:
        self.settings = self.settings.with_scope(directory)

    def reset_scope(self) -> None:
        """Restore the scope the session started with (TARGETED_DIRECTORY or --scope)."""
        self.settings = self._initial_settings

    def run_round(self, user_prompt: str) -> RoundOutcome:
        round_state = self.session.start_round(user_prompt)
        self.writer.start_round(round_state.number)
        self._end_session = False
        self._follow_up_prompt = None

        try:
            self._stage_plan(round_state)
            selection, scan = self._stage_act(user_prompt)
            bundle = file_reader.load_files(selection.entries, self.settings)
            observation = self._stage_observe(scan, bundle)
            findings = self._stage_agent(user_prompt, bundle, observation, round_state)
            accepted = self._stage_human_review(findings, round_state)
            plan_path = self._stage_adapt(
                user_prompt, accepted, findings, bundle, observation, round_state
            )
            if not findings:
                # Asked after ADAPT so the six stages always print in order.
                note = self._offer_next_step()
                if note:
                    self.writer.write_note(f"**Human next step:** {note}")
            return RoundOutcome(
                round_state,
                findings,
                accepted,
                plan_path,
                end_session=self._end_session,
                follow_up_prompt=self._follow_up_prompt,
            )

        except RoundAborted as abort:
            self._record_unreached(abort.reason)
            console.print_error(f"Round {round_state.number} stopped: {abort.reason}")
            return RoundOutcome(round_state, [], [], None, aborted_reason=abort.reason)
        except KeyboardInterrupt:
            self._record_unreached("interrupted by the user")
            console.print_warning("\nRound interrupted - evidence log preserved.")
            return RoundOutcome(round_state, [], [], None, aborted_reason="interrupted by user")
        except EOFError:
            self._record_unreached("input stream closed before the human replied")
            console.print_warning("\nInput closed - round ended, evidence log preserved.")
            return RoundOutcome(round_state, [], [], None, aborted_reason="input stream closed")
        except BaseException as exc:
            # Any other failure must still leave the six canonical stage headings
            # in the evidence log before it propagates to the REPL.
            self._record_unreached(f"unexpected error: {exc}")
            raise

    # -- stage 1 -------------------------------------------------------------

    def _stage_plan(self, round_state: RoundState) -> None:
        console.print_stage_banner(stages.PLAN, stages.PLAN.description)
        console.print_info(f'Prompt: "{round_state.prompt}"')
        self.writer.write_plan_stage(round_state.prompt, format_timestamp(round_state.started_at))

    # -- stage 2 -------------------------------------------------------------

    def _stage_act(
        self, user_prompt: str
    ) -> tuple[file_selector.SelectionResult, repo_scanner.ScanResult]:
        console.print_stage_banner(stages.ACT, stages.ACT.description)
        console.print_info(f"Scope: {self.settings.scope}")

        scan = repo_scanner.scan(self.settings)
        console.print_info(f"Scanned {len(scan.entries)} reviewable file(s).")

        if not scan.entries:
            body = (
                f"- Scope: `{self.settings.scope}`\n"
                "- No reviewable files were found.\n"
                f"- Ignored directories: {', '.join(sorted(self.settings.ignore_dirs))}"
            )
            self.writer.write_act_stage(body)
            raise RoundAborted(
                stages.ACT,
                "no reviewable files found in scope - check TARGETED_DIRECTORY "
                "and INCLUDE_EXTENSIONS",
            )

        try:
            selection = file_selector.select_files(
                scan=scan,
                user_prompt=user_prompt,
                settings=self.settings,
                prompts=self.prompts,
                client=self.client,
            )
        except GeminiError as exc:
            self.writer.write_act_stage(f"- Scope: `{self.settings.scope}`\n- Selection failed: {exc}")
            raise RoundAborted(stages.ACT, f"file selection failed: {exc}") from exc

        console.print_info(f"Selected {len(selection.entries)} file(s) for review.")
        preview = selection.entries[:CONSOLE_FILE_PREVIEW]
        for entry in preview:
            console.print_info(f"  - {entry.relative_path}")
        hidden = len(selection.entries) - len(preview)
        if hidden > 0:
            console.print_info(f"  ... and {hidden} more (full list in the evidence log)")

        lines = [
            f"- Scope: `{self.settings.scope}`",
            f"- Scope mode: {'TARGETED_DIRECTORY' if self.settings.scope_is_targeted else 'whole repository'}",
            f"- Files scanned: {len(scan.entries)} ({scan.total_bytes} bytes, {scan.total_lines} lines)",
            f"- Ignored directories encountered: {scan.ignored_dir_count}",
            f"- Secret-like files skipped: {len(scan.skipped_secrets)}",
            f"- Binary files skipped: {len(scan.skipped_binary)}",
            f"- Selection method: {'keyword fallback' if selection.fallback_used else 'model'}",
            "",
            "**Files selected for review**",
            "",
            selection.as_text(),
        ]
        if selection.rationale:
            lines.extend(["", f"**Selection rationale:** {selection.rationale}"])
        if selection.dropped_paths:
            lines.extend(
                [
                    "",
                    "**Paths returned by the model but not present in the manifest (dropped):** "
                    + ", ".join(f"`{path}`" for path in selection.dropped_paths),
                ]
            )
        if selection.response:
            lines.extend(["", f"_Selection call: {selection.response.usage_line()}_"])

        self.writer.write_act_stage("\n".join(lines))
        return selection, scan

    # -- stage 3 -------------------------------------------------------------

    def _stage_observe(
        self,
        scan: repo_scanner.ScanResult,
        bundle: file_reader.CodeBundle,
    ) -> repo_observer.Observation:
        console.print_stage_banner(stages.OBSERVE, stages.OBSERVE.description)
        observation = repo_observer.observe(scan, bundle)
        console.print_block(observation.as_text())
        self.writer.write_observe_stage(observation.as_text())
        return observation

    # -- stage 4 -------------------------------------------------------------

    def _stage_agent(
        self,
        user_prompt: str,
        bundle: file_reader.CodeBundle,
        observation: repo_observer.Observation,
        round_state: RoundState,
    ) -> list[Finding]:
        console.print_stage_banner(stages.AGENT, stages.AGENT.description)
        console.print_info(
            f"Implementation agent reading {len(bundle.files)} file(s) "
            f"({bundle.total_bytes} bytes) with {self.settings.model}..."
        )

        try:
            analysis = analyst.analyse(
                user_prompt=user_prompt,
                bundle=bundle,
                observation=observation,
                settings=self.settings,
                prompts=self.prompts,
                client=self.client,
            )
        except GeminiError as exc:
            self.writer.write_agent_stage(f"**Implementation agent failed:** {exc}")
            raise RoundAborted(stages.AGENT, f"implementation agent failed: {exc}") from exc

        round_state.tokens += analysis.response.total_tokens
        proposed = analysis.findings

        console.print_info(f"Implementation agent proposed {len(proposed.findings)} finding(s).")
        if self.settings.enable_review_agent and proposed.findings:
            console.print_info(f"Review agent critiquing with {self.settings.review_model}...")

        try:
            outcome = critic.critique(
                user_prompt=user_prompt,
                proposed=proposed,
                bundle=bundle,
                observation=observation,
                settings=self.settings,
                prompts=self.prompts,
                client=self.client,
            )
        except GeminiError as exc:
            console.print_warning(f"Review agent failed ({exc}); using unreviewed findings.")
            outcome = critic.CritiqueOutcome(
                result=critic.CritiqueResult(
                    findings=proposed.findings, summary=proposed.summary
                ),
                response=None,
                skipped_reason=f"Review agent call failed: {exc}",
            )

        if outcome.response:
            round_state.tokens += outcome.response.total_tokens

        findings = list(outcome.result.findings)
        round_state.findings_count = len(findings)

        summary = outcome.result.summary.strip() or proposed.summary.strip()
        console.print_findings(findings)
        console.print_summary(summary)

        self.writer.write_agent_stage(
            self._agent_stage_markdown(proposed, outcome, findings, analysis, summary)
        )
        return findings

    def _agent_stage_markdown(
        self,
        proposed,
        outcome: critic.CritiqueOutcome,
        findings: list[Finding],
        analysis: analyst.AnalysisResult,
        summary: str,
    ) -> str:
        lines = ["#### 4a. Implementation Agent", ""]
        if proposed.findings:
            for index, finding in enumerate(proposed.findings, start=1):
                lines.extend(
                    [
                        f"{index}. **{finding.title}** ({finding.severity.value})",
                        f"   - Problem: {finding.problem.strip()}",
                        f"   - Suggested fix: {finding.suggested_fix.strip()}",
                        f"   - Files: {', '.join(finding.files) if finding.files else '(none cited)'}",
                        f"   - Evidence: {finding.evidence.strip() or '(none cited)'}",
                    ]
                )
        else:
            lines.append("_No findings proposed._")
        lines.extend(["", f"_Call: {analysis.response.usage_line()}_", ""])

        lines.extend(["#### 4b. Review Agent", ""])
        if not outcome.was_run:
            lines.extend([f"_Skipped: {outcome.skipped_reason}_", ""])
        else:
            if outcome.result.notes:
                for note in outcome.result.notes:
                    lines.append(
                        f"- **{note.finding_title}** → `{note.verdict}`"
                        + (f" — {note.reason.strip()}" if note.reason.strip() else "")
                    )
            else:
                lines.append("_The review agent returned no per-finding notes._")
            if outcome.response:
                lines.extend(["", f"_Call: {outcome.response.usage_line()}_"])
            lines.append("")

        lines.extend(["#### 4c. Findings Presented to the Human", ""])
        if findings:
            for index, finding in enumerate(findings, start=1):
                lines.extend(
                    [
                        f"{index}. **Problem:** {finding.problem.strip()} "
                        f"({finding.severity.value})",
                        "",
                        f"   **Suggested fix:** {finding.suggested_fix.strip()}",
                        "",
                        f"   Files: {', '.join(f'`{f}`' for f in finding.files) if finding.files else '(none cited)'}",
                        "",
                    ]
                )
        else:
            lines.extend(["_No evidence-backed issues identified._", ""])

        if summary:
            lines.extend([f"**Agent summary:** {summary}", ""])
        return "\n".join(lines)

    # -- stage 5 -------------------------------------------------------------

    def _stage_human_review(
        self,
        findings: list[Finding],
        round_state: RoundState,
    ) -> list[Finding]:
        console.print_stage_banner(stages.HUMAN_REVIEW, stages.HUMAN_REVIEW.description)

        if not findings:
            console.print_info("Nothing to review - the agent raised no issues.")
            self.writer.write_human_review_stage(
                "_No findings were presented, so there was nothing to accept or reject._"
            )
            return []

        reply = self.input_fn(
            "Which suggestions would you like to accept? (e.g. 'fix 1 and 2 please', 'all', 'none'):"
        ).strip()

        decision = decision_parser.parse_decision(
            reply=reply,
            findings=findings,
            prompts=self.prompts,
            client=self.client,
        )
        if decision.response:
            round_state.tokens += decision.response.total_tokens

        accepted = [findings[index - 1] for index in decision.accepted]
        round_state.accepted_count = len(accepted)

        console.print_success(decision.interpretation)
        rejected = decision.rejected(len(findings))
        if rejected:
            console.print_info(f"Rejected: {', '.join(str(index) for index in rejected)}")

        lines = [
            "**User response (verbatim):**",
            "",
            f"```text\n{reply or '(empty reply)'}\n```",
            "",
            f"- Interpreted by: {decision.source}",
            f"- Interpretation: {decision.interpretation}",
            "",
            "| # | Finding | Decision |",
            "| --- | --- | --- |",
        ]
        for index, finding in enumerate(findings, start=1):
            verdict = "**ACCEPTED**" if index in decision.accepted else "REJECTED"
            lines.append(f"| {index} | {finding.title} | {verdict} |")
        if decision.error:
            lines.extend(["", f"_Interpretation warning: {decision.error}_"])

        self.writer.write_human_review_stage("\n".join(lines))
        return accepted

    # -- stage 6 -------------------------------------------------------------

    def _stage_adapt(
        self,
        user_prompt: str,
        accepted: list[Finding],
        findings: list[Finding],
        bundle: file_reader.CodeBundle,
        observation: repo_observer.Observation,
        round_state: RoundState,
    ) -> Path | None:
        console.print_stage_banner(stages.ADAPT, stages.ADAPT.description)

        if not accepted:
            note = (
                "No suggestions were accepted, so no implementation plan was created. "
                "The findings above remain on record for a future round."
                if findings
                else "No findings were raised, so there was nothing to adapt."
            )
            console.print_info(note)
            self.writer.write_adapt_stage(
                f"- Plan created: no\n- Note: {note}\n"
                f"- Round duration: {round_state.duration_seconds():.1f}s"
            )
            return None

        console.print_info(
            f"Building an implementation plan for {len(accepted)} accepted finding(s)..."
        )
        try:
            result = planner.build_plan(
                user_prompt=user_prompt,
                accepted=accepted,
                bundle=bundle,
                observation=observation,
                settings=self.settings,
                prompts=self.prompts,
                client=self.client,
            )
        except GeminiError as exc:
            self.writer.write_adapt_stage(
                f"- Plan created: no\n- Planning agent failed: {exc}\n"
                f"- Accepted findings preserved: {', '.join(f.title for f in accepted)}"
            )
            raise RoundAborted(stages.ADAPT, f"planning agent failed: {exc}") from exc

        round_state.tokens += result.response.total_tokens
        path = write_plan(
            plans_dir=self.settings.plans_dir,
            plan=result.plan,
            accepted=accepted,
            user_prompt=user_prompt,
            scope=self.settings.scope,
            session_guid=self.session.guid,
            round_number=round_state.number,
            model=self.settings.model,
        )
        round_state.plan_path = path
        console.print_plan_saved(path)

        self.writer.write_adapt_stage(
            "\n".join(
                [
                    f"- Accepted findings: {len(accepted)} of {len(findings)}",
                    "- Implementation plan saved to: " f"`{path}`",
                    f"- Plan items: {len(result.plan.items)}",
                    f"- Round duration: {round_state.duration_seconds():.1f}s",
                    "",
                    f"_Planning call: {result.response.usage_line()}_",
                    "",
                    "**Adaptation:** the accepted suggestions are now a concrete plan for the "
                    "human to implement. Rerun the loop after the changes land to verify them.",
                ]
            )
        )
        return path

    # -- helpers -------------------------------------------------------------

    def _offer_next_step(self) -> str:
        """Ask what to do when a round produced nothing to accept or reject.

        Returns a short description of the choice for the evidence log.
        """
        if not self.interactive:
            return ""

        try:
            reply = self.input_fn(
                "Nothing to accept. Type another prompt to try again, "
                "Enter to carry on, or 'q' to quit:"
            ).strip()
        except (EOFError, KeyboardInterrupt):
            self._end_session = True
            console.print_warning("\nEnding session.")
            return "ended the session"

        if not reply:
            return "carried on"
        if reply.lower() in {"q", "quit", "exit", "/exit", "/quit"}:
            self._end_session = True
            return "ended the session"

        self._follow_up_prompt = reply
        return f"re-prompted with: {reply}"

    def _record_unreached(self, reason: str) -> None:
        """Write a heading for every stage of this round that never reached the log.

        The writer decides what is missing, so a stage interrupted after its banner
        but before it wrote its section is still recorded.
        """
        for stage in self.writer.unwritten_stages():
            self.writer.write_stage_not_reached(stage, reason)
