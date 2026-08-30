<script setup lang="ts">
import DOMPurify from 'dompurify'
import MarkdownIt from 'markdown-it'
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AppIcon from '../components/AppIcon.vue'
import LessonContent from '../components/LessonContent.vue'
import LessonSidebar from '../components/LessonSidebar.vue'
import VocabularySheet from '../components/VocabularySheet.vue'
import { useCourse } from '../composables/useCourse'

const route = useRoute()
const router = useRouter()
const store = useCourse()
const vocabularyOpen = ref(false)
const markdown = new MarkdownIt({ html: false, linkify: true, typographer: true })

const courseCode = computed(() => String(route.params.courseCode))
const lessonSlug = computed(() =>
  typeof route.params.lessonSlug === 'string' ? route.params.lessonSlug : null,
)
const safeContent = computed(() =>
  DOMPurify.sanitize(markdown.render(store.lesson.value?.contentMarkdown ?? '')),
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
        @retry="lessonSlug && store.loadLesson(courseCode, lessonSlug)"
      />

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
  </div>
</template>
