<script setup lang="ts">
import { computed, watch } from 'vue'
import { useRoute } from 'vue-router'
import { coursesApi } from '../api/client'
import AppIcon from '../components/AppIcon.vue'
import CardAction from '../components/CardAction.vue'
import { languageOptions } from '../config/languages'
import { useAsyncResource } from '../composables/useAsyncResource'

const route = useRoute()
const courseCode = computed(() => String(route.params.courseCode))
const language = computed(() => languageOptions.find((item) => item.code === courseCode.value))
const flagsPath = `${import.meta.env.BASE_URL}flags`
const resource = useAsyncResource(async (signal) => {
  const [course, decks] = await Promise.all([
    coursesApi.get(courseCode.value, signal),
    coursesApi.flashcardDecks(courseCode.value, signal),
  ])
  return {
    course,
    decks: [...decks].sort((a, b) => a.lessonSortOrder - b.lessonSortOrder),
  }
})

watch(courseCode, resource.load, { immediate: true })
</script>

<template>
  <v-container class="feature-page">
    <v-btn :to="{ name: 'flashcards' }" variant="text" class="feature-back">
      <template #prepend><AppIcon name="arrow-left" /></template>
      Languages
    </v-btn>
    <header class="feature-heading compact">
      <div class="d-flex align-center ga-4 mb-4">
        <img
          v-if="language"
          class="flag"
          :src="`${flagsPath}/${language.flag}`"
          alt=""
          aria-hidden="true"
        />
        <v-chip color="secondary" variant="tonal">Flashcards</v-chip>
      </div>
      <h1>{{ resource.data.value?.course.title || language?.name || courseCode }}</h1>
      <p>Choose a lesson deck to begin revising.</p>
    </header>

    <div v-if="resource.loading.value" class="feature-loading">
      <v-progress-circular indeterminate color="secondary" size="52" />
    </div>
    <v-alert v-else-if="resource.error.value" type="error" variant="tonal" class="mt-8">
      {{ resource.error.value }}
      <template #append><v-btn variant="text" @click="resource.retry">Retry</v-btn></template>
    </v-alert>
    <v-empty-state
      v-else-if="!resource.data.value?.decks.length"
      title="No flashcard decks available"
      text="Decks will appear here as lessons are published."
    />
    <v-row v-else class="mt-7" align="stretch">
      <v-col v-for="deck in resource.data.value.decks" :key="deck.lessonId" cols="12" md="6">
        <v-card
          :to="{
            name: 'flashcard-revision',
            params: { courseCode, lessonSlug: deck.lessonSlug },
            query: { returnTo: route.fullPath },
          }"
          rounded="xl"
          elevation="0"
          class="feature-card flashcard-deck-card h-100"
        >
          <v-card-text class="pa-7">
            <div class="feature-card-meta">
              <span>Lesson {{ deck.lessonSortOrder }}</span>
              <v-chip color="secondary" size="small" variant="tonal">
                {{ deck.cardCount }} cards
              </v-chip>
            </div>
            <h2>{{ deck.lessonTitle }}</h2>
            <p>Review the vocabulary and phrases from this lesson.</p>
            <div class="feature-card-footer">
              <span>Active recall practice</span>
              <CardAction label="Open deck" />
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>
