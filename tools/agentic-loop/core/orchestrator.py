"""The agentic loop itself.

One round walks the six canonical stages in order:

    PLAN -> ACT -> OBSERVE -> AGENT -> HUMAN REVIEW -> ADAPT

Every stage prints a banner and writes a matching heading into the evidence log,
including stages that could not be reached, so the structure of the loop is always
visible in the record.
"""

from __future__ import annotations

import logging
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Callable

from agents import analyst, critic, decision_parser, file_selector, planner, reviewer, worker, workflow_planner
from collectors import file_reader, github_actions, mcp_observer, rag_observer, repo_observer, repo_scanner
from config.settings import Settings
from core import console, stages
from core.mcp_client import McpClient
from core.models import Finding, ReviewerAssessment, WorkerOutput, WorkflowDecision, WorkflowPlan
from core.openrouter_client import OpenRouterClient, OpenRouterError
from core.prompt_registry import PromptRegistry
from core.rag_client import RagClient
from core.session import RoundState, SessionState, format_timestamp, new_guid, utc_now
from output import workflow_log
from output.plan_writer import write_plan
from output.session_writer import SessionWriter

logger = logging.getLogger(__name__)

InputFn = Callable[[str], str]

CONSOLE_FILE_PREVIEW = 12

CI_FAILURE_PROMPT = (
    "The most recent GitHub Actions workflow run failed. The failure excerpts "
    "from its failed jobs are included in the observations below. Diagnose the "
    "root cause of each failure using the source code in scope, and propose "
    "concrete fixes. Each finding should name the failing test or build error "
    "and the specific code change that would make the pipeline pass again."
)

RAG_VALIDATION_PROMPT = (
    "Retrieved documentation passages describing how this service is expected "
    "to behave are included in the observations below. Compare the source code "
    "in scope against that documented behaviour, and flag any place where the "
    "code contradicts, is missing, or drifts from what the documentation "
    "describes. Each finding should name the specific code location that "
    "disagrees and cite the documentation passages it relies on by their "
    "bracketed number, for example [1] or [2][3], in its evidence. Only cite "
    "numbers that appear in the observations. If the documentation context is "
    "marked as insufficient, report that the topic is not documented instead "
    "of inferring expected behaviour."
)

MCP_VALIDATION_PROMPT = (
    "A live interaction with the shared local MCP server (initialize, "
    "tools/list, and one real tools/call probe) is included in the observations "
    "below. Validate the MCP server implementation in scope against that "
    "evidence: check that scope filtering only exposes <scope>_* tools, that "
    "the probe call returned a valid, read-only tool result (or a safe "
    "isError result with a non-leaking message), and that the server code "
    "matches the contract its tools actually exhibit. Flag any mismatch "
    "between the code and the observed behaviour, citing the specific tool or "
    "code location."
)


PARTICIPATING_AGENTS = ("planner_agent", "worker_agent", "reviewer_agent")

MULTI_AGENT_DESCRIPTION = (
    "Worker agent executes the plan against the evidence; reviewer agent assesses the risk."
)

REVIEWER_NOT_INDEPENDENT = (
    "The reviewer agent is running on the same model as the worker agent, so its review "
    "is not independent. Set OPENROUTER_REVIEW_MODEL to a different model for a "
    "separate second opinion."
)

ALLOWED_DECISIONS = [
    WorkflowDecision.ACCEPT.value,
    WorkflowDecision.PARTIALLY_ACCEPT.value,
    WorkflowDecision.REJECT.value,
]


@dataclass
class WorkflowRun:
    """State of one planner -> worker -> reviewer -> human workflow."""

    workflow_id: str = field(default_factory=new_guid)
    plan: WorkflowPlan | None = None
    worker_output: WorkerOutput | None = None
    assessment: ReviewerAssessment | None = None
    decision: WorkflowDecision = WorkflowDecision.PENDING
    decision_reason: str = ""
    decision_evidence: str = ""
    evidence_files: list[str] = field(default_factory=list)
    agent_status: dict[str, str] = field(
        default_factory=lambda: {agent: workflow_log.NOT_REACHED for agent in workflow_log.AGENT_KEYS}
    )


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
    workflow: WorkflowRun | None = None


class Orchestrator:
    def __init__(
        self,
        settings: Settings,
        prompts: PromptRegistry,
        client: OpenRouterClient,
        review_client: OpenRouterClient,
        session: SessionState,
        writer: SessionWriter,
        input_fn: InputFn | None = None,
        interactive: bool = True,
    ) -> None:
        self.settings = settings
        self._initial_settings = settings
        self.prompts = prompts
        self.client = client
        self.review_client = review_client
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

    def run_round(self, user_prompt: str, extra_context: str | None = None) -> RoundOutcome:
        return self._guarded_round(
            user_prompt, lambda round_state: self._review_round(round_state, extra_context)
        )

    def run_multi_agent_round(self, user_request: str) -> RoundOutcome:
        """Run the planner -> worker -> reviewer -> human workflow as one round.

        PLAN: the planner agent turns the request into a workflow plan.
        ACT/OBSERVE: files are selected using the request plus the plan.
        AGENT: the worker executes the plan; the reviewer assesses the risk.
        HUMAN REVIEW: the human accepts, partially accepts or rejects.
        ADAPT: the workflow history and coordination audit are appended.
        Aborted workflows are still recorded so the audit trail has no gaps.
        """
        workflow = WorkflowRun()
        outcome = self._guarded_round(
            user_request, lambda round_state: self._multi_agent_round(round_state, workflow)
        )
        outcome.workflow = workflow
        if outcome.aborted_reason:
            history, audit = self._workflow_records(
                workflow, outcome.round_state, "aborted", outcome.aborted_reason
            )
            workflow_log.append_workflow(self.settings.sessions_dir, history, audit)
        return outcome

    def _review_round(self, round_state: RoundState, extra_context: str | None) -> RoundOutcome:
        user_prompt = round_state.prompt
        self._stage_plan(round_state)
        selection, scan = self._stage_act(user_prompt)
        bundle = file_reader.load_files(selection.entries, self.settings)
        observation = self._stage_observe(scan, bundle, extra_context)
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

    def _multi_agent_round(self, round_state: RoundState, workflow: WorkflowRun) -> RoundOutcome:
        user_request = round_state.prompt
        plan = self._stage_plan_workflow(round_state, workflow)
        selection, scan = self._stage_act(_selection_prompt(user_request, plan))
        bundle = file_reader.load_files(selection.entries, self.settings)
        workflow.evidence_files = [file.relative_path for file in bundle.files]
        observation = self._stage_observe(scan, bundle)
        self._stage_coordinate(user_request, plan, bundle, observation, round_state, workflow)
        self._stage_workflow_decision(workflow)
        self._stage_record_workflow(workflow, round_state)
        return RoundOutcome(round_state, [], [], None)

    def _guarded_round(
        self, prompt: str, body: Callable[[RoundState], RoundOutcome]
    ) -> RoundOutcome:
        round_state = self.session.start_round(prompt)
        self.writer.start_round(round_state.number)
        self._end_session = False
        self._follow_up_prompt = None

        try:
            return body(round_state)

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

    def run_ci_failure_round(self, workflow: str | None = None) -> RoundOutcome:
        """Fetch the latest failed GitHub Actions run and review it as a round.

        The failure report is injected into the OBSERVE stage so both the
        implementation agent and the review agent see it alongside the code.
        Raises `GithubActionsError` if no usable failure data can be fetched.
        """
        client = github_actions.GithubActionsClient(self.settings)
        console.print_info("Fetching the latest failed GitHub Actions run...")
        report = client.fetch_failure_report(workflow=workflow)
        console.print_info(f"Found: {report.summary_line()}")
        for job in report.failed_jobs:
            console.print_info(f"  - {job.name}: {job.conclusion}")
        return self.run_round(CI_FAILURE_PROMPT, extra_context=report.as_prompt_text())

    def run_rag_validation_round(self, topic: str | None = None) -> RoundOutcome:
        """Retrieve documentation context from the local RAG server and cross-check it.

        The retrieved passages are injected into the OBSERVE stage so both the
        implementation agent and the review agent see them alongside the code.
        Raises `RagError` if the RAG server cannot be reached.
        """
        client = RagClient(self.settings)
        console.print_info("Querying the local RAG server for documentation context...")
        report = rag_observer.fetch_rag_report(client, topic)
        console.print_info(f"Found: {report.summary_line()}")
        return self.run_round(RAG_VALIDATION_PROMPT, extra_context=report.as_prompt_text())

    def run_mcp_validation_round(self, tool: str | None = None) -> RoundOutcome:
        """Exercise the shared local MCP server and validate it as a round.

        The live initialize/tools/list/tools/call report is injected into the
        OBSERVE stage so both the implementation agent and the review agent see
        it alongside the mcp-server source code. Raises `McpError` if the MCP
        server cannot be reached or rejects the request.
        """
        client = McpClient(self.settings)
        console.print_info(
            f"Exercising the MCP server at {client.endpoint} "
            f"(scope: {self.settings.mcp_tool_scope})..."
        )
        report = mcp_observer.fetch_mcp_report(client, tool=tool)
        console.print_info(f"Found: {report.summary_line()}")
        for listed in report.tools:
            console.print_info(f"  - {listed.name}")
        return self.run_round(MCP_VALIDATION_PROMPT, extra_context=report.as_prompt_text())

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
        except OpenRouterError as exc:
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
        extra_context: str | None = None,
    ) -> repo_observer.Observation:
        console.print_stage_banner(stages.OBSERVE, stages.OBSERVE.description)
        observation = repo_observer.observe(scan, bundle)
        if extra_context:
            observation.extra_sections.append(extra_context)
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
        except OpenRouterError as exc:
            self.writer.write_agent_stage(f"**Implementation agent failed:** {exc}")
            raise RoundAborted(stages.AGENT, f"implementation agent failed: {exc}") from exc

        round_state.tokens += analysis.response.total_tokens
        proposed = analysis.findings

        console.print_info(f"Implementation agent proposed {len(proposed.findings)} finding(s).")
        if proposed.findings:
            console.print_info(
                f"Review agent critiquing with {self.settings.review_model} via OpenRouter..."
            )

        try:
            outcome = critic.critique(
                user_prompt=user_prompt,
                proposed=proposed,
                bundle=bundle,
                observation=observation,
                settings=self.settings,
                prompts=self.prompts,
                client=self.review_client,
            )
        except OpenRouterError as exc:
            self.writer.write_agent_stage(f"**Review agent failed:** {exc}")
            raise RoundAborted(stages.AGENT, f"review agent failed: {exc}") from exc

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
        except OpenRouterError as exc:
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

    # -- multi-agent workflow stages ----------------------------------------

    def _stage_plan_workflow(self, round_state: RoundState, workflow: WorkflowRun) -> WorkflowPlan:
        self._stage_plan(round_state)
        console.print_info(f"Planner agent planning the workflow with {self.settings.model}...")
        try:
            result = workflow_planner.plan_workflow(
                user_request=round_state.prompt,
                settings=self.settings,
                prompts=self.prompts,
                client=self.client,
            )
        except OpenRouterError as exc:
            workflow.agent_status["planner"] = workflow_log.FAILED
            self.writer.write_note(f"**Planner agent failed:** {exc}")
            raise RoundAborted(stages.PLAN, f"planner agent failed: {exc}") from exc

        round_state.tokens += result.response.total_tokens
        workflow.plan = result.plan
        workflow.agent_status["planner"] = workflow_log.COMPLETED
        console.print_workflow_plan(result.plan)
        self.writer.write_note(
            "\n".join(
                [
                    "#### Planner Agent",
                    "",
                    f"- Status: `{workflow_log.COMPLETED}`",
                    "",
                    _plan_markdown(result.plan),
                    "",
                    f"_Call: {result.response.usage_line()}_",
                ]
            )
        )
        return result.plan

    def _stage_coordinate(
        self,
        user_request: str,
        plan: WorkflowPlan,
        bundle: file_reader.CodeBundle,
        observation: repo_observer.Observation,
        round_state: RoundState,
        workflow: WorkflowRun,
    ) -> None:
        console.print_stage_banner(stages.AGENT, MULTI_AGENT_DESCRIPTION)
        console.print_info(
            f"Worker agent executing the plan over {len(bundle.files)} file(s) "
            f"with {self.settings.model}..."
        )
        try:
            work = worker.execute_plan(
                user_request=user_request,
                plan=plan,
                bundle=bundle,
                observation=observation,
                settings=self.settings,
                prompts=self.prompts,
                client=self.client,
            )
        except OpenRouterError as exc:
            workflow.agent_status["worker"] = workflow_log.FAILED
            self.writer.write_agent_stage(f"**Worker agent failed:** {exc}")
            raise RoundAborted(stages.AGENT, f"worker agent failed: {exc}") from exc

        round_state.tokens += work.response.total_tokens
        workflow.worker_output = work.output
        workflow.agent_status["worker"] = workflow_log.COMPLETED
        console.print_worker_output(work.output)
        worker_section = [
            "#### 4a. Worker Agent",
            "",
            f"- Status: `{workflow_log.COMPLETED}`",
            f"- Evidence count: {len(bundle.files)} file(s)",
            "",
            _worker_markdown(work.output),
            "",
            f"_Call: {work.response.usage_line()}_",
            "",
        ]

        reviewer_section = ["#### 4b. Reviewer Agent", ""]
        if self.settings.review_model == self.settings.model:
            console.print_warning(REVIEWER_NOT_INDEPENDENT)
            reviewer_section += [f"> **Warning:** {REVIEWER_NOT_INDEPENDENT}", ""]

        console.print_info(
            f"Reviewer agent assessing with {self.settings.review_model} via OpenRouter..."
        )
        try:
            review = reviewer.review_output(
                user_request=user_request,
                plan=plan,
                worker_output=work.output,
                bundle=bundle,
                observation=observation,
                settings=self.settings,
                prompts=self.prompts,
                client=self.review_client,
            )
        except OpenRouterError as exc:
            workflow.agent_status["reviewer"] = workflow_log.FAILED
            self.writer.write_agent_stage(
                "\n".join(worker_section + reviewer_section + [f"**Reviewer agent failed:** {exc}"])
            )
            raise RoundAborted(stages.AGENT, f"reviewer agent failed: {exc}") from exc

        round_state.tokens += review.response.total_tokens
        workflow.assessment = review.assessment
        workflow.agent_status["reviewer"] = workflow_log.COMPLETED
        console.print_review(review.assessment)
        self.writer.write_agent_stage(
            "\n".join(
                [f"_{MULTI_AGENT_DESCRIPTION}_", ""]
                + worker_section
                + reviewer_section
                + [
                    f"- Status: `{workflow_log.COMPLETED}`",
                    "",
                    _review_markdown(review.assessment),
                    "",
                    f"_Call: {review.response.usage_line()}_",
                ]
            )
        )

    def _stage_workflow_decision(self, workflow: WorkflowRun) -> None:
        console.print_stage_banner(
            stages.HUMAN_REVIEW, "The human accepts, partially accepts or rejects the workflow output."
        )
        recommendation = workflow.assessment.recommendation if workflow.assessment else None
        recommended = recommendation.value if recommendation else "(none)"

        if not self.interactive:
            console.print_info("Non-interactive run - the human decision is left pending.")
            self.writer.write_human_review_stage(
                f"- Reviewer recommendation: `{recommended}`\n"
                f"- Human decision: **{WorkflowDecision.PENDING.value}** (non-interactive run)\n"
                f"- Record it after reviewing the output: "
                f'`python main.py --decide {workflow.workflow_id} "<accept|partial|reject> <reason>" '
                f'--evidence-used "<what informed the decision>"`'
            )
            return

        console.print_info(f"Reviewer recommendation: {recommended}")
        while True:
            reply = self.input_fn(
                "Your decision on this output? (accept / partial / reject, followed by your reason):"
            ).strip()
            decision, reason = WorkflowDecision.parse(reply)
            if decision:
                break
            console.print_warning("Reply with accept, partial or reject (or a / p / r).")
        evidence_used = self.input_fn(
            "Which evidence informed this decision? (files, agent fields or checks; Enter to skip):"
        ).strip()

        workflow.decision = decision
        workflow.decision_reason = reason
        workflow.decision_evidence = evidence_used
        console.print_success(f"Human decision: {decision.value}")
        self.writer.write_human_review_stage(
            _decision_markdown(
                reply, recommended, decision, reason, evidence_used,
                overrode=bool(recommendation) and decision.name.lower() != recommendation.value,
            )
        )

    def _stage_record_workflow(self, workflow: WorkflowRun, round_state: RoundState) -> None:
        console.print_stage_banner(stages.ADAPT, "Record the workflow history and coordination audit.")
        history, audit = self._workflow_records(workflow, round_state, "completed")
        history_path, audit_path = workflow_log.append_workflow(
            self.settings.sessions_dir, history, audit
        )
        self.session.workflows.append((workflow.workflow_id, workflow.decision.value))
        console.print_workflow_recorded(history_path, audit_path)
        self.writer.write_adapt_stage(
            "\n".join(
                [
                    f"- Workflow ID: `{workflow.workflow_id}`",
                    f"- Participating agents: {', '.join(PARTICIPATING_AGENTS)}",
                    f"- Evidence files: {len(workflow.evidence_files)}",
                    f"- Human decision: **{workflow.decision.value}**",
                    f"- Workflow history: `{history_path}`",
                    f"- Coordination audit: `{audit_path}`",
                    f"- Round duration: {round_state.duration_seconds():.1f}s",
                    "",
                    "#### Evidence Log",
                    "",
                    workflow_log.evidence_log_markdown(history, self.settings.sessions_dir),
                ]
            )
        )

    def _workflow_records(
        self,
        workflow: WorkflowRun,
        round_state: RoundState,
        status: str,
        reason: str | None = None,
    ) -> tuple[dict[str, Any], dict[str, Any]]:
        timestamp = utc_now().isoformat()
        duration_ms = int(round_state.duration_seconds() * 1000)
        assessment = workflow.assessment
        models = {
            "planner": self.settings.model,
            "worker": self.settings.model,
            "reviewer": self.settings.review_model,
        }
        outputs = {
            "planner": workflow.plan,
            "worker": workflow.worker_output,
            "reviewer": assessment,
        }
        common = {
            "workflow_id": workflow.workflow_id,
            "timestamp": timestamp,
            "duration_ms": duration_ms,
            "status": status,
            "participating_agents": list(PARTICIPATING_AGENTS),
        }
        history = {
            **common,
            "session_guid": self.session.guid,
            "round": round_state.number,
            "user_request": round_state.prompt,
            "scope": str(self.settings.scope),
            "models": {f"{agent}_agent": model for agent, model in models.items()},
            "evidence_count": len(workflow.evidence_files),
            "evidence_files": workflow.evidence_files,
            **{
                f"{agent}_result": {
                    "agent": f"{agent}_agent",
                    "model": models[agent],
                    "status": workflow.agent_status[agent],
                    "human_approval_required": True,
                    "output": _dump(outputs[agent]),
                }
                for agent in workflow_log.AGENT_KEYS
            },
            "human_decision": {
                "required": True,
                "status": workflow.decision.value,
                "reason": workflow.decision_reason,
                "evidence_used": workflow.decision_evidence,
                "allowed_values": ALLOWED_DECISIONS,
            },
            "tokens": round_state.tokens,
        }
        audit = {
            **common,
            "human_approval_required": True,
            "human_decision": workflow.decision.value,
            **{f"{agent}_status": workflow.agent_status[agent] for agent in workflow_log.AGENT_KEYS},
            "reviewer_independent": self.settings.review_model != self.settings.model,
            "reviewer_risk": assessment.risk.value if assessment else None,
            "reviewer_recommendation": assessment.recommendation.value if assessment else None,
            "evidence_count": len(workflow.evidence_files),
            "tokens": round_state.tokens,
        }
        if reason:
            history["aborted_reason"] = reason
            audit["aborted_reason"] = reason
        return history, audit

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


def _selection_prompt(user_request: str, plan: WorkflowPlan) -> str:
    """Let the planner's steps and evidence requirements steer file selection."""
    lines = [user_request, "", "Workflow plan steps:"]
    lines.extend(f"- {step}" for step in plan.steps)
    if plan.evidence_required:
        lines.extend(["", "Evidence required:"])
        lines.extend(f"- {item}" for item in plan.evidence_required)
    return "\n".join(lines)


def _dump(model: Any) -> dict[str, Any] | None:
    return model.model_dump(mode="json") if model is not None else None


def _decision_markdown(
    reply: str,
    recommended: str,
    decision: WorkflowDecision,
    reason: str,
    evidence_used: str,
    overrode: bool,
) -> str:
    lines = [
        "**User response (verbatim):**",
        "",
        f"```text\n{reply}\n```",
        "",
        f"- Reviewer recommendation: `{recommended}`",
        f"- Decision: **{decision.value}**",
        f"- Reason: {reason or '(not given)'}",
        f"- Evidence used: {evidence_used or '(not given)'}",
    ]
    if overrode:
        lines.append("- The human overrode the reviewer recommendation.")
    return "\n".join(lines)


def record_human_decision(
    sessions_dir: Path, workflow_id: str, reply: str, evidence_used: str = ""
) -> tuple[dict[str, Any], Path | None]:
    """Record the human decision for a workflow that was run non-interactively.

    The decision is appended to the workflow history and coordination audit as a
    separate event, and a dated addendum (decision plus the updated evidence log)
    is appended to the session log that recorded the workflow.
    Returns the updated workflow record and the session log path, if it exists.
    """
    decision, reason = WorkflowDecision.parse(reply)
    if decision is None:
        raise workflow_log.WorkflowLogError(
            "The decision must start with accept, partial or reject (or a / p / r)."
        )
    record = workflow_log.load_workflow(sessions_dir, workflow_id)
    if record.get("status") != "completed":
        raise workflow_log.WorkflowLogError(
            f"Workflow {record['workflow_id']} did not complete ({record.get('status')}); "
            "there is no reviewed output to decide on."
        )

    decided_at = utc_now()
    workflow_log.record_decision(
        sessions_dir,
        record,
        decision.value,
        reason,
        evidence_used.strip(),
        decided_at.isoformat(),
        ALLOWED_DECISIONS,
    )
    record = workflow_log.load_workflow(sessions_dir, record["workflow_id"])

    session_path = workflow_log.session_log_path(sessions_dir, record.get("session_guid", ""))
    if not session_path.is_file():
        return record, None

    reviewer_result = record.get("reviewer_result") or {}
    review = (reviewer_result.get("output") if "agent" in reviewer_result else reviewer_result) or {}
    recommended = review.get("recommendation") or "(none)"
    workflow_log.append_to_session_log(
        session_path,
        "\n".join(
            [
                "---",
                "",
                f"## Human Decision — Workflow `{record['workflow_id']}` (Round {record.get('round', '?')})",
                "",
                f"_Recorded after the run on {format_timestamp(decided_at)}; the reviewer "
                "output above was read before deciding._",
                "",
                _decision_markdown(
                    reply, recommended, decision, reason, evidence_used.strip(),
                    overrode=recommended != "(none)" and decision.name.lower() != recommended,
                ),
                "",
                "#### Evidence Log",
                "",
                workflow_log.evidence_log_markdown(record, sessions_dir),
            ]
        ),
    )
    return record, session_path


def _bullets(label: str, items: list[str]) -> list[str]:
    if not items:
        return [f"**{label}:** (none)"]
    return [f"**{label}:**", ""] + [f"- {item}" for item in items]


def _plan_markdown(plan: WorkflowPlan) -> str:
    lines = [f"**Objective:** {plan.objective}", "", "**Steps:**", ""]
    lines.extend(f"{index}. {step}" for index, step in enumerate(plan.steps, start=1))
    lines.append("")
    lines.extend(_bullets("Evidence required", plan.evidence_required))
    lines.extend(["", f"**Human approval:** {plan.human_approval or '(not stated)'}"])
    return "\n".join(lines)


def _worker_markdown(output: WorkerOutput) -> str:
    lines = ["**Output:**", "", output.output.strip() or "(empty)", ""]
    lines.extend(_bullets("Evidence used", output.evidence_used))
    lines.append("")
    lines.extend(_bullets("Limitations", output.limitations))
    return "\n".join(lines)


def _review_markdown(assessment: ReviewerAssessment) -> str:
    lines = [f"**Risk:** {assessment.risk.value} — {assessment.risk_summary}", ""]
    lines.extend(_bullets("Corrections", assessment.corrections))
    lines.append("")
    lines.extend(_bullets("Retest", assessment.retest))
    lines.extend(["", f"**Recommendation:** `{assessment.recommendation.value}`"])
    return "\n".join(lines)
