"""Thin wrapper around the OpenRouter chat-completions API.

Modelled on the mini-games service's ``OpenRouterVocabularyClient``: bearer
auth, ``POST {base}/chat/completions``, rate-limit retries that honour the
``Retry-After`` header (capped at 8s), and a repair round that asks the model
to re-emit invalid JSON instead of failing the whole round.

Everything the loop needs from the model goes through here: retries with
backoff, timeouts, token accounting, and guaranteed redaction of the API key
in any error surfaced to the console.
"""

from __future__ import annotations

import copy
import json
import logging
import random
import time
from dataclasses import dataclass, field
from typing import Any, TypeVar

import requests
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

# Mirrors MaximumRateLimitRetries in the mini-games OpenRouterVocabularyClient.
MAX_RATE_LIMIT_RETRIES = 3

# Attribution headers OpenRouter uses for its public rankings; harmless locally.
APP_REFERER = "https://github.com/LanguageWise"
APP_TITLE = "LanguageWise Agentic Loop"


class OpenRouterError(RuntimeError):
    """Raised when an OpenRouter call fails or returns something unusable."""


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
    portable for backends that do not dereference local pointers (Ollama's
    `format` parameter, among others).

    `propertyOrdering` is added to every object because models otherwise emit
    JSON keys in whatever order they like. When a long free-text field comes
    first the model treats it as a scratchpad, rambles until the output budget
    is gone, and the arrays that actually matter come back empty.
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


def _is_retryable_status(status_code: int) -> bool:
    return status_code == 429 or status_code >= 500


def _is_retryable(error: Exception) -> bool:
    message = str(error).lower()
    return any(marker in message for marker in RETRYABLE_MARKERS)


class OpenRouterClient:
    """Calls OpenRouter and returns validated results."""

    def __init__(self, settings: Settings, session: "requests.Session | None" = None) -> None:
        self._settings = settings
        self._session = session or requests.Session()

    def _sanitise(self, message: str) -> str:
        key = self._settings.api_key
        return message.replace(key, redact(key)) if key else message

    def _chat_url(self) -> str:
        return f"{self._settings.openrouter_base_url.rstrip('/')}/chat/completions"

    def _headers(self) -> dict[str, str]:
        return {
            "Authorization": f"Bearer {self._settings.api_key}",
            "Content-Type": "application/json",
            "HTTP-Referer": APP_REFERER,
            "X-Title": APP_TITLE,
        }

    def _request_body(
        self, messages: list[dict[str, str]], model: str, *, max_tokens: int
    ) -> dict[str, Any]:
        """Budget and reasoning level for every call.

        Without an explicit budget a reasoning model can spend its whole output
        allowance thinking and return a truncated reply. `reasoning.effort` is
        OpenRouter's unified knob; providers that do not support it ignore it.
        """
        body: dict[str, Any] = {
            "model": model,
            "messages": messages,
            "stream": False,
        }
        if max_tokens > 0:
            body["max_tokens"] = max_tokens
        if self._settings.thinking_level:
            body["reasoning"] = {"effort": self._settings.thinking_level}
        return body

    def _post(self, body: dict[str, Any]) -> tuple[dict[str, Any], int]:
        """POST with rate-limit and transient-failure retries.

        Rate limits (HTTP 429) honour the `Retry-After` header, capped at 8s,
        mirroring the mini-games OpenRouterVocabularyClient. Other transient
        failures use exponential backoff capped at 8s plus jitter.
        """
        attempts = 0
        rate_limit_retries = 0
        last_error: Exception | None = None

        while True:
            attempts += 1
            try:
                resp = self._session.post(
                    self._chat_url(),
                    headers=self._headers(),
                    json=body,
                    timeout=self._settings.request_timeout_seconds,
                )
            except requests.exceptions.RequestException as exc:
                last_error = exc
                if attempts >= self._settings.max_retries or not _is_retryable(exc):
                    break
                delay = min(2 ** (attempts - 1), 8) + random.uniform(0, 0.5)
                logger.warning(
                    "OpenRouter call failed (attempt %s/%s): %s - retrying in %.1fs",
                    attempts,
                    self._settings.max_retries,
                    self._sanitise(str(exc)),
                    delay,
                )
                time.sleep(delay)
                continue

            if resp.status_code == 429 and rate_limit_retries < MAX_RATE_LIMIT_RETRIES:
                rate_limit_retries += 1
                retry_after = resp.headers.get("Retry-After")
                try:
                    delay = float(retry_after) if retry_after else 2 ** rate_limit_retries
                except ValueError:
                    delay = 2 ** rate_limit_retries
                delay = min(delay, 8.0)
                logger.warning(
                    "OpenRouter rate limited (429), retry %s/%s in %.1fs",
                    rate_limit_retries,
                    MAX_RATE_LIMIT_RETRIES,
                    delay,
                )
                time.sleep(delay)
                continue

            if not resp.ok:
                detail = self._sanitise(resp.text[:500])
                error = OpenRouterError(
                    f"OpenRouter request failed with HTTP {resp.status_code}: {detail}"
                )
                if _is_retryable_status(resp.status_code) and attempts < self._settings.max_retries:
                    last_error = error
                    delay = min(2 ** (attempts - 1), 8) + random.uniform(0, 0.5)
                    logger.warning(
                        "OpenRouter returned HTTP %s (attempt %s/%s) - retrying in %.1fs",
                        resp.status_code,
                        attempts,
                        self._settings.max_retries,
                        delay,
                    )
                    time.sleep(delay)
                    continue
                raise error

            try:
                return resp.json(), attempts + rate_limit_retries
            except ValueError as exc:
                raise OpenRouterError(
                    f"OpenRouter returned a non-JSON response: {resp.text[:500]}"
                ) from exc

        raise OpenRouterError(
            f"OpenRouter request failed: {self._sanitise(str(last_error))}"
        ) from last_error

    def _completion(
        self, messages: list[dict[str, str]], model: str, *, max_tokens: int
    ) -> tuple[ModelResponse, dict[str, Any]]:
        body = self._request_body(messages, model, max_tokens=max_tokens)
        started = time.monotonic()
        payload, attempts = self._post(body)
        duration = time.monotonic() - started
        return self._to_response(payload, model, attempts, duration), payload

    def generate_text(
        self,
        *,
        prompt: str,
        system_instruction: str | None = None,
        model: str | None = None,
    ) -> ModelResponse:
        messages: list[dict[str, str]] = []
        if system_instruction:
            messages.append({"role": "system", "content": system_instruction})
        messages.append({"role": "user", "content": prompt})

        model_name = model or self._settings.model
        response, _ = self._completion(
            messages, model_name, max_tokens=self._settings.max_output_tokens
        )
        return response

    def generate_structured(
        self,
        *,
        prompt: str,
        schema: type[TModel],
        system_instruction: str | None = None,
        model: str | None = None,
    ) -> tuple[TModel, ModelResponse]:
        """Call OpenRouter and validate its JSON reply against `schema`.

        When the first reply is not valid JSON for the schema, the model gets
        one repair round (the same pattern as the mini-games vocabulary
        provider) before the call is declared failed.
        """
        schema_text = json.dumps(inline_schema_refs(schema.model_json_schema()), indent=2)
        system_text = (
            f"{system_instruction}\n\n" if system_instruction else ""
        ) + f"JSON schema your reply must match exactly (use these key names):\n{schema_text}"
        messages: list[dict[str, str]] = [
            {"role": "system", "content": system_text},
            {"role": "user", "content": prompt},
        ]

        model_name = model or self._settings.model
        response, _ = self._completion(
            messages, model_name, max_tokens=self._settings.max_output_tokens
        )

        try:
            return schema.model_validate_json(_strip_code_fence(response.text)), response
        except (ValidationError, ValueError) as exc:
            first_error = str(exc)  # fall through to the repair round

        messages.append({"role": "assistant", "content": response.text})
        messages.append(
            {
                "role": "user",
                "content": (
                    "That reply failed schema validation with this error:\n"
                    f"{self._sanitise(first_error)}\n"
                    "Reply again with only a corrected JSON object matching the "
                    "requested schema, no other text. Every required field named "
                    "in the error above must be present in every array item."
                ),
            }
        )
        repair, _ = self._completion(
            messages, model_name, max_tokens=self._settings.max_output_tokens
        )
        repair.attempts += response.attempts - 1

        try:
            return schema.model_validate_json(_strip_code_fence(repair.text)), repair
        except (ValidationError, ValueError) as exc:
            if repair.truncated:
                raise OpenRouterError(
                    "The model ran out of output budget and returned a partial reply "
                    f"(model={repair.model}, {repair.output_tokens} output tokens). "
                    "Raise MAX_OUTPUT_TOKENS, lower THINKING_LEVEL, or narrow the "
                    "review scope."
                ) from exc
            raise OpenRouterError(
                f"The model returned JSON that does not match {schema.__name__}: "
                f"{self._sanitise(str(exc))}\nRaw response: "
                f"{self._sanitise(repair.text[:800])}"
            ) from exc

    def _to_response(
        self, payload: dict[str, Any], model: str, attempts: int, duration: float
    ) -> ModelResponse:
        choices = payload.get("choices") or []
        message = choices[0].get("message") if choices else None
        text = (message or {}).get("content") or ""
        if isinstance(text, list):  # some providers return content parts
            text = "".join(
                part.get("text", "") for part in text if isinstance(part, dict)
            )
        if not text.strip():
            usage = payload.get("usage") or {}
            reasoning_tokens = int(
                (usage.get("completion_tokens_details") or {}).get("reasoning_tokens") or 0
            )
            if reasoning_tokens or (message or {}).get("reasoning"):
                raise OpenRouterError(
                    f"OpenRouter returned no content (model={model}) - the model spent "
                    f"its entire output budget on hidden reasoning ({reasoning_tokens} "
                    "reasoning tokens) and had none left for the reply. Set "
                    "THINKING_LEVEL blank in .env to stop requesting reasoning, or "
                    "raise MAX_OUTPUT_TOKENS if reasoning is wanted."
                )
            raise OpenRouterError(
                f"OpenRouter returned an empty response (model={model}). "
                "Check the model name in your .env file."
            )


        usage = payload.get("usage") or {}
        input_tokens = int(usage.get("prompt_tokens") or 0)
        output_tokens = int(usage.get("completion_tokens") or 0)
        finish_reason = str((choices[0] if choices else {}).get("finish_reason") or "")
        return ModelResponse(
            text=text,
            model=model,
            input_tokens=input_tokens,
            output_tokens=output_tokens,
            # Some responses report the parts but not the sum.
            total_tokens=int(usage.get("total_tokens") or 0)
            or (input_tokens + output_tokens),
            attempts=attempts,
            duration_seconds=duration,
            truncated=finish_reason.lower() in {"length", "max_tokens"},
            raw=payload,
        )


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
