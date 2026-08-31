<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { languageOptions } from '../config/languages'
import { useCourses } from '../composables/useCourses'
import CardAction from './CardAction.vue'

const props = defineProps<{ kind: 'quizzes' | 'flashcards' }>()
const store = useCourses()
const flagsPath = `${import.meta.env.BASE_URL}flags`
const title = computed(() =>
  props.kind === 'quizzes' ? 'Choose a quiz language' : 'Choose a flashcard language',
)
const description = computed(() =>
  props.kind === 'quizzes'
    ? 'Test what you have learned with focused lesson quizzes.'
    : 'Build recall with flashcard decks matched to each lesson.',
)
const routeName = computed(() =>
  props.kind === 'quizzes' ? 'quiz-list' : 'flashcard-decks',
)
const cards = computed(() =>
  languageOptions.map((language) => ({
    ...language,
    course: store.courses.value.find((course) => course.code === language.code),
  })),
)

onMounted(() => store.load())
</script>

<template>
  <v-container class="feature-page feature-language-page">
    <header class="feature-heading">
      <v-chip color="primary" variant="tonal" class="mb-5">
        {{ kind === 'quizzes' ? 'Test your knowledge' : 'Practice makes perfect' }}
      </v-chip>
      <h1>{{ title }}</h1>
      <p>{{ description }}</p>
    </header>

    <v-alert
      v-if="store.error.value"
      type="error"
      variant="tonal"
      title="Languages could not be loaded"
      class="mt-8"
    >
      {{ store.error.value }}
      <template #append><v-btn variant="text" @click="store.load(true)">Retry</v-btn></template>
    </v-alert>

    <div v-if="store.loading.value" class="feature-loading">
      <v-progress-circular indeterminate color="primary" size="52" />
      <span class="sr-only">Loading languages</span>
    </div>

    <v-row v-else class="mt-7" align="stretch">
      <v-col v-for="card in cards" :key="card.code" cols="12" sm="6" lg="4">
        <v-card
          :to="{ name: routeName, params: { courseCode: card.code } }"
          class="language-card h-100"
          rounded="xl"
          elevation="0"
          :aria-label="`Open ${card.name} ${kind}`"
        >
          <div class="card-accent" :style="{ background: card.color }" />
          <v-card-text class="pa-7">
            <img
              class="flag"
              :src="`${flagsPath}/${card.flag}`"
              alt=""
              aria-hidden="true"
            />
            <h2>{{ card.course?.title || card.name }}</h2>
            <p>
              {{
                kind === 'quizzes'
                  ? `Take ${card.name} lesson quizzes.`
                  : `Review ${card.name} lesson decks.`
              }}
            </p>
            <div class="card-action" :style="{ color: card.color }">
              <CardAction label="Continue" :size="46" />
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>
