"""Console rendering.

Stage banners are driven by `core.stages`, the same source the evidence log uses,
so the loop structure printed on screen matches the markdown record exactly.
"""

from __future__ import annotations

from pathlib import Path

from rich.console import Console
from rich.markdown import Markdown
from rich.markup import escape
from rich.panel import Panel
from rich.rule import Rule
from rich.table import Table
from rich.text import Text

from core import stages
from core.models import Finding, ReviewerAssessment, Severity, WorkerOutput, WorkflowPlan
from core.redaction import scrub

SEVERITY_STYLE = {
    Severity.HIGH: "bold red",
    Severity.MEDIUM: "yellow",
    Severity.LOW: "cyan",
}

console = Console()

# A model summary longer than this is a sign the model rambled; cap the console
# copy so the terminal stays readable.
SUMMARY_DISPLAY_LIMIT = 600


def _safe(text: object) -> str:
    """Model/user text is never markup and never allowed to leak a secret."""
    return escape(scrub(str(text)))


def print_startup(title: str, details: dict[str, str]) -> None:
    table = Table.grid(padding=(0, 2))
    table.add_column(style="dim")
    table.add_column()
    for key, value in details.items():
        table.add_row(_safe(key), _safe(value))
    console.print(Panel(table, title=title, border_style="cyan", expand=False))


def print_stage_banner(stage: stages.Stage, subtitle: str = "") -> None:
    text = Text(stage.banner, style="bold white on blue")
    console.print()
    console.print(Rule(text, style="blue"))
    if subtitle:
        console.print(f"[dim]{subtitle}[/dim]")


def print_info(message: str) -> None:
    console.print(f"[dim]{_safe(message)}[/dim]")


def print_success(message: str) -> None:
    console.print(f"[green]{_safe(message)}[/green]")


def print_warning(message: str) -> None:
    console.print(f"[yellow]{_safe(message)}[/yellow]")


def print_error(message: str) -> None:
    console.print(f"[bold red]{_safe(message)}[/bold red]")


def print_block(text: str) -> None:
    console.print(_safe(text))


def print_findings(findings: list[Finding]) -> None:
    """Render findings as the numbered Problem / Suggested fix list."""
    if not findings:
        console.print()
        console.print(
            Panel(
                "No evidence-backed issues identified for this request.",
                border_style="green",
                expand=False,
            )
        )
        return

    console.print()
    for index, finding in enumerate(findings, start=1):
        style = SEVERITY_STYLE.get(finding.severity, "white")
        console.print(
            f"[bold]{index}. Problem:[/bold] {_safe(finding.problem.strip())} "
            f"[{style}]({finding.severity.value})[/{style}]"
        )
        console.print(f"   [bold]Suggested fix:[/bold] {_safe(finding.suggested_fix.strip())}")
        if finding.files:
            console.print(f"   [dim]Files: {_safe(', '.join(finding.files))}[/dim]")
        if finding.evidence.strip():
            console.print(f"   [dim]Evidence: {_safe(finding.evidence.strip())}[/dim]")
        console.print()


def print_summary(summary: str) -> None:
    text = summary.strip()
    if not text:
        return
    if len(text) > SUMMARY_DISPLAY_LIMIT:
        # A model that ignores the two-sentence rule must not flood the terminal;
        # the full text is still recorded in the evidence log.
        text = text[:SUMMARY_DISPLAY_LIMIT].rstrip() + "... (full text in the evidence log)"
    console.print(f"[italic]{_safe(text)}[/italic]")


def _print_list(label: str, items: list[str]) -> None:
    if not items:
        return
    console.print(f"   [bold]{label}:[/bold]")
    for item in items:
        console.print(f"     - {_safe(item)}")


def print_workflow_plan(plan: WorkflowPlan) -> None:
    console.print()
    console.print(f"[bold]Objective:[/bold] {_safe(plan.objective)}")
    console.print("   [bold]Steps:[/bold]")
    for index, step in enumerate(plan.steps, start=1):
        console.print(f"     {index}. {_safe(step)}")
    _print_list("Evidence required", plan.evidence_required)
    if plan.human_approval.strip():
        console.print(f"   [bold]Human approval:[/bold] {_safe(plan.human_approval)}")


def print_worker_output(output: WorkerOutput) -> None:
    console.print()
    console.print(Panel(Markdown(scrub(output.output)), title="Worker output", expand=False))
    _print_list("Evidence used", output.evidence_used)
    _print_list("Limitations", output.limitations)


def print_review(assessment: ReviewerAssessment) -> None:
    style = SEVERITY_STYLE.get(assessment.risk, "white")
    console.print()
    console.print(
        f"[bold]Risk:[/bold] [{style}]{assessment.risk.value}[/{style}] - "
        f"{_safe(assessment.risk_summary)}"
    )
    _print_list("Corrections", assessment.corrections)
    _print_list("Retest", assessment.retest)
    console.print(f"   [bold]Recommendation:[/bold] {assessment.recommendation.value}")


def print_workflow_recorded(history_path: Path, audit_path: Path) -> None:
    console.print()
    console.print(
        Panel(
            f"Workflow history: [bold]{_safe(history_path)}[/bold]\n"
            f"Coordination audit: [bold]{_safe(audit_path)}[/bold]",
            title="ADAPT",
            border_style="green",
            expand=False,
        )
    )


def print_plan_saved(path: Path) -> None:
    console.print()
    console.print(
        Panel(
            f"Implementation plan saved to:\n[bold]{_safe(path)}[/bold]",
            title="ADAPT",
            border_style="green",
            expand=False,
        )
    )


def print_markdown(text: str) -> None:
    console.print(Markdown(text))


def print_stage_map() -> None:
    table = Table(title="Agentic Loop Stages", show_lines=False)
    table.add_column("#", style="dim", justify="right")
    table.add_column("Stage", style="bold")
    table.add_column("What happens")
    for stage in stages.STAGES:
        table.add_row(str(stage.number), stage.name, stage.description)
    console.print(table)


def ask(prompt_text: str) -> str:
    console.print()
    return console.input(f"[bold cyan]{prompt_text}[/bold cyan] ")
