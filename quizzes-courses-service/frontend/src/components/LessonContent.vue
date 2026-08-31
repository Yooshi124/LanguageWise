<script setup lang="ts">
import { useRoute } from 'vue-router'
import type { FlashcardDeckSummary, Lesson, QuizSummary } from '../models/api'

const route = useRoute()

defineProps<{
  lesson: Lesson | null
  safeContent: string
  loading: boolean
  error: string | null
  completed: boolean
  updatingMilestone: boolean
  flashcardDeck?: FlashcardDeckSummary
  quiz?: QuizSummary
}>()

defineEmits<{
  retry: []
  'update-completed': [completed: boolean]
}>()
</script>

<template>
  <div v-if="loading" class="lesson-loading">
    <v-progress-circular indeterminate color="primary" size="52" />
    <p>Preparing your lesson…</p>
  </div>
  <v-alert v-else-if="error" type="error" title="Lesson could not be loaded" variant="tonal">
    {{ error }}
    <template #append>
      <v-btn variant="text" @click="$emit('retry')">Retry</v-btn>
    </template>
  </v-alert>
  <article v-else-if="lesson" class="lesson-article">
    <div class="lesson-title-row">
      <div>
        <span class="eyebrow">Lesson {{ lesson.sortOrder }}</span>
        <h1>{{ lesson.title }}</h1>
      </div>
      <v-checkbox
        class="lesson-completion-checkbox"
        :model-value="completed"
        :loading="updatingMilestone"
        :disabled="updatingMilestone"
        label="Completed"
        color="success"
        hide-details
        @update:model-value="$emit('update-completed', Boolean($event))"
      />
    </div>
    <div class="markdown-body" v-html="safeContent" />
    <v-card rounded="xl" elevation="0" class="lesson-study-callout">
      <v-card-text class="pa-6">
        <span class="eyebrow">Keep practising</span>
        <h2>Review this lesson</h2>
        <p>Strengthen recall with flashcards, then check your understanding with a quiz.</p>
        <div class="lesson-study-actions">
          <v-btn
            v-if="flashcardDeck"
            color="secondary"
            :to="{
              name: 'flashcard-revision',
              params: { courseCode: lesson.course.code, lessonSlug: flashcardDeck.lessonSlug },
              query: { returnTo: route.fullPath },
            }"
          >
            {{ flashcardDeck.cardCount }} flashcards
          </v-btn>
          <v-btn v-else color="secondary" variant="outlined" disabled>
            Flashcards unavailable
          </v-btn>
          <v-btn
            v-if="quiz"
            color="primary"
            :to="{
              name: 'quiz-runner',
              params: { courseCode: lesson.course.code, quizId: quiz.id },
              query: { returnTo: route.fullPath },
            }"
          >
            Take quiz
          </v-btn>
          <v-btn v-else color="primary" variant="outlined" disabled>
            Quiz not available yet
          </v-btn>
        </div>
      </v-card-text>
    </v-card>
  </article>
  <v-empty-state v-else title="No lesson selected" text="Choose a lesson from the course list." />
</template>
