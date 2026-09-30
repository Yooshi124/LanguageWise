import { readonly, ref } from 'vue';
import {
  AssistantToolsUnavailableError,
  callAssistantTool,
  listAssistantTools,
  streamAssistantMessage
} from '../api/assistant.js';

const maximumStoredMessages = 12;
const maximumConversationCharacters = 12000;
const maximumHistoryMessageCharacters = 12000;
const maximumStoredToolResultCharacters = 20000;
const messages = ref([]);
const expanded = ref(false);
const streaming = ref(false);
const error = ref(null);
const toolsEnabled = ref(false);
const tools = ref([]);
const toolsLoading = ref(false);
const toolsError = ref(null);
const toolRunning = ref(null);
let activeUserId = null;
let controller = null;

function storageKey(userId) {
  return `languagewise:assistant:v1:user:${userId}`;
}

function toolsStorageKey(userId) {
  return `${storageKey(userId)}:tools`;
}

function initialize(userId) {
  if (activeUserId === userId) return;
  controller?.abort();
  activeUserId = userId;
  streaming.value = false;
  error.value = null;
  messages.value = loadMessages(userId);
  tools.value = [];
  toolsError.value = null;
  toolRunning.value = null;
  toolsEnabled.value = sessionStorage.getItem(toolsStorageKey(userId)) === 'on';
  if (toolsEnabled.value) void loadTools();
}

async function send(content, context) {
  const trimmed = content.trim();
  if (!trimmed || streaming.value || activeUserId === null) return;

  error.value = null;
  const history = boundedHistory(messages.value, trimmed.length);
  const userMessage = createMessage('user', trimmed);
  const assistantMessage = createMessage('assistant', '');
  messages.value.push(userMessage, assistantMessage);
  streaming.value = true;
  controller = new AbortController();
  const requestController = controller;

  try {
    await streamAssistantMessage(
      {
        message: trimmed,
        history,
        context
      },
      {
        onDelta: (delta) => {
          const index = messages.value.findIndex(
            (message) => message.id === assistantMessage.id
          );
          const current = messages.value[index];
          if (index >= 0 && current) {
            messages.value[index] = {
              ...current,
              content: current.content + delta
            };
          }
        },
        onTool: (result) => {
          updateMessage(assistantMessage.id, (current) => ({
            ...current,
            toolResults: [...(current.toolResults ?? []), result]
          }));
        },
        onDone: () => {
          persist();
        }
      },
      requestController.signal
    );
  } catch (cause) {
    messages.value = messages.value.filter((message) => message.id !== assistantMessage.id);
    persist();
    if (!(cause instanceof DOMException && cause.name === 'AbortError')) {
      error.value = cause instanceof Error ? cause.message : 'Garry could not respond.';
    }
  } finally {
    if (controller === requestController) {
      controller = null;
      streaming.value = false;
    }
  }
}

async function retry(context) {
  const last = messages.value.at(-1);
  if (!last || last.role !== 'user' || streaming.value) return;
  messages.value.pop();
  persist();
  await send(last.content, context);
}

function updateMessage(id, update) {
  const index = messages.value.findIndex((message) => message.id === id);
  const current = messages.value[index];
  if (index >= 0 && current) {
    messages.value[index] = update(current);
  }
}

async function setToolsEnabled(enabled) {
  toolsEnabled.value = enabled;
  if (activeUserId !== null) {
    sessionStorage.setItem(toolsStorageKey(activeUserId), enabled ? 'on' : 'off');
  }
  if (enabled && tools.value.length === 0) {
    await loadTools();
  }
}

async function loadTools() {
  if (toolsLoading.value) return;
  toolsLoading.value = true;
  toolsError.value = null;
  try {
    tools.value = await listAssistantTools();
  } catch (cause) {
    tools.value = [];
    toolsError.value =
      cause instanceof AssistantToolsUnavailableError && cause.disabled
        ? "Garry's tools are turned off on this server."
        : cause instanceof Error
          ? cause.message
          : "Garry's tools are unavailable right now.";
  } finally {
    toolsLoading.value = false;
  }
}

async function runTool(tool, args) {
  if (streaming.value || toolRunning.value || activeUserId === null) return;
  error.value = null;
  toolRunning.value = tool.name;
  try {
    const result = await callAssistantTool(tool.name, args);
    messages.value.push({
      ...createMessage(
        'assistant',
        result.isError
          ? `I couldn't use **${tool.title}** just now.`
          : `Here's what I found with **${tool.title}**.`
      ),
      toolResults: [result]
    });
    persist();
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : 'Garry could not use that tool.';
  } finally {
    toolRunning.value = null;
  }
}

function cancel() {
  controller?.abort();
}

function clear() {
  controller?.abort();
  messages.value = [];
  error.value = null;
  persist();
}

function persist() {
  if (activeUserId === null) return;
  const completeMessages = messages.value.filter((message) => message.content.trim());
  messages.value = completeMessages.slice(-maximumStoredMessages);
  const storedMessages = boundedMessages(
    messages.value.map(normalizeHistoryMessage)
  ).map(storableMessage);
  sessionStorage.setItem(storageKey(activeUserId), JSON.stringify(storedMessages));
}

function loadMessages(userId) {
  const stored = sessionStorage.getItem(storageKey(userId));
  if (!stored) return [];

  try {
    const value = JSON.parse(stored);
    if (!Array.isArray(value)) return [];
    const valid = value.filter(isAssistantMessage).slice(-maximumStoredMessages);
    return boundedMessages(valid);
  } catch {
    sessionStorage.removeItem(storageKey(userId));
    return [];
  }
}

function isAssistantMessage(value) {
  if (typeof value !== 'object' || value === null) return false;
  return (
    typeof value.id === 'string' &&
    (value.role === 'user' || value.role === 'assistant') &&
    typeof value.content === 'string' &&
    value.content.trim().length > 0 &&
    value.content.length <= maximumHistoryMessageCharacters
  );
}

function boundedHistory(source, nextMessageCharacters) {
  return boundedMessages(
    source.map(normalizeHistoryMessage),
    maximumConversationCharacters - nextMessageCharacters
  ).map(({ role, content }) => ({ role, content }));
}

function normalizeHistoryMessage(message) {
  return message.content.length <= maximumHistoryMessageCharacters
    ? message
    : {
        ...message,
        content: message.content.slice(0, maximumHistoryMessageCharacters)
      };
}

function boundedMessages(source, characterLimit = maximumConversationCharacters) {
  const selected = [];
  let characters = 0;

  for (let index = source.length - 1; index >= 0; index--) {
    const message = source[index];
    if (!message || selected.length >= maximumStoredMessages) break;
    if (characters + message.content.length > characterLimit) break;
    selected.unshift(message);
    characters += message.content.length;
  }

  return selected;
}

function createMessage(role, content) {
  return {
    id: crypto.randomUUID(),
    role,
    content
  };
}

function storableMessage(message) {
  if (!message.toolResults?.length) return message;
  return JSON.stringify(message.toolResults).length <= maximumStoredToolResultCharacters
    ? message
    : { id: message.id, role: message.role, content: message.content };
}

export function useAssistant(userId) {
  initialize(userId);
  return {
    messages: readonly(messages),
    expanded,
    streaming: readonly(streaming),
    error: readonly(error),
    send,
    retry,
    cancel,
    clear,
    tools: readonly(tools),
    toolsEnabled: readonly(toolsEnabled),
    toolsLoading: readonly(toolsLoading),
    toolsError: readonly(toolsError),
    toolRunning: readonly(toolRunning),
    setToolsEnabled,
    loadTools,
    runTool
  };
}
