import { readonly, ref } from 'vue'
import { streamAssistantMessage } from '../api/assistant'
import type {
  AssistantMessage,
  AssistantRouteContext,
} from '../models/api'

const maximumStoredMessages = 12
const maximumConversationCharacters = 12000
const maximumHistoryMessageCharacters = 12000
const messages = ref<AssistantMessage[]>([])
const expanded = ref(false)
const streaming = ref(false)
const error = ref<string | null>(null)
let activeUserId: number | null = null
let controller: AbortController | null = null

function storageKey(userId: number) {
  return `languagewise:garry:v1:user:${userId}`
}

function initialize(userId: number) {
  if (activeUserId === userId) return
  controller?.abort()
  activeUserId = userId
  streaming.value = false
  error.value = null
  messages.value = loadMessages(userId)
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
  )
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

function isAssistantMessage(value: unknown): value is AssistantMessage {
  if (typeof value !== 'object' || value === null) return false
  const message = value as Record<string, unknown>
  return (
    typeof message.id === 'string' &&
    (message.role === 'user' || message.role === 'assistant') &&
    typeof message.content === 'string' &&
    message.content.trim().length > 0 &&
    message.content.length <= maximumHistoryMessageCharacters
  )
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
    send,
    retry,
    cancel,
    clear,
  }
}
