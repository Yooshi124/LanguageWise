import type { AssistantToolResult } from '../federation/featureAssistants'

export interface AssistantTool {
  name: string
  title: string
  description: string
}

export interface AssistantMessageRequest {
  message: string
  history: { role: 'user' | 'assistant'; content: string }[]
  context: Record<string, unknown>
}

export type DocsConfidence = 'high' | 'medium' | 'low' | 'insufficient'

export interface DocsCitation {
  number: number
  source: string
  heading: string
  relevance: number
  text: string
}

export interface DocsAnswer {
  answer: string
  confidence: DocsConfidence
  citations: DocsCitation[]
}

export async function askDocs(query: string): Promise<DocsAnswer> {
  const response = await fetch('/api/rag/answer', {
    method: 'POST',
    credentials: 'same-origin',
    headers: { Accept: 'application/json', 'Content-Type': 'application/json' },
    body: JSON.stringify({ query }),
  })
  if (!response.ok) {
    const problem = await readProblem(response)
    const validationError = problem?.errors
      ? Object.values(problem.errors).flat().find(Boolean)
      : undefined
    throw new GarryApiError(
      response.status,
      validationError || problem?.title || 'Could not ask the docs right now.',
    )
  }
  const body = (await response.json()) as Partial<DocsAnswer>
  return {
    answer: body.answer ?? '',
    confidence: body.confidence ?? 'insufficient',
    citations: Array.isArray(body.citations) ? body.citations : [],
  }
}

interface AssistantStreamHandlers {
  onDelta: (content: string) => void
  onTool: (result: AssistantToolResult) => void
  onDone: (reason: string) => void
}

interface ProblemDetails {
  title?: string
  detail?: string
  code?: string
  errors?: Record<string, string[]>
}

export class GarryApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
    public readonly toolsDisabled = false,
  ) {
    super(message)
    this.name = 'GarryApiError'
  }
}

export async function listAssistantTools(apiBase: string) {
  const response = await fetch(`${apiBase}/assistant/tools`, {
    credentials: 'same-origin',
    headers: { Accept: 'application/json' },
  })
  await ensureToolResponse(response)
  const body = (await response.json()) as { tools?: AssistantTool[] }
  return Array.isArray(body.tools) ? body.tools : []
}

export async function callAssistantTool(
  apiBase: string,
  name: string,
  args: Record<string, unknown>,
): Promise<AssistantToolResult> {
  const response = await fetch(`${apiBase}/assistant/tools/${encodeURIComponent(name)}`, {
    method: 'POST',
    credentials: 'same-origin',
    headers: { Accept: 'application/json', 'Content-Type': 'application/json' },
    body: JSON.stringify(args),
  })
  await ensureToolResponse(response)
  const body = (await response.json()) as { tool?: string; isError?: boolean; result?: unknown }
  return {
    tool: body.tool ?? name,
    arguments: args,
    isError: body.isError === true,
    result: body.result ?? null,
  }
}

async function ensureToolResponse(response: Response) {
  if (response.ok) return
  if (response.status === 502 || response.status === 503) {
    const problem = await readProblem(response)
    throw new GarryApiError(
      response.status,
      problem?.detail || 'Garry’s tools are unavailable right now.',
      problem?.code === 'mcp_disabled',
    )
  }
  throw new GarryApiError(response.status, await responseError(response))
}

export async function streamAssistantMessage(
  apiBase: string,
  request: AssistantMessageRequest,
  handlers: AssistantStreamHandlers,
  signal: AbortSignal,
) {
  let response: Response
  try {
    response = await fetch(`${apiBase}/assistant/messages`, {
      method: 'POST',
      signal,
      credentials: 'same-origin',
      headers: { Accept: 'text/event-stream', 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    })
  } catch (cause) {
    if (cause instanceof DOMException && cause.name === 'AbortError') throw cause
    throw new GarryApiError(0, 'Garry is unavailable. Please try again.')
  }

  if (!response.ok) {
    throw new GarryApiError(response.status, await responseError(response))
  }
  if (!response.body) {
    throw new GarryApiError(response.status, 'Garry could not start a response. Please try again.')
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
      completed = handleFrame(buffer.slice(0, boundary), handlers) || completed
      buffer = buffer.slice(boundary + 2)
      boundary = buffer.indexOf('\n\n')
    }

    if (done) break
  }

  if (buffer.trim()) completed = handleFrame(buffer, handlers) || completed
  if (!completed) {
    throw new Error('Garry’s response ended unexpectedly. Please try again.')
  }
}

function handleFrame(frame: string, handlers: AssistantStreamHandlers) {
  let eventName = 'message'
  const dataLines: string[] = []

  for (const line of frame.split('\n')) {
    if (line.startsWith('event:')) eventName = line.slice(6).trim()
    else if (line.startsWith('data:')) dataLines.push(line.slice(5).trimStart())
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
  if (eventName === 'tool') {
    const tool = readString(payload, 'name')
    if (tool) {
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
  if (eventName === 'done') {
    handlers.onDone(readString(payload, 'reason') ?? 'stop')
    return true
  }
  if (eventName === 'error') {
    throw new Error(readString(payload, 'message') || 'Garry’s response was interrupted. Please try again.')
  }

  return false
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function readString(value: unknown, key: string) {
  if (!isRecord(value)) return null
  const property = value[key]
  return typeof property === 'string' ? property : null
}

async function readProblem(response: Response) {
  try {
    return (await response.json()) as ProblemDetails
  } catch {
    return undefined
  }
}

async function responseError(response: Response) {
  if (response.status === 401) return 'Please sign in again to talk to Garry.'
  const problem = await readProblem(response)
  const validationError = problem?.errors
    ? Object.values(problem.errors).flat().find(Boolean)
    : undefined
  return (
    validationError ||
    problem?.detail ||
    problem?.title ||
    `Garry is unavailable (${response.status} ${response.statusText}).`
  )
}
