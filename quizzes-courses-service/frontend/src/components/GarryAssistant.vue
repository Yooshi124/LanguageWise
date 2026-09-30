<script setup lang="ts">
import DOMPurify from 'dompurify'
import MarkdownIt from 'markdown-it'
import { computed, nextTick, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import {
  mdiDeleteOutline,
  mdiMinus,
  mdiRefresh,
  mdiSend,
  mdiStopCircleOutline,
  mdiToolboxOutline,
} from '@mdi/js'
import ToolResultCard from './ToolResultCard.vue'
import {
  assistantToolChips,
  missingRequirement,
  type AssistantToolChip,
  type AssistantToolContext,
} from '../config/assistantTools'
import { useGarryAssistant } from '../composables/useGarryAssistant'
import type { AssistantRouteContext } from '../models/api'

const props = defineProps<{ userId: number }>()
const route = useRoute()
const assistant = useGarryAssistant(props.userId)
const draft = ref('')
const messageList = ref<HTMLElement | null>(null)
const composer = ref<HTMLTextAreaElement | null>(null)
const markdown = new MarkdownIt({ html: false, linkify: true, typographer: true })
const garryImage = '/remotes/quizzes-courses/images/garry.png'

const context = computed<AssistantRouteContext>(() => ({
  routeName: route.name === 'quizzes-courses-home' ? 'home' : String(route.name ?? 'home'),
  ...(typeof route.params.courseCode === 'string'
    ? { courseCode: route.params.courseCode }
    : {}),
  ...(typeof route.params.lessonSlug === 'string'
    ? { lessonSlug: route.params.lessonSlug }
    : {}),
}))

const toolContext = computed<AssistantToolContext>(() => ({
  courseCode: context.value.courseCode?.toLowerCase(),
  lessonSlug: context.value.lessonSlug,
}))

const toolChips = computed(() =>
  assistantToolChips
    .map((chip) => ({
      chip,
      tool: assistant.tools.value.find((tool) => tool.name === chip.tool),
      unavailable: missingRequirement(chip, toolContext.value),
    }))
    .filter((entry) => entry.tool !== undefined),
)

const toolsBusy = computed(
  () => assistant.streaming.value || assistant.toolRunning.value !== null,
)

async function runTool(chip: AssistantToolChip) {
  const tool = assistant.tools.value.find((item) => item.name === chip.tool)
  if (!tool || missingRequirement(chip, toolContext.value)) return
  await assistant.runTool(tool, chip.arguments(toolContext.value))
}

const suggestions = computed(() => {
  if (route.name === 'lesson') {
    return [
      'Explain the main idea in this lesson.',
      'Give me a short practice example.',
      'Help me remember this vocabulary.',
    ]
  }
  if (route.name === 'quiz-list' || route.name === 'quizzes') {
    return [
      'How should I prepare for a quiz?',
      'Which language skills do these quizzes practise?',
    ]
  }
  return [
    'What can I learn on LanguageWise?',
    'Help me choose a course.',
    'How can I build a study routine?',
  ]
})

function render(content: string) {
  return DOMPurify.sanitize(markdown.render(content))
}

async function submit(content = draft.value) {
  const message = content.trim()
  if (!message || assistant.streaming.value) return
  draft.value = ''
  await assistant.send(message, context.value)
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
  assistant.expanded.value = false
}

watch(
  () =>
    assistant.messages.value
      .map((message) => `${message.content}\u0001${message.toolResults?.length ?? 0}`)
      .join('\u0000'),
  async () => {
    await nextTick()
    if (messageList.value) {
      messageList.value.scrollTop = messageList.value.scrollHeight
    }
  },
)

watch(
  () => assistant.expanded.value,
  async (isExpanded) => {
    if (!isExpanded) return
    await nextTick()
    composer.value?.focus()
  },
)
</script>

<template>
  <aside class="garry-assistant" aria-label="Garry language learning assistant">
    <Transition name="garry-panel">
      <section v-if="assistant.expanded.value" class="garry-panel">
        <header class="garry-header">
          <img :src="garryImage" alt="" class="garry-header-image" />
          <div>
            <strong>Garry</strong>
            <span>Hi, I’m Garry and I’m here to help you learn!</span>
          </div>
          <v-btn
            :icon="mdiToolboxOutline"
            :variant="assistant.toolsEnabled.value ? 'tonal' : 'text'"
            size="small"
            :aria-pressed="assistant.toolsEnabled.value"
            :aria-label="assistant.toolsEnabled.value ? 'Hide Garry’s tools' : 'Show Garry’s tools'"
            :title="assistant.toolsEnabled.value ? 'Hide tools' : 'Show tools'"
            @click="assistant.setToolsEnabled(!assistant.toolsEnabled.value)"
          />
          <v-btn
            :icon="mdiDeleteOutline"
            variant="text"
            size="small"
            aria-label="Clear conversation"
            :disabled="assistant.messages.value.length === 0"
            @click="assistant.clear"
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
          <template v-if="assistant.toolsEnabled.value">
            <p v-if="assistant.toolsLoading.value" class="garry-tools-status">
              Loading tools…
            </p>
            <p v-else-if="assistant.toolsError.value" class="garry-tools-status">
              {{ assistant.toolsError.value }}
              <button type="button" @click="assistant.loadTools()">Retry</button>
            </p>
            <div v-else class="garry-tool-chips" role="group" aria-label="Garry’s tools">
              <button
                v-for="entry in toolChips"
                :key="entry.chip.tool"
                type="button"
                :disabled="toolsBusy || entry.unavailable !== null"
                :title="entry.unavailable ?? entry.tool?.description"
                :aria-busy="assistant.toolRunning.value === entry.chip.tool"
                @click="runTool(entry.chip)"
              >
                {{ entry.chip.label }}
              </button>
            </div>
          </template>
        </div>

        <div ref="messageList" class="garry-messages">
          <div v-if="assistant.messages.value.length === 0" class="garry-welcome">
            <img :src="garryImage" alt="Garry the LanguageWise assistant" />
            <h2>Hi, I’m Garry!</h2>
            <p>
              Ask me about your course, the lesson you’re studying, or how quizzes
              work on LanguageWise.
            </p>
            <div class="garry-suggestions" aria-label="Suggested questions">
              <button
                v-for="suggestion in suggestions"
                :key="suggestion"
                type="button"
                @click="submit(suggestion)"
              >
                {{ suggestion }}
              </button>
            </div>
          </div>

          <div
            v-for="message in assistant.messages.value"
            :key="message.id"
            class="garry-message"
            :class="`garry-message-${message.role}`"
          >
            <span class="sr-only">
              {{ message.role === 'assistant' ? 'Garry' : 'You' }}:
            </span>
            <ToolResultCard
              v-for="(toolResult, index) in message.toolResults ?? []"
              :key="`${message.id}-tool-${index}`"
              :result="toolResult"
            />
            <div
              v-if="message.content"
              class="garry-message-content"
              v-html="render(message.content)"
            />
            <div v-else class="garry-typing" aria-hidden="true">
              <span /><span /><span />
            </div>
          </div>
        </div>

        <div
          class="garry-stream-status sr-only"
          role="status"
          aria-live="polite"
        >
          {{ assistant.streaming.value ? 'Garry is writing a response.' : '' }}
        </div>

        <v-alert
          v-if="assistant.error.value"
          type="error"
          variant="tonal"
          density="compact"
          class="garry-error"
        >
          {{ assistant.error.value }}
          <template #append>
            <v-btn
              :icon="mdiRefresh"
              variant="text"
              size="small"
              aria-label="Retry last message"
              @click="assistant.retry(context)"
            />
          </template>
        </v-alert>

        <form class="garry-composer" @submit.prevent="submit()">
          <textarea
            ref="composer"
            v-model="draft"
            rows="1"
            maxlength="4000"
            placeholder="Ask Garry about language learning…"
            aria-label="Message Garry"
            :disabled="assistant.streaming.value"
            @keydown="onComposerKeydown"
          />
          <v-btn
            v-if="assistant.streaming.value"
            :icon="mdiStopCircleOutline"
            color="primary"
            variant="text"
            aria-label="Stop Garry’s response"
            @click="assistant.cancel"
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
      v-if="!assistant.expanded.value"
      type="button"
      class="garry-launcher"
      aria-label="Open Garry language learning assistant"
      @click="assistant.expanded.value = true"
    >
      <img :src="garryImage" alt="" />
      <span>Ask Garry</span>
    </button>
  </aside>
</template>
