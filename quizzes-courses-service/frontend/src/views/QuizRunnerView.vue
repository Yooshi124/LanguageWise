<script setup lang="ts">
import { computed, onScopeDispose, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { coursesApi, quizzesApi } from '../api/client'
import AppIcon from '../components/AppIcon.vue'
import QuizQuestionControl from '../components/QuizQuestionControl.vue'
import { errorMessage, isAbortError, useAsyncResource } from '../composables/useAsyncResource'
import { useSafeBack } from '../composables/useSafeBack'
import type { QuizAttempt, QuizSubmission } from '../models/api'

const route = useRoute()
const courseCode = computed(() => String(route.params.courseCode))
const quizId = computed(() => {
  const rawId = String(route.params.quizId)
  if (!/^[1-9]\d*$/.test(rawId)) return null
  const parsedId = Number(rawId)
  return Number.isSafeInteger(parsedId) ? parsedId : null
})
const quizResource = useAsyncResource(async (signal) => {
  if (quizId.value === null) {
    throw new Error('This quiz link is invalid.')
  }

  const [quiz, courseQuizzes] = await Promise.all([
    quizzesApi.get(quizId.value, signal),
    coursesApi.quizzes(courseCode.value, signal),
  ])
  if (!courseQuizzes.some((item) => item.id === quiz.id)) {
    throw new Error('This quiz does not belong to the selected course.')
  }
  return quiz
})
const attempt = ref<QuizAttempt | null>(null)
const result = ref<QuizSubmission | null>(null)
const answers = ref<Record<number, string>>({})
const currentIndex = ref(0)
const submitting = ref(false)
const actionError = ref<string | null>(null)
const goBack = useSafeBack(() => ({
  name: 'quiz-list',
  params: { courseCode: courseCode.value },
}), ['lesson', 'quiz-list', 'course-completion'])
let actionController: AbortController | null = null

const questions = computed(() =>
  [...(quizResource.data.value?.questions ?? [])].sort((a, b) => a.sortOrder - b.sortOrder),
)
const currentQuestion = computed(() => questions.value[currentIndex.value] ?? null)
const answeredCount = computed(
  () => questions.value.filter((question) => answers.value[question.id]?.trim()).length,
)
const allAnswered = computed(
  () => questions.value.length > 0 && answeredCount.value === questions.value.length,
)
const currentReview = computed(() =>
  result.value?.answers.find((answer) => answer.questionId === currentQuestion.value?.id),
)

function setAnswer(questionId: number, response: string) {
  answers.value = { ...answers.value, [questionId]: response }
}

async function startFresh() {
  if (quizId.value === null) return
  actionController?.abort()
  actionController = new AbortController()
  const requestController = actionController
  actionError.value = null
  submitting.value = true
  try {
    attempt.value = await quizzesApi.startAttempt(quizId.value, requestController.signal)
    answers.value = {}
    result.value = null
    currentIndex.value = 0
  } catch (cause) {
    if (!isAbortError(cause)) {
      actionError.value = errorMessage(cause, 'Unable to start this quiz.')
    }
  } finally {
    if (actionController === requestController && !requestController.signal.aborted) {
      submitting.value = false
    }
  }
}

async function initialize() {
  const quiz = await quizResource.load()
  if (quiz) await startFresh()
}

async function submit() {
  if (!attempt.value || !allAnswered.value) return
  actionController?.abort()
  actionController = new AbortController()
  const requestController = actionController
  actionError.value = null
  submitting.value = true
  try {
    result.value = await quizzesApi.submitAttempt(
      attempt.value.id,
      questions.value.map((question) => ({
        questionId: question.id,
        response: answers.value[question.id].trim(),
      })),
      requestController.signal,
    )
    currentIndex.value = 0
  } catch (cause) {
    if (!isAbortError(cause)) {
      actionError.value = errorMessage(cause, 'Unable to submit this quiz.')
    }
  } finally {
    if (actionController === requestController && !requestController.signal.aborted) {
      submitting.value = false
    }
  }
}

watch([courseCode, () => route.params.quizId], initialize, { immediate: true })
onScopeDispose(() => actionController?.abort())
</script>

<template>
  <v-container class="quiz-runner-page">
    <v-btn variant="text" class="feature-back" @click="goBack">
      <template #prepend><AppIcon name="arrow-left" /></template>
      Back
    </v-btn>

    <div v-if="quizResource.loading.value" class="feature-loading">
      <v-progress-circular indeterminate color="primary" size="52" />
    </div>
    <v-alert
      v-else-if="quizResource.error.value"
      type="error"
      variant="tonal"
      class="mt-8"
    >
      {{ quizResource.error.value }}
      <template #append><v-btn variant="text" @click="initialize">Retry</v-btn></template>
    </v-alert>
    <v-card v-else-if="quizResource.data.value" rounded="xl" elevation="0" class="quiz-shell">
      <v-card-text class="pa-0">
        <header class="quiz-header">
          <div>
            <span class="eyebrow">{{ quizResource.data.value.lessonTitle }}</span>
            <h1>{{ quizResource.data.value.title }}</h1>
          </div>
          <v-chip v-if="!result" color="primary" variant="tonal">
            {{ answeredCount }}/{{ questions.length }} answered
          </v-chip>
          <v-chip v-else :color="result.passed ? 'success' : 'warning'" variant="tonal">
            {{ result.score }}/{{ result.totalQuestions }}
          </v-chip>
        </header>

        <v-progress-linear
          :model-value="
            result
              ? (result.score / Math.max(result.totalQuestions, 1)) * 100
              : (answeredCount / Math.max(questions.length, 1)) * 100
          "
          :color="result?.passed ? 'success' : 'primary'"
          height="8"
        />

        <nav class="question-navigation" aria-label="Quiz questions">
          <button
            v-for="(question, index) in questions"
            :key="question.id"
            type="button"
            class="question-nav-button"
            :class="{
              active: index === currentIndex,
              answered: answers[question.id]?.trim(),
              correct: result?.answers.find((item) => item.questionId === question.id)?.isCorrect,
              incorrect:
                result &&
                !result.answers.find((item) => item.questionId === question.id)?.isCorrect,
            }"
            :aria-label="`Question ${index + 1}`"
            :aria-current="index === currentIndex ? 'step' : undefined"
            @click="currentIndex = index"
          >
            {{ index + 1 }}
          </button>
        </nav>

        <section v-if="currentQuestion" class="question-panel">
          <span class="question-count">
            Question {{ currentIndex + 1 }} of {{ questions.length }}
          </span>
          <h2>{{ currentQuestion.content }}</h2>
          <QuizQuestionControl
            :key="`${currentQuestion.id}-${attempt?.id}`"
            :question="currentQuestion"
            :model-value="answers[currentQuestion.id] ?? ''"
            :disabled="Boolean(result) || !attempt"
            @update:model-value="setAnswer(currentQuestion.id, $event)"
          />

          <v-alert
            v-if="currentReview"
            :type="currentReview.isCorrect ? 'success' : 'error'"
            variant="tonal"
            class="mt-6"
          >
            <strong>{{ currentReview.isCorrect ? 'Correct' : 'Not quite' }}</strong>
            <div v-if="!currentReview.isCorrect" class="mt-1">
              Correct answer: {{ currentReview.correctAnswer }}
            </div>
          </v-alert>
        </section>

        <v-alert v-if="actionError" type="error" variant="tonal" class="mx-8 mb-0">
          {{ actionError }}
          <template v-if="!attempt" #append>
            <v-btn variant="text" :loading="submitting" @click="startFresh">Retry</v-btn>
          </template>
        </v-alert>

        <footer class="quiz-footer">
          <v-btn
            variant="outlined"
            :disabled="currentIndex === 0"
            @click="currentIndex--"
          >
            Previous
          </v-btn>
          <div class="quiz-result-actions">
            <v-btn
              v-if="result?.passed"
              :to="{ name: 'course-completion', params: { courseCode } }"
              color="success"
            >
              Complete quiz
            </v-btn>
            <v-btn
              v-if="result"
              color="primary"
              :variant="result.passed ? 'outlined' : 'flat'"
              :loading="submitting"
              @click="startFresh"
            >
              Retake quiz
            </v-btn>
            <v-btn
              v-else-if="currentIndex < questions.length - 1"
              color="primary"
              :disabled="!attempt"
              @click="currentIndex++"
            >
              Next
            </v-btn>
            <v-btn
              v-else
              color="primary"
              :disabled="!attempt || !allAnswered"
              :loading="submitting"
              @click="submit"
            >
              Submit answers
            </v-btn>
          </div>
        </footer>

        <p v-if="!result && !allAnswered" class="quiz-requirement" aria-live="polite">
          Answer every question before submitting.
        </p>
      </v-card-text>
    </v-card>
  </v-container>
</template>
