"""Thin client for the shared local MCP server (mcp-server/).

The MCP server runs locally and non-containerised, speaking JSON-RPC 2.0 over
streamable HTTP at `{MCP_BASE_URL}/mcp`. Every request carries the shared API
key and a tool scope; an optional user JWT is forwarded so per-user tools can
run on behalf of the signed-in user.

This mirrors what `scripts/Invoke-McpSmokeTest.ps1` does by hand: `initialize`,
then `tools/list` and `tools/call`. Responses arrive either as a plain JSON
body or as server-sent events (`data:` lines); both shapes are parsed.
"""

from __future__ import annotations

import itertools
import json
from dataclasses import dataclass, field
from typing import Any

import requests

from config.settings import Settings

PROTOCOL_VERSION = "2025-06-18"
CLIENT_NAME = "agentic-loop"
CLIENT_VERSION = "1.0"

MAX_RESULT_EXCERPT_CHARS = 2_000


class McpError(RuntimeError):
    """Raised when the local MCP server is unreachable or returns an error."""


@dataclass(frozen=True)
class McpTool:
    """One tool as returned by `tools/list`."""

    name: str
    title: str
    description: str
    input_schema: dict[str, Any] = field(default_factory=dict)

    @property
    def required_arguments(self) -> list[str]:
        required = self.input_schema.get("required")
        return [str(item) for item in required] if isinstance(required, list) else []


@dataclass(frozen=True)
class McpToolCallResult:
    """The outcome of one `tools/call`."""

    name: str
    arguments: dict[str, Any]
    is_error: bool
    text: str
    structured: Any = None


@dataclass(frozen=True)
class McpServerInfo:
    name: str
    version: str


def _parse_response_body(resp: requests.Response) -> dict[str, Any]:
    """Parse a JSON-RPC reply that may be plain JSON or an SSE stream."""
    content = resp.text.strip()
    if content.startswith("{"):
        try:
            return json.loads(content)
        except ValueError as exc:
            raise McpError(
                f"The MCP server returned malformed JSON: {content[:300]}"
            ) from exc
    # SSE framing: take the last data: line, like the smoke test does.
    data_lines = [
        line[len("data:"):].strip()
        for line in content.splitlines()
        if line.lstrip().startswith("data:")
    ]
    if not data_lines:
        raise McpError(
            f"The MCP server returned neither JSON nor SSE data: {content[:300]}"
        )
    try:
        return json.loads(data_lines[-1])
    except ValueError as exc:
        raise McpError(
            f"The MCP server's SSE payload was not valid JSON: {data_lines[-1][:300]}"
        ) from exc


class McpClient:
    """Calls the MCP server over streamable HTTP."""

    def __init__(self, settings: Settings, session: "requests.Session | None" = None) -> None:
        self._settings = settings
        self._session = session or requests.Session()
        self._ids = itertools.count(1)
        self._session_id: str | None = None
        self._initialised_scopes: set[str] = set()

    @property
    def endpoint(self) -> str:
        return f"{self._settings.mcp_base_url.rstrip('/')}/mcp"

    @property
    def default_scope(self) -> str:
        return self._settings.mcp_tool_scope

    # -- public RPC ------------------------------------------------------------

    def initialize(self, scope: str | None = None) -> McpServerInfo:
        """Handshake with the server; required once per scope before other calls."""
        scope = self._resolve_scope(scope)
        payload = self._rpc(
            "initialize",
            {
                "protocolVersion": PROTOCOL_VERSION,
                "capabilities": {},
                "clientInfo": {"name": CLIENT_NAME, "version": CLIENT_VERSION},
            },
            scope,
        )
        self._initialised_scopes.add(scope)
        info = payload.get("serverInfo") or {}
        return McpServerInfo(name=str(info.get("name", "")), version=str(info.get("version", "")))

    def list_tools(self, scope: str | None = None) -> list[McpTool]:
        scope = self._ensure_initialised(scope)
        payload = self._rpc("tools/list", {}, scope)
        tools = payload.get("tools") or []
        return [
            McpTool(
                name=str(tool.get("name", "")),
                title=str(tool.get("title") or tool.get("name", "")),
                description=str(tool.get("description") or ""),
                input_schema=tool.get("inputSchema") or {},
            )
            for tool in tools
        ]

    def call_tool(
        self,
        name: str,
        arguments: dict[str, Any] | None = None,
        scope: str | None = None,
    ) -> McpToolCallResult:
        scope = self._ensure_initialised(scope)
        payload = self._rpc(
            "tools/call",
            {"name": name, "arguments": arguments or {}},
            scope,
        )
        is_error = bool(payload.get("isError"))
        content = payload.get("content") or []
        text = "\n".join(
            str(block.get("text", ""))
            for block in content
            if isinstance(block, dict) and block.get("type") == "text"
        )
        structured = payload.get("structuredContent")
        return McpToolCallResult(
            name=name,
            arguments=arguments or {},
            is_error=is_error,
            text=text,
            structured=structured,
        )

    # -- internals ---------------------------------------------------------------

    def _resolve_scope(self, scope: str | None) -> str:
        resolved = scope or self._settings.mcp_tool_scope
        if not resolved:
            raise McpError("No MCP tool scope configured; set MCP_TOOL_SCOPE in .env.")
        return resolved

    def _ensure_initialised(self, scope: str | None) -> str:
        resolved = self._resolve_scope(scope)
        if resolved not in self._initialised_scopes:
            self.initialize(resolved)
        return resolved

    def _headers(self, scope: str) -> dict[str, str]:
        if not self._settings.mcp_api_key:
            raise McpError(
                "No MCP API key is configured. Set MCP_API_KEY in .env, or generate "
                "the shared key file with `pwsh mcp-server/scripts/New-McpApiKey.ps1` "
                "(the loop reads mcp-server/.mcp-api-key by default)."
            )
        headers = {
            "Accept": "application/json, text/event-stream",
            "Content-Type": "application/json",
            "X-LanguageWise-Mcp-Key": self._settings.mcp_api_key,
            "X-LanguageWise-Tool-Scope": scope,
        }
        if self._settings.mcp_user_token:
            headers["X-LanguageWise-User-Token"] = self._settings.mcp_user_token
        if self._session_id:
            headers["Mcp-Session-Id"] = self._session_id
        return headers

    def _rpc(self, method: str, params: dict[str, Any], scope: str) -> dict[str, Any]:
        body = {"jsonrpc": "2.0", "id": next(self._ids), "method": method, "params": params}
        try:
            resp = self._session.post(
                self.endpoint,
                headers=self._headers(scope),
                json=body,
                timeout=self._settings.mcp_request_timeout_seconds,
            )
        except requests.exceptions.ConnectionError as exc:
            raise McpError(
                f"Could not reach the MCP server at {self.endpoint}. It runs locally "
                "and is not containerised - start it with `dotnet run` from "
                "mcp-server/src/LanguageWise.McpServer."
            ) from exc
        except requests.exceptions.Timeout as exc:
            raise McpError(
                f"The MCP server at {self.endpoint} did not respond within "
                f"{self._settings.mcp_request_timeout_seconds}s."
            ) from exc
        except requests.exceptions.RequestException as exc:
            raise McpError(f"MCP server request failed: {exc}") from exc

        session_id = resp.headers.get("mcp-session-id")
        if session_id:
            self._session_id = session_id

        if resp.status_code == 401:
            raise McpError(
                "The MCP server rejected the shared API key (HTTP 401). Check "
                "MCP_API_KEY against mcp-server/.mcp-api-key."
            )
        if not resp.ok:
            raise McpError(
                f"MCP server request failed with HTTP {resp.status_code}: "
                f"{resp.text[:300]}"
            )

        payload = _parse_response_body(resp)
        if "error" in payload and payload["error"] is not None:
            error = payload["error"]
            message = error.get("message", error) if isinstance(error, dict) else error
            raise McpError(f"MCP '{method}' was rejected: {message}")
        result = payload.get("result")
        return result if isinstance(result, dict) else {}
