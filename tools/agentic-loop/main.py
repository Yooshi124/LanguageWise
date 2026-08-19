"""Agentic Loop - a read-only rubber duck code reviewer powered by Google Gemini.

Run from this directory:

    python main.py                          # interactive REPL
    python main.py --prompt "review tests"  # single round, then exit
    python main.py --scope ..\\..\\DatabaseService
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
from core import console, stages  # noqa: E402
from core.gemini_client import GeminiClient  # noqa: E402
from core.orchestrator import Orchestrator  # noqa: E402
from core.prompt_registry import PromptError, PromptRegistry  # noqa: E402
from core.redaction import register_secret  # noqa: E402
from core.session import SessionState, format_timestamp  # noqa: E402
from output.session_writer import SessionWriter  # noqa: E402

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
  /exit              End the session and write the log footer

Anything else is treated as a review prompt, for example:
  review the database validation logic
  is the test coverage for the user service adequate?
"""


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="agentic-loop",
        description="Read-only rubber duck code review powered by Google Gemini.",
    )
    parser.add_argument(
        "--prompt",
        help="Run a single review round with this prompt, then exit.",
    )
    parser.add_argument(
        "--scope",
        help="Directory to review for this run (overrides TARGETED_DIRECTORY).",
    )
    parser.add_argument(
        "--env",
        help="Path to an alternative .env file.",
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
        "Review agent": "on" if settings.enable_review_agent else "off",
        "Repo root": str(settings.repo_root),
        "Scope": str(settings.scope)
        + ("" if settings.scope_is_targeted else "  (whole repository)"),
        "Evidence log": str(session_path),
        "Mode": "read-only - no source file is ever modified",
    }


def run_repl(orchestrator: Orchestrator, writer: SessionWriter, single_prompt: str | None) -> int:
    if single_prompt:
        orchestrator.interactive = False
        orchestrator.run_round(single_prompt)
        writer.write_footer("Single-prompt run completed.")
        console.print_success(f"\nEvidence log: {writer.path}")
        return 0

    console.print_info("Type a review prompt, or /help for commands.")
    closing_reason = "Session ended normally."
    end_session = False

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
            pending: str | None = raw
            while pending:
                outcome = orchestrator.run_round(pending)
                pending = outcome.follow_up_prompt
                if outcome.end_session:
                    closing_reason = "Session ended by the user after an empty review."
                    end_session = True
                    break
        except KeyboardInterrupt:
            console.print_warning("\nRound interrupted.")
        except Exception as exc:  # keep the REPL alive, but record the failure
            logging.getLogger(__name__).exception("Round failed")
            console.print_error(f"Round failed: {exc}")

        if end_session:
            break

    writer.write_footer(closing_reason)
    console.print_success(f"\nEvidence log: {writer.path}")
    plans = orchestrator.session.plans_created
    if plans:
        console.print_success("Plans created this session:")
        for path in plans:
            console.print_info(f"  {path}")
    return 0


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)

    try:
        settings = load_settings(env_path=Path(args.env) if args.env else None)
        register_secret(settings.api_key)
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

    session = SessionState()
    writer = SessionWriter(settings.sessions_dir, session)
    writer.write_header(
        {
            "Repo root": str(settings.repo_root),
            "Scope": str(settings.scope),
            "Scope mode": "TARGETED_DIRECTORY" if settings.scope_is_targeted else "whole repository",
            "Analysis model": settings.model,
            "Selection model": settings.selection_model,
            "Review model": settings.review_model
            + ("" if settings.enable_review_agent else " (disabled)"),
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
        client=GeminiClient(settings),
        session=session,
        writer=writer,
    )

    try:
        return run_repl(orchestrator, writer, args.prompt)
    finally:
        writer.write_footer("Session closed.")


if __name__ == "__main__":
    raise SystemExit(main())
