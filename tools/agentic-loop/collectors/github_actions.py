"""GitHub Actions collector: pull failing CI runs and test logs over the REST API.

Used by the loop's CI-failures mode (`python main.py --ci-failures` or the
`/ci-failures` REPL command). It finds the most recent failed workflow run for
the repository, downloads the logs of each failed job, and extracts the failure
excerpts (failing tests, compiler errors, stack traces) so the implementation
agent can diagnose the root cause against the actual source code.

Authentication: `GITHUB_TOKEN` in `.env`. Optional for public repositories
(subject to tight rate limits); required for private ones. The repository is
auto-detected from `git remote get-url origin` unless `GITHUB_REPO` is set.
"""

from __future__ import annotations

import re
import subprocess
from dataclasses import dataclass, field
from pathlib import Path

import requests

from config.settings import Settings

GITHUB_API_BASE = "https://api.github.com"
GITHUB_API_VERSION = "2022-11-28"

MAX_FAILED_JOBS = 5
MAX_RUNS_TO_LIST = 10
MAX_EXCERPT_CHARS_PER_JOB = 6_000
REQUEST_TIMEOUT_SECONDS = 60

# Lines that usually carry the actual failure in a CI log. Deliberately tuned
# for the stack in this repository: dotnet test (xUnit), vitest, and compilers.
FAILURE_LINE = re.compile(
    r"(\[FAIL\]|\bFAILED\b|failed!|Error Message:|Stack Trace:|error CS\d+|"
    r"error TS\d+|error NETSDK\d+|AssertionError|Expected\b.*\bbut\b|"
    r"[A-Za-z]+Exception|✗|✘|×|::error|not ok\b|\bFAIL:\b|"
    r"\bfail(?:ed|ing)?\s+\d+)",
    re.IGNORECASE,
)

# GitHub prefixes every log line with an ISO timestamp; strip it for readability.
LOG_TIMESTAMP = re.compile(r"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d+Z\s?")

# Conclusions that mean the job did not pass and is worth inspecting.
FAILED_CONCLUSIONS = frozenset({"failure", "timed_out"})

REMOTE_PATTERNS = (
    re.compile(r"github\.com[:/](?P<slug>[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+?)(?:\.git)?/?$"),
)


class GithubActionsError(RuntimeError):
    """Raised when the GitHub Actions lookup fails or finds nothing usable."""


@dataclass
class FailedJob:
    name: str
    url: str
    conclusion: str
    excerpt: str


@dataclass
class CiFailureReport:
    """A failed workflow run distilled to the evidence the loop needs."""

    repo: str
    run_id: int
    run_name: str
    workflow_name: str
    branch: str
    commit_sha: str
    url: str
    created_at: str
    failed_jobs: list[FailedJob] = field(default_factory=list)

    def summary_line(self) -> str:
        return (
            f"{self.repo} run {self.run_id} ({self.workflow_name}) on "
            f"{self.branch}@{self.commit_sha[:8]} - {len(self.failed_jobs)} failed job(s)"
        )

    def as_prompt_text(self) -> str:
        lines = [
            "CI FAILURE REPORT (GitHub Actions)",
            f"Repository: {self.repo}",
            f"Workflow: {self.workflow_name}",
            f"Run: {self.run_name} (id {self.run_id})",
            f"Branch: {self.branch}  Commit: {self.commit_sha}",
            f"Run URL: {self.url}",
            f"Run at: {self.created_at}",
            "",
        ]
        for job in self.failed_jobs:
            lines.extend(
                [
                    f"=== Failed job: {job.name} (conclusion: {job.conclusion}) ===",
                    f"Job URL: {job.url}",
                    "--- Failure excerpt from the job log ---",
                    job.excerpt or "(no failure lines could be extracted from the log)",
                    "",
                ]
            )
        return "\n".join(lines)


def detect_repo_slug(repo_root: Path) -> str | None:
    """Parse `owner/name` from the origin remote (https or ssh)."""
    try:
        result = subprocess.run(
            ["git", "-C", str(repo_root), "remote", "get-url", "origin"],
            capture_output=True,
            text=True,
            timeout=5,
            check=False,
        )
    except (OSError, subprocess.SubprocessError):
        return None
    if result.returncode != 0:
        return None
    remote = result.stdout.strip()
    for pattern in REMOTE_PATTERNS:
        match = pattern.search(remote)
        if match:
            return match.group("slug")
    return None


def extract_failure_excerpt(log_text: str, max_chars: int = MAX_EXCERPT_CHARS_PER_JOB) -> str:
    """Pull the failure lines (plus a little context) out of a raw job log."""
    lines = [LOG_TIMESTAMP.sub("", line) for line in log_text.splitlines()]
    keep: set[int] = set()
    for index, line in enumerate(lines):
        if FAILURE_LINE.search(line):
            keep.update(range(max(0, index - 1), min(len(lines), index + 3)))

    excerpt_lines: list[str] = []
    previous = -2
    for index in sorted(keep):
        if index != previous + 1 and excerpt_lines:
            excerpt_lines.append("...")
        excerpt_lines.append(lines[index])
        previous = index

    excerpt = "\n".join(excerpt_lines).strip()
    if len(excerpt) > max_chars:
        excerpt = excerpt[:max_chars] + "\n... (excerpt truncated)"
    return excerpt


class GithubActionsClient:
    """Minimal GitHub REST client covering exactly what the loop needs."""

    def __init__(self, settings: Settings, session: "requests.Session | None" = None) -> None:
        self._settings = settings
        self._session = session or requests.Session()

    def fetch_failure_report(self, workflow: str | None = None) -> CiFailureReport:
        """Return the most recent failed run, with failure excerpts per failed job."""
        slug = self._settings.github_repo or detect_repo_slug(self._settings.repo_root)
        if not slug:
            raise GithubActionsError(
                "Could not determine the GitHub repository. Set GITHUB_REPO=owner/name "
                "in .env, or add a GitHub 'origin' remote to the repository."
            )

        run = self._latest_failed_run(slug, workflow or self._settings.github_workflow)
        jobs = self._failed_jobs(slug, run["id"])

        failed_jobs: list[FailedJob] = []
        for job in jobs[:MAX_FAILED_JOBS]:
            excerpt = ""
            try:
                excerpt = extract_failure_excerpt(self._job_log(slug, job["id"]))
            except GithubActionsError as exc:
                excerpt = f"(could not download the job log: {exc})"
            failed_jobs.append(
                FailedJob(
                    name=job.get("name", "(unnamed job)"),
                    url=job.get("html_url", ""),
                    conclusion=job.get("conclusion", "unknown"),
                    excerpt=excerpt,
                )
            )

        return CiFailureReport(
            repo=slug,
            run_id=run["id"],
            run_name=run.get("name") or run.get("display_title", ""),
            workflow_name=run.get("name", "(unknown workflow)"),
            branch=run.get("head_branch", "unknown"),
            commit_sha=run.get("head_sha", "unknown"),
            url=run.get("html_url", ""),
            created_at=run.get("created_at", ""),
            failed_jobs=failed_jobs,
        )

    # -- REST helpers ----------------------------------------------------------

    def _headers(self) -> dict[str, str]:
        headers = {
            "Accept": "application/vnd.github+json",
            "X-GitHub-Api-Version": GITHUB_API_VERSION,
        }
        if self._settings.github_token:
            headers["Authorization"] = f"Bearer {self._settings.github_token}"
        return headers

    def _get(self, url: str, *, params: dict | None = None) -> requests.Response:
        try:
            resp = self._session.get(
                url,
                headers=self._headers(),
                params=params,
                timeout=REQUEST_TIMEOUT_SECONDS,
            )
        except requests.exceptions.RequestException as exc:
            raise GithubActionsError(f"GitHub API request failed: {exc}") from exc
        if resp.status_code in {401, 403, 404} and not self._settings.github_token:
            raise GithubActionsError(
                f"GitHub API returned HTTP {resp.status_code}. For private repositories "
                "and higher rate limits, set GITHUB_TOKEN in .env "
                "(https://github.com/settings/tokens)."
            )
        if not resp.ok:
            raise GithubActionsError(
                f"GitHub API returned HTTP {resp.status_code} for {url}: "
                f"{resp.text[:300]}"
            )
        return resp

    def _latest_failed_run(self, slug: str, workflow: str | None) -> dict:
        params = {"status": "completed", "conclusion": "failure", "per_page": MAX_RUNS_TO_LIST}
        if workflow:
            url = f"{GITHUB_API_BASE}/repos/{slug}/actions/workflows/{workflow}/runs"
        else:
            url = f"{GITHUB_API_BASE}/repos/{slug}/actions/runs"
        payload = self._get(url, params=params).json()
        runs = payload.get("workflow_runs") or []
        if not runs:
            scope = f"workflow '{workflow}'" if workflow else "any workflow"
            raise GithubActionsError(
                f"No failed GitHub Actions runs found for {scope} in {slug}."
            )
        return runs[0]

    def _failed_jobs(self, slug: str, run_id: int) -> list[dict]:
        url = f"{GITHUB_API_BASE}/repos/{slug}/actions/runs/{run_id}/jobs"
        payload = self._get(url, params={"filter": "latest", "per_page": 100}).json()
        jobs = payload.get("jobs") or []
        failed = [job for job in jobs if job.get("conclusion") in FAILED_CONCLUSIONS]
        if not failed:
            raise GithubActionsError(
                f"Run {run_id} failed but has no failed jobs to inspect "
                "(it may have been cancelled before any job ran)."
            )
        return failed

    def _job_log(self, slug: str, job_id: int) -> str:
        url = f"{GITHUB_API_BASE}/repos/{slug}/actions/jobs/{job_id}/logs"
        # The API answers with a 302 to a signed download URL; requests follows it.
        return self._get(url).text
