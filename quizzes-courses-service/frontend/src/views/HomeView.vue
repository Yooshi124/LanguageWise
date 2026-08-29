<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useCourses } from '../composables/useCourses'

const flagsPath = `${import.meta.env.BASE_URL}flags`
const languages = [
  { code: 'de', flag: `${flagsPath}/de.svg`, name: 'German', color: '#f59e0b' },
  { code: 'fr', flag: `${flagsPath}/fr.svg`, name: 'French', color: '#3b82f6' },
  { code: 'it', flag: `${flagsPath}/it.svg`, name: 'Italian', color: '#16a34a' },
  { code: 'nl', flag: `${flagsPath}/nl.svg`, name: 'Dutch', color: '#f97316' },
  { code: 'es', flag: `${flagsPath}/es.svg`, name: 'Spanish', color: '#dc2626' },
] as const

const store = useCourses()
const cards = computed(() =>
  languages.map((language) => ({
    ...language,
    course: store.courses.value.find((course) => course.code === language.code),
  })),
)

onMounted(() => store.load())
</script>

<template>
  <section class="hero">
    <v-container class="py-16">
      <div class="hero-copy">
        <v-chip color="primary" variant="tonal" class="mb-5">Learn at your own pace</v-chip>
        <h1>Choose a language.<br /><span>Open a new world.</span></h1>
        <p>Practical, bite-sized lessons designed to build real confidence every day.</p>
      </div>

      <v-alert
        v-if="store.error.value"
        type="error"
        variant="tonal"
        class="mt-10"
        title="Courses could not be loaded"
      >
        {{ store.error.value }}
        <template #append><v-btn variant="text" @click="store.load(true)">Retry</v-btn></template>
      </v-alert>

      <div v-if="store.loading.value" class="d-flex justify-center py-16" aria-live="polite">
        <v-progress-circular indeterminate color="primary" size="52" />
        <span class="sr-only">Loading courses</span>
      </div>

      <v-row v-else class="mt-10" align="stretch">
        <v-col v-for="card in cards" :key="card.code" cols="12" sm="6" lg="4">
          <v-card
            :to="`/courses/${card.code}`"
            class="language-card h-100"
            rounded="xl"
            elevation="0"
            :aria-label="`Open ${card.name} course`"
          >
            <div class="card-accent" :style="{ background: card.color }" />
            <v-card-text class="pa-7">
              <img class="flag" :src="card.flag" alt="" aria-hidden="true" />
              <h2>{{ card.course?.title || card.name }}</h2>
              <p>{{ card.course?.description || `Start your ${card.name} learning journey.` }}</p>
              <div class="card-action" :style="{ color: card.color }">
                Explore course <span aria-hidden="true">→</span>
              </div>
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>

      <v-row class="mt-8">
        <v-col cols="12" md="6">
          <v-card to="/quizzes" rounded="xl" variant="tonal" color="primary" class="pa-3">
            <v-card-title>Quizzes <v-chip size="small">Coming soon</v-chip></v-card-title>
            <v-card-text>Check your progress with short, focused challenges.</v-card-text>
          </v-card>
        </v-col>
        <v-col cols="12" md="6">
          <v-card to="/flashcards" rounded="xl" variant="tonal" color="secondary" class="pa-3">
            <v-card-title>Flashcards <v-chip size="small">Coming soon</v-chip></v-card-title>
            <v-card-text>Build a vocabulary that sticks with active recall.</v-card-text>
          </v-card>
        </v-col>
      </v-row>
    </v-container>
  </section>
</template>
