<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { coursesApi } from '../api/client'
import AppIcon from '../components/AppIcon.vue'
import { useAsyncResource } from '../composables/useAsyncResource'
import { useSafeBack } from '../composables/useSafeBack'
import type { Flashcard } from '../models/api'

const route = useRoute()
const courseCode = computed(() => String(route.params.courseCode))
const lessonSlug = computed(() => String(route.params.lessonSlug))
const resource = useAsyncResource((signal) =>
  coursesApi.flashcardDeck(courseCode.value, lessonSlug.value, signal),
)
const selectedCard = ref<Flashcard | null>(null)
const flipped = ref(false)
const focusedCard = ref<HTMLElement | null>(null)
const goBack = useSafeBack(() => ({
  name: 'flashcard-decks',
  params: { courseCode: courseCode.value },
}), ['lesson', 'flashcard-decks'])
let opener: HTMLElement | null = null

async function openCard(card: Flashcard, event: MouseEvent | KeyboardEvent) {
  opener = event.currentTarget as HTMLElement
  selectedCard.value = card
  flipped.value = false
  await nextTick()
  focusedCard.value?.focus()
}

function closeCard() {
  selectedCard.value = null
  flipped.value = false
  nextTick(() => opener?.focus())
}

function toggleCard() {
  flipped.value = !flipped.value
}

watch([courseCode, lessonSlug], resource.load, { immediate: true })
</script>

<template>
  <v-container class="feature-page flashcard-page">
    <v-btn variant="text" class="feature-back" @click="goBack">
      <template #prepend><AppIcon name="arrow-left" /></template>
      Back
    </v-btn>
    <header class="feature-heading compact">
      <v-chip v-if="resource.data.value" color="secondary" variant="tonal" class="mb-4">
        Lesson {{ resource.data.value?.lessonSortOrder }}
      </v-chip>
      <h1>{{ resource.data.value?.lessonTitle || 'Flashcards' }}</h1>
      <p>Select a card to enlarge it, then click or press Enter to reveal the answer.</p>
    </header>

    <div v-if="resource.loading.value" class="feature-loading">
      <v-progress-circular indeterminate color="secondary" size="52" />
    </div>
    <v-alert v-else-if="resource.error.value" type="error" variant="tonal" class="mt-8">
      {{ resource.error.value }}
      <template #append><v-btn variant="text" @click="resource.retry">Retry</v-btn></template>
    </v-alert>
    <v-empty-state
      v-else-if="!resource.data.value?.cards.length"
      title="This deck is empty"
      text="Flashcards will appear here when they are available."
    />
    <div v-else class="flashcard-grid">
      <button
        v-for="(card, index) in resource.data.value.cards"
        :key="card.id"
        type="button"
        class="flashcard-tile"
        :aria-label="`Open flashcard ${index + 1}: ${card.frontText}`"
        @click="openCard(card, $event)"
      >
        <span class="flashcard-index">{{ String(index + 1).padStart(2, '0') }}</span>
        <strong>{{ card.frontText }}</strong>
        <span>Click to reveal</span>
      </button>
    </div>

    <Transition name="flashcard-focus">
      <div
        v-if="selectedCard"
        class="flashcard-overlay"
        role="presentation"
        @click="closeCard"
        @keydown.esc="closeCard"
      >
        <button
          ref="focusedCard"
          type="button"
          class="flashcard-focused"
          :class="{ flipped }"
          :aria-label="flipped ? 'Showing answer. Flip to front.' : 'Showing prompt. Flip to answer.'"
          @click.stop="toggleCard"
        >
          <span class="flashcard-face flashcard-front">
            <small>Prompt</small>
            <strong>{{ selectedCard.frontText }}</strong>
            <span>Click to reveal answer</span>
          </span>
          <span class="flashcard-face flashcard-back">
            <small>Answer</small>
            <strong>{{ selectedCard.backText }}</strong>
            <span>Click to see prompt</span>
          </span>
        </button>
        <v-btn class="flashcard-close" icon aria-label="Close flashcard" @click.stop="closeCard">
          <AppIcon name="close" />
        </v-btn>
      </div>
    </Transition>
  </v-container>
</template>
