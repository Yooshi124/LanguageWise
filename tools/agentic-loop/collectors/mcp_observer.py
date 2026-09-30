"""MCP collector: exercise the shared local MCP server and capture the evidence.

Used by the loop's MCP validation mode (`python main.py --mcp-validate` or the
`/mcp-validate` REPL command). It performs the same handshake the feature
backends perform - `initialize`, `tools/list`, and one real `tools/call` probe -
and formats the outcome as evidence the analyst can compare against the
mcp-server source code in scope.
"""

from __future__ import annotations

import json
from dataclasses import dataclass, field
from typing import Any

from core.mcp_client import (
    MAX_RESULT_EXCERPT_CHARS,
    McpClient,
    McpError,
    McpServerInfo,
    McpTool,
    McpToolCallResult,
)


@dataclass
class McpReport:
    """A live MCP interaction distilled to the evidence the loop needs."""

    endpoint: str
    scope: str
    server_info: McpServerInfo | None = None
    tools: list[McpTool] = field(default_factory=list)
    probe: McpToolCallResult | None = None
    probe_skipped_reason: str | None = None

    def summary_line(self) -> str:
        server = (
            f"{self.server_info.name} {self.server_info.version}".strip()
            if self.server_info
            else "unknown server"
        )
        probe = (
            f"probe {self.probe.name} -> {'isError' if self.probe.is_error else 'ok'}"
            if self.probe
            else f"no probe call ({self.probe_skipped_reason})"
        )
        return (
            f"{server} at {self.endpoint} | scope={self.scope} | "
            f"{len(self.tools)} tool(s) | {probe}"
        )

    def as_prompt_text(self) -> str:
        lines = [
            "MCP VALIDATION REPORT (live interaction with the local MCP server)",
            f"Endpoint: {self.endpoint}",
            f"Tool scope: {self.scope}",
        ]
        if self.server_info:
            lines.append(
                f"Server: {self.server_info.name} (version {self.server_info.version})"
            )
        lines.append("")

        lines.append(f"tools/list returned {len(self.tools)} tool(s) for this scope:")
        for tool in self.tools:
            required = ", ".join(tool.required_arguments) or "none"
            lines.append(f"  - {tool.name} (required args: {required})")
            if tool.description:
                lines.append(f"    {tool.description}")
        lines.append("")

        if self.probe is not None:
            lines.extend(
                [
                    "tools/call probe:",
                    f"  name: {self.probe.name}",
                    f"  arguments: {json.dumps(self.probe.arguments, default=str)}",
                    f"  isError: {self.probe.is_error}",
                    "  result excerpt:",
                    _indent(_probe_excerpt(self.probe)),
                    "",
                ]
            )
        else:
            lines.extend(
                [f"tools/call probe: skipped ({self.probe_skipped_reason})", ""]
            )
        return "\n".join(lines)


def _indent(text: str, prefix: str = "    ") -> str:
    return "\n".join(prefix + line for line in text.splitlines())


def _probe_excerpt(probe: McpToolCallResult) -> str:
    if probe.structured is not None:
        raw = json.dumps(probe.structured, indent=2, default=str)
    else:
        raw = probe.text or "(empty result)"
    if len(raw) > MAX_RESULT_EXCERPT_CHARS:
        raw = raw[:MAX_RESULT_EXCERPT_CHARS] + "\n... (result truncated)"
    return raw


def _pick_probe_tool(tools: list[McpTool]) -> McpTool | None:
    """Choose a probe: prefer tools that need no arguments, first listed wins."""
    for tool in tools:
        if not tool.required_arguments:
            return tool
    return None


def fetch_mcp_report(
    client: McpClient,
    scope: str | None = None,
    tool: str | None = None,
) -> McpReport:
    """Run initialize + tools/list + one tools/call probe against the MCP server.

    `tool` names an explicit probe target; without one, the first tool that
    requires no arguments is called. Raises `McpError` if the server cannot be
    reached, rejects the API key, or a named probe tool does not exist.
    """
    report = McpReport(endpoint=client.endpoint, scope=scope or client.default_scope)
    report.server_info = client.initialize(scope)
    report.tools = client.list_tools(scope)

    probe_tool: McpTool | None = None
    if tool:
        probe_tool = next((t for t in report.tools if t.name == tool), None)
        if probe_tool is None:
            available = ", ".join(t.name for t in report.tools) or "(none)"
            raise McpError(
                f"Tool '{tool}' is not listed for scope '{report.scope}'. "
                f"Available tools: {available}"
            )
    else:
        probe_tool = _pick_probe_tool(report.tools)

    if probe_tool is None:
        report.probe_skipped_reason = (
            "every listed tool requires arguments; name one explicitly, e.g. "
            "`/mcp-validate <tool_name>`"
        )
        return report

    arguments = _default_arguments(probe_tool)
    report.probe = client.call_tool(probe_tool.name, arguments, scope=scope)
    return report


def _default_arguments(tool: McpTool) -> dict[str, Any]:
    """Best-effort arguments for a probe call: empty unless inputs are required."""
    properties = tool.input_schema.get("properties") or {}
    arguments: dict[str, Any] = {}
    for name in tool.required_arguments:
        schema = properties.get(name) or {}
        arg_type = schema.get("type", "string")
        if arg_type == "integer":
            arguments[name] = 1
        elif arg_type == "number":
            arguments[name] = 1
        elif arg_type == "boolean":
            arguments[name] = False
        else:
            arguments[name] = ""
    return arguments


__all__ = ["McpReport", "fetch_mcp_report", "McpError"]
