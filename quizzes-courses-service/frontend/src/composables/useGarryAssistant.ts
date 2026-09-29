import { readonly, ref } from 'vue'
import {
  AssistantToolsUnavailableError,
  callAssistantTool,
  listAssistantTools,
  streamAssistantMessage,
} from '../api/assistant'
import type {
  AssistantMessage,
  AssistantRouteContext,
  AssistantTool,
  AssistantToolResult,
} from '../models/api'

const maximumStoredMessages = 12
const maximumConversationCharacters = 12000
const maximumHistoryMessageCharacters = 12000
const maximumStoredToolResultCharacters = 20000
const messages = ref<AssistantMessage[]>([])
const expanded = ref(false)
const streaming = ref(false)
const error = ref<string | null>(null)
const toolsEnabled = ref(false)
const tools = ref<AssistantTool[]>([])
const toolsLoading = ref(false)
const toolsError = ref<string | null>(null)
const toolRunning = ref<string | null>(null)
let activeUserId: number | null = null
let controller: AbortController | null = null

function storageKey(userId: number) {
  return `languagewise:garry:v1:user:${userId}`
}

function toolsStorageKey(userId: number) {
  return `${storageKey(userId)}:tools`
}

function initialize(userId: number) {
  if (activeUserId === userId) return
  controller?.abort()
  activeUserId = userId
  streaming.value = false
  error.value = null
  messages.value = loadMessages(userId)
  tools.value = []
  toolsError.value = null
  toolRunning.value = null
  toolsEnabled.value = sessionStorage.getItem(toolsStorageKey(userId)) === 'on'
  if (toolsEnabled.value) void loadTools()
}

async function send(content: string, context: AssistantRouteContext) {
  const trimmed = content.trim()
  if (!trimmed || streaming.value || activeUserId === null) return

  error.value = null
  const history = boundedHistory(messages.value, trimmed.length)
  const userMessage = createMessage('user', trimmed)
  const assistantMessage = createMessage('assistant', '')
  messages.value.push(userMessage, assistantMessage)
  streaming.value = true
  controller = new AbortController()
  const requestController = controller

  try {
    await streamAssistantMessage(
      {
        message: trimmed,
        history,
        context,
      },
      {
        onDelta: (delta) => {
          const index = messages.value.findIndex(
            (message) => message.id === assistantMessage.id,
          )
          const current = messages.value[index]
          if (index >= 0 && current) {
            messages.value[index] = {
              ...current,
              content: current.content + delta,
            }
          }
        },
        onTool: (result) => {
          updateMessage(assistantMessage.id, (current) => ({
            ...current,
            toolResults: [...(current.toolResults ?? []), result],
          }))
        },
        onDone: () => {
          persist()
        },
      },
      requestController.signal,
    )
  } catch (cause) {
    messages.value = messages.value.filter((message) => message.id !== assistantMessage.id)
    persist()
    if (!(cause instanceof DOMException && cause.name === 'AbortError')) {
      error.value = cause instanceof Error ? cause.message : 'Garry could not respond.'
    }
  } finally {
    if (controller === requestController) {
      controller = null
      streaming.value = false
    }
  }
}

function updateMessage(
  id: string,
  update: (message: AssistantMessage) => AssistantMessage,
) {
  const index = messages.value.findIndex((message) => message.id === id)
  const current = messages.value[index]
  if (index >= 0 && current) {
    messages.value[index] = update(current)
  }
}

async function setToolsEnabled(enabled: boolean) {
  toolsEnabled.value = enabled
  if (activeUserId !== null) {
    sessionStorage.setItem(toolsStorageKey(activeUserId), enabled ? 'on' : 'off')
  }
  if (enabled && tools.value.length === 0) {
    await loadTools()
  }
}

async function loadTools() {
  if (toolsLoading.value) return
  toolsLoading.value = true
  toolsError.value = null
  try {
    tools.value = await listAssistantTools()
  } catch (cause) {
    tools.value = []
    toolsError.value =
      cause instanceof AssistantToolsUnavailableError && cause.disabled
        ? 'Garry’s tools are turned off on this server.'
        : cause instanceof Error
          ? cause.message
          : 'Garry’s tools are unavailable right now.'
  } finally {
    toolsLoading.value = false
  }
}

async function runTool(tool: AssistantTool, args: Record<string, unknown>) {
  if (streaming.value || toolRunning.value || activeUserId === null) return
  error.value = null
  toolRunning.value = tool.name
  try {
    const result = await callAssistantTool(tool.name, args)
    messages.value.push({
      ...createMessage(
        'assistant',
        result.isError
          ? `I couldn’t use **${tool.title}** just now.`
          : `Here’s what I found with **${tool.title}**.`,
      ),
      toolResults: [result],
    })
    persist()
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : 'Garry could not use that tool.'
  } finally {
    toolRunning.value = null
  }
}

async function retry(context: AssistantRouteContext) {
  const last = messages.value.at(-1)
  if (!last || last.role !== 'user' || streaming.value) return
  messages.value.pop()
  persist()
  await send(last.content, context)
}

function cancel() {
  controller?.abort()
}

function clear() {
  controller?.abort()
  messages.value = []
  error.value = null
  persist()
}

function persist() {
  if (activeUserId === null) return
  const completeMessages = messages.value.filter((message) => message.content.trim())
  messages.value = completeMessages.slice(-maximumStoredMessages)
  const storedMessages = boundedMessages(
    messages.value.map(normalizeHistoryMessage),
  ).map(storableMessage)
  sessionStorage.setItem(storageKey(activeUserId), JSON.stringify(storedMessages))
}

function loadMessages(userId: number) {
  const stored = sessionStorage.getItem(storageKey(userId))
  if (!stored) return []

  try {
    const value: unknown = JSON.parse(stored)
    if (!Array.isArray(value)) return []
    const valid = value.filter(isAssistantMessage).slice(-maximumStoredMessages)
    return boundedMessages(valid)
  } catch {
    sessionStorage.removeItem(storageKey(userId))
    return []
  }
}

function storableMessage(message: AssistantMessage): AssistantMessage {
  if (!message.toolResults?.length) return message
  return JSON.stringify(message.toolResults).length <= maximumStoredToolResultCharacters
    ? message
    : { id: message.id, role: message.role, content: message.content }
}

function isAssistantMessage(value: unknown): value is AssistantMessage {
  if (typeof value !== 'object' || value === null) return false
  const message = value as Record<string, unknown>
  return (
    typeof message.id === 'string' &&
    (message.role === 'user' || message.role === 'assistant') &&
    typeof message.content === 'string' &&
    message.content.trim().length > 0 &&
    message.content.length <= maximumHistoryMessageCharacters &&
    (message.toolResults === undefined ||
      (Array.isArray(message.toolResults) && message.toolResults.every(isToolResult)))
  )
}

function isToolResult(value: unknown): value is AssistantToolResult {
  if (typeof value !== 'object' || value === null) return false
  const result = value as Record<string, unknown>
  return typeof result.tool === 'string' && typeof result.isError === 'boolean'
}

function boundedHistory(source: AssistantMessage[], nextMessageCharacters: number) {
  return boundedMessages(
    source.map(normalizeHistoryMessage),
    maximumConversationCharacters - nextMessageCharacters,
  )
    .map(({ role, content }) => ({ role, content }))
}

function normalizeHistoryMessage(message: AssistantMessage): AssistantMessage {
  return message.content.length <= maximumHistoryMessageCharacters
    ? message
    : {
        ...message,
        content: message.content.slice(0, maximumHistoryMessageCharacters),
      }
}

function boundedMessages(
  source: AssistantMessage[],
  characterLimit = maximumConversationCharacters,
) {
  const selected: AssistantMessage[] = []
  let characters = 0

  for (let index = source.length - 1; index >= 0; index--) {
    const message = source[index]
    if (!message || selected.length >= maximumStoredMessages) break
    if (characters + message.content.length > characterLimit) break
    selected.unshift(message)
    characters += message.content.length
  }

  return selected
}

function createMessage(role: AssistantMessage['role'], content: string): AssistantMessage {
  return {
    id: crypto.randomUUID(),
    role,
    content,
  }
}

export function useGarryAssistant(userId: number) {
  initialize(userId)
  return {
    messages: readonly(messages),
    expanded,
    streaming: readonly(streaming),
    error: readonly(error),
    toolsEnabled: readonly(toolsEnabled),
    tools: readonly(tools),
    toolsLoading: readonly(toolsLoading),
    toolsError: readonly(toolsError),
    toolRunning: readonly(toolRunning),
    send,
    retry,
    cancel,
    clear,
    setToolsEnabled,
    loadTools,
    runTool,
  }
}
