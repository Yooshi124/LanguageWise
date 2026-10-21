<script setup lang="ts">
import DOMPurify from 'dompurify'
import MarkdownIt from 'markdown-it'
import { computed, nextTick, onBeforeUnmount, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  mdiBookSearchOutline,
  mdiChatPlusOutline,
  mdiClose,
  mdiDeleteOutline,
  mdiFullscreen,
  mdiFullscreenExit,
  mdiMinus,
  mdiRefresh,
  mdiSend,
  mdiStopCircleOutline,
  mdiToolboxOutline,
} from '@mdi/js'
import garryImage from '../assets/garry.png'
import GarryToolResultCard from './GarryToolResultCard.vue'
import { useGarryAssistant } from '../composables/useGarryAssistant'
import { useAdminSettings } from '../composables/useAdminSettings'
import {
  featureAssistants,
  type FeatureAssistant,
  type FeatureAssistantTool,
} from '../federation/featureAssistants'

const props = defineProps<{
  featureKey?: string
  userId: number
  assistant?: FeatureAssistant
  sessionId: string
  number: number
  minimized: boolean
  order: number
  maximizedBounds: { left: number; top: number; width: number; height: number }
}>()
const emit = defineEmits<{ minimize: []; close: []; focus: []; newChat: []; docs: [] }>()

const route = useRoute()
const router = useRouter()
const garry = useGarryAssistant(props.userId, props.sessionId, () => {
  void router.push({ path: '/login', query: { returnUrl: route.fullPath } })
})
const adminSettings = useAdminSettings()
const draft = ref('')
const messageList = ref<HTMLElement | null>(null)
const composer = ref<HTMLTextAreaElement | null>(null)
const fullscreen = ref(false)
const geometry = reactive({
  left: window.innerWidth - 464,
  top: window.innerHeight - 738,
  width: 440,
  height: 650,
})
type InteractionKind = 'move' | 'resize' | 'resize-bottom-right'
let interaction: {
  pointerId: number
  kind: InteractionKind
  x: number
  y: number
  geometry: typeof geometry
} | null = null
const markdown = new MarkdownIt({ html: false, linkify: true, typographer: true })

watch(
  () => [props.featureKey, props.assistant] as const,
  ([featureKey, assistant]) => garry.activate(featureKey, assistant),
  { immediate: true },
)

const context = computed(() => props.assistant?.context(route) ?? {})
const suggestions = computed(() => props.assistant?.suggestions(route) ?? [])
const panelStyle = computed(() => {
  const bounds = fullscreen.value ? props.maximizedBounds : geometry
  return {
    left: `${bounds.left}px`,
    top: `${bounds.top}px`,
    width: `${bounds.width}px`,
    height: `${bounds.height}px`,
    zIndex: props.order,
  }
})

const toolChips = computed(() =>
  (props.assistant?.tools?.chips ?? [])
    .map((chip) => ({
      chip,
      tool: garry.tools.value.find((tool) => tool.name === chip.tool),
      unavailable: chip.unavailable?.(route) ?? null,
    }))
    .filter((entry) => entry.tool !== undefined),
)

const toolsBusy = computed(() => garry.streaming.value || garry.toolRunning.value !== null)

function messageAssistant(featureKey?: string) {
  return featureKey === props.featureKey ? props.assistant : featureAssistants.get(featureKey ?? '')
}

function toolLabel(name: string, featureKey?: string) {
  const chips = messageAssistant(featureKey)?.tools?.chips.filter((chip) => chip.tool === name) ?? []
  if (chips.length === 1) return chips[0]!.label
  return name
}

async function runTool(chip: FeatureAssistantTool) {
  const tool = garry.tools.value.find((item) => item.name === chip.tool)
  if (!tool || chip.unavailable?.(route)) return
  await garry.runTool(tool, chip.arguments(route))
}

function render(content: string) {
  return DOMPurify.sanitize(markdown.render(content))
}

async function submit(content = draft.value) {
  const message = content.trim()
  if (!message || toolsBusy.value || !props.assistant) return
  draft.value = ''
  await garry.send(message, context.value)
  await nextTick()
  if (!props.minimized && composer.value?.closest('.garry-panel')?.contains(document.activeElement)) {
    composer.value.focus()
  }
}

function onComposerKeydown(event: KeyboardEvent) {
  if (event.key === 'Enter' && !event.shiftKey) {
    event.preventDefault()
    void submit()
  }
}

function fitToViewport() {
  const bottom = props.maximizedBounds.top + props.maximizedBounds.height
  const width = Math.max(1, window.innerWidth - 32)
  const height = Math.max(1, bottom - 16)
  geometry.width = Math.min(Math.max(340, geometry.width), width)
  geometry.height = Math.min(Math.max(320, geometry.height), height)
  geometry.left = Math.max(16, Math.min(geometry.left, window.innerWidth - geometry.width - 16))
  geometry.top = Math.max(16, Math.min(geometry.top, bottom - geometry.height))
}

function resizeBottomRight(width: number, height: number) {
  const bottom = props.maximizedBounds.top + props.maximizedBounds.height
  geometry.width = Math.min(Math.max(340, width), window.innerWidth - geometry.left - 16)
  geometry.height = Math.min(Math.max(320, height), bottom - geometry.top)
}

function startInteraction(event: PointerEvent, kind: InteractionKind) {
  if (fullscreen.value || event.button !== 0) return
  event.preventDefault()
  emit('focus')
  interaction = {
    pointerId: event.pointerId,
    kind,
    x: event.clientX,
    y: event.clientY,
    geometry: { ...geometry },
  }
  if (event.currentTarget instanceof HTMLElement) {
    event.currentTarget.setPointerCapture(event.pointerId)
  }
}

function moveInteraction(event: PointerEvent) {
  if (!interaction || interaction.pointerId !== event.pointerId) return
  const dx = event.clientX - interaction.x
  const dy = event.clientY - interaction.y
  if (interaction.kind === 'move') {
    geometry.left = interaction.geometry.left + dx
    geometry.top = interaction.geometry.top + dy
  } else if (interaction.kind === 'resize-bottom-right') {
    resizeBottomRight(interaction.geometry.width + dx, interaction.geometry.height + dy)
  } else {
    const right = interaction.geometry.left + interaction.geometry.width
    const bottom = interaction.geometry.top + interaction.geometry.height
    geometry.width = Math.min(Math.max(340, interaction.geometry.width - dx), right - 16)
    geometry.height = Math.min(Math.max(320, interaction.geometry.height - dy), bottom - 16)
    geometry.left = right - geometry.width
    geometry.top = bottom - geometry.height
  }
  fitToViewport()
}

function endInteraction() {
  interaction = null
}

function keyboardGeometry(event: KeyboardEvent, kind: InteractionKind) {
  if (fullscreen.value || !['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown'].includes(event.key)) return
  event.preventDefault()
  const delta = event.shiftKey ? 40 : 10
  const horizontal = event.key === 'ArrowLeft' || event.key === 'ArrowRight'
  const amount = event.key === 'ArrowLeft' || event.key === 'ArrowUp' ? -delta : delta
  if (kind === 'move') {
    if (horizontal) geometry.left += amount
    else geometry.top += amount
  } else if (kind === 'resize-bottom-right') {
    resizeBottomRight(
      geometry.width + (horizontal ? amount : 0),
      geometry.height + (horizontal ? 0 : amount),
    )
  } else {
    if (horizontal) geometry.width += amount
    else geometry.height += amount
  }
  fitToViewport()
}

function toggleFullscreen() {
  interaction = null
  fullscreen.value = !fullscreen.value
  emit('focus')
}

function escape() {
  if (fullscreen.value) fullscreen.value = false
  else emit('minimize')
}

function close() {
  garry.dispose()
  emit('close')
}

watch(
  () =>
    garry.messages.value
      .map((message) => `${message.content}\u0001${message.toolResults?.length ?? 0}`)
      .join('\u0000'),
  async () => {
    await nextTick()
    if (messageList.value) messageList.value.scrollTop = messageList.value.scrollHeight
  },
)

watch(
  () => props.minimized,
  async (minimized) => {
    if (minimized) return
    await nextTick()
    composer.value?.focus()
  },
  { immediate: true },
)

watch(() => props.maximizedBounds, fitToViewport, { immediate: true })
onBeforeUnmount(() => {
  garry.dispose()
})
</script>

<template>
      <section
        v-show="!minimized"
        class="garry-panel"
        :class="{ 'garry-panel-fullscreen': fullscreen }"
        :style="panelStyle"
        role="dialog"
        :aria-label="`Garry chat ${number}`"
        @pointerdown="emit('focus')"
        @focusin="emit('focus')"
        @keydown.esc.stop="escape"
      >
        <header class="garry-header">
          <button
            type="button"
            class="garry-drag-handle"
            :aria-label="`Move Garry chat ${number}; use arrow keys to move`"
            :disabled="fullscreen"
            @pointerdown="startInteraction($event, 'move')"
            @pointermove="moveInteraction"
            @pointerup="endInteraction"
            @pointercancel="endInteraction"
            @keydown="keyboardGeometry($event, 'move')"
          >
            <img :src="garryImage" alt="" class="garry-header-image" />
            <span>
              <strong>Garry · Chat {{ number }}</strong>
              <span>{{ featureKey ? `Context: ${featureKey}` : 'Open a feature to continue chatting' }}</span>
            </span>
          </button>
          <div class="garry-window-controls">
          <v-btn
            :icon="mdiChatPlusOutline"
            variant="text"
            size="small"
            aria-label="New Garry chat"
            title="New independent chat"
            @click="emit('newChat')"
          />
          <v-btn
            v-if="adminSettings.rag.value"
            :icon="mdiBookSearchOutline"
            variant="text"
            size="small"
            aria-label="Ask the docs"
            title="Ask the docs"
            @click="emit('docs')"
          />
          <v-btn
            v-if="assistant?.tools && adminSettings.mcp.value"
            :icon="mdiToolboxOutline"
            :variant="garry.toolsEnabled.value ? 'tonal' : 'text'"
            size="small"
            :aria-pressed="garry.toolsEnabled.value"
            :aria-label="garry.toolsEnabled.value ? 'Hide Garry’s tools' : 'Show Garry’s tools'"
            :title="garry.toolsEnabled.value ? 'Hide tools' : 'Show tools'"
            @click="garry.setToolsEnabled(!garry.toolsEnabled.value)"
          />
          <v-btn
            :icon="mdiDeleteOutline"
            variant="text"
            size="small"
            aria-label="Clear conversation"
            :disabled="garry.messages.value.length === 0"
            @click="garry.clear"
          />
          <v-btn
            :icon="fullscreen ? mdiFullscreenExit : mdiFullscreen"
            variant="text"
            size="small"
            :aria-label="fullscreen ? 'Restore Garry window' : 'Maximize Garry'"
            :title="fullscreen ? 'Restore window size' : 'Fill the main content area'"
            :aria-pressed="fullscreen"
            @click="toggleFullscreen"
          />
          <v-btn
            :icon="mdiMinus"
            variant="text"
            size="small"
            aria-label="Minimize Garry"
            @click="emit('minimize')"
          />
          <v-btn
            :icon="mdiClose"
            variant="text"
            size="small"
            aria-label="Close Garry chat"
            title="Close this chat"
            @click="close"
          />
          </div>
        </header>

        <div class="garry-tools">
          <template v-if="assistant?.tools && adminSettings.mcp.value && garry.toolsEnabled.value">
            <p v-if="garry.toolsLoading.value" class="garry-tools-status">Loading tools…</p>
            <p v-else-if="garry.toolsError.value" class="garry-tools-status">
              {{ garry.toolsError.value }}
              <button type="button" @click="garry.loadTools()">Retry</button>
            </p>
            <div v-else class="garry-tool-chips" role="group" aria-label="Garry’s tools">
              <button
                v-for="entry in toolChips"
                :key="`${entry.chip.tool}:${entry.chip.label}`"
                type="button"
                :disabled="toolsBusy || entry.unavailable !== null"
                :title="entry.unavailable ?? entry.tool?.description"
                :aria-busy="garry.toolRunning.value === entry.chip.tool"
                @click="runTool(entry.chip)"
              >
                {{ entry.chip.label }}
              </button>
            </div>
          </template>
        </div>

        <div ref="messageList" class="garry-messages">
          <div v-if="garry.messages.value.length === 0" class="garry-welcome">
            <img :src="garryImage" alt="Garry the LanguageWise assistant" />
            <h2>Hi, I’m Garry!</h2>
            <p>{{ assistant?.welcome ?? 'Open a feature to start chatting. Your conversations stay here as you navigate.' }}</p>
            <div class="garry-suggestions" aria-label="Suggested questions">
              <button
                v-for="suggestion in suggestions"
                :key="suggestion"
                type="button"
                :disabled="toolsBusy"
                @click="submit(suggestion)"
              >
                {{ suggestion }}
              </button>
            </div>
          </div>

          <div
            v-for="message in garry.messages.value"
            :key="message.id"
            class="garry-message"
            :class="`garry-message-${message.role}`"
          >
            <span class="sr-only">{{ message.role === 'assistant' ? 'Garry' : 'You' }}:</span>
            <span v-if="message.featureKey" class="garry-message-context">{{ message.featureKey }}</span>
            <GarryToolResultCard
              v-for="(toolResult, index) in message.toolResults ?? []"
              :key="`${message.id}-tool-${index}`"
              :result="toolResult"
              :label="toolLabel(toolResult.tool, message.featureKey)"
              :present="messageAssistant(message.featureKey)?.tools?.view"
            />
            <div
              v-if="message.content"
              class="garry-message-content"
              v-html="render(message.content)"
            />
            <div v-else class="garry-typing" aria-hidden="true">
              <span /><span /><span />
            </div>
            <p v-if="message.fallback" class="garry-fallback-note">
              Answered from the help pages — the AI model is offline.
            </p>
          </div>
        </div>

        <div class="sr-only" role="status" aria-live="polite">
          {{ garry.streaming.value ? 'Garry is writing a response.' : '' }}
        </div>

        <v-alert
          v-if="garry.error.value"
          type="error"
          variant="tonal"
          density="compact"
          class="garry-error"
        >
          {{ garry.error.value }}
          <template #append>
            <v-btn
              :icon="mdiRefresh"
              variant="text"
              size="small"
              aria-label="Retry last message"
              :disabled="toolsBusy || !assistant"
              @click="garry.retry(context)"
            />
          </template>
        </v-alert>

        <form class="garry-composer" @submit.prevent="submit()">
          <textarea
            ref="composer"
            v-model="draft"
            rows="1"
            maxlength="4000"
            :placeholder="assistant?.placeholder ?? 'Open a feature to continue chatting…'"
            aria-label="Message Garry"
            :disabled="toolsBusy || !assistant"
            @keydown="onComposerKeydown"
          />
          <v-btn
            v-if="garry.streaming.value"
            :icon="mdiStopCircleOutline"
            color="primary"
            variant="text"
            aria-label="Stop Garry’s response"
            @click="garry.cancel"
          />
          <v-btn
            v-else
            :icon="mdiSend"
            color="primary"
            variant="flat"
            aria-label="Send message"
            type="submit"
            :disabled="!draft.trim() || toolsBusy || !assistant"
          />
        </form>
        <p class="garry-disclaimer">Garry can make mistakes. Check important answers.</p>
        <button
          v-if="!fullscreen"
          type="button"
          class="garry-resize-handle"
          aria-label="Resize Garry chat; use arrow keys to resize"
          title="Drag the upper-left corner to resize, or use arrow keys"
          @pointerdown="startInteraction($event, 'resize')"
          @pointermove="moveInteraction"
          @pointerup="endInteraction"
          @pointercancel="endInteraction"
          @keydown="keyboardGeometry($event, 'resize')"
        />
        <button
          v-if="!fullscreen"
          type="button"
          class="garry-resize-handle garry-resize-handle-bottom-right"
          aria-label="Resize Garry chat from bottom right; use arrow keys to resize"
          title="Drag the bottom-right corner to resize, or use arrow keys"
          @pointerdown="startInteraction($event, 'resize-bottom-right')"
          @pointermove="moveInteraction"
          @pointerup="endInteraction"
          @pointercancel="endInteraction"
          @keydown="keyboardGeometry($event, 'resize-bottom-right')"
        />
      </section>
</template>
