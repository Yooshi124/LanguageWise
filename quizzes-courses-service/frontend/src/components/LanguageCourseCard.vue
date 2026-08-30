<script setup lang="ts">
import type { Course } from '../models/api'
import AppIcon from './AppIcon.vue'

defineProps<{
  code: string
  flag: string
  name: string
  color: string
  course?: Course
}>()
</script>

<template>
  <v-card
    :to="`/courses/${code}`"
    class="language-card h-100"
    rounded="xl"
    elevation="0"
    :aria-label="`Open ${name} course`"
  >
    <div class="card-accent" :style="{ background: color }" />
    <v-card-text class="pa-7">
      <img class="flag" :src="flag" alt="" aria-hidden="true" />
      <h2>{{ course?.title || name }}</h2>
      <p>{{ course?.description || `Start your ${name} learning journey.` }}</p>
      <div class="card-action" :style="{ color }">
        Explore course <AppIcon class="course-action-arrow" name="arrow-right" :size="46" />
      </div>
    </v-card-text>
  </v-card>
</template>
