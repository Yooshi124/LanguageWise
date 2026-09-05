import { readonly, ref } from 'vue'
import { AssistantApiError, streamAssistantMessage } from '../api/assistant'
import type { AssistantMessage, AssistantRouteContext } from '../models'

const maximumStoredMessages = 12
const maximumConversationCharacters = 12000
const messages = ref<AssistantMessage[]>([])
const expanded = ref(false)
const streaming = ref(false)
const error = ref<string | null>(null)
let activeUserId: number | null = null
let controller: AbortController | null = null

function storageKey(userId: number) {
  return `languagewise:quests-achievements:assistant:v1:user:${userId}`
}

function initialize(userId: number) {
  if (activeUserId === userId) return
  controller?.abort()
  activeUserId = userId
  streaming.value = false
  error.value = null
  messages.value = loadMessages(userId)
}

async function send(
  content: string,
  context: AssistantRouteContext,
  onUnauthorized: () => void,
) {
  const trimmed = content.trim()
  if (!trimmed || streaming.value || activeUserId === null) return

  error.value = null
  const history = boundedMessages(messages.value, maximumConversationCharacters - trimmed.length)
    .map(({ role, content: historyContent }) => ({ role, content: historyContent }))
  const userMessage = createMessage('user', trimmed)
  const assistantMessage = createMessage('assistant', '')
  messages.value.push(userMessage, assistantMessage)
  streaming.value = true
  controller = new AbortController()
  const requestController = controller

  try {
    await streamAssistantMessage(
      { message: trimmed, history, context },
      {
        onDelta: (delta) => {
          const message = messages.value.find((item) => item.id === assistantMessage.id)
          if (message) message.content += delta
        },
        onDone: persist,
      },
      requestController.signal,
    )
  } catch (cause) {
    messages.value = messages.value.filter((message) =>
      message.id !== assistantMessage.id || message.content.trim())
    persist()
    if (cause instanceof AssistantApiError && cause.status === 401) onUnauthorized()
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

async function retry(context: AssistantRouteContext, onUnauthorized: () => void) {
  let lastUserIndex = -1
  for (let index = messages.value.length - 1; index >= 0; index--) {
    if (messages.value[index]?.role === 'user') {
      lastUserIndex = index
      break
    }
  }
  if (lastUserIndex < 0 || streaming.value) return
  const content = messages.value[lastUserIndex].content
  messages.value = messages.value.slice(0, lastUserIndex)
  persist()
  await send(content, context, onUnauthorized)
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
  messages.value = boundedMessages(messages.value.filter((message) => message.content.trim()))
  sessionStorage.setItem(storageKey(activeUserId), JSON.stringify(messages.value))
}

function loadMessages(userId: number) {
  const stored = sessionStorage.getItem(storageKey(userId))
  if (!stored) return []

  try {
    const value: unknown = JSON.parse(stored)
    if (!Array.isArray(value)) return []
    return boundedMessages(value.filter(isAssistantMessage))
  } catch {
    sessionStorage.removeItem(storageKey(userId))
    return []
  }
}

function isAssistantMessage(value: unknown): value is AssistantMessage {
  if (typeof value !== 'object' || value === null) return false
  const message = value as Record<string, unknown>
  return typeof message.id === 'string'
    && (message.role === 'user' || message.role === 'assistant')
    && typeof message.content === 'string'
    && message.content.trim().length > 0
    && message.content.length <= maximumConversationCharacters
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

export function useGarryAssistant(userId: number) {
  initialize(userId)
  return {
    messages: readonly(messages),
    expanded,
    streaming: readonly(streaming),
    error: readonly(error),
    send,
    retry,
    cancel,
    clear,
  }
}