"""Loads and renders the externalised prompt templates.

All prompt text lives in `prompts/<stage>/<name>.md`. Changing how the agent
reviews code is therefore a markdown edit, never a code change.

Templates use `{{PLACEHOLDER}}` substitution. Rendering is strict: a missing
template file or an unresolved placeholder raises rather than silently sending
a broken prompt to the model.
"""

from __future__ import annotations

import re
from pathlib import Path

PROMPTS_ROOT = Path(__file__).resolve().parent.parent / "prompts"

PLACEHOLDER_PATTERN = re.compile(r"\{\{\s*([A-Z0-9_]+)\s*\}\}")


class PromptError(RuntimeError):
    """Raised when a prompt template is missing or cannot be rendered."""


class PromptRegistry:
    def __init__(self, root: Path | None = None) -> None:
        self.root = Path(root) if root else PROMPTS_ROOT
        if not self.root.is_dir():
            raise PromptError(f"Prompts directory not found: {self.root}")
        self._cache: dict[tuple[str, str], str] = {}

    def path_for(self, stage: str, name: str, /) -> Path:
        return self.root / stage / f"{name}.md"

    def load(self, stage: str, name: str, /) -> str:
        key = (stage, name)
        if key not in self._cache:
            path = self.path_for(stage, name)
            if not path.is_file():
                raise PromptError(
                    f"Missing prompt template '{stage}/{name}.md' (expected at {path})."
                )
            self._cache[key] = path.read_text(encoding="utf-8")
        return self._cache[key]

    def render(self, stage: str, name: str, /, **values: object) -> str:
        """Render a template, requiring every placeholder to be supplied."""
        template = self.load(stage, name)
        provided = {key.upper(): "" if value is None else str(value) for key, value in values.items()}

        required = set(PLACEHOLDER_PATTERN.findall(template))
        missing = sorted(required - provided.keys())
        if missing:
            raise PromptError(
                f"Prompt '{stage}/{name}.md' expects values for: {', '.join(missing)}."
            )

        def substitute(match: re.Match[str]) -> str:
            return provided[match.group(1)]

        rendered = PLACEHOLDER_PATTERN.sub(substitute, template)
        if not rendered.strip():
            raise PromptError(f"Prompt '{stage}/{name}.md' rendered to an empty string.")
        return rendered

    def available(self) -> dict[str, list[str]]:
        """Map of stage -> template names, used by the `/config` command."""
        listing: dict[str, list[str]] = {}
        for stage_dir in sorted(p for p in self.root.iterdir() if p.is_dir()):
            names = sorted(p.stem for p in stage_dir.glob("*.md"))
            if names:
                listing[stage_dir.name] = names
        return listing

    def validate(self) -> None:
        """Fail fast at startup if any prompt required by the loop is absent."""
        required = (
            ("selection", "system"),
            ("selection", "task"),
            ("analysis", "system"),
            ("analysis", "task"),
            ("analysis", "context"),
            ("critique", "system"),
            ("critique", "task"),
            ("planning", "system"),
            ("planning", "task"),
            ("decision", "system"),
            ("decision", "task"),
        )
        missing = [f"{stage}/{name}.md" for stage, name in required if not self.path_for(stage, name).is_file()]
        if missing:
            raise PromptError(
                "The following prompt templates are missing from "
                f"{self.root}: {', '.join(missing)}"
            )
