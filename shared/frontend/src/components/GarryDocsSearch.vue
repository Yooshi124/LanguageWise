<script setup lang="ts">
import DOMPurify from 'dompurify'
import MarkdownIt from 'markdown-it'
import { nextTick, ref, watch } from 'vue'
import { searchDocs, type DocsSearchResult } from '../composables/garryApi'

const props = defineProps<{ visible: boolean; apiBase: string }>()
const emit = defineEmits<{ close: [] }>()

const cardRef = ref<HTMLElement | null>(null)
const closeButtonRef = ref<HTMLButtonElement | null>(null)
const query = ref('')
const results = ref<DocsSearchResult[]>([])
const loading = ref(false)
const searched = ref(false)
const error = ref('')
const markdown = new MarkdownIt({ html: false, linkify: true, typographer: true })
let previouslyFocused: Element | null = null

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
      results.value = []
      searched.value = false
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
  try {
    results.value = await searchDocs(props.apiBase, trimmed)
  } catch (caught) {
    results.value = []
    error.value = caught instanceof Error ? caught.message : 'Could not search the docs right now.'
  } finally {
    loading.value = false
    searched.value = true
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
            aria-label="Close docs search"
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
            aria-label="Search the documentation"
          />
          <button
            type="submit"
            class="docs-search-form__submit"
            :disabled="loading || !query.trim()"
          >
            {{ loading ? 'Searching…' : 'Search' }}
          </button>
        </form>

        <p v-if="error" class="docs-search-error">{{ error }}</p>

        <ul v-else-if="results.length" class="docs-search-results">
          <li
            v-for="(result, index) in results"
            :key="`${result.source}-${index}`"
            class="docs-search-results__item"
          >
            <span class="docs-search-results__source">{{ result.source }} — {{ result.heading }}</span>
            <div class="docs-search-results__text" v-html="render(result.text)" />
          </li>
        </ul>

        <p v-else-if="searched && !loading" class="docs-search-empty">
          No matching documentation found.
        </p>
      </div>
    </div>
  </Teleport>
</template>
