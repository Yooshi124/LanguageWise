<script setup lang="ts">
import type { LessonSummary } from '../models/api'

defineProps<{
  courseCode: string
  courseTitle?: string
  lessons: readonly LessonSummary[]
  activeLessonSlug: string | null
  loading: boolean
  error: string | null
}>()

defineEmits<{
  retry: []
}>()
</script>

<template>
  <aside class="lesson-sidebar">
    <div class="course-heading">
      <span class="eyebrow">Course</span>
      <h1>{{ courseTitle || 'Loading…' }}</h1>
    </div>
    <v-skeleton-loader v-if="loading" type="list-item-two-line@5" />
    <v-alert v-else-if="error && !courseTitle" type="error" variant="tonal">
      {{ error }}
      <v-btn block variant="text" class="mt-2" @click="$emit('retry')">Retry</v-btn>
    </v-alert>
    <nav v-else aria-label="Course lessons" class="lesson-list">
      <router-link
        v-for="(item, index) in lessons"
        :key="item.id"
        :to="{ name: 'lesson', params: { courseCode, lessonSlug: item.slug } }"
        class="lesson-link"
        :class="{ active: item.slug === activeLessonSlug }"
      >
        <span class="lesson-number">{{ String(index + 1).padStart(2, '0') }}</span>
        <span>{{ item.title }}</span>
      </router-link>
      <p v-if="!lessons.length" class="text-medium-emphasis pa-4">No lessons are available yet.</p>
    </nav>
  </aside>
</template>
