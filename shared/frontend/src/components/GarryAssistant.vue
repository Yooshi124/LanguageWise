<script setup lang="ts">
import DOMPurify from 'dompurify'
import MarkdownIt from 'markdown-it'
import { computed, nextTick, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  mdiBookSearchOutline,
  mdiDeleteOutline,
  mdiMinus,
  mdiRefresh,
  mdiSend,
  mdiStopCircleOutline,
  mdiToolboxOutline,
} from '@mdi/js'
import garryImage from '../assets/garry.png'
import GarryDocsSearch from './GarryDocsSearch.vue'
import GarryToolResultCard from './GarryToolResultCard.vue'
import { useGarryAssistant } from '../composables/useGarryAssistant'
import type { FeatureAssistant, FeatureAssistantTool } from '../federation/featureAssistants'

const props = defineProps<{
  featureKey: string
  userId: number
  assistant: FeatureAssistant
}>()

const route = useRoute()
const router = useRouter()
const garry = useGarryAssistant()
const draft = ref('')
const docsSearchVisible = ref(false)
const messageList = ref<HTMLElement | null>(null)
const composer = ref<HTMLTextAreaElement | null>(null)
const markdown = new MarkdownIt({ html: false, linkify: true, typographer: true })

garry.activate(props.featureKey, props.userId, props.assistant, () => {
  void router.push({ path: '/login', query: { returnUrl: route.fullPath } })
})

const context = computed(() => props.assistant.context(route))
const suggestions = computed(() => props.assistant.suggestions(route))

const toolChips = computed(() =>
  (props.assistant.tools?.chips ?? [])
    .map((chip) => ({
      chip,
      tool: garry.tools.value.find((tool) => tool.name === chip.tool),
      unavailable: chip.unavailable?.(route) ?? null,
    }))
    .filter((entry) => entry.tool !== undefined),
)

const toolsBusy = computed(() => garry.streaming.value || garry.toolRunning.value !== null)

function toolLabel(name: string) {
  const chips = props.assistant.tools?.chips.filter((chip) => chip.tool === name) ?? []
  if (chips.length === 1) return chips[0]!.label
  return garry.tools.value.find((tool) => tool.name === name)?.title ?? name
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
  if (!message || garry.streaming.value) return
  draft.value = ''
  await garry.send(message, context.value)
  await nextTick()
  composer.value?.focus()
}

function onComposerKeydown(event: KeyboardEvent) {
  if (event.key === 'Enter' && !event.shiftKey) {
    event.preventDefault()
    void submit()
  }
}

function minimize() {
  garry.expanded.value = false
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
  () => garry.expanded.value,
  async (isExpanded) => {
    if (!isExpanded) return
    await nextTick()
    composer.value?.focus()
  },
)
</script>

<template>
  <aside class="garry-assistant" aria-label="Garry the LanguageWise assistant">
    <Transition name="garry-panel">
      <section v-if="garry.expanded.value" class="garry-panel" @keydown.esc="minimize">
        <header class="garry-header">
          <img :src="garryImage" alt="" class="garry-header-image" />
          <div>
            <strong>Garry</strong>
            <span>Hi, I’m Garry and I’m here to help you learn!</span>
          </div>
          <v-btn
            :icon="mdiBookSearchOutline"
            variant="text"
            size="small"
            aria-label="Ask the docs"
            title="Ask the docs"
            @click="docsSearchVisible = true"
          />
          <v-btn
            v-if="assistant.tools"
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
            :icon="mdiMinus"
            variant="text"
            size="small"
            aria-label="Minimize Garry"
            @click="minimize"
          />
        </header>

        <div class="garry-tools">
          <template v-if="assistant.tools && garry.toolsEnabled.value">
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
            <p>{{ assistant.welcome }}</p>
            <div class="garry-suggestions" aria-label="Suggested questions">
              <button
                v-for="suggestion in suggestions"
                :key="suggestion"
                type="button"
                :disabled="garry.streaming.value"
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
            <GarryToolResultCard
              v-for="(toolResult, index) in message.toolResults ?? []"
              :key="`${message.id}-tool-${index}`"
              :result="toolResult"
              :label="toolLabel(toolResult.tool)"
              :present="assistant.tools?.view"
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
            :placeholder="assistant.placeholder"
            aria-label="Message Garry"
            :disabled="garry.streaming.value"
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
            :disabled="!draft.trim()"
          />
        </form>
        <p class="garry-disclaimer">Garry can make mistakes. Check important answers.</p>
      </section>
    </Transition>

    <button
      v-if="!garry.expanded.value"
      type="button"
      class="garry-launcher"
      aria-label="Open Garry the LanguageWise assistant"
      @click="garry.expanded.value = true"
    >
      <img :src="garryImage" alt="" />
      <span>Ask Garry</span>
    </button>

    <GarryDocsSearch :visible="docsSearchVisible" @close="docsSearchVisible = false" />
  </aside>
</template>
