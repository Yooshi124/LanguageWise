<script setup lang="ts">
import DOMPurify from 'dompurify'
import MarkdownIt from 'markdown-it'
import { computed, nextTick, ref, watch } from 'vue'
import { askDocs, type DocsAnswer } from '../composables/garryApi'

const props = defineProps<{ visible: boolean }>()
const emit = defineEmits<{ close: [] }>()

const cardRef = ref<HTMLElement | null>(null)
const closeButtonRef = ref<HTMLButtonElement | null>(null)
const query = ref('')
const answer = ref<DocsAnswer | null>(null)
const loading = ref(false)
const error = ref('')
const markdown = new MarkdownIt({ html: false, linkify: true, typographer: true })
let previouslyFocused: Element | null = null

const confidenceLabel = computed(() => {
  const confidence = answer.value?.confidence
  if (!confidence) return ''
  return confidence === 'insufficient'
    ? 'Insufficient context'
    : `${confidence[0]!.toUpperCase()}${confidence.slice(1)} confidence`
})

function close() {
  emit('close')
}

function render(content: string) {
  return DOMPurify.sanitize(markdown.render(content))
}

function focusableElements() {
  if (!cardRef.value) return []
  return Array.from(
    cardRef.value.querySelectorAll<HTMLElement>(
      'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])',
    ),
  ).filter((element) => !(element as HTMLButtonElement).disabled)
}

function trapFocus(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    event.preventDefault()
    close()
    return
  }
  if (event.key !== 'Tab') return
  const focusable = focusableElements()
  const first = focusable[0]
  const last = focusable[focusable.length - 1]
  if (!first || !last) return
  if (event.shiftKey && document.activeElement === first) {
    event.preventDefault()
    last.focus()
  } else if (!event.shiftKey && document.activeElement === last) {
    event.preventDefault()
    first.focus()
  }
}

watch(
  () => props.visible,
  (isVisible) => {
    if (isVisible) {
      query.value = ''
      answer.value = null
      error.value = ''
      previouslyFocused = document.activeElement
      void nextTick(() => closeButtonRef.value?.focus())
    } else if (previouslyFocused instanceof HTMLElement) {
      previouslyFocused.focus()
      previouslyFocused = null
    }
  },
)

async function submit() {
  const trimmed = query.value.trim()
  if (!trimmed || loading.value) return

  loading.value = true
  error.value = ''
  answer.value = null
  try {
    answer.value = await askDocs(trimmed)
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : 'Could not ask the docs right now.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <Teleport to="body">
    <div
      v-if="visible"
      class="docs-search-overlay"
      role="dialog"
      aria-modal="true"
      aria-label="Ask the docs"
      @click.self="close"
      @keydown="trapFocus"
    >
      <div ref="cardRef" class="docs-search-card">
        <header class="docs-search-card__header">
          <h2>Ask the docs</h2>
          <button
            ref="closeButtonRef"
            type="button"
            class="docs-search-card__close"
            aria-label="Close Ask the docs"
            @click="close"
          >
            &times;
          </button>
        </header>

        <form class="docs-search-form" @submit.prevent="submit">
          <input
            v-model="query"
            type="search"
            class="docs-search-form__input"
            placeholder="How does the leaderboard work?"
            aria-label="Ask a question about LanguageWise"
          />
          <button
            type="submit"
            class="docs-search-form__submit"
            :disabled="loading || !query.trim()"
          >
            {{ loading ? 'Asking…' : 'Ask' }}
          </button>
        </form>

        <p v-if="error" class="docs-search-error">{{ error }}</p>

        <p v-else-if="loading" class="docs-search-empty" aria-live="polite">
          Garry is reading the docs…
        </p>

        <div v-else-if="answer" class="docs-search-body" aria-live="polite">
          <span
            class="docs-answer__confidence"
            :class="`docs-answer__confidence--${answer.confidence}`"
          >
            {{ confidenceLabel }}
          </span>
          <div class="docs-search-results__text" v-html="render(answer.answer)" />

          <section v-if="answer.citations.length" class="docs-answer__sources">
            <h3>Sources</h3>
            <ol class="docs-search-results">
              <li
                v-for="citation in answer.citations"
                :key="citation.number"
                class="docs-search-results__item"
              >
                <details>
                  <summary class="docs-search-results__source">
                    [{{ citation.number }}] {{ citation.source }} — {{ citation.heading }}
                  </summary>
                  <div class="docs-search-results__text" v-html="render(citation.text)" />
                </details>
              </li>
            </ol>
          </section>
        </div>
      </div>
    </div>
  </Teleport>
</template>
