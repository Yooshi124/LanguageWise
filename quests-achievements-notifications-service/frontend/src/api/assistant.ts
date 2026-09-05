import type { AssistantMessageRequest } from '../models'

const apiBase = '/quests-and-achievements/api'

interface AssistantStreamHandlers {
  onDelta: (content: string) => void
  onDone: () => void
}

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export class AssistantApiError extends Error {
  constructor(public readonly status: number, message: string) {
    super(message)
  }
}

export async function streamAssistantMessage(
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
      headers: {
        Accept: 'text/event-stream',
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    })
  } catch (cause) {
    if (cause instanceof DOMException && cause.name === 'AbortError') throw cause
    throw new AssistantApiError(0, 'Garry is unavailable. Please try again.')
  }

  if (!response.ok) {
    throw new AssistantApiError(response.status, await responseError(response))
  }
  if (!response.body) {
    throw new AssistantApiError(response.status, 'Garry could not start a response. Please try again.')
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
    throw new AssistantApiError(response.status, 'Garry’s response ended unexpectedly. Please try again.')
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
  if (eventName === 'done') {
    handlers.onDone()
    return true
  }
  if (eventName === 'error') {
    throw new Error(readString(payload, 'message') || 'Garry’s response was interrupted. Please try again.')
  }

  return false
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

  return problem.errors
    ? Object.values(problem.errors).flat().find(Boolean)
      ?? problem.detail
      ?? problem.title
      ?? 'Garry could not start a response. Please try again.'
    : problem.detail ?? problem.title ?? 'Garry could not start a response. Please try again.'
}