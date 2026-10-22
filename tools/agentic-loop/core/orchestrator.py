"""The agentic loop itself.

One round is always the multi-agent workflow (planner -> worker -> reviewer -> human)
and walks the six canonical stages in order:

    PLAN -> ACT -> OBSERVE -> AGENT -> HUMAN REVIEW -> ADAPT

The CI, deployment, RAG and MCP modes only add extra context on top of it.
Every stage prints a banner and writes a matching heading into the evidence log,
including stages that could not be reached, so the structure of the loop is always
visible in the record.
"""

from __future__ import annotations

import logging
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Callable

from agents import file_selector, reviewer, worker, workflow_planner
from collectors import file_reader, github_actions, mcp_observer, rag_observer, repo_observer, repo_scanner
from config.settings import Settings
from core import console, stages
from core.mcp_client import McpClient
from core.models import ReviewerAssessment, WorkerOutput, WorkflowDecision, WorkflowPlan
from core.openrouter_client import OpenRouterClient, OpenRouterError
from core.prompt_registry import PromptRegistry
from core.rag_client import RagClient
from core.session import RoundState, SessionState, format_timestamp, new_guid, utc_now
from output import workflow_log
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

DEPLOY_LOGS_PROMPT = (
    "The deployment workflow run triggered by merging a pull request is "
    "included in the observations below: each job's outcome, duration, steps "
    "and a log excerpt (failure lines for failed jobs, the final lines for the "
    "others). Using the deployment scripts, infrastructure and workflow code "
    "in scope, explain what the deployment actually did and whether the "
    "change reached the running environment. If any job failed, diagnose the "
    "root cause and propose the concrete fix. If the run succeeded but the "
    "logs show no real rollout (for example the deploy job only published or "
    "skipped because the target environment was down), say so explicitly. "
    "If a log says its raw output was suppressed, state what is unknown "
    "rather than guessing, and name where the missing evidence would be found."
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
    "The reviewer agent is running on the same model as the planner or worker agent, so "
    "its review is not independent. Set OPENROUTER_PLANNER_MODEL, OPENROUTER_WORKER_MODEL "
    "and OPENROUTER_REVIEW_MODEL to three different models for a separate second opinion."
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
    aborted_reason: str | None = None
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

    def set_scope(self, directory: Path) -> None:
        self.settings = self.settings.with_scope(directory)

    def reset_scope(self) -> None:
        """Restore the scope the session started with (TARGETED_DIRECTORY or --scope)."""
        self.settings = self._initial_settings

    def run_round(self, user_request: str, extra_context: str | None = None) -> RoundOutcome:
        """Run the planner -> worker -> reviewer -> human workflow as one round.

        PLAN: the planner agent turns the request into a workflow plan.
        ACT/OBSERVE: files are selected using the request plus the plan;
        `extra_context` (CI, deployment, RAG or MCP evidence) is added to the observations.
        AGENT: the worker executes the plan; the reviewer assesses the risk.
        HUMAN REVIEW: the human accepts, partially accepts or rejects.
        ADAPT: the workflow history and coordination audit are appended.
        Aborted workflows are still recorded so the audit trail has no gaps.
        """
        workflow = WorkflowRun()
        outcome = self._guarded_round(
            user_request,
            lambda round_state: self._workflow_round(round_state, workflow, extra_context),
        )
        outcome.workflow = workflow
        if outcome.aborted_reason:
            history, audit = self._workflow_records(
                workflow, outcome.round_state, "aborted", outcome.aborted_reason
            )
            workflow_log.append_workflow(self.settings.sessions_dir, history, audit)
        return outcome

    def _workflow_round(
        self, round_state: RoundState, workflow: WorkflowRun, extra_context: str | None
    ) -> RoundOutcome:
        user_request = round_state.prompt
        plan = self._stage_plan_workflow(round_state, workflow)
        selection, scan = self._stage_act(_selection_prompt(user_request, plan))
        bundle = file_reader.load_files(selection.entries, self.settings)
        workflow.evidence_files = [file.relative_path for file in bundle.files]
        observation = self._stage_observe(scan, bundle, extra_context)
        self._stage_coordinate(user_request, plan, bundle, observation, round_state, workflow)
        self._stage_workflow_decision(workflow)
        self._stage_record_workflow(workflow, round_state)
        return RoundOutcome(round_state)

    def _guarded_round(
        self, prompt: str, body: Callable[[RoundState], RoundOutcome]
    ) -> RoundOutcome:
        round_state = self.session.start_round(prompt)
        self.writer.start_round(round_state.number)

        try:
            return body(round_state)

        except RoundAborted as abort:
            self._record_unreached(abort.reason)
            console.print_error(f"Round {round_state.number} stopped: {abort.reason}")
            return RoundOutcome(round_state, aborted_reason=abort.reason)
        except KeyboardInterrupt:
            self._record_unreached("interrupted by the user")
            console.print_warning("\nRound interrupted - evidence log preserved.")
            return RoundOutcome(round_state, aborted_reason="interrupted by user")
        except EOFError:
            self._record_unreached("input stream closed before the human replied")
            console.print_warning("\nInput closed - round ended, evidence log preserved.")
            return RoundOutcome(round_state, aborted_reason="input stream closed")
        except BaseException as exc:
            # Any other failure must still leave the six canonical stage headings
            # in the evidence log before it propagates to the REPL.
            self._record_unreached(f"unexpected error: {exc}")
            raise

    def run_ci_failure_round(self, workflow: str | None = None) -> RoundOutcome:
        """Fetch the latest failed GitHub Actions run and review it as a round.

        The failure report is injected into the OBSERVE stage so the worker and
        reviewer agents see it alongside the code.
        Raises `GithubActionsError` if no usable failure data can be fetched.
        """
        client = github_actions.GithubActionsClient(self.settings)
        console.print_info("Fetching the latest failed GitHub Actions run...")
        report = client.fetch_failure_report(workflow=workflow)
        console.print_info(f"Found: {report.summary_line()}")
        for job in report.failed_jobs:
            console.print_info(f"  - {job.name}: {job.conclusion}")
        return self.run_round(CI_FAILURE_PROMPT, extra_context=report.as_prompt_text())

    def run_deploy_logs_round(self, pr_number: int | None = None) -> RoundOutcome:
        """Fetch the deployment run a merged PR triggered and review it as a round.

        `pr_number=None` uses the most recently merged pull request. The
        deployment report is injected into the OBSERVE stage alongside the code.
        Raises `GithubActionsError` if no usable deployment data can be fetched.
        """
        client = github_actions.GithubActionsClient(self.settings)
        target = f"PR #{pr_number}" if pr_number is not None else "the latest merged PR"
        console.print_info(
            f"Fetching the {self.settings.github_deploy_workflow} deployment for {target}..."
        )
        report = client.fetch_deployment_report(pr_number=pr_number)
        console.print_info(f"Found: {report.summary_line()}")
        for job in report.jobs:
            console.print_info(f"  - {job.name}: {job.outcome}")
        return self.run_round(DEPLOY_LOGS_PROMPT, extra_context=report.as_prompt_text())

    def run_rag_validation_round(self, topic: str | None = None) -> RoundOutcome:
        """Retrieve documentation context from the local RAG server and cross-check it.

        The retrieved passages are injected into the OBSERVE stage so the worker
        and reviewer agents see them alongside the code.
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
        OBSERVE stage so the worker and reviewer agents see it alongside the
        mcp-server source code. Raises `McpError` if the MCP server cannot be
        reached or rejects the request.
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

    # -- workflow stages (4-6) ----------------------------------------------

    def _stage_plan_workflow(self, round_state: RoundState, workflow: WorkflowRun) -> WorkflowPlan:
        self._stage_plan(round_state)
        console.print_info(
            f"Planner agent planning the workflow with {self.settings.planner_model}..."
        )
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
            f"with {self.settings.worker_model}..."
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
        if not self._reviewer_independent():
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
            "planner": self.settings.planner_model,
            "worker": self.settings.worker_model,
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
            "reviewer_independent": self._reviewer_independent(),
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

    def _reviewer_independent(self) -> bool:
        """The reviewer must not share a model with the agents it is checking."""
        return self.settings.review_model not in (
            self.settings.planner_model,
            self.settings.worker_model,
        )

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
