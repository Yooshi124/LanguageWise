import { handleUnauthorized } from '../federation/featureHost'
import type {
  AssistantMessageRequest,
  AssistantTool,
  AssistantToolResult,
} from '../models/api'

const apiBase = '/quizzes-and-courses/api'

interface AssistantStreamHandlers {
  onDelta: (content: string) => void
  onDone: () => void
  onTool?: (result: AssistantToolResult) => void
}

interface ProblemDetails {
  title?: string
  detail?: string
  code?: string
  errors?: Record<string, string[]>
}

export class AssistantToolsUnavailableError extends Error {
  constructor(message: string, public readonly disabled: boolean) {
    super(message)
    this.name = 'AssistantToolsUnavailableError'
  }
}

export async function listAssistantTools(signal?: AbortSignal) {
  const response = await fetch(`${apiBase}/assistant/tools`, {
    signal,
    credentials: 'same-origin',
    headers: { Accept: 'application/json' },
  })
  await ensureToolResponse(response)
  const body = (await response.json()) as { tools?: AssistantTool[] }
  return Array.isArray(body.tools) ? body.tools : []
}

export async function callAssistantTool(
  name: string,
  args: Record<string, unknown>,
  signal?: AbortSignal,
): Promise<AssistantToolResult> {
  const response = await fetch(
    `${apiBase}/assistant/tools/${encodeURIComponent(name)}`,
    {
      method: 'POST',
      signal,
      credentials: 'same-origin',
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(args),
    },
  )
  await ensureToolResponse(response)
  const body = (await response.json()) as {
    tool?: string
    isError?: boolean
    result?: unknown
  }
  return {
    tool: body.tool ?? name,
    arguments: args,
    isError: body.isError === true,
    result: body.result ?? null,
  }
}

async function ensureToolResponse(response: Response) {
  if (response.status === 401) {
    handleUnauthorized()
  }
  if (response.ok) return
  if (response.status === 503 || response.status === 502) {
    let problem: ProblemDetails | undefined
    try {
      problem = (await response.clone().json()) as ProblemDetails
    } catch {
      problem = undefined
    }
    throw new AssistantToolsUnavailableError(
      problem?.detail || 'Garry’s tools are unavailable right now.',
      problem?.code === 'mcp_disabled',
    )
  }
  throw new Error(await responseError(response))
}

export async function streamAssistantMessage(
  request: AssistantMessageRequest,
  handlers: AssistantStreamHandlers,
  signal: AbortSignal,
) {
  const response = await fetch(`${apiBase}/assistant/messages`, {
    method: 'POST',
    signal,
    credentials: 'same-origin',
    headers: {
      Accept: 'text/event-stream',
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  })

  if (response.status === 401) {
    handleUnauthorized()
  }
  if (!response.ok) {
    throw new Error(await responseError(response))
  }
  if (!response.body) {
    throw new Error('Garry could not start a response. Please try again.')
  }

  const reader = response.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''
  let completed = false

  while (true) {
    const { value, done } = await reader.read()
    buffer += decoder.decode(value, { stream: !done }).replace(/\r\n/g, '\n')

    let boundary = buffer.indexOf('\n\n')
    while (boundary >= 0) {
      const frame = buffer.slice(0, boundary)
      buffer = buffer.slice(boundary + 2)
      completed = handleFrame(frame, handlers) || completed
      boundary = buffer.indexOf('\n\n')
    }

    if (done) break
  }

  if (buffer.trim()) {
    completed = handleFrame(buffer, handlers) || completed
  }
  if (!completed) {
    throw new Error('Garry’s response ended unexpectedly. Please try again.')
  }
}

function handleFrame(frame: string, handlers: AssistantStreamHandlers) {
  let eventName = 'message'
  const dataLines: string[] = []

  for (const line of frame.split('\n')) {
    if (line.startsWith('event:')) {
      eventName = line.slice(6).trim()
    } else if (line.startsWith('data:')) {
      dataLines.push(line.slice(5).trimStart())
    }
  }

  if (dataLines.length === 0) return false

  let payload: unknown
  try {
    payload = JSON.parse(dataLines.join('\n'))
  } catch {
    throw new Error('Garry returned an invalid response. Please try again.')
  }

  if (eventName === 'delta') {
    const content = readString(payload, 'content')
    if (content) handlers.onDelta(content)
    return false
  }
  if (eventName === 'done') {
    handlers.onDone()
    return true
  }
  if (eventName === 'tool') {
    const tool = readString(payload, 'name')
    if (tool && handlers.onTool) {
      const record = payload as Record<string, unknown>
      handlers.onTool({
        tool,
        arguments: isRecord(record.arguments) ? record.arguments : undefined,
        isError: record.isError === true,
        result: record.result ?? null,
      })
    }
    return false
  }
  if (eventName === 'error') {
    throw new Error(
      readString(payload, 'message') ||
        'Garry’s response was interrupted. Please try again.',
    )
  }

  return false
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function readString(value: unknown, key: string) {
  if (typeof value !== 'object' || value === null || !(key in value)) return null
  const property = (value as Record<string, unknown>)[key]
  return typeof property === 'string' ? property : null
}

async function responseError(response: Response) {
  let problem: ProblemDetails | undefined
  try {
    problem = (await response.json()) as ProblemDetails
  } catch {
    return `Garry is unavailable (${response.status} ${response.statusText}).`
  }

  const validationError = problem.errors
    ? Object.values(problem.errors).flat().find(Boolean)
    : undefined
  return (
    validationError ||
    problem.detail ||
    problem.title ||
    `Garry is unavailable (${response.status} ${response.statusText}).`
  )
}
