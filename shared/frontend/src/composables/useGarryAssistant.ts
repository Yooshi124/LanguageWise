import { readonly, ref } from 'vue'
import type { AssistantToolResult, FeatureAssistant } from '../federation/featureAssistants'
import {
  callAssistantTool,
  GarryApiError,
  listAssistantTools,
  streamAssistantMessage,
  type AssistantTool,
} from './garryApi'

export interface AssistantMessage {
  id: string
  role: 'user' | 'assistant'
  content: string
  toolResults?: AssistantToolResult[]
  /** The backend answered from stored help content because the model was unavailable. */
  fallback?: boolean
}

interface Conversation {
  storageKey: string
  assistant: FeatureAssistant
  onUnauthorized: () => void
}

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
let active: Conversation | null = null
let controller: AbortController | null = null

/** Points Garry at one feature's conversation for one user; each keeps its own transcript. */
function activate(
  featureKey: string,
  userId: number,
  assistant: FeatureAssistant,
  onUnauthorized: () => void,
) {
  const storageKey = `languagewise:garry:v2:${featureKey}:user:${userId}`
  if (active?.storageKey === storageKey) return

  controller?.abort()
  controller = null
  active = { storageKey, assistant, onUnauthorized }
  streaming.value = false
  error.value = null
  messages.value = loadMessages(storageKey)
  tools.value = []
  toolsLoading.value = false
  toolsError.value = null
  toolRunning.value = null
  toolsEnabled.value =
    assistant.tools !== undefined && sessionStorage.getItem(`${storageKey}:tools`) === 'on'
  if (toolsEnabled.value) void loadTools()
}

async function send(content: string, context: Record<string, unknown>) {
  const conversation = active
  const trimmed = content.trim()
  if (!trimmed || streaming.value || !conversation) return

  error.value = null
  const history = boundedHistory(messages.value, trimmed.length)
  const assistantMessage = createMessage('assistant', '')
  messages.value.push(createMessage('user', trimmed), assistantMessage)
  streaming.value = true
  controller = new AbortController()
  const requestController = controller

  try {
    await streamAssistantMessage(
      conversation.assistant.apiBase,
      { message: trimmed, history, context },
      {
        onDelta: (delta) =>
          updateMessage(assistantMessage.id, (current) => ({
            ...current,
            content: current.content + delta,
          })),
        onTool: (result) =>
          updateMessage(assistantMessage.id, (current) => ({
            ...current,
            toolResults: [...(current.toolResults ?? []), result],
          })),
        onDone: (reason) => {
          if (reason === 'fallback') {
            updateMessage(assistantMessage.id, (current) => ({ ...current, fallback: true }))
          }
          persist()
        },
      },
      requestController.signal,
    )
  } catch (cause) {
    if (active !== conversation) return
    messages.value = messages.value.filter(
      (message) => message.id !== assistantMessage.id || message.content.trim(),
    )
    persist()
    if (cause instanceof GarryApiError && cause.status === 401) conversation.onUnauthorized()
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

function updateMessage(id: string, update: (message: AssistantMessage) => AssistantMessage) {
  const index = messages.value.findIndex((message) => message.id === id)
  const current = messages.value[index]
  if (index >= 0 && current) {
    messages.value[index] = update(current)
  }
}

async function setToolsEnabled(enabled: boolean) {
  if (!active?.assistant.tools) return
  toolsEnabled.value = enabled
  sessionStorage.setItem(`${active.storageKey}:tools`, enabled ? 'on' : 'off')
  if (enabled && tools.value.length === 0) {
    await loadTools()
  }
}

async function loadTools() {
  const conversation = active
  if (!conversation?.assistant.tools || toolsLoading.value) return
  toolsLoading.value = true
  toolsError.value = null
  try {
    const available = await listAssistantTools(conversation.assistant.apiBase)
    if (active === conversation) tools.value = available
  } catch (cause) {
    if (active !== conversation) return
    tools.value = []
    if (cause instanceof GarryApiError && cause.status === 401) conversation.onUnauthorized()
    toolsError.value =
      cause instanceof GarryApiError && cause.toolsDisabled
        ? 'Garry’s tools are turned off on this server.'
        : cause instanceof Error
          ? cause.message
          : 'Garry’s tools are unavailable right now.'
  } finally {
    if (active === conversation) toolsLoading.value = false
  }
}

async function runTool(tool: AssistantTool, args: Record<string, unknown>) {
  const conversation = active
  if (!conversation || streaming.value || toolRunning.value) return
  error.value = null
  toolRunning.value = tool.name
  try {
    const result = await callAssistantTool(conversation.assistant.apiBase, tool.name, args)
    if (active !== conversation) return
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
    if (active !== conversation) return
    if (cause instanceof GarryApiError && cause.status === 401) conversation.onUnauthorized()
    error.value = cause instanceof Error ? cause.message : 'Garry could not use that tool.'
  } finally {
    if (active === conversation) toolRunning.value = null
  }
}

/** Re-sends the last question, replacing any partial answer that followed it. */
async function retry(context: Record<string, unknown>) {
  if (streaming.value) return
  let asked = messages.value.length - 1
  while (asked >= 0 && messages.value[asked]?.role !== 'user') asked--
  const question = messages.value[asked]
  if (!question) return
  messages.value = messages.value.slice(0, asked)
  persist()
  await send(question.content, context)
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
  if (!active) return
  const completeMessages = messages.value.filter((message) => message.content.trim())
  messages.value = completeMessages.slice(-maximumStoredMessages)
  const storedMessages = boundedMessages(messages.value.map(normalizeHistoryMessage)).map(
    storableMessage,
  )
  try {
    sessionStorage.setItem(active.storageKey, JSON.stringify(storedMessages))
  } catch {
    // A full session storage only costs the transcript on reload.
  }
}

function loadMessages(storageKey: string) {
  const stored = sessionStorage.getItem(storageKey)
  if (!stored) return []

  try {
    const value: unknown = JSON.parse(stored)
    if (!Array.isArray(value)) return []
    return boundedMessages(value.filter(isAssistantMessage).slice(-maximumStoredMessages))
  } catch {
    sessionStorage.removeItem(storageKey)
    return []
  }
}

function storableMessage(message: AssistantMessage): AssistantMessage {
  if (!message.toolResults?.length) return message
  if (JSON.stringify(message.toolResults).length <= maximumStoredToolResultCharacters) {
    return message
  }
  const { toolResults: _omitted, ...withoutToolResults } = message
  return withoutToolResults
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
    (message.fallback === undefined || typeof message.fallback === 'boolean') &&
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
  ).map(({ role, content }) => ({ role, content }))
}

function normalizeHistoryMessage(message: AssistantMessage): AssistantMessage {
  return message.content.length <= maximumHistoryMessageCharacters
    ? message
    : { ...message, content: message.content.slice(0, maximumHistoryMessageCharacters) }
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
  return { id: crypto.randomUUID(), role, content }
}

export function useGarryAssistant() {
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
    activate,
    send,
    retry,
    cancel,
    clear,
    setToolsEnabled,
    loadTools,
    runTool,
  }
}
