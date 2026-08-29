<script setup lang="ts">
import DOMPurify from 'dompurify'
import MarkdownIt from 'markdown-it'
import { computed, ref, watch } from 'vue'
import { useDisplay } from 'vuetify'
import { useRoute, useRouter } from 'vue-router'
import ServiceMenu from '../components/ServiceMenu.vue'
import { useCourse } from '../composables/useCourse'

const route = useRoute()
const router = useRouter()
const { mdAndUp } = useDisplay()
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
  <div v-if="!mdAndUp" class="mobile-notice">
    <v-card max-width="520" rounded="xl" class="pa-8 text-center" elevation="2">
      <div class="notice-icon">🖥️</div>
      <h1 class="text-h4 font-weight-bold mb-3">Continue on desktop</h1>
      <p class="text-medium-emphasis mb-6">
        The immersive course workspace is designed for a larger screen.
      </p>
      <v-btn to="/" color="primary" size="large">Back to courses</v-btn>
    </v-card>
  </div>

  <div v-else class="course-shell">
    <aside class="lesson-sidebar">
      <ServiceMenu class="sidebar-brand" />
      <div class="course-heading">
        <span class="eyebrow">Course</span>
        <h1>{{ store.course.value?.title || 'Loading…' }}</h1>
      </div>
      <v-skeleton-loader v-if="store.loadingCourse.value" type="list-item-two-line@5" />
      <v-alert v-else-if="store.error.value && !store.course.value" type="error" variant="tonal">
        {{ store.error.value }}
        <v-btn block variant="text" class="mt-2" @click="initialize">Retry</v-btn>
      </v-alert>
      <nav v-else aria-label="Course lessons" class="lesson-list">
        <router-link
          v-for="(item, index) in store.lessons.value"
          :key="item.id"
          :to="{ name: 'lesson', params: { courseCode, lessonSlug: item.slug } }"
          class="lesson-link"
          :class="{ active: item.slug === lessonSlug }"
        >
          <span class="lesson-number">{{ String(index + 1).padStart(2, '0') }}</span>
          <span>{{ item.title }}</span>
        </router-link>
        <p v-if="!store.lessons.value.length" class="text-medium-emphasis pa-4">
          No lessons are available yet.
        </p>
      </nav>
    </aside>

    <main class="lesson-main">
      <div v-if="store.loadingLesson.value || store.loadingCourse.value" class="lesson-loading">
        <v-progress-circular indeterminate color="primary" size="52" />
        <p>Preparing your lesson…</p>
      </div>
      <v-alert
        v-else-if="store.error.value"
        type="error"
        title="Lesson could not be loaded"
        variant="tonal"
      >
        {{ store.error.value }}
        <template #append>
          <v-btn variant="text" @click="lessonSlug && store.loadLesson(courseCode, lessonSlug)">
            Retry
          </v-btn>
        </template>
      </v-alert>
      <article v-else-if="store.lesson.value" class="lesson-article">
        <span class="eyebrow">Lesson {{ store.lesson.value.sortOrder }}</span>
        <h1>{{ store.lesson.value.title }}</h1>
        <div class="markdown-body" v-html="safeContent" />
      </article>
      <v-empty-state v-else title="No lesson selected" text="Choose a lesson from the course list." />

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

    <v-bottom-sheet v-model="vocabularyOpen" inset>
      <v-card rounded="t-xl" class="vocabulary-sheet">
        <v-card-title class="d-flex align-center px-8 pt-7">
          Lesson vocabulary
          <v-spacer />
          <v-btn icon variant="text" aria-label="Close vocabulary" @click="vocabularyOpen = false">
            <span class="text-h5" aria-hidden="true">×</span>
          </v-btn>
        </v-card-title>
        <v-card-text class="px-8 pb-8">
          <v-row>
            <v-col
              v-for="item in store.lesson.value?.vocabulary"
              :key="`${item.word}-${item.meaning}`"
              cols="12"
              sm="6"
              lg="4"
            >
              <div class="vocabulary-item">
                <strong>{{ item.word }}</strong>
                <span>{{ item.meaning }}</span>
              </div>
            </v-col>
          </v-row>
        </v-card-text>
      </v-card>
    </v-bottom-sheet>
  </div>
</template>
