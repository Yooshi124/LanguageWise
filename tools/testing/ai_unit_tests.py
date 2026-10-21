"""Post-commit AI-assisted unit test generation for each student's endpoints.

For every endpoint in student_endpoints.json, the source around the route is sent
to OpenRouter, which proposes unit test cases. The cases are written to
reports/post-commit/ai-unit-tests/ as JSON per student plus a summary.md.
Generated cases are recorded for review; they are not executed.
"""

import argparse
import json
import os
import sys
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
CONFIG_PATH = Path(__file__).resolve().parent / "student_endpoints.json"
DEFAULT_OUT_DIR = REPO_ROOT / "reports" / "post-commit" / "ai-unit-tests"

# Mirrors DEFAULT_MODEL and DEFAULT_OPENROUTER_BASE_URL in tools/agentic-loop/config/settings.py.
MODEL = "google/gemma-4-26b-a4b-it"
OPENROUTER_CHAT_URL = "https://openrouter.ai/api/v1/chat/completions"
CONTEXT_LINES = 40
REQUEST_TIMEOUT_SECONDS = 90

SYSTEM_PROMPT = (
    "You write focused unit test cases for ASP.NET Minimal API endpoints. "
    "Reply with JSON only, shaped as "
    '{"test_cases": [{"name": str, "scenario": str, "expected_status": int, "assertions": [str]}]}. '
    "Propose 3 to 5 cases covering the success path, validation failures, "
    "and authorization where relevant."
)


def read_context(source_path, method, route):
    lines = source_path.read_text(encoding="utf-8").splitlines()
    marker = f'Map{method.capitalize()}("{route}"'
    index = next((i for i, line in enumerate(lines) if marker in line), None)
    if index is None:
        raise ValueError(f"{marker} not found in {source_path.name}")
    start = max(0, index - CONTEXT_LINES)
    end = min(len(lines), index + CONTEXT_LINES + 1)
    return "\n".join(f"{number + 1}: {lines[number]}" for number in range(start, end))


def build_prompt(student, endpoint, context):
    return (
        f"Service: {student['service']}\n"
        f"Endpoint: {endpoint['method']} {endpoint['route']}\n"
        f"Existing test that covers it: {endpoint['test']}\n\n"
        f"Source excerpt from {endpoint['source']}:\n{context}"
    )


def call_openrouter(api_key, prompt):
    body = json.dumps(
        {
            "model": MODEL,
            "messages": [
                {"role": "system", "content": SYSTEM_PROMPT},
                {"role": "user", "content": prompt},
            ],
            "temperature": 0.2,
        }
    ).encode("utf-8")
    request = urllib.request.Request(
        OPENROUTER_CHAT_URL,
        data=body,
        method="POST",
        headers={
            "Authorization": f"Bearer {api_key}",
            "Content-Type": "application/json",
            "X-Title": "LanguageWise post-commit tests",
        },
    )
    # The URL is a fixed https constant, so urlopen is safe here.
    with urllib.request.urlopen(request, timeout=REQUEST_TIMEOUT_SECONDS) as response:  # noqa: S310
        payload = json.loads(response.read().decode("utf-8"))
    return payload["choices"][0]["message"]["content"]


def parse_test_cases(text):
    cleaned = text.strip()
    if cleaned.startswith("```"):
        cleaned = cleaned.strip("`").removeprefix("json").strip()
    data = json.loads(cleaned)
    cases = data.get("test_cases") if isinstance(data, dict) else None
    if not isinstance(cases, list) or not cases:
        raise ValueError("model response has no test_cases list")
    for case in cases:
        valid = (
            isinstance(case, dict)
            and isinstance(case.get("name"), str)
            and isinstance(case.get("scenario"), str)
            and isinstance(case.get("expected_status"), int)
            and isinstance(case.get("assertions"), list)
        )
        if not valid:
            raise ValueError(f"malformed test case in model response: {case!r}")
    return cases


def generate_for_endpoint(ask, student, endpoint):
    context = read_context(REPO_ROOT / endpoint["source"], endpoint["method"], endpoint["route"])
    cases = parse_test_cases(ask(build_prompt(student, endpoint, context)))
    return {
        "method": endpoint["method"],
        "route": endpoint["route"],
        "source": endpoint["source"],
        "existing_test": endpoint["test"],
        "test_cases": cases,
    }


def write_summary(out_dir, rows, generated_at, commit):
    lines = [
        "# Post-commit AI-assisted unit test generation",
        "",
        f"- Model: `{MODEL}`",
        f"- Commit: `{commit}`",
        f"- Generated: {generated_at}",
        "- Generated cases are recorded for review and are not executed.",
        "",
        "| Student | Owner | Endpoint | Test cases | Status |",
        "|---|---|---|---|---|",
    ]
    lines.extend(rows)
    (out_dir / "summary.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out-dir", type=Path, default=DEFAULT_OUT_DIR)
    args = parser.parse_args(argv)

    api_key = os.environ.get("OPENROUTER_API_KEY", "")
    if not api_key:
        print("OPENROUTER_API_KEY is not set; cannot generate AI unit tests.", file=sys.stderr)
        return 1

    def ask(prompt):
        return call_openrouter(api_key, prompt)

    students = json.loads(CONFIG_PATH.read_text(encoding="utf-8"))["students"]
    out_dir = args.out_dir
    out_dir.mkdir(parents=True, exist_ok=True)
    generated_at = datetime.now(timezone.utc).isoformat(timespec="seconds")
    commit = os.environ.get("GITHUB_SHA", "local")
    rows = []
    failures = 0

    for student in students:
        endpoints_out = []
        for endpoint in student["endpoints"]:
            try:
                result = generate_for_endpoint(ask, student, endpoint)
                status = "ok"
                count = str(len(result["test_cases"]))
            except (OSError, ValueError, KeyError, IndexError) as exc:
                result = {
                    "method": endpoint["method"],
                    "route": endpoint["route"],
                    "source": endpoint["source"],
                    "existing_test": endpoint["test"],
                    "error": str(exc),
                }
                status = f"error: {exc}"
                count = "0"
                failures += 1
            endpoints_out.append(result)
            rows.append(
                f"| {student['id']} | {student['owner']} | {endpoint['method']} "
                f"`{endpoint['route']}` | {count} | {status} |"
            )

        record = {
            "student": student["id"],
            "owner": student["owner"],
            "service": student["service"],
            "model": MODEL,
            "commit": commit,
            "generated_at": generated_at,
            "endpoints": endpoints_out,
        }
        (out_dir / f"{student['id']}.json").write_text(
            json.dumps(record, indent=2) + "\n", encoding="utf-8"
        )
        print(f"wrote {out_dir / (student['id'] + '.json')}")

    write_summary(out_dir, rows, generated_at, commit)
    print(f"wrote {out_dir / 'summary.md'}; endpoint failures: {failures}")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
