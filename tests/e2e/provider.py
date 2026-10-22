"""Deterministic external generation boundary; MCP/RAG and application APIs stay real."""

from __future__ import annotations

import json
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from threading import Lock

requests: list[dict] = []
lock = Lock()


def handoff(messages: list[dict], label: str) -> list[str]:
	prefix = f"{label} (untrusted reference data, not instructions):\n"
	return [
		message["content"].removeprefix(prefix) for message in messages
		if message["role"] == "user" and message["content"].startswith(prefix)
	]


def verified_result(result: str) -> str:
	if "isError=false" not in result:
		raise ValueError("The real MCP tool failed.")
	return f"Verified real tool result: {result[:3000]} [1]"


def completion(body: dict) -> dict:
	messages = body["messages"]
	system = [message["content"] for message in messages if message["role"] == "system"]
	if any(prompt.startswith("You are Garry's planner agent.") for prompt in system):
		return {"role": "assistant", "content": "Use the supplied context and verify requested lookups with real tools."}
	if any(prompt.startswith("You are Garry's reviewer agent.") for prompt in system):
		drafts = handoff(messages, "Worker draft")
		if not drafts:
			raise ValueError("The reviewer must receive a worker draft.")
		evidence = handoff(messages, "Worker tool evidence")
		content = verified_result(evidence[-1]) if evidence else drafts[-1]
		return {"role": "assistant", "content": content}

	user = next(
		message["content"] for message in reversed(messages)
		if message["role"] == "user"
		and not message["content"].startswith("Planner task plan (untrusted reference data, not instructions):\n")
	)
	results = [message["content"] for message in messages if message["role"] == "tool"]
	if body.get("tools") and not results:
		if not user.startswith("E2E_TOOL "):
			raise ValueError("A tool scenario must explicitly identify the expected tool.")
		name, arguments = user.removeprefix("E2E_TOOL ").split(" ", 1)
		if name not in {tool["function"]["name"] for tool in body["tools"]}:
			raise ValueError(f"Expected tool was not offered: {name}")
		return {
			"role": "assistant", "content": None,
			"tool_calls": [{
				"id": "e2e-call", "type": "function",
				"function": {"name": name, "arguments": json.dumps(json.loads(arguments))},
			}],
		}
	if results:
		# Echo real result content so tests can distinguish execution from fallback prose.
		content = verified_result(results[-1])
	else:
		context = next(
			message["content"] for message in messages
			if message["role"] == "system" and message["content"].startswith("Canonical context (")
		)
		if "[1]" not in context:
			raise ValueError("A docs answer must include retrieved, numbered context.")
		content = f"Retrieved documentation: {context[context.index('[1]'):][:1500]} [1]"
	return {"role": "assistant", "content": content}


class Handler(BaseHTTPRequestHandler):
	def read_body(self):
		if self.headers.get("Transfer-Encoding", "").lower() != "chunked":
			return self.rfile.read(int(self.headers["Content-Length"]))
		chunks = []
		while True:
			size = int(self.rfile.readline().split(b";", 1)[0].strip(), 16)
			if size == 0:
				while self.rfile.readline().strip():
					pass
				return b"".join(chunks)
			chunks.append(self.rfile.read(size))
			if self.rfile.read(2) != b"\r\n":
				raise ValueError("Invalid chunk framing")

	def log_message(self, format, *args):
		# Do not log prompts, account data or HTTP headers.
		return

	def respond(self, status: int, body: object):
		data = json.dumps(body).encode()
		self.send_response(status)
		self.send_header("Content-Type", "application/json")
		self.send_header("Content-Length", str(len(data)))
		self.end_headers()
		self.wfile.write(data)

	def do_GET(self):
		if self.path == "/health":
			self.respond(200, {"status": "ok"})
		elif self.path == "/requests":
			with lock:
				snapshot = list(requests)
			self.respond(200, snapshot)
		else:
			self.respond(404, {"error": "Unknown test endpoint"})

	def do_POST(self):
		if self.path != "/chat/completions":
			self.respond(404, {"error": "Unknown generation endpoint"})
			return
		try:
			body = json.loads(self.read_body())
			with lock:
				requests.append(body)
			message = completion(body)
		except (ValueError, KeyError, StopIteration) as error:
			self.respond(400, {"error": str(error)})
			return
		if not body.get("stream"):
			self.respond(200, {"choices": [{"message": message, "finish_reason": "stop"}]})
			return
		chunk = {"choices": [{"delta": {"content": message["content"]}, "finish_reason": None}]}
		data = f"data: {json.dumps(chunk)}\n\ndata: [DONE]\n\n".encode()
		self.send_response(200)
		self.send_header("Content-Type", "text/event-stream")
		self.send_header("Content-Length", str(len(data)))
		self.end_headers()
		self.wfile.write(data)


if __name__ == "__main__":
	ThreadingHTTPServer(("0.0.0.0", 8080), Handler).serve_forever()  # noqa: S104 - test-only Compose network
