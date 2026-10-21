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
  featureKey?: string
  toolResults?: AssistantToolResult[]
  /** The backend answered from stored help content because the model was unavailable. */
  fallback?: boolean
}

interface Conversation {
  featureKey: string
  assistant: FeatureAssistant
}

const maximumStoredMessages = 12
const maximumConversationCharacters = 12000
const maximumHistoryMessageCharacters = 12000
const maximumStoredToolResultCharacters = 20000
export function garryStorageKey(userId: number, sessionId: string) {
  return `languagewise:garry:v3:user:${userId}:session:${sessionId}`
}

/** Each window owns its transcript and requests; navigation only changes future requests. */
export function useGarryAssistant(
  userId: number,
  sessionId: string,
  onUnauthorized: () => void,
) {
  const storageKey = garryStorageKey(userId, sessionId)
  const messages = ref<AssistantMessage[]>(loadMessages(storageKey))
  const streaming = ref(false)
  const error = ref<string | null>(null)
  const toolsEnabled = ref(false)
  const tools = ref<AssistantTool[]>([])
  const toolsLoading = ref(false)
  const toolsError = ref<string | null>(null)
  const toolRunning = ref<string | null>(null)
  let active: Conversation | null = null
  let controller: AbortController | null = null
  let toolsController: AbortController | null = null
  let toolController: AbortController | null = null
  let disposed = false
  let legacyImported = sessionStorage.getItem(storageKey) !== null

  function activate(featureKey?: string, assistant?: FeatureAssistant) {
    if (disposed || (active?.featureKey === featureKey && active?.assistant === assistant)) return

    toolsController?.abort()
    toolsController = null
    active = featureKey && assistant ? { featureKey, assistant } : null
    tools.value = []
    toolsLoading.value = false
    toolsError.value = null
    toolsEnabled.value = false
    if (!active) return

    if (!legacyImported && sessionId === 'main') {
      messages.value = loadMessages(`languagewise:garry:v2:${featureKey}:user:${userId}`)
        .map((message) => ({ ...message, featureKey }))
      legacyImported = true
      persist()
    }
    toolsEnabled.value =
      assistant?.tools !== undefined &&
      sessionStorage.getItem(`${storageKey}:tools:${featureKey}`) === 'on'
    if (toolsEnabled.value) void loadTools()
  }

  async function send(content: string, context: Record<string, unknown>) {
    const conversation = active
    const trimmed = content.trim()
    if (!trimmed || streaming.value || toolRunning.value || !conversation || disposed) return

    error.value = null
    const history = boundedHistory(messages.value, trimmed.length)
    const assistantMessage = createMessage('assistant', '', conversation.featureKey)
    messages.value.push(createMessage('user', trimmed, conversation.featureKey), assistantMessage)
    streaming.value = true
    controller = new AbortController()
    const requestController = controller

    try {
      await streamAssistantMessage(
        conversation.assistant.apiBase,
        { message: trimmed, history, context },
        {
          onDelta: (delta) => {
            if (controller !== requestController) return
            updateMessage(assistantMessage.id, (current) => ({
              ...current,
              content: current.content + delta,
            }))
          },
          onTool: (result) => {
            if (controller !== requestController) return
            updateMessage(assistantMessage.id, (current) => ({
              ...current,
              toolResults: [...(current.toolResults ?? []), result],
            }))
          },
          onDone: (reason) => {
            if (controller !== requestController) return
            if (reason === 'fallback') {
              updateMessage(assistantMessage.id, (current) => ({ ...current, fallback: true }))
            }
            persist()
          },
        },
        requestController.signal,
      )
    } catch (cause) {
      if (controller !== requestController) return
      messages.value = messages.value.filter(
        (message) => message.id !== assistantMessage.id || message.content.trim(),
      )
      persist()
      if (cause instanceof GarryApiError && cause.status === 401) onUnauthorized()
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
    sessionStorage.setItem(`${storageKey}:tools:${active.featureKey}`, enabled ? 'on' : 'off')
    if (enabled && tools.value.length === 0) {
      await loadTools()
    }
  }

  async function loadTools() {
    const conversation = active
    if (!conversation?.assistant.tools || toolsLoading.value || disposed) return
    toolsLoading.value = true
    toolsError.value = null
    const requestController = new AbortController()
    toolsController = requestController
    try {
      const available = await listAssistantTools(conversation.assistant.apiBase, requestController.signal)
      if (toolsController === requestController) tools.value = available
    } catch (cause) {
      if (toolsController !== requestController) return
      tools.value = []
      if (cause instanceof GarryApiError && cause.status === 401) onUnauthorized()
      toolsError.value =
        cause instanceof GarryApiError && cause.toolsDisabled
          ? 'Garry’s tools are turned off on this server.'
          : cause instanceof Error
            ? cause.message
            : 'Garry’s tools are unavailable right now.'
    } finally {
      if (toolsController === requestController) {
        toolsController = null
        toolsLoading.value = false
      }
    }
  }

  async function runTool(tool: AssistantTool, args: Record<string, unknown>) {
    const conversation = active
    if (!conversation || streaming.value || toolRunning.value || disposed) return
    error.value = null
    toolRunning.value = tool.name
    const requestController = new AbortController()
    toolController = requestController
    try {
      const result = await callAssistantTool(
        conversation.assistant.apiBase, tool.name, args, requestController.signal,
      )
      if (toolController !== requestController) return
      messages.value.push({
        ...createMessage(
          'assistant',
          result.isError
            ? `I couldn’t use **${tool.title}** just now.`
            : `Here’s what I found with **${tool.title}**.`,
          conversation.featureKey,
        ),
        toolResults: [result],
      })
      persist()
    } catch (cause) {
      if (toolController !== requestController) return
      if (cause instanceof GarryApiError && cause.status === 401) onUnauthorized()
      error.value = cause instanceof Error ? cause.message : 'Garry could not use that tool.'
    } finally {
      if (toolController === requestController) {
        toolController = null
        toolRunning.value = null
      }
    }
  }

  /** Re-sends the last question in the current page context. */
  async function retry(context: Record<string, unknown>) {
    if (streaming.value || toolRunning.value || !active) return
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
    toolController?.abort()
    controller = null
    toolController = null
    streaming.value = false
    toolRunning.value = null
    messages.value = []
    error.value = null
    persist()
  }

  function dispose() {
    disposed = true
    controller?.abort()
    toolsController?.abort()
    toolController?.abort()
    controller = null
    toolsController = null
    toolController = null
  }

  function persist() {
    const completeMessages = messages.value.filter((message) => message.content.trim())
    messages.value = completeMessages.slice(-maximumStoredMessages)
    const storedMessages = boundedMessages(messages.value.map(normalizeHistoryMessage)).map(
      storableMessage,
    )
    try {
      sessionStorage.setItem(storageKey, JSON.stringify(storedMessages))
    } catch (cause) {
      console.warn('Garry could not save this conversation for reload.', cause)
    }
  }

  return {
    messages: readonly(messages),
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
    dispose,
    setToolsEnabled,
    loadTools,
    runTool,
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
    (message.featureKey === undefined || typeof message.featureKey === 'string') &&
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

function createMessage(
  role: AssistantMessage['role'],
  content: string,
  featureKey: string,
): AssistantMessage {
  return { id: crypto.randomUUID(), role, content, featureKey }
}
