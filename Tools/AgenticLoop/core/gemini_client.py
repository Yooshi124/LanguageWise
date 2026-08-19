"""Thin wrapper around the Google Gen AI SDK (`google-genai`).

Everything the loop needs from Gemini goes through here: structured JSON output
validated by pydantic, retries with backoff, timeouts, token accounting, and
guaranteed redaction of the API key in any error surfaced to the console.
"""

from __future__ import annotations

import copy
import json
import logging
import random
import time
from dataclasses import dataclass, field
from typing import Any, TypeVar

from pydantic import BaseModel, ValidationError

from config.settings import Settings, redact

logger = logging.getLogger(__name__)

TModel = TypeVar("TModel", bound=BaseModel)

RETRYABLE_MARKERS = (
    "429",
    "500",
    "502",
    "503",
    "504",
    "resource_exhausted",
    "unavailable",
    "deadline",
    "timeout",
    "timed out",
    "connection",
    "internal error",
)


class GeminiError(RuntimeError):
    """Raised when a Gemini call fails or returns something unusable."""


@dataclass
class ModelResponse:
    """The parts of an interaction the loop actually records."""

    text: str
    model: str
    input_tokens: int = 0
    output_tokens: int = 0
    total_tokens: int = 0
    attempts: int = 1
    duration_seconds: float = 0.0
    truncated: bool = False
    raw: Any = field(default=None, repr=False)

    def usage_line(self) -> str:
        return (
            f"model={self.model} | tokens in/out/total="
            f"{self.input_tokens}/{self.output_tokens}/{self.total_tokens} | "
            f"attempts={self.attempts} | {self.duration_seconds:.1f}s"
            + (" | TRUNCATED" if self.truncated else "")
        )


def inline_schema_refs(schema: dict[str, Any]) -> dict[str, Any]:
    """Resolve `$ref`/`$defs` into a self-contained schema and pin field order.

    Pydantic emits `$defs` for nested models and enums; inlining keeps the schema
    portable across API versions that do not dereference local pointers.

    `propertyOrdering` is added to every object because Gemini emits JSON keys in
    whatever order it likes otherwise. When a long free-text field comes first the
    model treats it as a scratchpad, rambles until the output budget is gone, and
    the arrays that actually matter come back empty.
    """
    definitions = schema.get("$defs", {})

    def resolve(node: Any, seen: frozenset[str]) -> Any:
        if isinstance(node, list):
            return [resolve(item, seen) for item in node]
        if not isinstance(node, dict):
            return node

        if "$ref" in node:
            ref = node["$ref"]
            name = ref.rsplit("/", 1)[-1]
            if name in seen or name not in definitions:
                # Cyclic or unknown reference: degrade to a permissive object.
                return {"type": "object"}
            merged = resolve(copy.deepcopy(definitions[name]), seen | {name})
            extras = {k: v for k, v in node.items() if k != "$ref"}
            if isinstance(merged, dict):
                merged.update(extras)
            return merged

        return {
            key: resolve(value, seen)
            for key, value in node.items()
            if key != "$defs"
        }

    resolved = resolve({k: v for k, v in schema.items() if k != "$defs"}, frozenset())
    if not isinstance(resolved, dict):
        return schema
    return _add_property_ordering(resolved)


def _add_property_ordering(node: Any) -> Any:
    if isinstance(node, list):
        for item in node:
            _add_property_ordering(item)
    elif isinstance(node, dict):
        properties = node.get("properties")
        if isinstance(properties, dict) and properties:
            node["propertyOrdering"] = list(properties)
        for value in node.values():
            _add_property_ordering(value)
    return node


def _is_retryable(error: Exception) -> bool:
    message = str(error).lower()
    return any(marker in message for marker in RETRYABLE_MARKERS)


class GeminiClient:
    """Calls Gemini and returns validated results."""

    def __init__(self, settings: Settings, client: Any | None = None) -> None:
        self._settings = settings
        self._client = client

    @property
    def client(self) -> Any:
        if self._client is None:
            try:
                from google import genai
            except ImportError as exc:  # pragma: no cover - dependency guard
                raise GeminiError(
                    "The 'google-genai' package is not installed. "
                    "Run: pip install -r requirements.txt"
                ) from exc
            self._client = genai.Client(api_key=self._settings.api_key)
        return self._client

    def _sanitise(self, message: str) -> str:
        key = self._settings.api_key
        return message.replace(key, redact(key)) if key else message

    def _generation_config(self) -> dict[str, Any]:
        """Budget and thinking level for every call.

        Without an explicit budget a reasoning model can spend its whole output
        allowance thinking and return a truncated reply.
        """
        config: dict[str, Any] = {}
        if self._settings.max_output_tokens > 0:
            config["max_output_tokens"] = self._settings.max_output_tokens
        if self._settings.thinking_level:
            config["thinking_level"] = self._settings.thinking_level
        return config

    def _create(self, body: dict[str, Any]) -> Any:
        last_error: Exception | None = None
        for attempt in range(1, self._settings.max_retries + 1):
            try:
                return self.client.interactions.create(
                    timeout=self._settings.request_timeout_seconds, **body
                ), attempt
            except Exception as exc:  # SDK raises a variety of transport errors
                last_error = exc
                if attempt >= self._settings.max_retries or not _is_retryable(exc):
                    break
                delay = min(2 ** (attempt - 1), 8) + random.uniform(0, 0.5)
                logger.warning(
                    "Gemini call failed (attempt %s/%s): %s - retrying in %.1fs",
                    attempt,
                    self._settings.max_retries,
                    self._sanitise(str(exc)),
                    delay,
                )
                time.sleep(delay)

        raise GeminiError(
            f"Gemini request failed: {self._sanitise(str(last_error))}"
        ) from last_error

    def generate_text(
        self,
        *,
        prompt: str,
        system_instruction: str | None = None,
        model: str | None = None,
    ) -> ModelResponse:
        body: dict[str, Any] = {
            "model": model or self._settings.model,
            "input": prompt,
        }
        generation_config = self._generation_config()
        if generation_config:
            body["generation_config"] = generation_config
        if system_instruction:
            body["system_instruction"] = system_instruction

        started = time.monotonic()
        interaction, attempts = self._create(body)
        return self._to_response(
            interaction, body["model"], attempts, time.monotonic() - started
        )

    def generate_structured(
        self,
        *,
        prompt: str,
        schema: type[TModel],
        system_instruction: str | None = None,
        model: str | None = None,
    ) -> tuple[TModel, ModelResponse]:
        """Call Gemini and validate its JSON reply against `schema`."""
        body: dict[str, Any] = {
            "model": model or self._settings.model,
            "input": prompt,
            "response_format": {
                "type": "text",
                "mime_type": "application/json",
                "schema": inline_schema_refs(schema.model_json_schema()),
            },
        }
        generation_config = self._generation_config()
        if generation_config:
            body["generation_config"] = generation_config
        if system_instruction:
            body["system_instruction"] = system_instruction

        started = time.monotonic()
        interaction, attempts = self._create(body)
        response = self._to_response(
            interaction, body["model"], attempts, time.monotonic() - started
        )

        try:
            return schema.model_validate_json(_strip_code_fence(response.text)), response
        except (ValidationError, ValueError) as exc:
            if response.truncated:
                raise GeminiError(
                    "Gemini ran out of output budget and returned a partial reply "
                    f"(model={response.model}, {response.output_tokens} output tokens). "
                    "Raise MAX_OUTPUT_TOKENS, lower THINKING_LEVEL, or narrow the "
                    "review scope."
                ) from exc
            raise GeminiError(
                f"Gemini returned JSON that does not match {schema.__name__}: "
                f"{self._sanitise(str(exc))}\nRaw response: "
                f"{self._sanitise(response.text[:800])}"
            ) from exc

    def _to_response(
        self, interaction: Any, model: str, attempts: int, duration: float
    ) -> ModelResponse:
        text = getattr(interaction, "output_text", None) or ""
        if not text.strip():
            text = _text_from_steps(interaction)
        if not text.strip():
            raise GeminiError(
                f"Gemini returned an empty response (model={model}). "
                "Check the model name in your .env file."
            )

        usage = getattr(interaction, "usage", None)
        input_tokens = int(getattr(usage, "total_input_tokens", 0) or 0)
        output_tokens = int(getattr(usage, "total_output_tokens", 0) or 0)
        status = str(getattr(interaction, "status", "") or "").lower()
        return ModelResponse(
            text=text,
            model=model,
            input_tokens=input_tokens,
            output_tokens=output_tokens,
            # Some responses report the parts but not the sum.
            total_tokens=int(getattr(usage, "total_tokens", 0) or 0)
            or (input_tokens + output_tokens),
            attempts=attempts,
            duration_seconds=duration,
            truncated=status in {"incomplete", "max_tokens", "length"},
            raw=interaction,
        )


def _text_from_steps(interaction: Any) -> str:
    """Fallback extraction for responses without a populated `output_text`."""
    chunks: list[str] = []
    for step in getattr(interaction, "steps", None) or []:
        if getattr(step, "type", None) != "model_output":
            continue
        for item in getattr(step, "content", None) or []:
            value = getattr(item, "text", None)
            if value:
                chunks.append(value)
    return "\n".join(chunks)


def _strip_code_fence(text: str) -> str:
    """Tolerate a model that wraps JSON in a markdown fence."""
    stripped = text.strip()
    if not stripped.startswith("```"):
        return stripped
    body = stripped.split("\n", 1)[1] if "\n" in stripped else ""
    if body.rstrip().endswith("```"):
        body = body.rstrip()[: -len("```")]
    return body.strip()


def dump_json(value: Any) -> str:
    """Compact, deterministic JSON used when embedding data inside prompts."""
    if isinstance(value, BaseModel):
        value = value.model_dump(mode="json")
    return json.dumps(value, indent=2, ensure_ascii=False, default=str)
