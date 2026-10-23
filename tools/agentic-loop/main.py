r"""Agentic Loop - a read-only planner/worker/reviewer code workflow powered by OpenRouter.

Every round runs the multi-agent workflow; the flags below add context on top of it.
Run from this directory:

    python main.py                          # interactive REPL
    python main.py --prompt "review tests"  # single round, then exit
    python main.py --scope ..\..\DatabaseService
    python main.py --ci-failures            # diagnose the latest failed CI run
    python main.py --deploy-logs 66         # review the deployment PR #66 triggered
    python main.py --decide <workflow-id> "partial <reason>"  # record a pending human decision
"""

from __future__ import annotations

import argparse
import logging
import sys
from pathlib import Path

TOOL_ROOT = Path(__file__).resolve().parent
if str(TOOL_ROOT) not in sys.path:
    sys.path.insert(0, str(TOOL_ROOT))

for _stream in (sys.stdout, sys.stderr):
    # Keep box drawing and accented paths readable on legacy Windows code pages.
    reconfigure = getattr(_stream, "reconfigure", None)
    if reconfigure:
        try:
            reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

from config.settings import ConfigError, Settings, load_settings  # noqa: E402
from collectors.github_actions import GithubActionsError, parse_pr_reference  # noqa: E402
from core.mcp_client import McpClient, McpError  # noqa: E402
from core.rag_client import RagClient, RagError  # noqa: E402
from core import console, stages  # noqa: E402
from core.openrouter_client import OpenRouterClient  # noqa: E402
from core.orchestrator import Orchestrator, record_human_decision  # noqa: E402
from core.prompt_registry import PromptError, PromptRegistry  # noqa: E402
from core.redaction import register_secret  # noqa: E402
from core.session import SessionState, format_timestamp  # noqa: E402
from output.session_writer import SessionWriter  # noqa: E402
from output.workflow_log import WorkflowLogError  # noqa: E402

BANNER_TITLE = "Agentic Loop - Rubber Duck Code Review"

HELP_TEXT = """
Commands
  /help              Show this help
  /stages            Show the six loop stages
  /status            Show the current scope, model and session file
  /config            Show the full configuration and available prompt templates
  /scope <path>      Review only this directory (relative paths resolve from the repo root)
  /scope reset       Go back to the configured scope
  /session           Print the path of this session's evidence log
  /ci-failures [wf]  Pull the latest failed GitHub Actions run and diagnose it
  /deploy-logs [pr]  Pull the deployment run a merged PR triggered (default: the
                     latest merged PR) and review its logs / failure
  /rag-validate [t]  Cross-check the code in scope against RAG-retrieved docs
  /rag <question>    Query the local RAG server directly (no review round)
  /mcp-validate [t]  Exercise the local MCP server and validate it against the code
  /mcp [tool]        List MCP tools (or call one) directly (no review round)
  /decide <id> <d>   Record the human decision (accept / partial / reject + reason)
                     for a workflow that was left pending
  /exit              End the session and write the log footer

Anything else runs the planner -> worker -> reviewer workflow, then you decide
accept / partial / reject (logged to workflow_history.jsonl and
coordination_audit.jsonl in the sessions dir), for example:
  review the database validation logic
  is the test coverage for the user service adequate?
"""


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="agentic-loop",
        description="Read-only rubber duck code review powered by OpenRouter.",
    )
    parser.add_argument(
        "--prompt",
        help=(
            "Run a single planner -> worker -> reviewer workflow for this request, "
            "leaving the human decision pending, then exit."
        ),
    )
    parser.add_argument(
        "--scope",
        help="Directory to review for this run (overrides TARGETED_DIRECTORY).",
    )
    parser.add_argument(
        "--env",
        help="Path to an alternative .env file.",
    )
    parser.add_argument(
        "--ci-failures",
        nargs="?",
        const="",
        metavar="WORKFLOW",
        help=(
            "Fetch the latest failed GitHub Actions run (optionally for one "
            "workflow, e.g. student-3.yml) and diagnose it, then exit."
        ),
    )
    parser.add_argument(
        "--deploy-logs",
        nargs="?",
        const="",
        metavar="PR",
        help=(
            "Fetch the deployment run (GITHUB_DEPLOY_WORKFLOW, default aws-release.yml) "
            "triggered by merging this pull request (66, #66 or a PR URL; default: the "
            "latest merged PR), review its job logs or failure, then exit."
        ),
    )
    parser.add_argument(
        "--rag-validate",
        nargs="?",
        const="",
        metavar="TOPIC",
        help=(
            "Retrieve documentation context from the local RAG server (optionally "
            "for one topic) and cross-check the code in scope against it, then exit. "
            "Requires the RAG server running locally (rag-server/, python server.py)."
        ),
    )
    parser.add_argument(
        "--mcp-validate",
        nargs="?",
        const="",
        metavar="TOOL",
        help=(
            "Exercise the local MCP server (initialize, tools/list, and a tools/call "
            "probe - optionally a named tool) and validate the mcp-server code in "
            "scope against the observed behaviour, then exit. Requires the MCP server "
            "running locally (mcp-server/, dotnet run)."
        ),
    )
    parser.add_argument(
        "--decide",
        nargs=2,
        metavar=("WORKFLOW_ID", "DECISION"),
        help=(
            "Record the human decision for a workflow left pending, e.g. "
            "--decide 7212a99a \"partial seed row clashes with Id 1\". DECISION is "
            "accept / partial / reject followed by the reason. No model is called."
        ),
    )
    parser.add_argument(
        "--evidence-used",
        default="",
        metavar="TEXT",
        help="With --decide: which evidence (files, agent fields, checks) informed the decision.",
    )
    return parser


def resolve_scope(settings: Settings, raw: str) -> Path:
    candidate = Path(raw).expanduser()
    if not candidate.is_absolute():
        candidate = (settings.repo_root / candidate).resolve()
    return candidate


def _quieten_third_party_logs(log_level: str) -> None:
    """Keep HTTP client chatter out of the review transcript.

    The SDK logs a line per request, which buries the findings. Set LOG_LEVEL=DEBUG
    in `.env` to see it again when diagnosing a connection problem.
    """
    if log_level == "DEBUG":
        return
    for name in ("httpx", "httpcore", "google_genai", "google.genai", "urllib3", "requests"):
        logging.getLogger(name).setLevel(logging.WARNING)


def session_details(settings: Settings, session_path: Path) -> dict[str, str]:
    return {
        "Model": settings.model,
        "Review agent": f"{settings.review_model} (via OpenRouter - mandatory)",
        "Agent models": (
            f"planner {settings.planner_model} | worker {settings.worker_model} | "
            f"reviewer {settings.review_model}"
        ),
        "Repo root": str(settings.repo_root),
        "Scope": str(settings.scope)
        + ("" if settings.scope_is_targeted else "  (whole repository)"),
        "Evidence log": str(session_path),
        "Mode": "read-only - no source file is ever modified",
    }


def run_decide(sessions_dir: Path, workflow_id: str, reply: str, evidence_used: str) -> int:
    try:
        record, session_path = record_human_decision(sessions_dir, workflow_id, reply, evidence_used)
    except (WorkflowLogError, ValueError) as exc:
        console.print_error(str(exc))
        return 1
    console.print_success(
        f"Human decision for workflow {record['workflow_id']}: "
        f"{record['human_decision']['status']}"
    )
    if session_path:
        console.print_success(f"Evidence log updated: {session_path}")
    else:
        console.print_warning("The session log for this workflow was not found; JSONL logs updated.")
    return 0


def run_repl(
    orchestrator: Orchestrator,
    writer: SessionWriter,
    single_prompt: str | None,
    ci_failures: str | None = None,
    rag_validate: str | None = None,
    mcp_validate: str | None = None,
    deploy_logs: str | None = None,
) -> int:
    if ci_failures is not None:
        orchestrator.interactive = False
        try:
            orchestrator.run_ci_failure_round(ci_failures or None)
        except GithubActionsError as exc:
            console.print_error(f"Could not fetch CI failures: {exc}")
            writer.write_footer("CI-failures run failed before the first round.")
            return 1
        writer.write_footer("CI-failures run completed.")
        console.print_success(f"\nEvidence log: {writer.path}")
        return 0

    if deploy_logs is not None:
        orchestrator.interactive = False
        try:
            orchestrator.run_deploy_logs_round(parse_pr_reference(deploy_logs))
        except GithubActionsError as exc:
            console.print_error(f"Could not fetch deployment logs: {exc}")
            writer.write_footer("Deploy-logs run failed before the first round.")
            return 1
        writer.write_footer("Deploy-logs run completed.")
        console.print_success(f"\nEvidence log: {writer.path}")
        return 0

    if rag_validate is not None:
        orchestrator.interactive = False
        try:
            orchestrator.run_rag_validation_round(rag_validate or None)
        except RagError as exc:
            console.print_error(f"Could not query the RAG server: {exc}")
            writer.write_footer("RAG-validate run failed before the first round.")
            return 1
        writer.write_footer("RAG-validate run completed.")
        console.print_success(f"\nEvidence log: {writer.path}")
        return 0

    if mcp_validate is not None:
        orchestrator.interactive = False
        try:
            orchestrator.run_mcp_validation_round(mcp_validate or None)
        except McpError as exc:
            console.print_error(f"Could not reach the MCP server: {exc}")
            writer.write_footer("MCP-validate run failed before the first round.")
            return 1
        writer.write_footer("MCP-validate run completed.")
        console.print_success(f"\nEvidence log: {writer.path}")
        return 0

    if single_prompt:
        orchestrator.interactive = False
        outcome = orchestrator.run_round(single_prompt)
        writer.write_footer("Single-prompt run completed.")
        console.print_success(f"\nEvidence log: {writer.path}")
        return 1 if outcome.aborted_reason else 0

    console.print_info("Type a request, or /help for commands.")
    closing_reason = "Session ended normally."

    while True:
        try:
            raw = console.ask("agentic-loop >").strip()
        except (EOFError, KeyboardInterrupt):
            closing_reason = "Session ended by user interrupt."
            console.print_warning("\nEnding session.")
            break

        if not raw:
            continue

        if raw.startswith("/"):
            command, _, argument = raw.partition(" ")
            command = command.lower()
            argument = argument.strip()

            if command in {"/exit", "/quit", "/q"}:
                break
            if command == "/help":
                console.print_markdown(HELP_TEXT)
                continue
            if command == "/stages":
                console.print_stage_map()
                continue
            if command == "/session":
                console.print_info(str(writer.path))
                continue
            if command == "/ci-failures":
                try:
                    orchestrator.run_ci_failure_round(argument or None)
                except GithubActionsError as exc:
                    console.print_error(str(exc))
                continue
            if command == "/deploy-logs":
                try:
                    orchestrator.run_deploy_logs_round(parse_pr_reference(argument))
                except GithubActionsError as exc:
                    console.print_error(str(exc))
                continue
            if command == "/rag-validate":
                try:
                    orchestrator.run_rag_validation_round(argument or None)
                except RagError as exc:
                    console.print_error(str(exc))
                continue
            if command == "/mcp-validate":
                try:
                    orchestrator.run_mcp_validation_round(argument or None)
                except McpError as exc:
                    console.print_error(str(exc))
                continue
            if command == "/mcp":
                client = McpClient(orchestrator.settings)
                try:
                    if argument:
                        result = client.call_tool(argument, {})
                        state = "isError" if result.is_error else "ok"
                        console.print_info(f"{result.name} -> {state}")
                        console.print_markdown(result.text or "(empty result)")
                    else:
                        tools = client.list_tools()
                        if not tools:
                            console.print_warning("No tools listed for this scope.")
                            continue
                        for tool in tools:
                            console.print_info(f"- {tool.name}: {tool.description}")
                except McpError as exc:
                    console.print_error(str(exc))
                continue
            if command == "/decide":
                workflow_id, _, reply = argument.partition(" ")
                if not workflow_id or not reply.strip():
                    console.print_warning("Usage: /decide <workflow-id> <accept|partial|reject> <reason>")
                    continue
                evidence_used = console.ask("Which evidence informed this decision? (Enter to skip):")
                run_decide(orchestrator.settings.sessions_dir, workflow_id, reply, evidence_used)
                continue
            if command == "/rag":
                if not argument:
                    console.print_warning("Usage: /rag <question>")
                    continue
                try:
                    result = RagClient(orchestrator.settings).query(argument)
                except RagError as exc:
                    console.print_error(str(exc))
                    continue
                if not result.results:
                    console.print_warning(
                        f"No matching passages found (confidence: {result.confidence})."
                    )
                    continue
                console.print_info(f"Confidence: {result.confidence}")
                for rank, item in enumerate(result.results, start=1):
                    console.print_info(
                        f"[{rank}] {item.source} | {item.heading} "
                        f"(relevance {item.relevance:.3f})"
                    )
                    console.print_markdown(item.text)
                continue
            if command == "/status":
                console.print_startup(
                    "Status", session_details(orchestrator.settings, writer.path)
                )
                continue
            if command == "/config":
                details = dict(orchestrator.settings.describe())
                for stage_name, templates in orchestrator.prompts.available().items():
                    details[f"Prompts [{stage_name}]"] = ", ".join(templates)
                console.print_startup("Configuration", details)
                continue
            if command == "/scope":
                if not argument:
                    console.print_warning("Usage: /scope <path>  or  /scope reset")
                    continue
                try:
                    if argument.lower() == "reset":
                        orchestrator.reset_scope()
                    else:
                        orchestrator.set_scope(resolve_scope(orchestrator.settings, argument))
                    console.print_success(f"Scope set to {orchestrator.settings.scope}")
                except ConfigError as exc:
                    console.print_error(str(exc))
                continue

            console.print_warning(f"Unknown command '{command}'. Try /help.")
            continue

        try:
            orchestrator.run_round(raw)
        except KeyboardInterrupt:
            console.print_warning("\nRound interrupted.")
        except Exception as exc:  # keep the REPL alive, but record the failure
            logging.getLogger(__name__).exception("Round failed")
            console.print_error(f"Round failed: {exc}")

    writer.write_footer(closing_reason)
    console.print_success(f"\nEvidence log: {writer.path}")
    return 0


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)

    try:
        settings = load_settings(env_path=Path(args.env) if args.env else None)
        register_secret(settings.api_key)
        register_secret(settings.github_token)
        register_secret(settings.mcp_api_key)
        register_secret(settings.mcp_user_token)
        if args.scope:
            settings = settings.with_scope(resolve_scope(settings, args.scope))
        prompts = PromptRegistry()
        prompts.validate()
    except (ConfigError, PromptError) as exc:
        console.print_error(str(exc))
        return 1

    logging.basicConfig(
        level=getattr(logging, settings.log_level, logging.INFO),
        format="%(levelname)s %(name)s: %(message)s",
    )
    _quieten_third_party_logs(settings.log_level)

    if args.decide:
        workflow_id, reply = args.decide
        return run_decide(settings.sessions_dir, workflow_id, reply, args.evidence_used)

    session = SessionState()
    writer = SessionWriter(settings.sessions_dir, session)
    writer.write_header(
        {
            "Repo root": str(settings.repo_root),
            "Scope": str(settings.scope),
            "Scope mode": "TARGETED_DIRECTORY" if settings.scope_is_targeted else "whole repository",
            "Analysis model": settings.model,
            "Selection model": settings.selection_model,
            "Review model": f"{settings.review_model} (via OpenRouter - mandatory)",
            "Planner agent model": settings.planner_model,
            "Worker agent model": settings.worker_model,
            "Reviewer agent model": settings.review_model,
            "Started": format_timestamp(session.started_at),
        }
    )

    console.print_startup(BANNER_TITLE, session_details(settings, writer.path))
    console.print_info(
        "Loop stages: " + " -> ".join(stages.STAGE_NAMES) + "   (/stages for detail)"
    )

    orchestrator = Orchestrator(
        settings=settings,
        prompts=prompts,
        client=OpenRouterClient(settings),
        review_client=OpenRouterClient(settings),
        session=session,
        writer=writer,
    )

    try:
        return run_repl(
            orchestrator,
            writer,
            args.prompt,
            args.ci_failures,
            args.rag_validate,
            args.mcp_validate,
            args.deploy_logs,
        )
    finally:
        writer.write_footer("Session closed.")


if __name__ == "__main__":
    raise SystemExit(main())
