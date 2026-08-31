<script setup lang="ts">
import DOMPurify from 'dompurify'
import MarkdownIt from 'markdown-it'
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { lessonsApi } from '../api/client'
import AppIcon from '../components/AppIcon.vue'
import LessonContent from '../components/LessonContent.vue'
import LessonSidebar from '../components/LessonSidebar.vue'
import VocabularySheet from '../components/VocabularySheet.vue'
import { useCourse } from '../composables/useCourse'
import { errorMessage } from '../composables/useAsyncResource'

const route = useRoute()
const router = useRouter()
const store = useCourse()
const vocabularyOpen = ref(false)
const milestoneDialogOpen = ref(false)
const updatingMilestone = ref(false)
const milestoneError = ref<string | null>(null)
const markdown = new MarkdownIt({ html: false, linkify: true, typographer: true })

const courseCode = computed(() => String(route.params.courseCode))
const lessonSlug = computed(() =>
  typeof route.params.lessonSlug === 'string' ? route.params.lessonSlug : null,
)
const safeContent = computed(() =>
  DOMPurify.sanitize(markdown.render(store.lesson.value?.contentMarkdown ?? '')),
)
const lessonCompleted = computed(
  () =>
    store.progress.value?.lessons.find((item) => item.lessonId === store.lesson.value?.id)
      ?.completed ?? false,
)
const completedLessonIds = computed(
  () => store.progress.value?.lessons.filter((item) => item.completed).map((item) => item.lessonId) ?? [],
)
const lessonQuiz = computed(() =>
  store.quizzes.value.find((quiz) => quiz.lessonId === store.lesson.value?.id),
)
const lessonFlashcardDeck = computed(() =>
  store.flashcardDecks.value.find((deck) => deck.lessonId === store.lesson.value?.id),
)

async function initialize() {
  await store.loadCourse(courseCode.value)
  if (store.error.value) return
  const slug = lessonSlug.value ?? store.lessons.value[0]?.slug
  if (!slug) return
  if (!lessonSlug.value) {
    await router.replace({
      name: 'lesson',
      params: { courseCode: courseCode.value, lessonSlug: slug },
    })
    return
  }
  await store.loadLesson(courseCode.value, slug)
}

function requestMilestone(completed: boolean) {
  milestoneError.value = null
  if (completed) {
    milestoneDialogOpen.value = true
  } else {
    updateMilestone(false)
  }
}

async function updateMilestone(completed: boolean) {
  if (!store.lesson.value || !store.progress.value) return
  updatingMilestone.value = true
  milestoneError.value = null
  try {
    if (completed) {
      await lessonsApi.complete(store.lesson.value.id)
    } else {
      await lessonsApi.uncomplete(store.lesson.value.id)
    }
    const item = store.progress.value.lessons.find(
      (progress) => progress.lessonId === store.lesson.value?.id,
    )
    if (item) item.completed = completed
    milestoneDialogOpen.value = false
  } catch (cause) {
    milestoneError.value = errorMessage(cause, 'Unable to update lesson completion.')
  } finally {
    updatingMilestone.value = false
  }
}

watch(courseCode, initialize, { immediate: true })
watch(lessonSlug, async (next, previous) => {
  if (next && next !== previous && store.course.value?.code === courseCode.value) {
    vocabularyOpen.value = false
    await store.loadLesson(courseCode.value, next)
  }
})
</script>

<template>
  <div class="course-shell">
    <LessonSidebar
      :course-code="courseCode"
      :course-title="store.course.value?.title"
      :lessons="store.lessons.value"
      :active-lesson-slug="lessonSlug"
      :completed-lesson-ids="completedLessonIds"
      :course-completed="store.progress.value?.courseCompleted"
      :loading="store.loadingCourse.value"
      :error="store.error.value"
      @retry="initialize"
    />

    <main class="lesson-main">
      <v-btn to="/" variant="text" class="course-back-button">
        <template #prepend><AppIcon name="arrow-left" /></template>
        Back to courses
      </v-btn>
      <LessonContent
        :lesson="store.lesson.value"
        :safe-content="safeContent"
        :loading="store.loadingLesson.value || store.loadingCourse.value"
        :error="store.error.value"
        :completed="lessonCompleted"
        :updating-milestone="updatingMilestone"
        :flashcard-deck="lessonFlashcardDeck"
        :quiz="lessonQuiz"
        @retry="lessonSlug && store.loadLesson(courseCode, lessonSlug)"
        @update-completed="requestMilestone"
      />
      <v-alert
        v-if="milestoneError"
        type="error"
        variant="tonal"
        class="lesson-milestone-error"
        closable
        @click:close="milestoneError = null"
      >
        {{ milestoneError }}
      </v-alert>

      <v-btn
        v-if="store.lesson.value?.vocabulary.length"
        class="vocabulary-activator"
        color="primary"
        rounded="pill"
        size="large"
        elevation="8"
        aria-label="Open lesson vocabulary"
        @click="vocabularyOpen = true"
      >
        <span class="up-arrow" aria-hidden="true">↑</span>
        Vocabulary
      </v-btn>
    </main>

    <VocabularySheet
      v-model="vocabularyOpen"
      :items="store.lesson.value?.vocabulary ?? []"
    />

    <v-dialog v-model="milestoneDialogOpen" max-width="520">
      <v-card rounded="xl">
        <v-card-title class="pa-6 pb-2">Complete this lesson?</v-card-title>
        <v-card-text class="px-6">
          Mark “{{ store.lesson.value?.title }}” as complete. You can undo this at any time.
        </v-card-text>
        <v-card-actions class="pa-6 pt-3">
          <v-spacer />
          <v-btn variant="text" :disabled="updatingMilestone" @click="milestoneDialogOpen = false">
            Cancel
          </v-btn>
          <v-btn color="success" :loading="updatingMilestone" @click="updateMilestone(true)">
            Mark complete
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>
