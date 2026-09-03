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
const flagsPath = '/remotes/quizzes-courses/flags'
const resource = useAsyncResource(async (signal) => {
  const [course, quizzes, progress] = await Promise.all([
    coursesApi.get(courseCode.value, signal),
    coursesApi.quizzes(courseCode.value, signal),
    coursesApi.progress(courseCode.value, signal),
  ])
  return {
    course,
    quizzes: [...quizzes].sort((a, b) => a.lessonSortOrder - b.lessonSortOrder),
    progress,
  }
})

const quizCards = computed(() =>
  (resource.data.value?.quizzes ?? []).map((quiz) => ({
    quiz,
    progress: resource.data.value?.progress.quizzes.find((item) => item.quizId === quiz.id),
  })),
)

watch(courseCode, resource.load, { immediate: true })
</script>

<template>
  <v-container class="feature-page">
    <v-btn :to="{ name: 'quizzes' }" variant="text" class="feature-back">
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
        <v-chip color="primary" variant="tonal">Quizzes</v-chip>
      </div>
      <h1>{{ resource.data.value?.course.title || language?.name || courseCode }}</h1>
      <p>Choose a lesson quiz and check your progress.</p>
    </header>

    <div v-if="resource.loading.value" class="feature-loading">
      <v-progress-circular indeterminate color="primary" size="52" />
    </div>
    <v-alert v-else-if="resource.error.value" type="error" variant="tonal" class="mt-8">
      {{ resource.error.value }}
      <template #append><v-btn variant="text" @click="resource.retry">Retry</v-btn></template>
    </v-alert>
    <v-empty-state
      v-else-if="!quizCards.length"
      title="No quizzes available"
      text="Quizzes will appear here as lessons are published."
    />
    <v-row v-else class="mt-7" align="stretch">
      <v-col v-for="{ quiz, progress } in quizCards" :key="quiz.id" cols="12" md="6">
        <v-card
          :to="{
            name: 'quiz-runner',
            params: { courseCode, quizId: quiz.id },
            query: { returnTo: route.fullPath },
          }"
          rounded="xl"
          elevation="0"
          class="feature-card h-100"
        >
          <v-card-text class="pa-7">
            <div class="feature-card-meta">
              <span>Lesson {{ quiz.lessonSortOrder }}</span>
              <v-chip
                v-if="progress?.completed"
                color="success"
                size="small"
                variant="tonal"
              >
                Passed
              </v-chip>
            </div>
            <h2>{{ quiz.title }}</h2>
            <p>{{ quiz.lessonTitle }}</p>
            <div class="feature-card-footer">
              <span v-if="progress?.bestScore !== null && progress?.bestScore !== undefined">
                Best score: {{ progress.bestScore }}/{{ progress.totalQuestions }}
              </span>
              <span v-else>Not attempted</span>
              <CardAction
                :label="
                  progress?.bestScore !== null && progress?.bestScore !== undefined
                    ? 'Try again'
                    : 'Start quiz'
                "
              />
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>
