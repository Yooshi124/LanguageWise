<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{ kind: 'quizzes' | 'flashcards' }>()
const content = computed(() =>
  props.kind === 'quizzes'
    ? {
        icon: '✦',
        title: 'Quizzes are on the way',
        text: 'Soon you’ll be able to test your knowledge and celebrate every milestone.',
      }
    : {
        icon: '◫',
        title: 'Flashcards are taking shape',
        text: 'A smarter way to review vocabulary and make every new word stick is coming soon.',
      },
)
</script>

<template>
  <v-container class="wip-page">
    <v-card max-width="680" rounded="xl" elevation="0" class="wip-card text-center pa-12">
      <div class="wip-icon">{{ content.icon }}</div>
      <v-chip color="primary" variant="tonal" class="mb-6">Work in progress</v-chip>
      <h1>{{ content.title }}</h1>
      <p>{{ content.text }}</p>
      <v-btn to="/" color="primary" size="large" rounded="lg">Explore courses</v-btn>
    </v-card>
  </v-container>
</template>
