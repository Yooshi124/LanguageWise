<script setup lang="ts">
import type { Lesson } from '../models/api'

defineProps<{
  lesson: Lesson | null
  safeContent: string
  loading: boolean
  error: string | null
}>()

defineEmits<{
  retry: []
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
    <span class="eyebrow">Lesson {{ lesson.sortOrder }}</span>
    <h1>{{ lesson.title }}</h1>
    <div class="markdown-body" v-html="safeContent" />
  </article>
  <v-empty-state v-else title="No lesson selected" text="Choose a lesson from the course list." />
</template>
