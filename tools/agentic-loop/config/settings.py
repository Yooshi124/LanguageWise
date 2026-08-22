"""Typed configuration loaded from the environment / `.env` file.

Every tunable part of the agentic loop lives here so that behaviour can be
changed without editing Python source.
"""

from __future__ import annotations

import os
from dataclasses import dataclass, field
from pathlib import Path

from dotenv import load_dotenv

TOOL_ROOT = Path(__file__).resolve().parent.parent

DEFAULT_MODEL = "gemini-3.7-flash"
DEFAULT_OLLAMA_HOST = "http://localhost:11434"
DEFAULT_OLLAMA_REVIEW_MODEL = "gemma4:e2b"

DEFAULT_IGNORE_DIRS = (
    ".git,node_modules,.venv,venv,env,__pycache__,bin,obj,dist,build,out,target,"
    ".vs,.idea,.pytest_cache,.mypy_cache,coverage,htmlcov,Sessions,Plans"
)

DEFAULT_INCLUDE_EXTENSIONS = (
    ".py,.cs,.js,.jsx,.ts,.tsx,.java,.kt,.go,.rb,.php,.rs,.c,.h,.cpp,.hpp,"
    ".sql,.html,.css,.scss,.vue,.svelte,.sh,.ps1,.yml,.yaml,.json,.toml,.ini,"
    ".cfg,.xml,.csproj,.sln,.md,.txt,.dockerfile,.tf,.gradle"
)

EXTENSIONLESS_FILENAMES = frozenset(
    {"dockerfile", "makefile", "procfile", "jenkinsfile", "vagrantfile"}
)

SECRET_FILE_PATTERNS = (
    ".env",
    "*.env",
    ".env.*",
    "*.pem",
    "*.key",
    "*.pfx",
    "*.p12",
    "id_rsa*",
    "id_dsa*",
    "secrets.*",
    "*.secrets",
    "credentials*",
    "*.keystore",
    "*.jks",
)

# Accepted values for THINKING_LEVEL; blank leaves it to the model default.
THINKING_LEVELS = frozenset({"minimal", "low", "medium", "high"})


class ConfigError(RuntimeError):
    """Raised when the environment configuration is missing or invalid."""


def _clean(value: str | None) -> str:
    return (value or "").strip()


def _csv(value: str, fallback: str) -> tuple[str, ...]:
    raw = value if _clean(value) else fallback
    return tuple(part.strip() for part in raw.split(",") if part.strip())


def _extensions(value: str, fallback: str) -> frozenset[str]:
    parts = _csv(value, fallback)
    normalised = set()
    for part in parts:
        lowered = part.lower()
        normalised.add(lowered if lowered.startswith(".") else f".{lowered}")
    return frozenset(normalised)


def _bool(value: str, default: bool) -> bool:
    cleaned = _clean(value).lower()
    if not cleaned:
        return default
    if cleaned in {"1", "true", "yes", "y", "on"}:
        return True
    if cleaned in {"0", "false", "no", "n", "off"}:
        return False
    raise ConfigError(f"Expected a boolean value but got '{value}'.")


def _int(name: str, value: str, default: int, minimum: int = 1) -> int:
    cleaned = _clean(value)
    if not cleaned:
        return default
    try:
        parsed = int(cleaned)
    except ValueError as exc:
        raise ConfigError(f"{name} must be a whole number, got '{value}'.") from exc
    if parsed < minimum:
        raise ConfigError(f"{name} must be >= {minimum}, got {parsed}.")
    return parsed


def _find_repo_root(start: Path) -> Path:
    """Walk upwards looking for a git repository root, else use the grandparent."""
    for candidate in [start, *start.parents]:
        if (candidate / ".git").exists():
            return candidate
    return start.parent.parent if len(start.parents) >= 2 else start


def _resolve_output_dir(value: str, fallback: str) -> Path:
    raw = _clean(value) or fallback
    path = Path(raw)
    return path if path.is_absolute() else (TOOL_ROOT / path)


def _is_within(candidate: Path, root: Path) -> bool:
    """True when `candidate` is `root` or sits beneath it (case-insensitive on Windows)."""
    try:
        if candidate.is_relative_to(root):
            return True
    except (OSError, ValueError):
        return False
    if os.name != "nt":
        return False
    try:
        return Path(str(candidate).lower()).is_relative_to(Path(str(root).lower()))
    except (OSError, ValueError):
        return False


@dataclass(frozen=True)
class Settings:
    """Immutable snapshot of the tool's configuration."""

    api_key: str
    model: str
    selection_model: str
    ollama_host: str
    ollama_review_model: str
    ollama_request_timeout_seconds: int
    repo_root: Path
    targeted_directory: Path | None
    ignore_dirs: frozenset[str]
    include_extensions: frozenset[str]
    max_file_bytes: int
    max_files_in_context: int
    max_total_context_bytes: int
    sessions_dir: Path
    plans_dir: Path
    request_timeout_seconds: int
    max_retries: int
    max_output_tokens: int
    thinking_level: str
    log_level: str
    env_path: Path | None = None
    secret_patterns: tuple[str, ...] = field(default=SECRET_FILE_PATTERNS)

    @property
    def scope(self) -> Path:
        """The directory that will actually be reviewed."""
        return self.targeted_directory or self.repo_root

    @property
    def scope_is_targeted(self) -> bool:
        return self.targeted_directory is not None

    def with_scope(self, directory: Path) -> "Settings":
        """Return a copy scoped to `directory` (used by `--scope` and `/scope`)."""
        resolved = Path(directory).expanduser().resolve()
        if not resolved.exists():
            raise ConfigError(f"Scope directory does not exist: {resolved}")
        if not resolved.is_dir():
            raise ConfigError(f"Scope must be a directory, not a file: {resolved}")
        if not _is_within(resolved, self.repo_root):
            raise ConfigError(
                f"Scope must sit inside the repository being reviewed.\n"
                f"  Scope:     {resolved}\n"
                f"  Repo root: {self.repo_root}"
            )
        targeted = None if resolved == self.repo_root else resolved
        return Settings(**{**self.__dict__, "targeted_directory": targeted})

    def describe(self) -> dict[str, str]:
        """Human-readable, secret-free view of the configuration."""
        return {
            "Model": self.model,
            "Selection model": self.selection_model,
            "Review agent (mandatory, local)": (
                f"{self.ollama_review_model} via Ollama at {self.ollama_host}"
            ),
            "Repo root": str(self.repo_root),
            "Scope": str(self.scope),
            "Scope mode": "TARGETED_DIRECTORY" if self.scope_is_targeted else "whole repository",
            "Ignored directories": ", ".join(sorted(self.ignore_dirs)),
            "Included extensions": ", ".join(sorted(self.include_extensions)),
            "Max file bytes": str(self.max_file_bytes),
            "Max files in context": str(self.max_files_in_context),
            "Max total context bytes": str(self.max_total_context_bytes),
            "Sessions dir": str(self.sessions_dir),
            "Plans dir": str(self.plans_dir),
            "Request timeout (s)": str(self.request_timeout_seconds),
            "Max retries": str(self.max_retries),
            "Max output tokens": str(self.max_output_tokens),
            "Thinking level": self.thinking_level or "model default",
            "Log level": self.log_level,
            "API key": redact(self.api_key),
        }


def redact(secret: str) -> str:
    """Mask a secret for safe display/logging."""
    if not secret:
        return "(not set)"
    if len(secret) <= 8:
        return "*" * len(secret)
    return f"{secret[:4]}{'*' * 8}{secret[-4:]}"


def load_settings(
    env_path: Path | None = None,
    environ: dict[str, str] | None = None,
    require_api_key: bool = True,
) -> Settings:
    """Build a `Settings` instance from a `.env` file and the process environment.

    Passing `environ` bypasses `.env` loading entirely, which keeps tests hermetic.
    """
    if environ is None:
        resolved_env_path = env_path or (TOOL_ROOT / ".env")
        if resolved_env_path.exists():
            load_dotenv(dotenv_path=resolved_env_path, override=False)
        else:
            resolved_env_path = None
        source: dict[str, str] = dict(os.environ)
    else:
        resolved_env_path = env_path
        source = dict(environ)

    def get(name: str) -> str:
        return _clean(source.get(name))

    api_key = get("GEMINI_API_KEY")
    if require_api_key and not api_key:
        raise ConfigError(
            "GEMINI_API_KEY is not set.\n"
            f"Create {TOOL_ROOT / '.env'} (copy .env.example) and add your Google AI "
            "Studio key from https://aistudio.google.com/apikey"
        )

    model = get("GEMINI_MODEL") or DEFAULT_MODEL
    selection_model = get("GEMINI_SELECTION_MODEL") or model
    ollama_host = get("OLLAMA_HOST") or DEFAULT_OLLAMA_HOST
    ollama_review_model = get("OLLAMA_REVIEW_MODEL") or DEFAULT_OLLAMA_REVIEW_MODEL
    ollama_request_timeout_seconds = _int(
        "OLLAMA_REQUEST_TIMEOUT_SECONDS", get("OLLAMA_REQUEST_TIMEOUT_SECONDS"), 600
    )

    repo_root_raw = get("REPO_ROOT")
    if repo_root_raw:
        repo_root = Path(repo_root_raw).expanduser().resolve()
        if not repo_root.is_dir():
            raise ConfigError(f"REPO_ROOT is not an existing directory: {repo_root}")
    else:
        repo_root = _find_repo_root(TOOL_ROOT)

    # Accept the common misspelling so a typo never silently reviews the whole repo.
    targeted_raw = get("TARGETED_DIRECTORY") or get("TARGETTED_DIRECTORY")
    targeted: Path | None = None
    if targeted_raw:
        targeted = Path(targeted_raw).expanduser().resolve()
        if not targeted.exists():
            raise ConfigError(f"TARGETED_DIRECTORY does not exist: {targeted}")
        if not targeted.is_dir():
            raise ConfigError(
                f"TARGETED_DIRECTORY must be a directory, not a file: {targeted}"
            )
        if not _is_within(targeted, repo_root):
            raise ConfigError(
                f"TARGETED_DIRECTORY ({targeted}) must live inside REPO_ROOT ({repo_root})."
            )
        if targeted == repo_root:
            targeted = None

    max_file_bytes = _int("MAX_FILE_BYTES", get("MAX_FILE_BYTES"), 200_000, minimum=100)
    max_files = _int("MAX_FILES_IN_CONTEXT", get("MAX_FILES_IN_CONTEXT"), 40)
    max_total = _int(
        "MAX_TOTAL_CONTEXT_BYTES",
        get("MAX_TOTAL_CONTEXT_BYTES"),
        1_500_000,
        minimum=1000,
    )
    if max_total < max_file_bytes:
        raise ConfigError(
            f"MAX_TOTAL_CONTEXT_BYTES ({max_total}) must be at least "
            f"MAX_FILE_BYTES ({max_file_bytes})."
        )

    max_output_tokens = _int(
        "MAX_OUTPUT_TOKENS", get("MAX_OUTPUT_TOKENS"), 32_000, minimum=1024
    )
    thinking_level = (get("THINKING_LEVEL") or "low").strip().lower()
    if thinking_level in {"", "default", "auto"}:
        thinking_level = ""
    elif thinking_level not in THINKING_LEVELS:
        raise ConfigError(
            "THINKING_LEVEL must be one of "
            f"{', '.join(sorted(THINKING_LEVELS))} (or 'default'), got '{thinking_level}'."
        )

    log_level = (get("LOG_LEVEL") or "INFO").upper()
    if log_level not in {"DEBUG", "INFO", "WARNING", "ERROR", "CRITICAL"}:
        raise ConfigError(f"LOG_LEVEL must be a standard logging level, got '{log_level}'.")

    return Settings(
        api_key=api_key,
        model=model,
        selection_model=selection_model,
        ollama_host=ollama_host,
        ollama_review_model=ollama_review_model,
        ollama_request_timeout_seconds=ollama_request_timeout_seconds,
        repo_root=repo_root,
        targeted_directory=targeted,
        ignore_dirs=frozenset(_csv(get("IGNORE_DIRS"), DEFAULT_IGNORE_DIRS)),
        include_extensions=_extensions(
            get("INCLUDE_EXTENSIONS"), DEFAULT_INCLUDE_EXTENSIONS
        ),
        max_file_bytes=max_file_bytes,
        max_files_in_context=max_files,
        max_total_context_bytes=max_total,
        sessions_dir=_resolve_output_dir(get("SESSIONS_DIR"), "Sessions"),
        plans_dir=_resolve_output_dir(get("PLANS_DIR"), "Plans"),
        request_timeout_seconds=_int(
            "REQUEST_TIMEOUT_SECONDS", get("REQUEST_TIMEOUT_SECONDS"), 180
        ),
        max_retries=_int("MAX_RETRIES", get("MAX_RETRIES"), 3),
        max_output_tokens=max_output_tokens,
        thinking_level=thinking_level,
        log_level=log_level,
        env_path=resolved_env_path,
    )
