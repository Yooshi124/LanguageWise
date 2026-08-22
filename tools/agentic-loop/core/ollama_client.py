"""Thin wrapper around a local Ollama model, used only by the Review Agent.

The review agent is mandatory and runs on a separate, locally-hosted model
(Ollama + Gemma) so that every finding from the implementation agent (Gemini)
is checked by a genuinely independent second model before a human ever sees
it. This mirrors `GeminiClient.generate_structured` exactly - same
`ModelResponse`, same JSON-schema-enforced structured output - so
`agents/critic.py` does not need to know which backend it is talking to.

Nothing here ever calls the network except the local Ollama daemon
(`OLLAMA_HOST`, default `http://localhost:11434`). No source code leaves the
machine for the review pass.
"""

from __future__ import annotations

import time
from typing import Any, TypeVar

import requests
from pydantic import BaseModel, ValidationError

from config.settings import Settings
from core.gemini_client import ModelResponse, _strip_code_fence, inline_schema_refs

TModel = TypeVar("TModel", bound=BaseModel)


class OllamaError(RuntimeError):
    """Raised when the local Ollama review agent is unreachable or fails.

    The review agent cannot be disabled, so this is always allowed to abort
    the round rather than being swallowed into a silent fallback.
    """


class OllamaClient:
    """Calls a local Ollama model and returns validated structured results."""

    def __init__(self, settings: Settings, session: "requests.Session | None" = None) -> None:
        self._settings = settings
        self._session = session or requests.Session()

    def generate_structured(
        self,
        *,
        prompt: str,
        schema: type[TModel],
        system_instruction: str | None = None,
        model: str | None = None,
    ) -> tuple[TModel, ModelResponse]:
        """Call Ollama and validate its JSON reply against `schema`."""
        model_name = model or self._settings.ollama_review_model

        messages: list[dict[str, str]] = []
        if system_instruction:
            messages.append({"role": "system", "content": system_instruction})
        messages.append({"role": "user", "content": prompt})

        body: dict[str, Any] = {
            "model": model_name,
            "messages": messages,
            "stream": False,
            "format": inline_schema_refs(schema.model_json_schema()),
        }

        started = time.monotonic()
        payload = self._post(body, model_name)
        duration = time.monotonic() - started
        response = self._to_response(payload, model_name, duration)

        try:
            return schema.model_validate_json(_strip_code_fence(response.text)), response
        except (ValidationError, ValueError) as exc:
            raise OllamaError(
                f"The local review agent ({model_name}) returned JSON that does not "
                f"match {schema.__name__}: {exc}\nRaw response: {response.text[:800]}"
            ) from exc

    def _post(self, body: dict[str, Any], model_name: str) -> dict[str, Any]:
        url = f"{self._settings.ollama_host.rstrip('/')}/api/chat"
        try:
            resp = self._session.post(
                url, json=body, timeout=self._settings.ollama_request_timeout_seconds
            )
        except requests.exceptions.ConnectionError as exc:
            raise OllamaError(
                f"Could not reach the local review agent at {self._settings.ollama_host}. "
                "The review agent is mandatory and cannot be skipped: install Ollama "
                "(https://ollama.com), make sure it is running (`ollama serve`), and pull "
                f"the review model with `ollama pull {model_name}`."
            ) from exc
        except requests.exceptions.Timeout as exc:
            raise OllamaError(
                f"The local review agent at {self._settings.ollama_host} did not respond "
                f"within {self._settings.ollama_request_timeout_seconds}s. The model may "
                "still be loading into memory - try again, or raise "
                "OLLAMA_REQUEST_TIMEOUT_SECONDS in .env."
            ) from exc
        except requests.exceptions.RequestException as exc:
            raise OllamaError(f"Local review agent request failed: {exc}") from exc

        if resp.status_code == 404:
            raise OllamaError(
                f"Ollama model '{model_name}' was not found. Run: ollama pull {model_name}"
            )
        if not resp.ok:
            raise OllamaError(
                f"Local review agent request failed with HTTP {resp.status_code}: "
                f"{resp.text[:500]}"
            )

        try:
            return resp.json()
        except ValueError as exc:
            raise OllamaError(
                f"Ollama returned a non-JSON response: {resp.text[:500]}"
            ) from exc

    def _to_response(
        self, payload: dict[str, Any], model_name: str, duration: float
    ) -> ModelResponse:
        message = payload.get("message") or {}
        text = str(message.get("content") or "")
        if not text.strip():
            raise OllamaError(
                f"The local review agent ({model_name}) returned an empty response. "
                f"Check that the model is pulled: ollama pull {model_name}"
            )

        input_tokens = int(payload.get("prompt_eval_count", 0) or 0)
        output_tokens = int(payload.get("eval_count", 0) or 0)
        done_reason = payload.get("done_reason")
        return ModelResponse(
            text=text,
            model=model_name,
            input_tokens=input_tokens,
            output_tokens=output_tokens,
            total_tokens=input_tokens + output_tokens,
            attempts=1,
            duration_seconds=duration,
            truncated=bool(done_reason) and done_reason != "stop",
            raw=payload,
        )
