/**
 * Streaming client for the mini games assistant.
 * An answer arrives as server-sent events rather than one JSON body,
 * so this module parses the stream frame by frame.
 */

import { handleUnauthorized } from '../federation/featureHost.js';

const API_BASE = '/mini-games/api';

export class AssistantToolsUnavailableError extends Error {
  constructor(message, disabled) {
    super(message);
    this.name = 'AssistantToolsUnavailableError';
    this.disabled = disabled;
  }
}

/**
 * Lists the MCP tools Garry can use for this feature (empty when tools are disabled server-side).
 * @param {AbortSignal} [signal]
 */
export async function listAssistantTools(signal) {
  const response = await fetch(`${API_BASE}/assistant/tools`, {
    signal,
    credentials: 'same-origin',
    headers: { Accept: 'application/json' }
  });
  await ensureToolResponse(response);
  const body = await response.json();
  return Array.isArray(body.tools) ? body.tools : [];
}

/**
 * Calls one MCP tool directly (not through a chat turn).
 * @param {string} name
 * @param {Record<string, unknown>} args
 * @param {AbortSignal} [signal]
 */
export async function callAssistantTool(name, args, signal) {
  const response = await fetch(`${API_BASE}/assistant/tools/${encodeURIComponent(name)}`, {
    method: 'POST',
    signal,
    credentials: 'same-origin',
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(args)
  });
  await ensureToolResponse(response);
  const body = await response.json();
  return {
    tool: body.tool ?? name,
    arguments: args,
    isError: body.isError === true,
    result: body.result ?? null
  };
}

async function ensureToolResponse(response) {
  if (response.status === 401) {
    handleUnauthorized();
  }
  if (response.ok) return;
  if (response.status === 503 || response.status === 502) {
    let problem;
    try {
      problem = await response.clone().json();
    } catch {
      problem = undefined;
    }
    throw new AssistantToolsUnavailableError(
      problem?.detail || "Garry's tools are unavailable right now.",
      problem?.code === 'mcp_disabled'
    );
  }
  throw new Error(await responseError(response));
}

/**
 * Stream an assistant reply.
 * @param {{message: string, history: Array<{role: string, content: string}>, context: {routeName: string, courseCode?: string, mode?: string}}} request
 * @param {{onDelta: (content: string) => void, onDone: () => void, onTool?: (result: {tool: string, arguments?: Record<string, unknown>, isError: boolean, result: unknown}) => void}} handlers
 * @param {AbortSignal} signal
 */
export async function streamAssistantMessage(request, handlers, signal) {
  const response = await fetch(`${API_BASE}/assistant/messages`, {
    method: 'POST',
    signal,
    credentials: 'same-origin',
    headers: {
      Accept: 'text/event-stream',
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(request)
  });

  if (response.status === 401) {
    handleUnauthorized();
  }
  if (!response.ok) {
    throw new Error(await responseError(response));
  }
  if (!response.body) {
    throw new Error('Garry could not start a response. Please try again.');
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';
  let completed = false;

  while (true) {
    const { value, done } = await reader.read();
    buffer += decoder.decode(value, { stream: !done }).replace(/\r\n/g, '\n');

    let boundary = buffer.indexOf('\n\n');
    while (boundary >= 0) {
      const frame = buffer.slice(0, boundary);
      buffer = buffer.slice(boundary + 2);
      completed = handleFrame(frame, handlers) || completed;
      boundary = buffer.indexOf('\n\n');
    }

    if (done) break;
  }

  if (buffer.trim()) {
    completed = handleFrame(buffer, handlers) || completed;
  }
  if (!completed) {
    throw new Error('Garry’s response ended unexpectedly. Please try again.');
  }
}

function handleFrame(frame, handlers) {
  let eventName = 'message';
  const dataLines = [];

  for (const line of frame.split('\n')) {
    if (line.startsWith('event:')) {
      eventName = line.slice(6).trim();
    } else if (line.startsWith('data:')) {
      dataLines.push(line.slice(5).trimStart());
    }
  }

  if (dataLines.length === 0) return false;

  let payload;
  try {
    payload = JSON.parse(dataLines.join('\n'));
  } catch {
    throw new Error('Garry returned an invalid response. Please try again.');
  }

  if (eventName === 'delta') {
    const content = readString(payload, 'content');
    if (content) handlers.onDelta(content);
    return false;
  }
  if (eventName === 'done') {
    handlers.onDone();
    return true;
  }
  if (eventName === 'tool') {
    const tool = readString(payload, 'name');
    if (tool && handlers.onTool) {
      handlers.onTool({
        tool,
        arguments: isRecord(payload.arguments) ? payload.arguments : undefined,
        isError: payload.isError === true,
        result: payload.result ?? null
      });
    }
    return false;
  }
  if (eventName === 'error') {
    throw new Error(
      readString(payload, 'message') ||
        'Garry’s response was interrupted. Please try again.'
    );
  }

  return false;
}

function readString(value, key) {
  if (typeof value !== 'object' || value === null || !(key in value)) return null;
  const property = value[key];
  return typeof property === 'string' ? property : null;
}

function isRecord(value) {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

async function responseError(response) {
  let problem;
  try {
    problem = await response.json();
  } catch {
    return `Garry is unavailable (${response.status} ${response.statusText}).`;
  }

  const validationError = problem.errors
    ? Object.values(problem.errors).flat().find(Boolean)
    : undefined;
  return (
    validationError ||
    problem.detail ||
    problem.title ||
    `Garry is unavailable (${response.status} ${response.statusText}).`
  );
}
