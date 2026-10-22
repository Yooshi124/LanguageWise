"""GitHub Actions collector: pull failing CI runs and test logs over the REST API.

Used by the loop's CI-failures mode (`python main.py --ci-failures` or the
`/ci-failures` REPL command). It finds the most recent failed workflow run for
the repository, downloads the logs of each failed job, and extracts the failure
excerpts (failing tests, compiler errors, stack traces) so the implementation
agent can diagnose the root cause against the actual source code.

Also used by the deploy-logs mode (`python main.py --deploy-logs [PR]` or the
`/deploy-logs` REPL command). It resolves a merged pull request to its merge
commit, finds the deployment workflow run that commit triggered
(`GITHUB_DEPLOY_WORKFLOW`), and extracts the failure excerpt or outcome tail
of every job in that run.

Authentication: `GITHUB_TOKEN` in `.env`. Optional for public repositories
(subject to tight rate limits); required for private ones. The repository is
auto-detected from `git remote get-url origin` unless `GITHUB_REPO` is set.
"""

from __future__ import annotations

import re
import subprocess
from dataclasses import dataclass, field
from datetime import datetime
from pathlib import Path

import requests

from config.settings import Settings

GITHUB_API_BASE = "https://api.github.com"
GITHUB_API_VERSION = "2022-11-28"

MAX_FAILED_JOBS = 5
MAX_RUNS_TO_LIST = 10
MAX_EXCERPT_CHARS_PER_JOB = 6_000
MAX_DEPLOY_JOBS = 8
MAX_OUTCOME_TAIL_LINES = 40
MAX_PULLS_TO_SCAN = 30
REQUEST_TIMEOUT_SECONDS = 60

# Lines that usually carry the actual failure in a CI log. Deliberately tuned
# for the stack in this repository: dotnet test (xUnit), vitest, and compilers.
FAILURE_LINE = re.compile(
    r"(\[FAIL\]|\bFAILED\b|failed!|Error Message:|Stack Trace:|error CS\d+|"
    r"error TS\d+|error NETSDK\d+|AssertionError|Expected\b.*\bbut\b|"
    r"[A-Za-z]+Exception|✗|✘|×|::error|##\[error\]|not ok\b|\bFAIL:\b|"
    r"\bfail(?:ed|ing)?\s+\d+)",
    re.IGNORECASE,
)

# GitHub prefixes every log line with an ISO timestamp; strip it for readability.
LOG_TIMESTAMP = re.compile(r"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d+Z\s?")

# Conclusions that mean the job did not pass and is worth inspecting.
FAILED_CONCLUSIONS = frozenset({"failure", "timed_out"})

# Conclusions for which GitHub never produced a meaningful job log.
NO_LOG_CONCLUSIONS = frozenset({"skipped", "cancelled"})

# Step echo blocks (`##[group]Run ...` up to `##[endgroup]`) repeat the command,
# shell and every env var - including multi-line config JSON - so only the
# header line is kept.
LOG_GROUP_START = re.compile(r"^##\[group\](?P<title>.*)$")
LOG_GROUP_END = "##[endgroup]"
LOG_POST_JOB = "Post job cleanup."
LOG_NOISE = re.compile(
    r"DeprecationWarning|--trace-deprecation|^Cleaning up orphan processes|"
    # docker push/pull per-layer progress
    r"^[0-9a-f]{12}: (?:Preparing|Waiting|Pushing|Pushed|Layer already exists|Mounted from|"
    r"Retrying|Already exists|Pulling fs layer|Verifying Checksum|Download complete|"
    r"Pull complete|Downloading|Extracting)"
)

PR_REFERENCE = re.compile(r"^(?:.*/pull/|#)?(?P<number>\d+)/?$")

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


@dataclass
class DeployJob:
    name: str
    url: str
    status: str
    conclusion: str
    started_at: str
    completed_at: str
    steps: list[str] = field(default_factory=list)
    excerpt: str = ""

    @property
    def outcome(self) -> str:
        return self.conclusion if self.status == "completed" else self.status


@dataclass
class DeploymentReport:
    """A merged PR's deployment run distilled to the evidence the loop needs."""

    repo: str
    pr_number: int
    pr_title: str
    pr_url: str
    merged_at: str
    merge_commit_sha: str
    workflow: str
    run_id: int
    run_name: str
    run_status: str
    run_conclusion: str
    run_attempt: int
    url: str
    created_at: str
    jobs: list[DeployJob] = field(default_factory=list)

    @property
    def run_outcome(self) -> str:
        return self.run_conclusion if self.run_status == "completed" else self.run_status

    def summary_line(self) -> str:
        return (
            f"{self.repo} PR #{self.pr_number} -> {self.workflow} run {self.run_id} "
            f"on {self.merge_commit_sha[:8]} - {self.run_outcome}, {len(self.jobs)} job(s)"
        )

    def as_prompt_text(self) -> str:
        lines = [
            "DEPLOYMENT REPORT (GitHub Actions)",
            f"Repository: {self.repo}",
            f"Pull request: #{self.pr_number} {self.pr_title}",
            f"PR URL: {self.pr_url}",
            f"Merged at: {self.merged_at}  Merge commit: {self.merge_commit_sha}",
            f"Deployment workflow: {self.workflow}",
            f"Run: {self.run_name} (id {self.run_id}, attempt {self.run_attempt})",
            f"Run status: {self.run_outcome}",
            f"Run URL: {self.url}",
            f"Run at: {self.created_at}",
            "",
        ]
        for job in self.jobs:
            lines.extend(
                [
                    f"=== Job: {job.name} (outcome: {job.outcome}) ===",
                    f"Job URL: {job.url}",
                    f"Started: {job.started_at or '-'}  Completed: {job.completed_at or '-'}"
                    f"  Duration: {_duration(job.started_at, job.completed_at)}",
                    "Steps:",
                    *(f"  {step}" for step in job.steps or ["(no steps reported)"]),
                    "--- Log excerpt ---",
                    job.excerpt or "(no log lines could be extracted)",
                    "",
                ]
            )
        return "\n".join(lines)


def _duration(started_at: str, completed_at: str) -> str:
    if not started_at or not completed_at:
        return "-"
    try:
        start = datetime.fromisoformat(started_at.replace("Z", "+00:00"))
        end = datetime.fromisoformat(completed_at.replace("Z", "+00:00"))
    except ValueError:
        return "-"
    seconds = max(0, int((end - start).total_seconds()))
    return f"{seconds // 60}m{seconds % 60:02d}s"


def parse_pr_reference(raw: str | None) -> int | None:
    """Accept `66`, `#66` or a `.../pull/66` URL; blank means "latest merged PR"."""
    text = (raw or "").strip()
    if not text:
        return None
    match = PR_REFERENCE.match(text)
    if not match:
        raise GithubActionsError(
            f"'{text}' is not a pull request reference. Use a number (66), "
            "#66, or a GitHub pull request URL."
        )
    return int(match.group("number"))


def detect_repo_slug(repo_root: Path) -> str | None:
    """Parse `owner/name` from the origin remote (https or ssh)."""
    try:
        result = subprocess.run(  # noqa: S603 - fixed git argv, no user input
            ["git", "-C", str(repo_root), "remote", "get-url", "origin"],  # noqa: S607 - git resolved from PATH by design
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


def clean_log_lines(log_text: str) -> list[str]:
    """Strip timestamps, step echo/env blocks, runner noise and post-job cleanup."""
    cleaned: list[str] = []
    in_group = False
    for raw in log_text.splitlines():
        line = LOG_TIMESTAMP.sub("", raw).rstrip()
        group = LOG_GROUP_START.match(line)
        if group:
            in_group = True
            cleaned.append(f"> {group.group('title').strip()}")
            continue
        if line == LOG_GROUP_END:
            in_group = False
            continue
        if line == LOG_POST_JOB:
            break
        if in_group or not line or LOG_NOISE.search(line):
            continue
        cleaned.append(line)
    return cleaned


def extract_outcome_tail(
    lines: list[str], max_lines: int = MAX_OUTCOME_TAIL_LINES, max_chars: int = MAX_EXCERPT_CHARS_PER_JOB
) -> str:
    """The last meaningful lines of a job that did not fail - its observable outcome."""
    tail = lines[-max_lines:]
    excerpt = "\n".join(tail).strip()
    if len(lines) > max_lines:
        excerpt = "... (earlier lines omitted)\n" + excerpt
    if len(excerpt) > max_chars:
        excerpt = "... (excerpt truncated)\n" + excerpt[-max_chars:]
    return excerpt


class GithubActionsClient:
    """Minimal GitHub REST client covering exactly what the loop needs."""

    def __init__(self, settings: Settings, session: "requests.Session | None" = None) -> None:
        self._settings = settings
        self._session = session or requests.Session()

    def fetch_failure_report(self, workflow: str | None = None) -> CiFailureReport:
        """Return the most recent failed run, with failure excerpts per failed job."""
        slug = self._require_slug()

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

    def fetch_deployment_report(
        self, pr_number: int | None = None, workflow: str | None = None
    ) -> DeploymentReport:
        """Return the deployment run triggered by a merged PR, with per-job log excerpts.

        `pr_number=None` uses the most recently merged pull request.
        """
        slug = self._require_slug()
        deploy_workflow = workflow or self._settings.github_deploy_workflow
        pull = self._pull(slug, pr_number) if pr_number is not None else self._latest_merged_pull(slug)
        number = pull["number"]
        if not pull.get("merged_at"):
            raise GithubActionsError(
                f"PR #{number} is not merged, so it has not triggered a deployment."
            )
        merge_sha = pull.get("merge_commit_sha") or ""
        if not merge_sha:
            raise GithubActionsError(f"PR #{number} is merged but GitHub reports no merge commit.")

        run = self._deployment_run(slug, deploy_workflow, merge_sha, number)
        jobs = self._run_jobs(slug, run["id"])
        if not any(job.get("status") == "completed" for job in jobs):
            raise GithubActionsError(
                f"Deployment run {run['id']} for PR #{number} is {run.get('status', 'pending')} "
                "and no job has completed yet - GitHub only publishes job logs once a job "
                f"finishes. Watch it at {run.get('html_url', '')}"
            )

        deploy_jobs = [self._deploy_job(slug, job) for job in jobs[:MAX_DEPLOY_JOBS]]
        return DeploymentReport(
            repo=slug,
            pr_number=number,
            pr_title=pull.get("title", ""),
            pr_url=pull.get("html_url", ""),
            merged_at=pull.get("merged_at", ""),
            merge_commit_sha=merge_sha,
            workflow=deploy_workflow,
            run_id=run["id"],
            run_name=run.get("display_title") or run.get("name", ""),
            run_status=run.get("status", "unknown"),
            run_conclusion=run.get("conclusion") or "none",
            run_attempt=run.get("run_attempt", 1),
            url=run.get("html_url", ""),
            created_at=run.get("created_at", ""),
            jobs=deploy_jobs,
        )

    def _deploy_job(self, slug: str, job: dict) -> DeployJob:
        status = job.get("status", "unknown")
        conclusion = job.get("conclusion") or "none"
        if status != "completed":
            excerpt = "(job has not completed - GitHub publishes its log once it finishes)"
        elif conclusion in NO_LOG_CONCLUSIONS:
            excerpt = f"(job {conclusion} - no log to inspect)"
        else:
            try:
                lines = clean_log_lines(self._job_log(slug, job["id"]))
                if conclusion in FAILED_CONCLUSIONS:
                    excerpt = extract_failure_excerpt("\n".join(lines)) or extract_outcome_tail(lines)
                else:
                    excerpt = extract_outcome_tail(lines)
            except GithubActionsError as exc:
                excerpt = f"(could not download the job log: {exc})"
        return DeployJob(
            name=job.get("name", "(unnamed job)"),
            url=job.get("html_url", ""),
            status=status,
            conclusion=conclusion,
            started_at=job.get("started_at") or "",
            completed_at=job.get("completed_at") or "",
            steps=[
                f"{step.get('number', '?')}. {step.get('name', '(unnamed step)')}: "
                f"{step.get('conclusion') or step.get('status', 'unknown')}"
                for step in job.get("steps") or []
            ],
            excerpt=excerpt,
        )

    # -- REST helpers ----------------------------------------------------------

    def _require_slug(self) -> str:
        slug = self._settings.github_repo or detect_repo_slug(self._settings.repo_root)
        if not slug:
            raise GithubActionsError(
                "Could not determine the GitHub repository. Set GITHUB_REPO=owner/name "
                "in .env, or add a GitHub 'origin' remote to the repository."
            )
        return slug

    def _pull(self, slug: str, number: int) -> dict:
        url = f"{GITHUB_API_BASE}/repos/{slug}/pulls/{number}"
        try:
            return self._get(url).json()
        except GithubActionsError as exc:
            if "HTTP 404" in str(exc) and self._settings.github_token:
                raise GithubActionsError(f"Pull request #{number} was not found in {slug}.") from exc
            raise

    def _latest_merged_pull(self, slug: str) -> dict:
        url = f"{GITHUB_API_BASE}/repos/{slug}/pulls"
        params = {"state": "closed", "sort": "updated", "direction": "desc", "per_page": MAX_PULLS_TO_SCAN}
        merged = [pull for pull in self._get(url, params=params).json() or [] if pull.get("merged_at")]
        if not merged:
            raise GithubActionsError(f"No merged pull requests found in {slug}.")
        return max(merged, key=lambda pull: pull["merged_at"])

    def _deployment_run(self, slug: str, workflow: str, merge_sha: str, pr_number: int) -> dict:
        url = f"{GITHUB_API_BASE}/repos/{slug}/actions/workflows/{workflow}/runs"
        try:
            payload = self._get(url, params={"head_sha": merge_sha, "per_page": MAX_RUNS_TO_LIST}).json()
        except GithubActionsError as exc:
            if "HTTP 404" in str(exc) and self._settings.github_token:
                raise GithubActionsError(
                    f"Deployment workflow '{workflow}' was not found in {slug}. "
                    "Set GITHUB_DEPLOY_WORKFLOW to the workflow file that deploys merges."
                ) from exc
            raise
        runs = payload.get("workflow_runs") or []
        if not runs:
            raise GithubActionsError(
                f"No '{workflow}' run was triggered by PR #{pr_number}'s merge commit "
                f"{merge_sha[:8]}. The PR may have merged into a branch that does not deploy, "
                "or the run has aged out of GitHub's retention window."
            )
        return max(runs, key=lambda run: run.get("created_at", ""))

    def _run_jobs(self, slug: str, run_id: int) -> list[dict]:
        url = f"{GITHUB_API_BASE}/repos/{slug}/actions/runs/{run_id}/jobs"
        return self._get(url, params={"filter": "latest", "per_page": 100}).json().get("jobs") or []

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
