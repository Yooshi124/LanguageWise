<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { ApiError, coursesApi } from '../api/client'
import AppIcon from '../components/AppIcon.vue'
import LessonSidebar from '../components/LessonSidebar.vue'
import { errorMessage } from '../composables/useAsyncResource'
import { useCourse } from '../composables/useCourse'

const route = useRoute()
const store = useCourse()
const courseCode = computed(() => String(route.params.courseCode))
const dialogOpen = ref(false)
const desiredCompletion = ref(false)
const updating = ref(false)
const actionError = ref<string | null>(null)
const completedLessonIds = computed(
  () => store.progress.value?.lessons.filter((item) => item.completed).map((item) => item.lessonId) ?? [],
)
const lessonRows = computed(() =>
  store.lessons.value.map((lesson) => ({
    lesson,
    completed:
      store.progress.value?.lessons.find((item) => item.lessonId === lesson.id)?.completed ?? false,
  })),
)
const quizRows = computed(() =>
  store.quizzes.value.map((quiz) => ({
    quiz,
    progress: store.progress.value?.quizzes.find((item) => item.quizId === quiz.id),
  })),
)

function requestCourseCompletion(completed: boolean) {
  desiredCompletion.value = completed
  actionError.value = null
  dialogOpen.value = true
}

async function updateCourseCompletion() {
  if (!store.progress.value) return
  updating.value = true
  actionError.value = null
  try {
    if (desiredCompletion.value) {
      await coursesApi.complete(courseCode.value)
    } else {
      await coursesApi.uncomplete(courseCode.value)
    }
    store.progress.value.courseCompleted = desiredCompletion.value
    dialogOpen.value = false
  } catch (cause) {
    actionError.value =
      cause instanceof ApiError && cause.status === 409
        ? 'Complete every lesson and pass each available quiz before completing the course.'
        : errorMessage(cause, 'Unable to update course completion.')
    dialogOpen.value = false
  } finally {
    updating.value = false
  }
}

watch(courseCode, () => store.loadCourse(courseCode.value), { immediate: true })
</script>

<template>
  <div class="course-shell">
    <LessonSidebar
      :course-code="courseCode"
      :course-title="store.course.value?.title"
      :lessons="store.lessons.value"
      :active-lesson-slug="null"
      :completed-lesson-ids="completedLessonIds"
      :completion-active="true"
      :course-completed="store.progress.value?.courseCompleted"
      :loading="store.loadingCourse.value"
      :error="store.error.value"
      @retry="store.loadCourse(courseCode)"
    />

    <main class="lesson-main completion-main">
      <v-btn :to="{ name: 'quizzes-courses-home' }" variant="text" class="course-back-button">
        <template #prepend><AppIcon name="arrow-left" /></template>
        Back to courses
      </v-btn>

      <div v-if="store.loadingCourse.value" class="lesson-loading">
        <v-progress-circular indeterminate color="primary" size="52" />
        <p>Checking your progress…</p>
      </div>
      <v-alert v-else-if="store.error.value" type="error" variant="tonal">
        {{ store.error.value }}
        <template #append>
          <v-btn variant="text" @click="store.loadCourse(courseCode)">Retry</v-btn>
        </template>
      </v-alert>
      <article v-else class="completion-content">
        <v-chip
          :color="store.progress.value?.courseEligible ? 'success' : 'primary'"
          variant="tonal"
          class="mb-5"
        >
          Course milestone
        </v-chip>
        <h1>
          {{
            store.progress.value?.courseEligible
              ? 'Congratulations — you made it!'
              : 'Your course finish line'
          }}
        </h1>
        <p class="completion-intro">
          {{
            store.progress.value?.courseEligible
              ? 'Every current lesson and quiz is complete. Confirm your course milestone below.'
              : 'Finish the remaining lessons and available quizzes to unlock course completion.'
          }}
        </p>

        <v-alert v-if="actionError" type="error" variant="tonal" class="mb-6" closable>
          {{ actionError }}
        </v-alert>

        <v-card rounded="xl" elevation="0" class="completion-card">
          <v-card-title>Lessons</v-card-title>
          <v-list lines="two">
            <v-list-item
              v-for="{ lesson, completed } in lessonRows"
              :key="lesson.id"
              :to="
                completed
                  ? undefined
                  : {
                      name: 'lesson',
                      params: { courseCode, lessonSlug: lesson.slug },
                    }
              "
            >
              <template #prepend>
                <span class="completion-status" :class="{ done: completed }">
                  {{ completed ? '✓' : lesson.sortOrder }}
                </span>
              </template>
              <v-list-item-title>{{ lesson.title }}</v-list-item-title>
              <v-list-item-subtitle>
                {{ completed ? 'Completed' : 'Open lesson to finish' }}
              </v-list-item-subtitle>
              <template v-if="!completed" #append>
                <AppIcon name="arrow-right" />
              </template>
            </v-list-item>
          </v-list>
        </v-card>

        <v-card rounded="xl" elevation="0" class="completion-card mt-6">
          <v-card-title>Available quizzes</v-card-title>
          <v-list v-if="quizRows.length" lines="two">
            <v-list-item
              v-for="{ quiz, progress } in quizRows"
              :key="quiz.id"
              :to="
                progress?.completed
                  ? undefined
                  : {
                      name: 'quiz-runner',
                      params: { courseCode, quizId: quiz.id },
                      query: { returnTo: route.fullPath },
                    }
              "
            >
              <template #prepend>
                <span class="completion-status" :class="{ done: progress?.completed }">
                  {{ progress?.completed ? '✓' : '?' }}
                </span>
              </template>
              <v-list-item-title>{{ quiz.title }}</v-list-item-title>
              <v-list-item-subtitle>
                {{
                  progress
                    ? `Best score: ${progress.bestScore ?? 0}/${progress.totalQuestions}`
                    : quiz.lessonTitle
                }}
              </v-list-item-subtitle>
              <template v-if="!progress?.completed" #append>
                <AppIcon name="arrow-right" />
              </template>
            </v-list-item>
          </v-list>
          <v-card-text v-else class="text-medium-emphasis">
            No quizzes are currently available for this course.
          </v-card-text>
        </v-card>

        <v-card rounded="xl" elevation="0" class="course-confirmation-card mt-6">
          <v-card-text class="pa-6">
            <v-checkbox
              :model-value="store.progress.value?.courseCompleted"
              :disabled="
                updating ||
                (!store.progress.value?.courseEligible &&
                  !store.progress.value?.courseCompleted)
              "
              color="success"
              label="I have completed this course"
              hide-details
              @update:model-value="requestCourseCompletion(Boolean($event))"
            />
            <p v-if="!store.progress.value?.courseEligible" class="mt-3 text-medium-emphasis">
              This checkbox unlocks when all current requirements are complete.
            </p>
          </v-card-text>
        </v-card>
      </article>
    </main>

    <v-dialog v-model="dialogOpen" max-width="520">
      <v-card rounded="xl">
        <v-card-title class="pa-6 pb-2">
          {{ desiredCompletion ? 'Complete this course?' : 'Remove course completion?' }}
        </v-card-title>
        <v-card-text class="px-6">
          {{
            desiredCompletion
              ? 'Confirm that you are ready to record this course milestone.'
              : 'Your lesson and quiz progress will remain saved, and you can complete the course again.'
          }}
        </v-card-text>
        <v-card-actions class="pa-6 pt-3">
          <v-spacer />
          <v-btn variant="text" :disabled="updating" @click="dialogOpen = false">Cancel</v-btn>
          <v-btn color="success" :loading="updating" @click="updateCourseCompletion">
            Confirm
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>
